using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Routing;
using Elmah;
using JabbR.ContentProviders.Core;
using JabbR.Handlers;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Ninject;
using RouteMagic;
using SignalR;
using SignalR.Hosting.Common;
using SignalR.Ninject;

[assembly: WebActivator.PostApplicationStartMethod(typeof(JabbR.App_Start.Bootstrapper), "PreAppStart")]

namespace JabbR.App_Start
{
    public static class Bootstrapper
    {
        // Background task info
        private static bool _sweeping;
        private static Timer _timer;
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(10);

        private const string SqlClient = "System.Data.SqlClient";

        internal static IKernel Kernel = null;

        public static void PreAppStart()
        {
            if (HostingEnvironment.InClientBuildManager)
            {
                // If we're in the VS app domain then do nothing
                return;
            }

            var kernel = new StandardKernel();

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>()
                .InRequestScope();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>()
                .InRequestScope();

            kernel.Bind<IChatService>()
                  .To<ChatService>()
                  .InRequestScope();

            kernel.Bind<ICryptoService>()
                .To<CryptoService>()
                .InSingletonScope();

            kernel.Bind<IResourceProcessor>()
                .To<ResourceProcessor>()
                .InSingletonScope();

            kernel.Bind<IApplicationSettings>()
                  .To<ApplicationSettings>()
                  .InSingletonScope();

            Kernel = kernel;

            var resolver = new NinjectDependencyResolver(kernel);

            var host = new Host(resolver);
            host.Configuration.KeepAlive = TimeSpan.FromSeconds(30);

            RouteTable.Routes.MapHubs(resolver);

            // Perform the required migrations
            DoMigrations();

            // Start the sweeper
            var repositoryFactory = new Func<IJabbrRepository>(() => kernel.Get<IJabbrRepository>());
            _timer = new Timer(_ => Sweep(repositoryFactory, resolver), null, _sweepInterval, _sweepInterval);

            SetupErrorHandling();

            SetupAdminUsers(kernel);

            ClearConnectedClients(repositoryFactory());

            SetupRoutes(kernel);
        }

        private static void SetupAdminUsers(IKernel kernel)
        {
            var repository = kernel.Get<IJabbrRepository>();
            var chatService = kernel.Get<IChatService>();
            var settings = kernel.Get<IApplicationSettings>();

            if (!repository.Users.Any(u => u.IsAdmin))
            {
                string defaultAdminUserName = settings.DefaultAdminUserName;
                string defaultAdminPassword = settings.DefaultAdminPassword;

                if (String.IsNullOrWhiteSpace(defaultAdminUserName) || String.IsNullOrWhiteSpace(defaultAdminPassword))
                {
                    throw new InvalidOperationException("You have not provided a default admin username and/or password");
                }

                ChatUser defaultAdmin = repository.GetUserByName(defaultAdminUserName);

                if (defaultAdmin == null)
                {
                    defaultAdmin = chatService.AddUser(defaultAdminUserName, null, null, defaultAdminPassword);
                }

                defaultAdmin.IsAdmin = true;
                repository.CommitChanges();
            }
            
        }

        private static void SetupRoutes(IKernel kernel)
        {
            RouteTable.Routes.MapHttpHandler("Download", "api/v1/messages/{room}/{format}", 
                                             new { format = "json" },
                                             new { }, 
                                             ctx => kernel.Get<MessagesHandler>());
        }

        private static void ClearConnectedClients(IJabbrRepository repository)
        {
            try
            {
                foreach (var u in repository.Users)
                {
                    if (u.IsAfk)
                    {
                        u.Status = (int)UserStatus.Offline;
                    }
                }

                repository.RemoveAllClients();
                repository.CommitChanges();
            }
            catch (Exception ex)
            {
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }
        }

        private static void DoMigrations()
        {
            // Get the Jabbr connection string
            var connectionString = ConfigurationManager.ConnectionStrings["Jabbr"];

            if (String.IsNullOrEmpty(connectionString.ProviderName) ||
                !connectionString.ProviderName.Equals(SqlClient, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Only run migrations for SQL server (Sql ce not supported as yet)
            var settings = new JabbR.Models.Migrations.MigrationsConfiguration();
            var migrator = new DbMigrator(settings);
            migrator.Update();
        }

        private static void SetupErrorHandling()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    Elmah.ErrorLog.GetDefault(null).Log(new Error(e.Exception.GetBaseException()));
                }
                catch
                {
                    // Swallow!
                }
                finally
                {
                    e.SetObserved();
                }
            };
        }

        private static void Sweep(Func<IJabbrRepository> repositoryFactory, IDependencyResolver resolver)
        {
            if (_sweeping)
            {
                return;
            }

            _sweeping = true;

            try
            {
                using (IJabbrRepository repo = repositoryFactory())
                {
                    MarkInactiveUsers(repo, resolver);

                    repo.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorLog.GetDefault(null).Log(new Error(ex));
            }
            finally
            {
                _sweeping = false;
            }
        }

        private static void MarkInactiveUsers(IJabbrRepository repo, IDependencyResolver resolver)
        {
            var connectionManager = resolver.Resolve<IConnectionManager>();
            var clients = connectionManager.GetHubContext<Chat>().Clients;
            var inactiveUsers = new List<ChatUser>();

            foreach (var user in repo.Users)
            {
                var status = (UserStatus)user.Status;
                if (status == UserStatus.Offline)
                {
                    // Skip offline users
                    continue;
                }

                var elapsed = DateTime.UtcNow - user.LastActivity;

                if (!user.IsAfk && elapsed.TotalMinutes > 30)
                {
                    // After 30 minutes of inactivity make the user afk
                    user.IsAfk = true;
                }

                if (elapsed.TotalMinutes > 15)
                {
                    user.Status = (int)UserStatus.Inactive;
                    inactiveUsers.Add(user);
                }
            }

            var roomGroups = from u in inactiveUsers
                             from r in u.Rooms
                             select new { User = u, Room = r } into tuple
                             group tuple by tuple.Room into g
                             select new
                                        {
                                            Room = g.Key,
                                            Users = g.Select(t => new UserViewModel(t.User))
                                        };

            foreach (var roomGroup in roomGroups)
            {
                clients[roomGroup.Room.Name].markInactive(roomGroup.Users).Wait();
            }
        }
    }
}
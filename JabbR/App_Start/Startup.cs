using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Elmah;
using JabbR.Auth;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ninject;
using Owin;

namespace JabbR
{
    public class Startup
    {
        // Background task info
        private static bool _sweeping;
        private static Timer _timer;
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(10);

        private const string SqlClient = "System.Data.SqlClient";

        internal static IKernel Kernel = null;

        public void Configuration(IAppBuilder app)
        {
            var kernel = new StandardKernel();

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            kernel.Bind<Chat>()
                  .ToMethod(context =>
                  {
                      // We're doing this manually since we want the chat repository to be shared
                      // between the chat service and the chat hub itself
                      var settings = context.Kernel.Get<IApplicationSettings>();
                      var resourceProcessor = context.Kernel.Get<IResourceProcessor>();
                      var repository = context.Kernel.Get<IJabbrRepository>();
                      var cache = context.Kernel.Get<ICache>();
                      var crypto = context.Kernel.Get<ICryptoService>();

                      var service = new ChatService(cache, repository, crypto);

                      return new Chat(settings,
                                      resourceProcessor,
                                      service,
                                      repository,
                                      cache);
                  });

            kernel.Bind<ICryptoService>()
                .To<CryptoService>()
                .InSingletonScope();

            kernel.Bind<IResourceProcessor>()
                .To<ResourceProcessor>()
                .InSingletonScope();

            kernel.Bind<IApplicationSettings>()
                  .To<ApplicationSettings>()
                  .InSingletonScope();

            kernel.Bind<IVirtualPathUtility>()
                  .To<VirtualPathUtilityWrapper>();

            kernel.Bind<IJavaScriptMinifier>()
                  .To<AjaxMinMinifier>()
                  .InSingletonScope();

            kernel.Bind<ICache>()
                  .To<AspNetCache>()
                  .InSingletonScope();

            var serializer = new JsonNetSerializer(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            });

            kernel.Bind<IJsonSerializer>()
                  .ToConstant(serializer);


            Kernel = kernel;

            SetupSignalR(kernel, app);
            SetupWebApi(kernel, app);

            app.UseShowExceptions();

            app.UseStaticFiles("/", ".");
            app.UseRazor();
            app.Use(typeof(LoginHandler));
            app.Use(typeof(ProxyHandler));

            // Perform the required migrations
            DoMigrations();

            SetupErrorHandling();
        }

        private static void SetupSignalR(StandardKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectDependencyResolver(kernel);

            var config = new HubConfiguration
            {
                Resolver = resolver,
                EnableDetailedErrors = true
            };

            app.MapHubs(config);

            // Start the sweeper
            var repositoryFactory = new Func<IJabbrRepository>(() => kernel.Get<IJabbrRepository>());
            _timer = new Timer(_ => Sweep(repositoryFactory, resolver), null, _sweepInterval, _sweepInterval);

            // Clear all connections on app start
            ClearConnectedClients(repositoryFactory());
        }

        private static void SetupWebApi(IKernel kernel, IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.Formatters.Clear();
            JsonMediaTypeFormatter jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.Add(jsonFormatter);
            config.DependencyResolver = new NinjectWebApiDependencyResolver(kernel);

            config.Routes.MapHttpRoute(
                name: "MessagesV1",
                routeTemplate: "api/v1/{controller}/{room}");

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api",
                defaults: new { controller = "ApiFrontPage" }
            );

            app.UseHttpServer(config);
        }
        
        private static void ClearConnectedClients(IJabbrRepository repository)
        {
            try
            {
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
            var hubContext = connectionManager.GetHubContext<Chat>();
            var inactiveUsers = new List<ChatUser>();

            IQueryable<ChatUser> users = repo.GetOnlineUsers();

            foreach (var user in users)
            {
                var status = (UserStatus)user.Status;
                var elapsed = DateTime.UtcNow - user.LastActivity;

                if (user.ConnectedClients.Count == 0)
                {
                    // Fix users that are marked as inactive but have no clients
                    user.Status = (int)UserStatus.Offline;
                }
                else if (elapsed.TotalMinutes > 15)
                {
                    user.Status = (int)UserStatus.Inactive;
                    inactiveUsers.Add(user);
                }
            }

            if (inactiveUsers.Count > 0)
            {
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
                    hubContext.Clients.Group(roomGroup.Room.Name).markInactive(roomGroup.Users).Wait();
                }
            }
        }
    }
}
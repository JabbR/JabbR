using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elmah;
using JabbR.Migrations;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.CSharp.RuntimeBinder;
using Ninject;
using SignalR;
using SignalR.Hubs;
using SignalR.Infrastructure;
using SignalR.Ninject;

[assembly: WebActivator.PreApplicationStartMethod(typeof(JabbR.App_Start.Bootstrapper), "PreAppStart")]

namespace JabbR.App_Start
{
    public static class Bootstrapper
    {
        // Background task info
        private static bool _sweeping;
        private static Timer _timer;
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(1);

        private const string SqlClient = "System.Data.SqlClient";

        public static void PreAppStart()
        {
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

            DependencyResolver.SetResolver(new NinjectDependencyResolver(kernel));

            // Perform the required migrations
            DoMigrations();

            // Start the sweeper
            var repositoryFactory = new Func<IJabbrRepository>(() => kernel.Get<IJabbrRepository>());
            _timer = new Timer(_ => Sweep(repositoryFactory), null, _sweepInterval, _sweepInterval);

            SetupErrorHandling();

            Signaler.Instance.DefaultTimeout = TimeSpan.FromSeconds(25);
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
            var settings = new Settings();
            var migrator = new DbMigrator(settings);
            migrator.Update();
        }

        private static void SetupErrorHandling()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                var ex = e.Exception.GetBaseException();
                if (!(ex is InvalidOperationException) &&
                    !(ex is RuntimeBinderException) &&
                    !(ex is MissingMethodException) &&
                    !(ex is ThreadAbortException))
                {
                    // ErrorSignal.Get(this).Raise(ex);
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                // ErrorSignal.Get(this).Raise(e.Exception.GetBaseException());
                e.SetObserved();
            };
        }

        private static void Sweep(Func<IJabbrRepository> repositoryFactory)
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
                    MarkInactiveUsers(repo);

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

        private static void MarkInactiveUsers(IJabbrRepository repo)
        {
            var clients = Hub.GetClients<Chat>();
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
                if (elapsed.TotalMinutes > 5)
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
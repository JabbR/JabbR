using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JabbR.Migrations;
using JabbR.Models;
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
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(5);

        private static Func<IJabbrRepository> _repoFactory;
        private static bool _persistChat;

        public static void PreAppStart()
        {
            var kernel = new StandardKernel();
            var persistChatSetting = ConfigurationManager.AppSettings["persistChat"];
            _persistChat = false;

            if (!String.IsNullOrEmpty(persistChatSetting) &&
                Boolean.TryParse(persistChatSetting, out _persistChat) &&
                _persistChat)
            {
                kernel.Bind<JabbrContext>()
                    .To<JabbrContext>()
                    .InRequestScope();

                kernel.Bind<IJabbrRepository>()
                    .To<PersistedRepository>()
                    .InRequestScope();
            }
            else
            {
                kernel.Bind<IJabbrRepository>()
                    .To<InMemoryRepository>()
                    .InSingletonScope();
            }

            // Setup the repository factory
            _repoFactory = new Func<IJabbrRepository>(() => kernel.Get<IJabbrRepository>());

            // 
            DependencyResolver.SetResolver(new NinjectDependencyResolver(kernel));

            if (_persistChat)
            {
                // Run the migrations if we're persisting chat
                var settings = new Settings();
                var migrator = new DbMigrator(settings);
                migrator.Update();
            }

            // Start the sweeper
            _timer = new Timer(_ => Sweep(), null, _sweepInterval, _sweepInterval);

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

            Signaler.Instance.DefaultTimeout = TimeSpan.FromSeconds(25);
        }

        private static void Sweep()
        {
            if (_sweeping)
            {
                return;
            }

            _sweeping = true;

            try
            {
                using (var repo = _repoFactory())
                {
                    MarkInactiveUsers(repo);

                    RemoveInactiveRooms(repo);

                    repo.Update();
                }
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

        private static void RemoveInactiveRooms(IJabbrRepository repo)
        {
            // Don't remove rooms if the chat is persistant
            if (_persistChat)
            {
                return;
            }

            var inactiveRooms = new List<ChatRoom>();
            foreach (var room in repo.Rooms)
            {
                var elapsed = DateTime.UtcNow - room.LastActivity;
                if (room.Users.Count == 0 && elapsed.TotalMinutes > 30)
                {
                    inactiveRooms.Add(room);
                }
            }

            foreach (var room in inactiveRooms)
            {
                repo.Remove(room);
            }
        }
    }
}
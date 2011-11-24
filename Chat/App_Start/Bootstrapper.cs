using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using Chat.Migrations;
using Chat.Models;
using Chat.ViewModels;
using Ninject;
using SignalR.Hubs;
using SignalR.Infrastructure;
using SignalR.Ninject;

[assembly: WebActivator.PreApplicationStartMethod(typeof(Chat.App_Start.Bootstrapper), "PreAppStart")]

namespace Chat.App_Start
{
    public static class Bootstrapper
    {
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(5);
        private static bool _sweeping;
        private static Timer _timer;
        private static Func<IJabbrRepository> repoCreator;

        public static void PreAppStart()
        {
            var kernel = new StandardKernel();

            var setting = ConfigurationManager.AppSettings["persistChat"];
            bool persistChat = false;
            if (!String.IsNullOrEmpty(setting) &&
                Boolean.TryParse(setting, out persistChat) &&
                persistChat)
            {
                kernel.Bind<JabbrContext>()
                    .To<JabbrContext>()
                    .InRequestScope();

                kernel.Bind<IJabbrRepository>()
                    .To<PersistedRepository>()
                    .InRequestScope();

                repoCreator = new Func<IJabbrRepository>(() => new PersistedRepository(new JabbrContext()));
            }
            else
            {
                kernel.Bind<IJabbrRepository>()
                    .To<InMemoryRepository>()
                    .InSingletonScope();

                repoCreator = new Func<IJabbrRepository>(() => new InMemoryRepository());
            }

            DependencyResolver.SetResolver(new NinjectDependencyResolver(kernel));

            if (persistChat)
                new DbMigrator(new Settings()).Update();

            _timer = new Timer(_ => Sweep(), null, _sweepInterval, _sweepInterval);
        }

        private static void Sweep()
        {
            if (_sweeping)
            {
                return;
            }

            _sweeping = true;

            using (var repo = repoCreator())
            {
                MarkInactiveUsers(repo);

                RemoveInactiveRooms(repo);

                repo.Update();
            }

            _sweeping = false;
        }

        private static void MarkInactiveUsers(IJabbrRepository repo)
        {
            var clients = Hub.GetClients<SignalR.Samples.Hubs.Chat.Chat>();

            var inactiveUsers = new List<ChatUser>();

            foreach (var user in repo.Users)
            {
                var elapsed = DateTime.UtcNow - user.LastActivity;
                if (elapsed.TotalMinutes > 5)
                {
                    user.Active = false;
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
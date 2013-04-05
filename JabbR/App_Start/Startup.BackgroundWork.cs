using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Ninject;

namespace JabbR
{
    public partial class Startup
    {
        // Background task info
        private static volatile bool _sweeping;
        private static Timer _backgroundTimer;
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _sweepStart = TimeSpan.FromMinutes(10);

        private static void StartBackgroundWork(IKernel kernel, IDependencyResolver resolver)
        {
            // Resolve the hub context, so we can broadcast to the hub from a background thread
            var connectionManager = resolver.Resolve<IConnectionManager>();

            // Start the sweeper
            _backgroundTimer = new Timer(_ =>
            {
                var hubContext = connectionManager.GetHubContext<Chat>();
                Sweep(kernel, hubContext);
            },
            null,
            _sweepStart,
            _sweepInterval);

            // Clear all connections on app start
            ClearConnectedClients(kernel);
        }

        private static void ClearConnectedClients(IKernel kernel)
        {
            using (var repository = kernel.Get<IJabbrRepository>())
            {
                try
                {
                    repository.RemoveAllClients();
                    repository.CommitChanges();
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
            }
        }

        private static void Sweep(IKernel kernel, IHubContext hubContext)
        {
            if (_sweeping)
            {
                return;
            }

            _sweeping = true;

            try
            {
                using (var repo = kernel.Get<IJabbrRepository>())
                {
                    CheckUserStatus(repo, hubContext);

                    repo.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                _sweeping = false;
            }
        }

        private static void CheckUserStatus(IJabbrRepository repo, IHubContext hubContext)
        {
            var inactiveUsers = new List<ChatUser>();
            var offlineUsers = new List<ChatUser>();

            IQueryable<ChatUser> users = repo.GetOnlineUsers();

            foreach (var user in users)
            {
                var status = (UserStatus)user.Status;
                var elapsed = DateTime.UtcNow - user.LastActivity;

                if (user.ConnectedClients.Count == 0)
                {
                    // Fix users that are marked as inactive but have no clients
                    user.Status = (int)UserStatus.Offline;
                    offlineUsers.Add(user);
                }
                else if (elapsed.TotalMinutes > 5)
                {
                    user.Status = (int)UserStatus.Inactive;
                    inactiveUsers.Add(user);
                }
            }

            if (inactiveUsers.Count > 0)
            {
                PerformRoomAction(inactiveUsers, roomGroup =>
                {
                    hubContext.Clients.Group(roomGroup.Room.Name).markInactive(roomGroup.Users);
                });
            }

            // TODO: Only remove users relevant to this server.
            //if (offlineUsers.Count > 0)
            //{
            //    PerformRoomAction(offlineUsers, roomGroup =>
            //    {
            //        foreach (var user in roomGroup.Users)
            //        {
            //            hubContext.Clients.Group(roomGroup.Room.Name).leave(user, roomGroup.Room.Name);
            //        }
            //    });
            //}
        }

        private static void PerformRoomAction(List<ChatUser> users, Action<RoomGroup> action)
        {
            var roomGroups = from u in users
                             from r in u.Rooms
                             select new { User = u, Room = r } into tuple
                             group tuple by tuple.Room into g
                             select new RoomGroup
                             {
                                 Room = g.Key,
                                 Users = g.Select(t => new UserViewModel(t.User))
                             };

            foreach (var roomGroup in roomGroups)
            {
                action(roomGroup);
            }
        }

        private class RoomGroup
        {
            public ChatRoom Room { get; set; }

            public IEnumerable<UserViewModel> Users { get; set; }
        }
    }
}
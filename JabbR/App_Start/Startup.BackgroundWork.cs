using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Threading;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Ninject;

namespace JabbR
{
    public partial class Startup
    {
        // Background task info
        private static volatile bool _sweeping;
        private static Timer _backgroundTimer;
        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(1);

        private static void StartBackgroundWork(IKernel kernel, IDependencyResolver resolver)
        {
            // Resolve the hub context, so we can broadcast to the hub from a background thread
            var connectionManager = resolver.Resolve<IConnectionManager>();
            var heartbeat = resolver.Resolve<ITransportHeartbeat>();

            // Start the sweeper
            _backgroundTimer = new Timer(_ =>
            {
                var hubContext = connectionManager.GetHubContext<Chat>();
                Sweep(kernel, hubContext, heartbeat);
            },
            null,
            TimeSpan.Zero,
            _sweepInterval);
        }

        private static void Sweep(IKernel kernel, IHubContext hubContext, ITransportHeartbeat heartbeat)
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
                    var cache = kernel.Get<ICache>();

                    // Update the connection presence
                    UpdatePresence(cache, repo, heartbeat);

                    // Remove zombie connections
                    RemoveZombies(repo);

                    // Remove users with no connections
                    RemoveOfflineUsers(repo, hubContext);

                    // Check the user status
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

        private static void UpdatePresence(ICache cache, IJabbrRepository repo, ITransportHeartbeat heartbeat)
        {
            var service = new ChatService(cache, repo);

            // Get all connections on this node and update the activity
            foreach (var connection in heartbeat.GetConnections())
            {
                service.UpdateActivity(connection.ConnectionId);
            }

            repo.CommitChanges();
        }

        private static void RemoveZombies(IJabbrRepository repo)
        {
            // Remove all zombie clients 
            var zombies = repo.Clients.Where(c =>
                SqlFunctions.DateDiff("mi", c.LastActivity, DateTimeOffset.UtcNow) > 3);

            // We're doing to list since there's no MARS support on azure
            foreach (var client in zombies.ToList())
            {
                repo.Remove(client);
            }
        }

        private static void RemoveOfflineUsers(IJabbrRepository repo, IHubContext hubContext)
        {
            var offlineUsers = new List<ChatUser>();
            IQueryable<ChatUser> users = repo.GetOnlineUsers();

            foreach (var user in users.ToList())
            {
                if (user.ConnectedClients.Count == 0)
                {
                    // Fix users that are marked as inactive but have no clients
                    user.Status = (int)UserStatus.Offline;
                    offlineUsers.Add(user);
                }
            }

            if (offlineUsers.Count > 0)
            {
                PerformRoomAction(offlineUsers, roomGroup =>
                {
                    foreach (var user in roomGroup.Users)
                    {
                        hubContext.Clients.Group(roomGroup.Room.Name).leave(user, roomGroup.Room.Name);
                    }
                });
            }
        }

        private static void CheckUserStatus(IJabbrRepository repo, IHubContext hubContext)
        {
            var inactiveUsers = new List<ChatUser>();

            IQueryable<ChatUser> users = repo.GetOnlineUsers().Where(u => 
                u.Status != (int)UserStatus.Inactive &&
                SqlFunctions.DateDiff("mi", u.LastActivity, DateTime.UtcNow) > 5);

            foreach (var user in users.ToList())
            {
                user.Status = (int)UserStatus.Inactive;
                inactiveUsers.Add(user);
            }

            if (inactiveUsers.Count > 0)
            {
                PerformRoomAction(inactiveUsers, roomGroup =>
                {
                    hubContext.Clients.Group(roomGroup.Room.Name).markInactive(roomGroup.Users);
                });
            }
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
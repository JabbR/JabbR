using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Threading;
using JabbR.Models;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Ninject;

namespace JabbR.Services
{
    public class PresenceMonitor
    {
        private volatile bool _running;
        private Timer _timer;
        private readonly TimeSpan _presenceCheckInterval = TimeSpan.FromMinutes(1);

        private readonly IKernel _kernel;
        private readonly IHubContext _hubContext;
        private readonly ITransportHeartbeat _heartbeat;

        public PresenceMonitor(IKernel kernel, 
                               IConnectionManager connectionManager, 
                               ITransportHeartbeat heartbeat)
        {
            _kernel = kernel;
            _hubContext = connectionManager.GetHubContext<Chat>();
            _heartbeat = heartbeat;
        }

        public void Start()
        {
            // Start the timer
            _timer = new Timer(_ =>
            {
                Check();
            },
            null,
            TimeSpan.Zero,
            _presenceCheckInterval);
        }

        private void Check()
        {
            if (_running)
            {
                return;
            }

            _running = true;

            try
            {
                using (var repo = _kernel.Get<IJabbrRepository>())
                {
                    // Update the connection presence
                    UpdatePresence(repo);

                    // Remove zombie connections
                    RemoveZombies(repo);

                    // Remove users with no connections
                    RemoveOfflineUsers(repo);

                    // Check the user status
                    CheckUserStatus(repo);
                }
            }
            catch (Exception)
            {
                // TODO: Log
            }
            finally
            {
                _running = false;
            }
        }

        private void UpdatePresence(IJabbrRepository repo)
        {
            // Get all connections on this node and update the activity
            foreach (var connection in _heartbeat.GetConnections())
            {
                ChatClient client = repo.GetClientById(connection.ConnectionId);

                if (client != null)
                {
                    client.LastActivity = DateTimeOffset.UtcNow;
                }
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

        private void RemoveOfflineUsers(IJabbrRepository repo)
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
                        _hubContext.Clients.Group(roomGroup.Room.Name).leave(user, roomGroup.Room.Name);
                    }
                });

                repo.CommitChanges();
            }
        }

        private void CheckUserStatus(IJabbrRepository repo)
        {
            var inactiveUsers = new List<ChatUser>();

            IQueryable<ChatUser> users = repo.Users.Where(u =>
                u.Status == (int)UserStatus.Active &&
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
                    _hubContext.Clients.Group(roomGroup.Room.Name).markInactive(roomGroup.Users);
                });

                repo.CommitChanges();
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
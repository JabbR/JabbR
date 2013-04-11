using System;
using System.Linq;
using JabbR.Models;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace JabbR.Services
{
    public class ChatNotificationService : IChatNotificationService
    {
        private readonly IConnectionManager _connectionManager;

        public ChatNotificationService(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public void OnUserNameChanged(ChatUser user, string oldUserName, string newUserName)
        {
            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Tell the user's connected clients that the name changed
            foreach (var client in user.ConnectedClients)
            {
                HubContext.Clients.Client(client.Id).userNameChanged(userViewModel);
            }

            // Notify all users in the rooms
            foreach (var room in user.Rooms)
            {
                HubContext.Clients.Group(room.Name).changeUserName(oldUserName, userViewModel, room.Name);
            }
        }

        public void UpdateUnreadMentions(ChatUser mentionedUser, int unread)
        {
            foreach (var client in mentionedUser.ConnectedClients)
            {
                HubContext.Clients.Client(client.Id).updateUnreadNotifications(unread);
            }
        }

        private IHubContext _hubContext;
        private IHubContext HubContext
        {
            get
            {
                if (_hubContext == null)
                {
                    _hubContext = _connectionManager.GetHubContext<Chat>();
                }

                return _hubContext;
            }
        }
    }
}
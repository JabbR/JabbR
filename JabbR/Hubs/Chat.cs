using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JabbR.Commands;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;

namespace JabbR
{
    [Authorize]
    public class Chat : Hub, INotificationService
    {
        private static readonly TimeSpan _disconnectThreshold = TimeSpan.FromSeconds(10);

        private readonly IJabbrRepository _repository;
        private readonly IChatService _service;
        private readonly ICache _cache;
        private readonly ContentProviderProcessor _resourceProcessor;

        public Chat(ContentProviderProcessor resourceProcessor,
                    IChatService service,
                    IJabbrRepository repository,
                    ICache cache)
        {
            _resourceProcessor = resourceProcessor;
            _service = service;
            _repository = repository;
            _cache = cache;
        }

        private string UserAgent
        {
            get
            {
                if (Context.Headers != null)
                {
                    return Context.Headers["User-Agent"];
                }
                return null;
            }
        }

        private bool OutOfSync
        {
            get
            {
                string version = Context.QueryString["version"];

                if (String.IsNullOrEmpty(version))
                {
                    return true;
                }

                return new Version(version) != Constants.JabbRVersion;
            }
        }

        public override Task OnConnected()
        {
            CheckStatus();

            return base.OnConnected();
        }

        public void Join()
        {
            Join(reconnecting: false);
        }

        public void Join(bool reconnecting)
        {
            // Get the client state
            var userId = Context.User.Identity.Name;

            // Try to get the user from the client state
            ChatUser user = _repository.GetUserById(userId);

            if (!reconnecting)
            {
                // Update some user values
                _service.UpdateActivity(user, Context.ConnectionId, UserAgent);
                _repository.CommitChanges();
            }

            ClientState clientState = GetClientState();

            OnUserInitialize(clientState, user, reconnecting);
        }

        private void CheckStatus()
        {
            if (OutOfSync)
            {
                Clients.Caller.outOfSync();
            }
        }

        private void OnUserInitialize(ClientState clientState, ChatUser user, bool reconnecting)
        {
            // Update the active room on the client (only if it's still a valid room)
            if (user.Rooms.Any(room => room.Name.Equals(clientState.ActiveRoom, StringComparison.OrdinalIgnoreCase)))
            {
                // Update the active room on the client (only if it's still a valid room)
                Clients.Caller.activeRoom = clientState.ActiveRoom;
            }

            LogOn(user, Context.ConnectionId, reconnecting);
        }

        public bool Send(string content, string roomName)
        {
            var message = new ClientMessage
            {
                Content = content,
                Room = roomName
            };

            return Send(message);
        }

        public bool Send(ClientMessage clientMessage)
        {
            CheckStatus();

            // See if this is a valid command (starts with /)
            if (TryHandleCommand(clientMessage.Content, clientMessage.Room))
            {
                return true;
            }

            var userId = Context.User.Identity.Name;

            ChatUser user = _repository.VerifyUserId(userId);
            ChatRoom room = _repository.VerifyUserRoom(_cache, user, clientMessage.Room);

            if (room == null || (room.Private && !user.AllowedRooms.Contains(room)))
            {
                return false;
            }

            // REVIEW: Is it better to use _repository.VerifyRoom(message.Room, mustBeOpen: false)
            // here?
            if (room.Closed)
            {
                throw new InvalidOperationException(String.Format("You cannot post messages to '{0}'. The room is closed.", clientMessage.Room));
            }

            // Update activity *after* ensuring the user, this forces them to be active
            UpdateActivity(user, room);

            // Create a true unique id and save the message to the db
            string id = Guid.NewGuid().ToString("d");
            ChatMessage chatMessage = _service.AddMessage(user, room, id, clientMessage.Content);
            _repository.CommitChanges();


            var messageViewModel = new MessageViewModel(chatMessage);

            if (clientMessage.Id == null)
            {
                // If the client didn't generate an id for the message then just
                // send it to everyone. The assumption is that the client has some ui
                // that it wanted to update immediately showing the message and
                // then when the actual message is roundtripped it would "solidify it".
                Clients.Group(room.Name).addMessage(messageViewModel, room.Name);
            }
            else
            {
                // If the client did set an id then we need to give everyone the real id first
                Clients.OthersInGroup(room.Name).addMessage(messageViewModel, room.Name);

                // Now tell the caller to replace the message
                Clients.Caller.replaceMessage(clientMessage.Id, messageViewModel, room.Name);
            }

            // Add mentions
            AddMentions(chatMessage);
            
            var urls = UrlExtractor.ExtractUrls(chatMessage.Content);
            if (urls.Count > 0)
            {
                _resourceProcessor.ProcessUrls(urls, Clients, room.Name, chatMessage.Id);
            }

            return true;
        }

        private void AddMentions(ChatMessage message)
        {
            bool anyMentions = false;

            foreach (var userName in MentionExtractor.ExtractMentions(message.Content))
            {
                ChatUser mentionedUser = _repository.GetUserByName(userName);

                // Don't create a mention if
                // 1. If the mentioned user doesn't exist.
                // 2. If you mention yourself
                // 3. If you're mentioned in a private room that you don't have access to
                if (mentionedUser == null || 
                    mentionedUser == message.User || 
                    (message.Room.Private && !mentionedUser.AllowedRooms.Contains(message.Room)))
                {
                    continue;
                }

                anyMentions = true;

                // Mark the notification as read if the user is online
                bool markAsRead = mentionedUser.Status == (int)UserStatus.Active;

                _service.AddNotification(mentionedUser, message, markAsRead);
            }

            if (anyMentions)
            {
                _repository.CommitChanges();
            }
        }

        public UserViewModel GetUserInfo()
        {
            var userId = Context.User.Identity.Name;

            ChatUser user = _repository.VerifyUserId(userId);

            return new UserViewModel(user);
        }

        public override Task OnReconnected()
        {
            CheckStatus();

            var userId = Context.User.Identity.Name;

            ChatUser user = _repository.VerifyUserId(userId);

            // Make sure this client is being tracked
            _service.AddClient(user, Context.ConnectionId, UserAgent);

            var currentStatus = (UserStatus)user.Status;

            if (currentStatus == UserStatus.Offline)
            {
                // Mark the user as inactive
                user.Status = (int)UserStatus.Inactive;
                _repository.CommitChanges();

                // If the user was offline that means they are not in the user list so we need to tell
                // everyone the user is really in the room
                var userViewModel = new UserViewModel(user);

                foreach (var room in user.Rooms)
                {
                    var isOwner = user.OwnedRooms.Contains(room);

                    // Tell the people in this room that you've joined
                    Clients.Group(room.Name).addUser(userViewModel, room.Name, isOwner);

                    // Add the caller to the group so they receive messages
                    Groups.Add(Context.ConnectionId, room.Name);
                }
            }

            return base.OnReconnected();
        }

        public override Task OnDisconnected()
        {
            DisconnectClient(Context.ConnectionId);

            return base.OnDisconnected();
        }

        public object GetCommands()
        {
            return CommandManager.GetCommandsMetaData();
        }

        public object GetShortcuts()
        {
            return new[] {
                new { Name = "Tab or Shift + Tab", Category = "shortcut", Description = "Go to the next open room tab or Go to the previous open room tab." },
                new { Name = "Alt + L", Category = "shortcut", Description = "Go to the Lobby."},
                new { Name = "Alt + Number", Category = "shortcut", Description = "Go to specific Tab."}
            };
        }

        public IEnumerable<LobbyRoomViewModel> GetRooms()
        {
            string userId = Context.User.Identity.Name;
            ChatUser user = _repository.VerifyUserId(userId);

            var rooms = _repository.GetAllowedRooms(user).Select(r => new LobbyRoomViewModel
            {
                Name = r.Name,
                Count = r.Users.Count(u => u.Status != (int)UserStatus.Offline),
                Private = r.Private,
                Closed = r.Closed,
                Topic = r.Topic
            }).ToList();

            return rooms;
        }

        public IEnumerable<MessageViewModel> GetPreviousMessages(string messageId)
        {
            var previousMessages = (from m in _repository.GetPreviousMessages(messageId)
                                    orderby m.When descending
                                    select m).Take(100);


            return previousMessages.AsEnumerable()
                                   .Reverse()
                                   .Select(m => new MessageViewModel(m));
        }

        public RoomViewModel GetRoomInfo(string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                return null;
            }

            string userId = Context.User.Identity.Name;
            ChatUser user = _repository.VerifyUserId(userId);

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room == null || (room.Private && !user.AllowedRooms.Contains(room)))
            {
                return null;
            }

            var recentMessages = (from m in _repository.GetMessagesByRoom(room)
                                  orderby m.When descending
                                  select m).Take(50).ToList();

            // Reverse them since we want to get them in chronological order
            recentMessages.Reverse();

            // Get online users through the repository
            IEnumerable<ChatUser> onlineUsers = _repository.GetOnlineUsers(room).ToList();

            return new RoomViewModel
            {
                Name = room.Name,
                Users = from u in onlineUsers
                        select new UserViewModel(u),
                Owners = from u in room.Owners.Online()
                         select u.Name,
                RecentMessages = recentMessages.Select(m => new MessageViewModel(m)),
                Topic = room.Topic ?? "",
                Welcome = room.Welcome ?? "",
                Closed = room.Closed
            };
        }

        public void Typing(string roomName)
        {
            string userId = Context.User.Identity.Name;

            ChatUser user = _repository.GetUserById(userId);
            ChatRoom room = _repository.VerifyUserRoom(_cache, user, roomName);

            if (room == null || (room.Private && !user.AllowedRooms.Contains(room)))
            {
                return;
            }

            UpdateActivity(user, room);

            var userViewModel = new UserViewModel(user);
            Clients.Group(room.Name).setTyping(userViewModel, room.Name);
        }

        public void UpdateActivity()
        {
            CheckStatus();

            string userId = Context.User.Identity.Name;

            ChatUser user = _repository.GetUserById(userId);

            foreach (var room in user.Rooms)
            {
                UpdateActivity(user, room);
            }
        }

        private void LogOn(ChatUser user, string clientId, bool reconnecting)
        {
            if (!reconnecting)
            {
                // Update the client state
                Clients.Caller.id = user.Id;
                Clients.Caller.name = user.Name;
                Clients.Caller.hash = user.Hash;
            }

            var rooms = new List<RoomViewModel>();
            var privateRooms = new List<LobbyRoomViewModel>();
            var userViewModel = new UserViewModel(user);
            var ownedRooms = user.OwnedRooms.Select(r => r.Key);

            foreach (var room in user.Rooms)
            {
                var isOwner = ownedRooms.Contains(room.Key);

                // Tell the people in this room that you've joined
                Clients.Group(room.Name).addUser(userViewModel, room.Name, isOwner);

                // Add the caller to the group so they receive messages
                Groups.Add(clientId, room.Name);

                if (!reconnecting)
                {
                    // Add to the list of room names
                    rooms.Add(new RoomViewModel
                    {
                        Name = room.Name,
                        Private = room.Private,
                        Closed = room.Closed
                    });
                }
            }


            if (!reconnecting)
            {
                foreach (var r in user.AllowedRooms)
                {
                    privateRooms.Add(new LobbyRoomViewModel
                    {
                        Name = r.Name,
                        Count = _repository.GetOnlineUsers(r).Count(),
                        Private = r.Private,
                        Closed = r.Closed,
                        Topic = r.Topic
                    });
                }

                // Initialize the chat with the rooms the user is in
                Clients.Caller.logOn(rooms, privateRooms);
            }
        }

        private void UpdateActivity(ChatUser user, ChatRoom room)
        {
            UpdateActivity(user);

            OnUpdateActivity(user, room);
        }

        private void UpdateActivity(ChatUser user)
        {
            _service.UpdateActivity(user, Context.ConnectionId, UserAgent);

            _repository.CommitChanges();
        }

        private bool TryHandleCommand(string command, string room)
        {
            string clientId = Context.ConnectionId;
            string userId = Context.User.Identity.Name;

            var commandManager = new CommandManager(clientId, UserAgent, userId, room, _service, _repository, _cache, this);
            return commandManager.TryHandleCommand(command);
        }

        private void DisconnectClient(string clientId)
        {
            string userId = _service.DisconnectClient(clientId);

            if (String.IsNullOrEmpty(userId))
            {
                return;
            }

            // To avoid the leave/join that happens when a user refreshes the page
            // we sleep for a second before we check the status.
            Thread.Sleep(_disconnectThreshold);

            // Query for the user to get the updated status
            ChatUser user = _repository.GetUserById(userId);

            // There's no associated user for this client id
            if (user == null)
            {
                return;
            }

            _repository.Reload(user);

            // The user will be marked as offline if all clients leave
            if (user.Status == (int)UserStatus.Offline)
            {
                foreach (var room in user.Rooms)
                {
                    var userViewModel = new UserViewModel(user);

                    Clients.OthersInGroup(room.Name).leave(userViewModel, room.Name);
                }
            }
        }

        private void OnUpdateActivity(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            Clients.Group(room.Name).updateActivity(userViewModel, room.Name);
        }

        private void LeaveRoom(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            Clients.Group(room.Name).leave(userViewModel, room.Name);

            foreach (var client in user.ConnectedClients)
            {
                Groups.Remove(client.Id, room.Name);
            }

            OnRoomChanged(room);
        }

        void INotificationService.LogOn(ChatUser user, string clientId)
        {
            LogOn(user, clientId, reconnecting: true);
        }

        void INotificationService.ChangePassword()
        {
            Clients.Caller.changePassword();
        }

        void INotificationService.SetPassword()
        {
            Clients.Caller.setPassword();
        }

        void INotificationService.KickUser(ChatUser targetUser, ChatRoom room)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Kick the user from this room
                Clients.Client(client.Id).kick(room.Name);

                // Remove the user from this the room group so he doesn't get the leave message
                Groups.Remove(client.Id, room.Name);
            }

            // Tell the room the user left
            LeaveRoom(targetUser, room);
        }

        void INotificationService.OnUserCreated(ChatUser user)
        {
            // Set some client state
            Clients.Caller.name = user.Name;
            Clients.Caller.id = user.Id;
            Clients.Caller.hash = user.Hash;

            // Tell the client a user was created
            Clients.Caller.userCreated();
        }

        void INotificationService.JoinRoom(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            var roomViewModel = new RoomViewModel
            {
                Name = room.Name,
                Private = room.Private,
                Welcome = room.Welcome ?? "",
                Closed = room.Closed
            };

            var isOwner = user.OwnedRooms.Contains(room);

            // Tell all clients to join this room
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).joinRoom(roomViewModel);
            }

            // Tell the people in this room that you've joined
            Clients.Group(room.Name).addUser(userViewModel, room.Name, isOwner);

            // Notify users of the room count change
            OnRoomChanged(room);

            foreach (var client in user.ConnectedClients)
            {
                Groups.Add(client.Id, room.Name);
            }
        }

        void INotificationService.AllowUser(ChatUser targetUser, ChatRoom targetRoom)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's an owner
                Clients.Client(client.Id).allowUser(targetRoom.Name);
            }

            // Tell the calling client the granting permission into the room was successful
            Clients.Caller.userAllowed(targetUser.Name, targetRoom.Name);
        }

        void INotificationService.UnallowUser(ChatUser targetUser, ChatRoom targetRoom)
        {
            // Kick the user from the room when they are unallowed
            ((INotificationService)this).KickUser(targetUser, targetRoom);

            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's an owner
                Clients.Client(client.Id).unallowUser(targetRoom.Name);
            }

            // Tell the calling client the granting permission into the room was successful
            Clients.Caller.userUnallowed(targetUser.Name, targetRoom.Name);
        }

        void INotificationService.AddOwner(ChatUser targetUser, ChatRoom targetRoom)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's an owner
                Clients.Client(client.Id).makeOwner(targetRoom.Name);
            }

            var userViewModel = new UserViewModel(targetUser);

            // If the target user is in the target room.
            // Tell everyone in the target room that a new owner was added
            if (_repository.IsUserInRoom(_cache, targetUser, targetRoom))
            {
                Clients.Group(targetRoom.Name).addOwner(userViewModel, targetRoom.Name);
            }

            // Tell the calling client the granting of ownership was successful
            Clients.Caller.ownerMade(targetUser.Name, targetRoom.Name);
        }

        void INotificationService.RemoveOwner(ChatUser targetUser, ChatRoom targetRoom)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's no longer an owner
                Clients.Client(client.Id).demoteOwner(targetRoom.Name);
            }

            var userViewModel = new UserViewModel(targetUser);

            // If the target user is in the target room.
            // Tell everyone in the target room that the owner was removed
            if (_repository.IsUserInRoom(_cache, targetUser, targetRoom))
            {
                Clients.Group(targetRoom.Name).removeOwner(userViewModel, targetRoom.Name);
            }

            // Tell the calling client the removal of ownership was successful
            Clients.Caller.ownerRemoved(targetUser.Name, targetRoom.Name);
        }

        void INotificationService.ChangeGravatar(ChatUser user)
        {
            Clients.Caller.hash = user.Hash;

            // Update the calling client
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).gravatarChanged();
            }

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Tell all users in rooms to change the gravatar
            foreach (var room in user.Rooms)
            {
                Clients.Group(room.Name).changeGravatar(userViewModel, room.Name);
            }
        }

        void INotificationService.OnSelfMessage(ChatRoom room, ChatUser user, string content)
        {
            Clients.Group(room.Name).sendMeMessage(user.Name, content, room.Name);
        }

        void INotificationService.SendPrivateMessage(ChatUser fromUser, ChatUser toUser, string messageText)
        {
            // Send a message to the sender and the sendee
            foreach (var client in fromUser.ConnectedClients)
            {
                Clients.Client(client.Id).sendPrivateMessage(fromUser.Name, toUser.Name, messageText);
            }

            foreach (var client in toUser.ConnectedClients)
            {
                Clients.Client(client.Id).sendPrivateMessage(fromUser.Name, toUser.Name, messageText);
            }
        }

        void INotificationService.PostNotification(ChatRoom room, ChatUser user, string message)
        {
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).postNotification(message, room.Name);
            }
        }

        void INotificationService.ListRooms(ChatUser user)
        {
            string userId = Context.User.Identity.Name;

            var userModel = new UserViewModel(user);

            Clients.Caller.showUsersRoomList(userModel, user.Rooms.Allowed(userId).Select(r => r.Name));
        }

        void INotificationService.ListUsers()
        {
            var users = _repository.Users.Online().Select(s => s.Name).OrderBy(s => s);
            Clients.Caller.listUsers(users);
        }

        void INotificationService.ListUsers(IEnumerable<ChatUser> users)
        {
            Clients.Caller.listUsers(users.Select(s => s.Name));
        }

        void INotificationService.ListUsers(ChatRoom room, IEnumerable<string> names)
        {
            Clients.Caller.showUsersInRoom(room.Name, names);
        }

        void INotificationService.LockRoom(ChatUser targetUser, ChatRoom room)
        {
            var userViewModel = new UserViewModel(targetUser);

            // Tell the room it's locked
            Clients.All.lockRoom(userViewModel, room.Name);

            // Tell the caller the room was successfully locked
            Clients.Caller.roomLocked(room.Name);

            // Notify people of the change
            OnRoomChanged(room);
        }

        void INotificationService.CloseRoom(IEnumerable<ChatUser> users, ChatRoom room)
        {
            // notify all members of room that it is now closed
            foreach (var user in users)
            {
                foreach (var client in user.ConnectedClients)
                {
                    Clients.Client(client.Id).roomClosed(room.Name);
                }
            }
        }

        void INotificationService.UnCloseRoom(IEnumerable<ChatUser> users, ChatRoom room)
        {
            // notify all members of room that it is now re-opened
            foreach (var user in users)
            {
                foreach (var client in user.ConnectedClients)
                {
                    Clients.Client(client.Id).roomUnClosed(room.Name);
                }
            }
        }

        void INotificationService.LogOut(ChatUser user, string clientId)
        {
            foreach (var client in user.ConnectedClients)
            {
                DisconnectClient(client.Id);
                Clients.Client(client.Id).logOut();
            }
        }

        void INotificationService.ShowUserInfo(ChatUser user)
        {
            string userId = Context.User.Identity.Name;

            Clients.Caller.showUserInfo(new
            {
                Name = user.Name,
                OwnedRooms = user.OwnedRooms
                    .Allowed(userId)
                    .Where(r => !r.Closed)
                    .Select(r => r.Name),
                Status = ((UserStatus)user.Status).ToString(),
                LastActivity = user.LastActivity,
                IsAfk = user.IsAfk,
                AfkNote = user.AfkNote,
                Note = user.Note,
                Hash = user.Hash,
                Rooms = user.Rooms.Allowed(userId).Select(r => r.Name)
            });
        }

        void INotificationService.ShowHelp()
        {
            Clients.Caller.showCommands();
        }

        void INotificationService.Invite(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            // Send the invite message to the sendee
            foreach (var client in targetUser.ConnectedClients)
            {
                Clients.Client(client.Id).sendInvite(user.Name, targetUser.Name, targetRoom.Name);
            }

            // Send the invite notification to the sender
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).sendInvite(user.Name, targetUser.Name, targetRoom.Name);
            }
        }

        void INotificationService.NugeUser(ChatUser user, ChatUser targetUser)
        {
            // Send a nudge message to the sender and the sendee
            foreach (var client in targetUser.ConnectedClients)
            {
                Clients.Client(client.Id).nudge(user.Name, targetUser.Name);
            }

            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).sendPrivateMessage(user.Name, targetUser.Name, "nudged " + targetUser.Name);
            }
        }

        void INotificationService.NudgeRoom(ChatRoom room, ChatUser user)
        {
            Clients.Group(room.Name).nudge(user.Name);
        }

        void INotificationService.LeaveRoom(ChatUser user, ChatRoom room)
        {
            LeaveRoom(user, room);
        }

        void INotificationService.OnUserNameChanged(ChatUser user, string oldUserName, string newUserName)
        {
            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Tell the user's connected clients that the name changed
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).userNameChanged(userViewModel);
            }

            // Notify all users in the rooms
            foreach (var room in user.Rooms)
            {
                Clients.Group(room.Name).changeUserName(oldUserName, userViewModel, room.Name);
            }
        }

        void INotificationService.ChangeNote(ChatUser user)
        {
            bool isNoteCleared = user.Note == null;

            // Update the calling client
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).noteChanged(user.IsAfk, isNoteCleared);
            }

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Tell all users in rooms to change the note
            foreach (var room in user.Rooms)
            {
                Clients.Group(room.Name).changeNote(userViewModel, room.Name);
            }
        }

        void INotificationService.ChangeFlag(ChatUser user)
        {
            bool isFlagCleared = String.IsNullOrWhiteSpace(user.Flag);

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Update the calling client
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).flagChanged(isFlagCleared, userViewModel.Country);
            }

            // Tell all users in rooms to change the flag
            foreach (var room in user.Rooms)
            {
                Clients.Group(room.Name).changeFlag(userViewModel, room.Name);
            }
        }

        void INotificationService.ChangeTopic(ChatUser user, ChatRoom room)
        {
            bool isTopicCleared = String.IsNullOrWhiteSpace(room.Topic);
            var parsedTopic = room.Topic ?? "";
            Clients.Group(room.Name).topicChanged(room.Name, isTopicCleared, parsedTopic, user.Name);
            // Create the view model
            var roomViewModel = new RoomViewModel
            {
                Name = room.Name,
                Topic = parsedTopic,
                Closed = room.Closed
            };
            Clients.Group(room.Name).changeTopic(roomViewModel);
        }

        void INotificationService.ChangeWelcome(ChatUser user, ChatRoom room)
        {
            bool isWelcomeCleared = String.IsNullOrWhiteSpace(room.Welcome);
            var parsedWelcome = room.Welcome ?? "";
            foreach (var client in user.ConnectedClients)
            {
                Clients.Client(client.Id).welcomeChanged(isWelcomeCleared, parsedWelcome);
            }
        }

        void INotificationService.AddAdmin(ChatUser targetUser)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's an owner
                Clients.Client(client.Id).makeAdmin();
            }

            var userViewModel = new UserViewModel(targetUser);

            // Tell all users in rooms to change the admin status
            foreach (var room in targetUser.Rooms)
            {
                Clients.Group(room.Name).addAdmin(userViewModel, room.Name);
            }

            // Tell the calling client the granting of admin status was successful
            Clients.Caller.adminMade(targetUser.Name);
        }

        void INotificationService.RemoveAdmin(ChatUser targetUser)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's no longer an owner
                Clients.Client(client.Id).demoteAdmin();
            }

            var userViewModel = new UserViewModel(targetUser);

            // Tell all users in rooms to change the admin status
            foreach (var room in targetUser.Rooms)
            {
                Clients.Group(room.Name).removeAdmin(userViewModel, room.Name);
            }

            // Tell the calling client the removal of admin status was successful
            Clients.Caller.adminRemoved(targetUser.Name);
        }

        void INotificationService.BroadcastMessage(ChatUser user, string messageText)
        {
            // Tell all users in all rooms about this message
            foreach (var room in _repository.Rooms)
            {
                Clients.Group(room.Name).broadcastMessage(messageText, room.Name);
            }
        }

        void INotificationService.ForceUpdate()
        {
            Clients.All.forceUpdate();
        }

        private void OnRoomChanged(ChatRoom room)
        {
            var roomViewModel = new RoomViewModel
            {
                Name = room.Name,
                Private = room.Private,
                Closed = room.Closed
            };

            // Update the room count
            Clients.All.updateRoomCount(roomViewModel, _repository.GetOnlineUsers(room).Count());
        }

        private ClientState GetClientState()
        {
            // New client state
            var jabbrState = GetCookieValue("jabbr.state");

            ClientState clientState = null;

            if (String.IsNullOrEmpty(jabbrState))
            {
                clientState = new ClientState();
            }
            else
            {
                clientState = JsonConvert.DeserializeObject<ClientState>(jabbrState);
            }

            return clientState;
        }

        private string GetCookieValue(string key)
        {
            Cookie cookie;
            Context.RequestCookies.TryGetValue(key, out cookie);
            string value = cookie != null ? cookie.Value : null;
            return value != null ? Uri.UnescapeDataString(value) : null;
        }

        void INotificationService.BanUser(ChatUser targetUser)
        {
            var rooms = targetUser.Rooms.Select(x => x.Name);

            foreach (var room in rooms)
            {
                foreach (var client in targetUser.ConnectedClients)
                {
                    // Kick the user from this room
                    Clients.Client(client.Id).kick(room);

                    // Remove the user from this the room group so he doesn't get the leave message
                    Groups.Remove(client.Id, room);
                }
            }

            Clients.Client(targetUser.ConnectedClients.First().Id).logOut(rooms);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repository.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

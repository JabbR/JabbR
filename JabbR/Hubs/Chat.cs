using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using JabbR.Commands;
using JabbR.ContentProviders;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Microsoft.Security.Application;
using Newtonsoft.Json;
using SignalR.Hubs;

namespace JabbR
{
    public class Chat : Hub, IDisconnect, INotificationService
    {
        private readonly IJabbrRepository _repository;
        private readonly IChatService _service;

        public Chat(IChatService service, IJabbrRepository repository)
        {
            _service = service;
            _repository = repository;
        }

        private static readonly List<IContentProvider> _contentProviders = new List<IContentProvider>() {
            new ImageContentProvider(),
            new YouTubeContentProvider(),
            new CollegeHumorContentProvider(),
            new TweetContentProvider(),
            new PastieContentProvider(),
            new ImgurContentProvider(),
            new GistContentProvider()
        };

        public bool OutOfSync
        {
            get
            {
                string version = Caller.version;
                return String.IsNullOrEmpty(version) ||
                        new Version(version) != typeof(Chat).Assembly.GetName().Version;
            }
        }

        public bool Join()
        {
            // Set the version on the client
            Caller.version = typeof(Chat).Assembly.GetName().Version.ToString();

            // Get the client state
            ClientState clientState = GetClientState();

            // Try to get the user from the client state
            ChatUser user = _repository.GetUserById(clientState.UserId);

            // Threre's no user being tracked
            if (user == null)
            {
                return false;
            }

            // Update some user values
            _service.AddClient(user, Context.ClientId);
            _service.UpdateActivity(user);
            _repository.CommitChanges();

            OnUserInitialize(clientState, user);

            return true;
        }

        private void OnUserInitialize(ClientState clientState, ChatUser user)
        {
            // Update the active room on the client (only if it's still a valid room)
            if (user.Rooms.Any(room => room.Name.Equals(clientState.ActiveRoom, StringComparison.OrdinalIgnoreCase)))
            {
                // Update the active room on the client (only if it's still a valid room)
                Caller.activeRoom = clientState.ActiveRoom;
            }

            LogOn(user, Context.ClientId);
        }

        public void Send(string content)
        {
            // If the client and server are out of sync then tell the client to refresh
            if (OutOfSync)
            {
                throw new InvalidOperationException("Chat was just updated, please refresh you browser");
            }

            // Sanitize the content (strip and bad html out)
            content = Sanitizer.GetSafeHtmlFragment(content);

            // See if this is a valid command (starts with /)
            if (TryHandleCommand(content))
            {
                return;
            }

            string roomName = Caller.activeRoom;
            string id = Caller.id;

            ChatUser user = _repository.VerifyUserId(id);
            ChatRoom room = _repository.VerifyUserRoom(user, roomName);

            // Update activity *after* ensuring the user, this forces them to be active
            UpdateActivity(user, room);

            HashSet<string> links;
            var messageText = Transform(content, out links);

            ChatMessage chatMessage = _service.AddMessage(user, room, messageText);

            var messageViewModel = new MessageViewModel(chatMessage);
            Clients[room.Name].addMessage(messageViewModel, room.Name);

            _repository.CommitChanges();

            if (!links.Any())
            {
                return;
            }

            ProcessUrls(links, room, chatMessage);
        }

        public void Disconnect()
        {
            DisconnectClient(Context.ClientId);
        }

        public IEnumerable<RoomViewModel> GetRooms()
        {
            var rooms = _repository.Rooms.Select(r => new RoomViewModel
            {
                Name = r.Name,
                Count = r.Users.Count(u => u.Status != (int)UserStatus.Offline)
            });

            return rooms;
        }

        public RoomViewModel GetRoomInfo(string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                return null;
            }

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room == null)
            {
                return null;
            }

            return new RoomViewModel
            {
                Name = room.Name,
                Users = from u in room.Users.Online()
                        select new UserViewModel(u),
                Owners = from u in room.Owners.Online()
                         select u.Name,
                RecentMessages = (from m in room.Messages
                                  orderby m.When descending
                                  select new MessageViewModel(m)).Take(20).Reverse()
            };
        }

        public void Typing(bool isTyping)
        {
            if (OutOfSync)
            {
                return;
            }

            string id = Caller.id;
            string roomName = Caller.activeRoom;

            if (String.IsNullOrEmpty(id))
            {
                return;
            }

            ChatUser user = _repository.GetUserById(id);

            if (user == null)
            {
                return;
            }

            ChatRoom room = _repository.VerifyUserRoom(user, roomName);

            if (isTyping)
            {
                UpdateActivity(user, room);
            }

            var userViewModel = new UserViewModel(user);
            Clients[room.Name].setTyping(userViewModel, room.Name, isTyping);
        }

        private void LogOn(ChatUser user, string clientId)
        {
            // Update the client state
            Caller.id = user.Id;
            Caller.name = user.Name;

            var userViewModel = new UserViewModel(user);
            var roomNames = new List<string>();

            foreach (var room in user.Rooms)
            {
                var isOwner = user.OwnedRooms.Contains(room);

                // Tell the people in this room that you've joined
                Clients[room.Name].addUser(userViewModel, room.Name, isOwner).Wait();

                // Update the room count
                OnRoomCountChanged(room);

                // Update activity
                UpdateActivity(user, room);

                // Add the caller to the group so they receive messages
                GroupManager.AddToGroup(clientId, room.Name).Wait();

                // Add to the list of room names
                roomNames.Add(room.Name);
            }

            // Initialize the chat with the rooms the user is in
            Caller.logOn(roomNames);
        }

        private void UpdateActivity(ChatUser user, ChatRoom room)
        {
            _service.UpdateActivity(user);

            _repository.CommitChanges();

            OnUpdateActivity(user, room);
        }

        private void ProcessUrls(IEnumerable<string> links, ChatRoom room, ChatMessage chatMessage)
        {
            // REVIEW: is this safe to do? We're holding on to this instance 
            // when this should really be a fire and forget.
            var contentTasks = links.Select(ExtractContent).ToArray();
            Task.Factory.ContinueWhenAll(contentTasks, tasks =>
            {
                foreach (var task in tasks)
                {
                    if (task.IsFaulted)
                    {
                        Trace.TraceError(task.Exception.GetBaseException().Message);
                        continue;
                    }

                    if (String.IsNullOrEmpty(task.Result))
                    {
                        continue;
                    }

                    // Try to get content from each url we're resolved in the query
                    string extractedContent = "<p>" + task.Result + "</p>";

                    // If we did get something, update the message and notify all clients
                    chatMessage.Content += extractedContent;

                    // Notify the room
                    Clients[room.Name].addMessageContent(chatMessage.Id, extractedContent, room.Name);

                    // Commit the changes
                    _repository.CommitChanges();
                }
            });
        }

        private bool TryHandleCommand(string command)
        {
            string clientId = Context.ClientId;
            string userId = Caller.id;
            string room = Caller.activeRoom;

            var commandManager = new CommandManager(clientId, userId, room, _service, _repository, this);
            return commandManager.TryHandleCommand(command);
        }

        private void DisconnectClient(string clientId)
        {
            ChatUser user = _service.DisconnectClient(clientId);

            // There's no associated user for this client id
            if (user == null)
            {
                return;
            }

            // The user will be marked as offline if all clients leave
            if (user.Status == (int)UserStatus.Offline)
            {
                foreach (var room in user.Rooms)
                {
                    var userViewModel = new UserViewModel(user);

                    Clients[room.Name].leave(userViewModel, room.Name).Wait();

                    OnRoomCountChanged(room);
                }
            }
        }

        private void OnUpdateActivity(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            Clients[room.Name].updateActivity(userViewModel, room.Name);
        }

        private void LeaveRoom(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            Clients[room.Name].leave(userViewModel, room.Name).Wait();

            foreach (var client in user.ConnectedClients)
            {
                GroupManager.RemoveFromGroup(client.Id, room.Name).Wait();
            }

            OnRoomCountChanged(room);
        }

        void INotificationService.LogOn(ChatUser user, string clientId)
        {
            LogOn(user, clientId);
        }

        void INotificationService.ChangePassword()
        {
            Caller.changePassword();
        }

        void INotificationService.SetPassword()
        {
            Caller.setPassword();
        }

        void INotificationService.KickUser(ChatUser targetUser, ChatRoom room)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Kick the user from this room
                Clients[client.Id].kick(room.Name);

                // Remove the user from this the room group so he doesn't get the leave message
                GroupManager.RemoveFromGroup(client.Id, room.Name).Wait();
            }

            // Tell the room the user left
            LeaveRoom(targetUser, room);
        }

        void INotificationService.OnUserCreated(ChatUser user)
        {
            // Set some client state
            Caller.name = user.Name;
            Caller.id = user.Id;

            // Tell the client a user was created
            Caller.userCreated();
        }

        void INotificationService.JoinRoom(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            var isOwner = user.OwnedRooms.Contains(room);

            // Tell all clients to join this room
            foreach (var client in user.ConnectedClients)
            {
                Clients[client.Id].joinRoom(room.Name);
            }

            // Tell the people in this room that you've joined
            Clients[room.Name].addUser(userViewModel, room.Name, isOwner).Wait();

            // Notify users of the room count change
            OnRoomCountChanged(room);

            foreach (var client in user.ConnectedClients)
            {
                // Add the caller to the group so they receive messages
                GroupManager.AddToGroup(client.Id, room.Name).Wait();
            }
        }

        void INotificationService.OnOwnerAdded(ChatUser targetUser, ChatRoom targetRoom)
        {
            foreach (var client in targetUser.ConnectedClients)
            {
                // Tell this client it's an owner
                Clients[client.Id].makeOwner(targetRoom.Name);
            }

            var userViewModel = new UserViewModel(targetUser);

            // If the target user is in the target room.
            // Tell everyone in the target room that a new owner was added
            if (ChatService.IsUserInRoom(targetRoom, targetUser))
            {
                Clients[targetRoom.Name].addOwner(userViewModel, targetRoom.Name);
            }

            // Tell the calling client the granting of ownership was successful
            Caller.ownerMade(targetUser.Name, targetRoom.Name);
        }

        void INotificationService.ChangeGravatar(ChatUser user)
        {
            // Update the calling client
            foreach (var client in user.ConnectedClients)
            {
                Clients[client.Id].gravatarChanged();
            }

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Tell all users in rooms to change the gravatar
            foreach (var room in user.Rooms)
            {
                Clients[room.Name].changeGravatar(userViewModel, room.Name);
            }
        }

        void INotificationService.OnSelfMessage(ChatRoom room, ChatUser user, string content)
        {
            Clients[room.Name].sendMeMessage(user.Name, content, room.Name);
        }

        void INotificationService.SendPrivateMessage(ChatUser user, ChatUser toUser, string messageText)
        {
            // Send a message to the sender and the sendee
            foreach (var client in user.ConnectedClients)
            {
                Clients[client.Id].sendPrivateMessage(user.Name, toUser.Name, messageText);
            }

            foreach (var client in toUser.ConnectedClients)
            {
                Clients[client.Id].sendPrivateMessage(user.Name, toUser.Name, messageText);
            }
        }

        void INotificationService.ListRooms(ChatUser user)
        {
            Caller.showUsersRoomList(user.Name, user.Rooms.Select(r => r.Name));
        }

        void INotificationService.ListUsers()
        {
            var users = _repository.Users.Online().Select(s => s.Name);
            Caller.listUsers(users);
        }

        void INotificationService.ListUsers(IEnumerable<ChatUser> users)
        {
            Caller.listUsers(users.Select(s => s.Name));
        }

        void INotificationService.ListUsers(ChatRoom room, IEnumerable<string> names)
        {
            Caller.showUsersInRoom(room.Name, names);
        }

        void INotificationService.LogOut(ChatUser user, string clientId)
        {
            DisconnectClient(clientId);

            var rooms = user.Rooms.Select(r => r.Name);

            Caller.logOut(rooms);
        }

        void INotificationService.ShowHelp()
        {
            Caller.showCommands(new[] { 
                new { Name = "help", Description = "Type /help to show the list of commands" },
                new { Name = "nick", Description = "Type /nick [user] [password] to create a user or change your nickname. You can change your password with /nick [user] [oldpassword] [newpassword]" },
                new { Name = "join", Description = "Type /join [room] - to join a channel of your choice" },
                new { Name = "create", Description = "Type /create [room] to create a room" },
                new { Name = "me", Description = "Type /me 'does anything'" },
                new { Name = "msg", Description = "Type /msg @nickname (message) to send a private message to nickname. @ is optional." },
                new { Name = "leave", Description = "Type /leave to leave the current room. Type /leave [room name] to leave a specific room." },
                new { Name = "rooms", Description = "Type /rooms to show the list of rooms" },
                new { Name = "who", Description = "Type /who to show a list of all users, /who [name] to the rooms that user is in" },
                new { Name = "list", Description = "Type /list (room) to show a list of users in the room" },
                new { Name = "gravatar", Description = "Type /gravatar [email] to set your gravatar." },
                new { Name = "nudge", Description = "Type /nudge to send a nudge to the whole room, or \"/nudge @nickname\" to nudge a particular user. @ is optional." },
                new { Name = "kick", Description = "Type /kick [user] to kick a user from the room. Note, this is only valid for owners of the room." },
                new { Name = "logout", Description = "Type /logout - To logout from this client (chat cookie will be removed)." },
                new { Name = "addowner", Description = "Type /addowner [user] [room] - To add an owner a user as an owner to the specified room. Only works if you're an owner of that room." }
            });
        }

        void INotificationService.ShowRooms()
        {
            Caller.showRooms(GetRooms());
        }

        void INotificationService.NugeUser(ChatUser user, ChatUser targetUser)
        {
            // Send a nudge message to the sender and the sendee
            foreach (var client in targetUser.ConnectedClients)
            {
                Clients[client.Id].nudge(user.Name, targetUser.Name);
            }

            foreach (var client in user.ConnectedClients)
            {
                Clients[client.Id].sendPrivateMessage(user.Name, targetUser.Name, "nudged " + targetUser.Name);
            }
        }

        void INotificationService.NudgeRoom(ChatRoom room, ChatUser user)
        {
            Clients[room.Name].nudge(user.Name);
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
                Clients[client.Id].userNameChanged(userViewModel);
            }

            // Notify all users in the rooms
            foreach (var room in user.Rooms)
            {
                Clients[room.Name].changeUserName(oldUserName, userViewModel, room.Name);
            }
        }

        private void OnRoomCountChanged(ChatRoom room)
        {
            // Update the room count
            Clients.updateRoomCount(room.Name, room.Users.Online().Count());
        }

        private string Transform(string message, out HashSet<string> extractedUrls)
        {
            const string urlPattern = @"((https?|ftp)://|www\.)[\w]+(.[\w]+)([\w\-\.\[\],@?^=%&amp;:/~\+#!]*[\w\-\@?^=%&amp;/~\+#\[\]])";

            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            message = Regex.Replace(message, urlPattern, m =>
            {
                string httpPortion = String.Empty;
                if (!m.Value.Contains("://"))
                {
                    httpPortion = "http://";
                }

                string url = httpPortion + m.Value;

                urls.Add(HttpUtility.HtmlDecode(url));

                return String.Format(CultureInfo.InvariantCulture,
                                     "<a rel=\"nofollow external\" target=\"_blank\" href=\"{0}\" title=\"{1}\">{1}</a>",
                                     url, m.Value);
            });

            extractedUrls = urls;
            return message;
        }

        private Task<string> ExtractContent(string url)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            var requestTask = Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state), ar => request.EndGetResponse(ar), null);
            return requestTask.ContinueWith(task => ExtractContent((HttpWebResponse)task.Result));
        }

        private string ExtractContent(HttpWebResponse response)
        {
            return _contentProviders.Select(c => c.GetContent(response))
                                    .FirstOrDefault(content => content != null);
        }

        private ClientState GetClientState()
        {
            // New client state
            var jabbrState = GetCookieValue("jabbr.state");

            if (!String.IsNullOrEmpty(jabbrState))
            {
                return JsonConvert.DeserializeObject<ClientState>(jabbrState);
            }

            return new ClientState
            {
                UserId = GetCookieValue("userid"),
                ActiveRoom = GetCookieValue("currentroom")
            };
        }

        private string GetCookieValue(string key)
        {
            HttpCookie cookie = Context.Cookies[key];
            return cookie != null ? HttpUtility.UrlDecode(cookie.Value) : null;
        }

        private class ClientState
        {
            public string UserId { get; set; }
            public string ActiveRoom { get; set; }
        }
    }
}

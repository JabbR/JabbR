using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Chat.Infrastructure;
using Chat.Models;
using Chat.ViewModels;
using Microsoft.Security.Application;
using SignalR.Hubs;
using SignalR.Samples.Hubs.Chat.ContentProviders;

namespace SignalR.Samples.Hubs.Chat {
    public class Chat : Hub, IDisconnect {
        private static ChatRepository _db = new ChatRepository();

        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(2);
        private static bool _sweeping;

        private static Timer _timer = new Timer(_ => Sweep(), null, _sweepInterval, _sweepInterval);

        private static readonly List<IContentProvider> _contentProviders = new List<IContentProvider>() {
            new ImageContentProvider(),
            new YouTubeContentProvider(),
            new CollegeHumorContentProvider()
        };

        public bool OutOfSync {
            get {
                string version = Caller.version;
                return String.IsNullOrEmpty(version) ||
                        new Version(version) != typeof(Chat).Assembly.GetName().Version;
            }
        }

        public bool Join() {
            Caller.version = typeof(Chat).Assembly.GetName().Version.ToString();

            // Check the user id cookie
            HttpCookie cookie = Context.Cookies["userid"];
            if (cookie == null) {
                return false;
            }

            ChatUser user = _db.Users.FirstOrDefault(u => u.Id == cookie.Value);

            // If there's no registered user, return false
            if (user == null) {
                return false;
            }

            // Update the users's client id mapping
            user.ClientId = Context.ClientId;
            user.Active = true;
            user.LastActivity = DateTime.UtcNow;

            var userViewModel = new UserViewModel(user);

            LeaveAllRooms(user);
            Caller.room = null;

            // Set some client state
            Caller.id = user.Id;
            Caller.name = user.Name;
            Caller.hash = user.Hash;

            // Add this user to the list of users
            Caller.addUser(userViewModel);
            return true;
        }

        public void Send(string content) {
            if (OutOfSync) {
                throw new InvalidOperationException("Chat was just updated, please refresh you browser and rejoin " + Caller.room);
            }

            UpdateActivity();

            content = Sanitizer.GetSafeHtmlFragment(content);

            // See if this is a valid command (starts with /)
            if (TryHandleCommand(content)) {
                return;
            }

            string roomName = Caller.room;
            string name = Caller.name;

            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom chatRoom = tuple.Item2;

            HashSet<string> links;
            var messageText = Transform(content, out links);

            var chatMessage = new ChatMessage {
                Id = Guid.NewGuid().ToString("d"),
                User = user,
                Content = messageText,
                When = DateTimeOffset.UtcNow
            };

            chatRoom.Messages.Add(chatMessage);

            var messageViewModel = new MessageViewModel(chatMessage);

            Clients[chatRoom.Name].addMessage(messageViewModel);

            if (!links.Any()) {
                return;
            }

            ProcessUrls(links, chatRoom, chatMessage);
        }

        public void Disconnect() {
            Disconnect(Context.ClientId);
        }

        public IEnumerable<UserViewModel> GetUsers() {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room)) {
                return Enumerable.Empty<UserViewModel>();
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null) {
                return Enumerable.Empty<UserViewModel>();
            }

            return chatRoom.Users.Select(u => new UserViewModel(u));
        }

        public IEnumerable<MessageViewModel> GetRecentMessages() {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room)) {
                return Enumerable.Empty<MessageViewModel>();
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null) {
                return Enumerable.Empty<MessageViewModel>();
            }

            return (from m in chatRoom.Messages
                    orderby m.When descending
                    select new MessageViewModel(m)).Take(20).Reverse();
        }

        private void Disconnect(string clientId) {
            ChatUser user = _db.Users.FirstOrDefault(u => u.ClientId == clientId);

            if (user == null) {
                return;
            }

            LeaveAllRooms(user);
        }

        private void UpdateActivity() {
            ChatUser user = _db.Users.FirstOrDefault(u => u.ClientId == Context.ClientId);            
            if (user == null) {
                return;
            }

            string room = Caller.room;
            if (String.IsNullOrEmpty(room)) {
                return;
            }

            Clients[room].updateActivity(new UserViewModel(user));
        }

        private void ProcessUrls(IEnumerable<string> links, ChatRoom chatRoom, ChatMessage chatMessage) {
            // REVIEW: is this safe to do? We're holding on to this instance 
            // when this should really be a fire and forget.
            var contentTasks = links.Select(ExtractContent).ToArray();
            Task.Factory.ContinueWhenAll(contentTasks, tasks => {
                foreach (var task in tasks) {
                    if (task.IsFaulted) {
                        Trace.TraceError(task.Exception.GetBaseException().Message);
                        continue;
                    }

                    if (String.IsNullOrEmpty(task.Result)) {
                        continue;
                    }

                    // Try to get content from each url we're resolved in the query
                    string extractedContent = "<p>" + task.Result + "</p>";

                    // If we did get something, update the message and notify all clients
                    chatMessage.Content += extractedContent;

                    Clients[chatRoom.Name].addMessageContent(chatMessage.Id, extractedContent);
                }
            });
        }

        private bool TryHandleCommand(string command) {
            command = command.Trim();
            if (!command.StartsWith("/")) {
                return false;
            }

            string room = Caller.room;
            string name = Caller.name;
            string[] parts = command.Substring(1).Split(' ');
            string commandName = parts[0];

            if (commandName.Equals("help", StringComparison.OrdinalIgnoreCase)) {
                HandleHelp();

                return true;
            }
            else if (commandName.Equals("nick", StringComparison.OrdinalIgnoreCase)) {
                HandleNick(name, parts);

                return true;
            }
            else {
                ChatUser user = EnsureUser();
                if (commandName.Equals("rooms", StringComparison.OrdinalIgnoreCase)) {
                    HandleRooms();

                    return true;
                }
                else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase)) {
                    HandleJoin(room, user, parts);

                    return true;
                }
                else if (commandName.Equals("msg", StringComparison.OrdinalIgnoreCase)) {
                    HandleMsg(user, parts);

                    return true;
                }
                else if (commandName.Equals("gravatar", StringComparison.OrdinalIgnoreCase)) {
                    HandleGravatar(user, parts);

                    return true;
                }
                else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase) && parts.Length == 2) {
                    HandleLeave(user, parts);

                    return true;
                }
                else {
                    Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();
                    if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase)) {
                        HandleMe(tuple.Item2, tuple.Item1, parts);
                        return true;
                    }
                    else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase)) {
                        HandleLeave(tuple.Item2, tuple.Item1);

                        return true;
                    }

                    throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", parts[0]));
                }
            }
        }

        private void HandleHelp() {
            Caller.showCommands(new[] { 
                new { Name = "help", Description = "Shows the list of commands" },
                new { Name = "nick", Description = "/nick changes your nickname" },
                new { Name = "join", Description = "Type /join room -- to join a channel of your choice" },
                new { Name = "me", Description = "Type /me 'does anything'" },
                new { Name = "msg", Description = "Type /msg nickname (message) to send a private message to nickname." },
                new { Name = "leave", Description = "Type /leave to leave the current room. Type /leave [room name] to leave a specific room." },
                new { Name = "rooms", Description = "Type /rooms to show the list of rooms" },
                new { Name = "gravatar", Description = "Type \"/gravatar email\" to set your gravatar." }
            });
        }

        private void HandleLeave(ChatUser user, string[] parts) {
            if (string.IsNullOrWhiteSpace(parts[1])) {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _db.Rooms.FirstOrDefault(r => r.Name.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
            if (room == null) {
                throw new InvalidOperationException("No room with that name!");
            }

            HandleLeave(room, user);
        }

        private void HandleLeave(ChatRoom room, ChatUser user) {
            room.Users.Remove(user);
            user.Rooms.Remove(room);

            var userViewModel = new UserViewModel(user);
            Clients[room.Name].leave(userViewModel).Wait();

            RemoveFromGroup(room.Name).Wait();
        }

        private void HandleMe(ChatRoom room, ChatUser user, string[] parts) {
            if (parts.Length < 2) {
                throw new InvalidOperationException("You what?");
            }

            var content = String.Join(" ", parts.Skip(1));

            Clients[room.Name].sendMeMessage(user.Name, content);
        }

        private void HandleMsg(ChatUser user, string[] parts) {
            if (_db.Users.Count == 1) {
                throw new InvalidOperationException("You're the only person in here...");
            }

            if (parts.Length < 2 || String.IsNullOrWhiteSpace(parts[1])) {
                throw new InvalidOperationException("Who are you trying send a private message to?");
            }

            ChatUser toUser = _db.Users.FirstOrDefault(u => u.Name.Equals(parts[1], StringComparison.OrdinalIgnoreCase));

            if (toUser == null) {
                throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", toUser.Name));
            }

            if (toUser == user) {
                throw new InvalidOperationException("You can't private message yourself!");
            }

            string messageText = String.Join(" ", parts.Skip(2)).Trim();

            if (String.IsNullOrEmpty(messageText)) {
                throw new InvalidOperationException(String.Format("What did you want to say to '{0}'.", toUser.Name));
            }

            // Send a message to the sender and the sendee                        
            Clients[toUser.ClientId].sendPrivateMessage(user.Name, toUser.Name, messageText);
            Caller.sendPrivateMessage(user.Name, toUser.Name, messageText);
        }

        private void HandleJoin(string oldRoomName, ChatUser user, string[] parts) {
            if (parts.Length < 2) {
                throw new InvalidOperationException("Join which room?");
            }

            var userViewModel = new UserViewModel(user);

            // Only support joining one room at a time for now (until we support tabs)
            ChatRoom oldRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(oldRoomName, StringComparison.OrdinalIgnoreCase));
            if (oldRoom != null) {
                HandleLeave(oldRoom, user);
            }

            // Create the room if it doesn't exist
            string newRoomName = parts[1];
            ChatRoom newRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(newRoomName, StringComparison.OrdinalIgnoreCase));
            if (newRoom == null) {
                newRoom = new ChatRoom {
                    Name = newRoomName
                };

                _db.Rooms.Add(newRoom);
            }

            if (user.Rooms.Contains(newRoom)) {
                throw new InvalidOperationException("You're already in that room!");
            }

            // Add this room to the user's list of rooms
            user.Rooms.Add(newRoom);

            // Add this user to the list of room's users
            newRoom.Users.Add(user);

            // Tell the people in this room that you're joining
            Clients[newRoom.Name].addUser(userViewModel).Wait();

            // Set the room on the caller
            Caller.room = newRoom.Name;

            // Add the caller to the group so they receive messages
            AddToGroup(newRoomName).Wait();

            Caller.joinRoom(newRoomName);
        }

        private void HandleGravatar(ChatUser user, string[] parts) {
            string email = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrWhiteSpace(email)) {
                throw new InvalidOperationException("Email was not specified!");
            }

            user.Hash = email.ToMD5();
            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any()) {
                foreach (var room in user.Rooms) {
                    Clients[room.Name].changeGravatar(userViewModel);
                }
            }
            else {
                Caller.changeGravatar(userViewModel);
            }
        }

        private void HandleRooms() {
            var rooms = _db.Rooms.Select(r => new {
                Name = r.Name,
                Count = r.Users.Count
            });

            Caller.showRooms(rooms);
        }

        private void HandleNick(string name, string[] parts) {
            string newUserName = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrWhiteSpace(newUserName)) {
                throw new InvalidOperationException("No username specified!");
            }

            ChatUser user = _db.Users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (user == null) {
                AddUser(newUserName);
            }
            else {
                ChangeUserName(user, newUserName);
            }
        }

        private void ChangeUserName(ChatUser user, string newUserName) {
            if (user.Name.Equals(newUserName, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("That's already your username...");
            }

            ChatUser newUser = _db.Users.FirstOrDefault(u => u.Name.Equals(newUserName, StringComparison.OrdinalIgnoreCase));

            if (newUser != null) {
                throw new InvalidOperationException(String.Format("Username '{0}' is already taken!", newUserName));
            }

            string oldUserName = user.Name;
            user.Name = newUserName;
            Caller.name = newUserName;

            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any()) {
                foreach (var room in user.Rooms) {
                    Clients[room.Name].changeUserName(userViewModel, oldUserName, newUserName);
                }
            }
            else {
                Caller.changeUserName(userViewModel, oldUserName, newUserName);
            }
        }

        private static void Sweep() {
            if (_sweeping) {
                return;
            }

            _sweeping = true;

            var clients = GetClients<Chat>();

            var inactiveUsers = new List<ChatUser>();

            foreach (var user in _db.Users) {
                var elapsed = DateTime.UtcNow - user.LastActivity;
                if (elapsed.TotalMinutes > 5) {
                    user.Active = false;
                    inactiveUsers.Add(user);
                }
            }

            var roomGroups = from u in inactiveUsers
                             from r in u.Rooms
                             select new { User = u, Room = r } into tuple
                             group tuple by tuple.Room into g
                             select new {
                                 Room = g.Key,
                                 Users = g.Select(t => new UserViewModel(t.User))
                             };

            foreach (var roomGroup in roomGroups) {
                clients[roomGroup.Room.Name].markInactive(roomGroup.Users).Wait();
            }

            _sweeping = false;
        }

        private void AddUser(string name) {
            var user = new ChatUser {
                Name = name,
                Active = true,
                Id = Guid.NewGuid().ToString("d"),
                LastActivity = DateTime.UtcNow,
                ClientId = Context.ClientId
            };

            _db.Users.Add(user);

            Caller.name = user.Name;
            Caller.hash = user.Hash;
            Caller.id = user.Id;

            var userViewModel = new UserViewModel(user);
            Caller.addUser(userViewModel);
        }

        private void LeaveAllRooms(ChatUser user) {
            // Leave all rooms
            foreach (var room in user.Rooms.ToList()) {
                HandleLeave(room, user);
            }
        }

        private Tuple<ChatUser, ChatRoom> EnsureUserAndRoom() {
            ChatUser user = EnsureUser();

            string room = Caller.room;
            string name = Caller.name;

            if (String.IsNullOrEmpty(room)) {
                throw new InvalidOperationException("Use '/join room' to join a room.");
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null) {
                throw new InvalidOperationException(String.Format("You're in '{0}' but it doesn't exist. Use /join '{1}' to create this room."));
            }

            if (!chatRoom.Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
            }

            return Tuple.Create(user, chatRoom);
        }

        private ChatUser EnsureUser() {
            string name = Caller.name;

            if (String.IsNullOrEmpty(name)) {
                throw new InvalidOperationException("You don't have a name. Pick a name using '/nick nickname'.");
            }

            ChatUser user = _db.Users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (user == null) {
                throw new InvalidOperationException(String.Format("You go by the name '{0}' but the server has no idea who you are. Maybe it got reset :(.", name));
            }

            // Keep the client id up to date
            if (String.IsNullOrEmpty(user.ClientId)) {
                user.ClientId = Context.ClientId;
            }

            user.Active = true;
            user.LastActivity = DateTime.UtcNow;

            return user;
        }

        private string Transform(string message, out HashSet<string> extractedUrls) {
            const string urlPattern = @"((https?|ftp)://|www\.)[\w]+(.[\w]+)([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])";

            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            message = Regex.Replace(message, urlPattern, m => {
                string httpPortion = String.Empty;
                if (!m.Value.Contains("://")) {
                    httpPortion = "http://";
                }

                string url = httpPortion + m.Value;

                urls.Add(url);

                return String.Format(CultureInfo.InvariantCulture,
                                     "<a rel=\"nofollow external\" target=\"_blank\" href=\"{0}\" title=\"{1}\">{1}</a>",
                                     url, m.Value);
            });

            extractedUrls = urls;
            return message;
        }

        private Task<string> ExtractContent(string url) {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            var requestTask = Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state), ar => request.EndGetResponse(ar), null);
            return requestTask.ContinueWith(task => ExtractContent((HttpWebResponse)task.Result));
        }

        private string ExtractContent(HttpWebResponse response) {
            return _contentProviders.Select(c => c.GetContent(response))
                                    .FirstOrDefault(content => content != null);
        }
    }
}
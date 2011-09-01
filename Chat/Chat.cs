using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Chat.Infrastructure;
using Chat.Models;
using Microsoft.Security.Application;
using SignalR.Hubs;
using SignalR.Samples.Hubs.Chat.ContentProviders;
using System.IO;
using System.Xml.Linq;

namespace SignalR.Samples.Hubs.Chat {
    public class Chat : Hub, IDisconnect {
        private static readonly Dictionary<string, DateTime> _userActivity = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ChatUser> _users = new Dictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HashSet<string>> _userRooms = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ChatRoom> _rooms = new Dictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(2);

        private static Timer _timer = new Timer(_ => Sweep(), null, _sweepInterval, _sweepInterval);

        private static readonly List<IContentProvider> _contentProviders = new List<IContentProvider>() {
            new ImageContentProvider(),
            new YouTubeContentProvider(),
            new CollegeHumorContentProvider()
        };

        public bool OldVersion {
            get {
                string version = Caller.version;
                return String.IsNullOrEmpty(version) ||
                        new Version(version) < typeof(Chat).Assembly.GetName().Version;
            }
        }

        public bool Join() {
            Caller.version = typeof(Chat).Assembly.GetName().Version.ToString();

            // Check the user id cookie
            var cookie = Context.Cookies["userid"];
            if (cookie == null) {
                return false;
            }

            ChatUser user = _users.Values.FirstOrDefault(u => u.Id == cookie.Value);

            if (user != null) {
                // Update the users's client id mapping
                user.ClientId = Context.ClientId;
                UpdateActivity();

                // Set some client state
                Caller.id = user.Id;
                Caller.name = user.Name;
                Caller.hash = user.Hash;

                // Leave all rooms
                HashSet<string> rooms;
                if (_userRooms.TryGetValue(user.Name, out rooms)) {
                    foreach (var room in rooms) {
                        Clients[room].leave(user);
                        ChatRoom chatRoom = _rooms[room];
                        chatRoom.Users.Remove(user.Name);
                    }
                }

                _userRooms[user.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Add this user to the list of users
                Caller.addUser(user);
                return true;
            }

            return false;
        }

        private void UpdateActivity() {
            _userActivity[Context.ClientId] = DateTime.UtcNow;
            var user = GetUserByClientId(Context.ClientId);
            if (user != null) {
                Clients.updateActivity(user);
            }
        }

        public void Send(string content) {
            if (OldVersion) {
                throw new InvalidOperationException("Chat was just updated, please refresh you browser and rejoin " + Caller.room);
            }

            UpdateActivity();

            content = Sanitizer.GetSafeHtmlFragment(content);

            if (TryHandleCommand(content)) {
                return;
            }

            string roomName = Caller.room;
            string name = Caller.name;

            EnsureUserAndRoom();

            HashSet<string> links;
            var messageText = Transform(content, out links);
            var chatMessage = new ChatMessage(_users[name], messageText);

            _rooms[roomName].Messages.Add(chatMessage);

            Clients[roomName].addMessage(chatMessage.Id,
                                         chatMessage.User,
                                         chatMessage.Content,
                                         chatMessage.When);

            if (links.Any()) {
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

                        Clients[roomName].addMessageContent(chatMessage.Id, extractedContent);
                    }
                });
            }
        }

        public void Disconnect() {
            Disconnect(Context.ClientId);
        }

        private void Disconnect(string clientId) {
            ChatUser user = GetUserByClientId(clientId);
            if (user == null) {
                return;
            }

            // Leave all rooms
            HashSet<string> rooms;
            if (_userRooms.TryGetValue(user.Name, out rooms)) {
                foreach (var room in rooms) {
                    Clients[room].leave(user);
                    ChatRoom chatRoom = _rooms[room];
                    chatRoom.Users.Remove(user.Name);
                }
            }

            _userRooms.Remove(user.Name);
        }

        public IEnumerable<ChatUser> GetUsers() {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room)) {
                return Enumerable.Empty<ChatUser>();
            }

            return from name in _rooms[room].Users
                   let user = _users[name]
                   orderby user.Name
                   select user;
        }

        public IEnumerable<ChatMessage> GetRecentMessages() {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room)) {
                return Enumerable.Empty<ChatMessage>();
            }

            return (from m in _rooms[room].Messages
                    orderby m.When descending
                    select m).Take(20).Reverse();
        }

        private static ChatUser GetUserByClientId(string clientId) {
            return _users.Values.FirstOrDefault(u => u.ClientId == clientId);
        }

        private bool TryHandleCommand(string command) {
            string room = Caller.room;
            string name = Caller.name;

            command = command.Trim();
            if (command.StartsWith("/")) {
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
                    EnsureUser();
                    if (commandName.Equals("rooms", StringComparison.OrdinalIgnoreCase)) {
                        HandleRooms();

                        return true;
                    }
                    else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase)) {
                        HandleJoin(room, name, parts);

                        return true;
                    }
                    else if (commandName.Equals("msg", StringComparison.OrdinalIgnoreCase)) {
                        HandleMsg(name, parts);

                        return true;
                    }
                    else if (commandName.Equals("gravatar", StringComparison.OrdinalIgnoreCase)) {
                        HandleGravatar(name, parts);

                        return true;
                    }
                    else {
                        EnsureUserAndRoom();
                        if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase)) {
                            HandleMe(room, name, parts);
                            return true;
                        }
                        else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase)) {
                            HandleLeave(room, name);

                            return true;
                        }

                        throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", parts[0]));
                    }
                }
            }
            return false;
        }

        private void HandleHelp() {
            Caller.showCommands(new[] { 
                new { Name = "help", Description = "Shows the list of commands" },
                new { Name = "nick", Description = "/nick changes your nickname" },
                new { Name = "join", Description = "Type /join room -- to join a channel of your choice" },
                new { Name = "me", Description = "Type /me 'does anything'" },
                new { Name = "msg", Description = "Type /msg nickname (message) to send a private message to nickname." },
                new { Name = "leave", Description = "Type /leave to leave the current room." },
                new { Name = "rooms", Description = "Type /rooms to show the list of rooms" },
                new { Name = "gravatar", Description = "Type \"/gravatar email\" to set your gravatar." }
            });
        }

        private void HandleLeave(string room, string name) {
            ChatRoom chatRoom;
            if (_rooms.TryGetValue(room, out chatRoom)) {
                chatRoom.Users.Remove(name);
                _userRooms[name].Remove(room);

                Clients[room].leave(_users[name]);
            }

            RemoveFromGroup(room);

            Caller.room = null;
        }

        private void HandleMe(string room, string name, string[] parts) {
            if (parts.Length < 2) {
                throw new InvalidOperationException("You what?");
            }

            var content = String.Join(" ", parts.Skip(1));

            Clients[room].sendMeMessage(name, content);
        }

        private void HandleMsg(string name, string[] parts) {
            if (_users.Count == 1) {
                throw new InvalidOperationException("You're the only person in here...");
            }

            if (parts.Length < 2) {
                throw new InvalidOperationException("Who are you trying send a private message to?");
            }

            string to = parts[1];
            if (to.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("You can't private message yourself!");
            }

            if (!_users.ContainsKey(to)) {
                throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", to));
            }

            string messageText = String.Join(" ", parts.Skip(2)).Trim();

            if (String.IsNullOrEmpty(messageText)) {
                throw new InvalidOperationException(String.Format("What did you want to say to '{0}'.", to));
            }

            string recipientId = _users[to].ClientId;
            // Send a message to the sender and the sendee                        
            Clients[recipientId].sendPrivateMessage(name, to, messageText);
            Caller.sendPrivateMessage(name, to, messageText);
        }

        private void HandleJoin(string room, string name, string[] parts) {
            if (parts.Length < 2) {
                throw new InvalidOperationException("Join which room?");
            }

            string newRoom = parts[1];
            ChatRoom chatRoom;
            // Create the room if it doesn't exist
            if (!_rooms.TryGetValue(newRoom, out chatRoom)) {
                chatRoom = new ChatRoom();
                _rooms.Add(newRoom, chatRoom);
            }

            // Only support one room at a time for now (until we support tabs)
            // Remove the old room
            if (!String.IsNullOrEmpty(room)) {
                _userRooms[name].Remove(room);
                _rooms[room].Users.Remove(name);
                RemoveFromGroup(room);
                Clients[room].leave(_users[name]).Wait();
            }

            _userRooms[name].Add(newRoom);
            if (!chatRoom.Users.Add(name)) {
                throw new InvalidOperationException("You're already in that room!");
            }

            // Tell the people in this room that you're joining
            Clients[newRoom].addUser(_users[name]);

            // Set the room on the caller
            Caller.room = newRoom;

            AddToGroup(newRoom).Wait();

            Caller.joinRoom(newRoom);
        }

        private void HandleGravatar(string name, string[] parts)
        {
            string email = string.Join(" ", parts.Skip(1));

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email was not specified!");
            }

            var user = _users[name];
            user.Hash = email.ToMD5();

            bool inRooms = _userRooms[name].Any();

            if (inRooms)
            {
                foreach (var room in _userRooms[name])
                {
                    Clients[room].changeGravatar(user);
                }
            }


            if (!inRooms)
            {
                Caller.changeGravatar(user);
            }

        }

        private void HandleRooms() {
            var rooms = _rooms.Select(r => new {
                Name = r.Key,
                Count = r.Value.Users.Count
            });

            Caller.showRooms(rooms);
        }

        private void HandleNick(string name, string[] parts) {
            string newUserName = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrEmpty(newUserName)) {
                throw new InvalidOperationException("No username specified!");
            }

            if (newUserName.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("That's already your username...");
            }

            if (!_users.ContainsKey(newUserName)) {
                if (String.IsNullOrEmpty(name) || !_users.ContainsKey(name)) {
                    var user = AddUser(newUserName);
                }
                else {
                    var oldUser = _users[name];
                    var newUser = new ChatUser {
                        Name = newUserName,
                        Hash = newUserName.ToMD5(),
                        Id = oldUser.Id,
                        ClientId = oldUser.ClientId,
                        Offset = oldUser.Offset,
                        Timezone = oldUser.Timezone
                    };

                    _users[newUserName] = newUser;
                    _userRooms[newUserName] = new HashSet<string>(_userRooms[name]);

                    bool inRooms = _userRooms[name].Any();

                    if (inRooms) {
                        foreach (var room in _userRooms[name]) {
                            _rooms[room].Users.Remove(name);
                            _rooms[room].Users.Add(newUserName);
                            Clients[room].changeUserName(oldUser, newUser);
                        }
                    }

                    _userRooms.Remove(name);
                    _users.Remove(name);

                    Caller.hash = newUser.Hash;
                    Caller.name = newUser.Name;

                    if (!inRooms) {
                        Caller.changeUserName(oldUser, newUser);
                    }
                }
            }
            else {
                throw new InvalidOperationException(String.Format("Username '{0}' is already taken!", newUserName));
            }
        }

        private static void Sweep() {
            var users = _userActivity.ToList();
            var clients = GetClients<Chat>();

            foreach (var uid in users) {
                var elapsed = DateTime.UtcNow - uid.Value;
                if (elapsed.TotalMinutes > 5) {
                    var user = GetUserByClientId(uid.Key);
                    if (user != null) {
                        clients.markInactive(user);
                    }
                }
            }
        }

        private ChatUser AddUser(string newUserName) {
            var user = new ChatUser(newUserName) {
                ClientId = Context.ClientId
            };

            _users[newUserName] = user;
            _userRooms[newUserName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Caller.name = user.Name;
            Caller.hash = user.Hash;
            Caller.id = user.Id;

            Caller.addUser(user);

            return user;
        }

        private void EnsureUserAndRoom() {
            EnsureUser();

            string room = Caller.room;
            string name = Caller.name;

            if (String.IsNullOrEmpty(room) || !_rooms.ContainsKey(room)) {
                throw new InvalidOperationException("Use '/join room' to join a room.");
            }

            HashSet<string> rooms;
            if (!_userRooms.TryGetValue(name, out rooms) || !rooms.Contains(room)) {
                throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
            }
        }

        private void EnsureUser() {
            string name = Caller.name;
            ChatUser user;
            if (String.IsNullOrEmpty(name) || !_users.TryGetValue(name, out user)) {
                throw new InvalidOperationException("You don't have a name. Pick a name using '/nick nickname'.");
            }

            if (!_userRooms.ContainsKey(name)) {
                _userRooms[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (String.IsNullOrEmpty(user.ClientId)) {
                user.ClientId = Context.ClientId;
            }
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
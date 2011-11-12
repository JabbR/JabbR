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

namespace SignalR.Samples.Hubs.Chat
{
    public class Chat : Hub, IDisconnect
    {
        private static readonly ChatRepository _repository = new ChatRepository();

        // For testability
        private readonly ChatRepository _db;

        public Chat()
            : this(_repository)
        {
        }

        public Chat(ChatRepository db)
        {
            _db = db;
        }

        private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(5);
        private static bool _sweeping;
        private static Timer _timer = new Timer(_ => Sweep(_repository), null, _sweepInterval, _sweepInterval);

        private static readonly List<IContentProvider> _contentProviders = new List<IContentProvider>() {
            new ImageContentProvider(),
            new YouTubeContentProvider(),
            new CollegeHumorContentProvider(),
            new TweetContentProvider(),
            new PastieContentProvider(),
            new ImgurContentProvider()
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
            Caller.version = typeof(Chat).Assembly.GetName().Version.ToString();

            // Check the user id cookie
            HttpCookie userIdCookie = Context.Cookies["userid"];
            HttpCookie userNameCookie = Context.Cookies["username"];
            HttpCookie userRoomCookie = Context.Cookies["userroom"];
            HttpCookie userHashCookie = Context.Cookies["userhash"];

            // setup user 
            ChatUser user = null;

            // First try to retrieve user by id if exists
            if (userIdCookie != null && !String.IsNullOrWhiteSpace(userIdCookie.Value))
            {
                user = _db.Users.FirstOrDefault(u => u.Id == userIdCookie.Value);
            }

            // If we couldn't get user by id try it by name server could be reset
            if (user == null && userNameCookie != null && !String.IsNullOrWhiteSpace(userNameCookie.Value))
            {
                user = AddUser(userNameCookie.Value);
            }

            // If we have no user return false will force user to set new nick
            if (user == null)
            {
                return false;
            }

            // If we have user hash cookie set gravatar
            if (userHashCookie != null)
            {
                SetGravatar(user, userHashCookie.Value);
            }

            // Update the users's client id mapping
            user.ClientId = Context.ClientId;
            user.Active = true;
            user.LastActivity = DateTime.UtcNow;

            var userViewModel = new UserViewModel(user);

            Caller.room = null;

            // Set some client state
            Caller.id = user.Id;
            Caller.name = user.Name;
            Caller.hash = user.Hash;

            // If we have room add user to room
            if (userRoomCookie != null && !String.IsNullOrWhiteSpace(userRoomCookie.Value))
            {
                var userRoom = userRoomCookie.Value;

                var room = _db.Rooms.Where(x => x.Name == userRoom).FirstOrDefault();

                // If user has room name in the cookie but it doesn't exists create it!
                if (room == null)
                {
                    room = AddRoom(userRoom);
                }

                // Check if the user is already in the room if so let him rejoin
                if (IsUserInRoom(room, user))
                {
                    HandleRejoin(room, user);
                }
                // if the user is not in the room join the room
                else
                {
                    HandleJoin(null, user, new[] { room.Name, room.Name });
                }
            }
            // if user is in a room rejoin it
            else if (IsUserInARoom(user))
            {

                // retrieve user room
                var room = GetUserRoom(user);

                // handle the join of the room
                HandleRejoin(room, user);
            }

            // Add this user to the list of users
            Caller.addUser(userViewModel);
            return true;
        }

        public void Send(string content)
        {
            if (OutOfSync)
            {
                throw new InvalidOperationException("Chat was just updated, please refresh you browser and rejoin " + Caller.room);
            }

            content = Sanitizer.GetSafeHtmlFragment(content);

            // See if this is a valid command (starts with /)
            if (TryHandleCommand(content))
            {
                return;
            }

            string roomName = Caller.room;
            string name = Caller.name;

            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom chatRoom = tuple.Item2;

            // Update activity *after* ensuring the user, this forces them to be active
            UpdateActivity();

            HashSet<string> links;
            var messageText = Transform(content, out links);

            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString("d"),
                User = user,
                Content = messageText,
                When = DateTimeOffset.UtcNow
            };

            chatRoom.Messages.Add(chatMessage);

            var messageViewModel = new MessageViewModel(chatMessage);

            Clients[chatRoom.Name].addMessage(messageViewModel);

            if (!links.Any())
            {
                return;
            }

            ProcessUrls(links, chatRoom, chatMessage);
        }

        public void Disconnect()
        {
            Disconnect(Context.ClientId);
        }

        public IEnumerable<UserViewModel> GetUsers()
        {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room))
            {
                return Enumerable.Empty<UserViewModel>();
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null)
            {
                return Enumerable.Empty<UserViewModel>();
            }

            return chatRoom.Users.Select(u => new UserViewModel(u));
        }

        public IEnumerable<MessageViewModel> GetRecentMessages()
        {
            string room = Caller.room;

            if (String.IsNullOrEmpty(room))
            {
                return Enumerable.Empty<MessageViewModel>();
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null)
            {
                return Enumerable.Empty<MessageViewModel>();
            }

            return (from m in chatRoom.Messages
                    orderby m.When descending
                    select new MessageViewModel(m)).Take(20).Reverse();
        }

        public ChatRoom GetUserRoom(ChatUser user)
        {
            ChatRoom room = null;

            // check if the user has an room
            if (user.Rooms.Any())
            {

                // retrieve room
                var tempRoom = user.Rooms.FirstOrDefault();

                // ensure room is valid
                if (tempRoom != null)
                {
                    room = tempRoom;
                }
            }

            // check if rooms has the user
            if (room == null)
            {
                //retrieve room
                var tempRoom = _db.Rooms.First(x => x.Users.Any(u => u.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase)));

                // ensure room is valid
                if (tempRoom != null)
                {
                    room = tempRoom;
                }
            }

            return room;
        }

        public void Typing(bool isTyping)
        {
            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom chatRoom = tuple.Item2;
            var userViewModel = new UserViewModel(user);

            if (isTyping)
            {
                UpdateActivity();
            }

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    Clients[room.Name].setTyping(userViewModel, isTyping);
                }
            }
            else
            {
                Caller.setTyping(userViewModel, isTyping);
            }
        }

        private void Disconnect(string clientId)
        {
            ChatUser user = _db.Users.FirstOrDefault(u => u.ClientId == clientId);

            if (user == null)
            {
                return;
            }

            LeaveAllRooms(user);

            // Remove the user
            _db.Users.Remove(user);
        }

        private void UpdateActivity()
        {
            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();
            ChatUser user = tuple.Item1;
            ChatRoom room = tuple.Item2;

            if (user == null || room == null)
            {
                return;
            }

            Clients[room.Name].updateActivity(new UserViewModel(user));
        }

        private void ProcessUrls(IEnumerable<string> links, ChatRoom chatRoom, ChatMessage chatMessage)
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

                    Clients[chatRoom.Name].addMessageContent(chatMessage.Id, extractedContent);
                }
            });
        }

        private bool TryHandleCommand(string command)
        {
            command = command.Trim();
            if (!command.StartsWith("/"))
            {
                return false;
            }

            string room = Caller.room;
            string name = Caller.name;
            string[] parts = command.Substring(1).Split(' ');
            string commandName = parts[0];

            if (commandName.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                HandleHelp();

                return true;
            }
            else if (commandName.Equals("nick", StringComparison.OrdinalIgnoreCase))
            {
                HandleNick(name, parts);

                return true;
            }
            else
            {
                ChatUser user = EnsureUser();
                if (commandName.Equals("rooms", StringComparison.OrdinalIgnoreCase))
                {
                    HandleRooms();

                    return true;
                }
                else if (commandName.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    HandleList(parts);
                    return true;
                }
                else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase))
                {
                    HandleJoin(room, user, parts);

                    return true;
                }
                else if (commandName.Equals("msg", StringComparison.OrdinalIgnoreCase))
                {
                    HandleMsg(user, parts);

                    return true;
                }
                else if (commandName.Equals("gravatar", StringComparison.OrdinalIgnoreCase))
                {
                    HandleGravatar(user, parts);

                    return true;
                }
                else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
                {
                    HandleLeave(user, parts);

                    return true;
                }
                else if (commandName.Equals("nudge", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
                {
                    HandleNudge(user, parts);

                    return true;
                }
                else
                {
                    Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();
                    if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMe(tuple.Item2, tuple.Item1, parts);
                        return true;
                    }
                    else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleLeave(tuple.Item2, tuple.Item1);

                        return true;
                    }
                    else if (commandName.Equals("nudge", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleNudge(tuple.Item2, tuple.Item1, parts);

                        return true;
                    }

                    throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", parts[0]));
                }
            }
        }

        private void HandleList(string[] parts)
        {
            if (String.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _db.Rooms.FirstOrDefault(r => r.Name.Equals(parts[1], StringComparison.OrdinalIgnoreCase));

            if (room == null)
            {
                throw new InvalidOperationException("No room with that name!");
            }

            var names = room.Users.Select(s => s.Name);
            Caller.showUsersInRoom(room.Name, names);
        }

        private void HandleHelp()
        {
            Caller.showCommands(new[] { 
                new { Name = "help", Description = "Shows the list of commands" },
                new { Name = "nick", Description = "/nick changes your nickname" },
                new { Name = "join", Description = "Type /join room -- to join a channel of your choice" },
                new { Name = "me", Description = "Type /me 'does anything'" },
                new { Name = "msg", Description = "Type /msg @nickname (message) to send a private message to nickname. @ is optional." },
                new { Name = "leave", Description = "Type /leave to leave the current room. Type /leave [room name] to leave a specific room." },
                new { Name = "rooms", Description = "Type /rooms to show the list of rooms" },
                new { Name = "list", Description = "Type /list (room) to show a list of users in the room" },
                new { Name = "gravatar", Description = "Type \"/gravatar email\" to set your gravatar." },
                new { Name = "nudge", Description = "Type \"/nudge\" to send a nudge to the whole room, or \"/nudge @nickname\" to nudge a particular user. @ is optional." }
            });
        }

        private void HandleLeave(ChatUser user, string[] parts)
        {
            if (String.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _db.Rooms.FirstOrDefault(r => r.Name.Equals(parts[1], StringComparison.OrdinalIgnoreCase));

            if (room == null)
            {
                throw new InvalidOperationException("No room with that name!");
            }

            HandleLeave(room, user);
        }

        private void HandleLeave(ChatRoom room, ChatUser user)
        {
            room.Users.Remove(user);
            user.Rooms.Remove(room);

            var userViewModel = new UserViewModel(user);
            Clients[room.Name].leave(userViewModel).Wait();

            RemoveFromGroup(room.Name).Wait();
        }

        private void HandleMe(ChatRoom room, ChatUser user, string[] parts)
        {
            if (parts.Length < 2)
            {
                throw new InvalidOperationException("You what?");
            }

            var content = String.Join(" ", parts.Skip(1));

            Clients[room.Name].sendMeMessage(user.Name, content);
        }

        private void HandleMsg(ChatUser user, string[] parts)
        {
            if (_db.Users.Count == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            if (parts.Length < 2 || String.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException("Who are you trying send a private message to?");
            }
            var toUserName = NormalizeUserName(parts[1]);
            ChatUser toUser = _db.Users.FirstOrDefault(u => u.Name.Equals(toUserName, StringComparison.OrdinalIgnoreCase));

            if (toUser == null)
            {
                throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", toUserName));
            }

            if (toUser == user)
            {
                throw new InvalidOperationException("You can't private message yourself!");
            }

            string messageText = String.Join(" ", parts.Skip(2)).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException(String.Format("What did you want to say to '{0}'.", toUser.Name));
            }

            // Send a message to the sender and the sendee                        
            Clients[toUser.ClientId].sendPrivateMessage(user.Name, toUser.Name, messageText);
            Caller.sendPrivateMessage(user.Name, toUser.Name, messageText);
        }

        private string NormalizeUserName(string userName)
        {
            return userName.StartsWith("@") ? userName.Substring(1) : userName;
        }

        private bool IsUserInRoom(ChatRoom room, ChatUser user)
        {
            return room.Users.Any(x => x.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase)) || user.Rooms.Any(x => x.Name.Equals(room.Name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsUserInRoom(string roomName, ChatUser user)
        {
            var room = _db.Rooms.FirstOrDefault(x => x.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

            if (room == null)
            {
                return false;
            }

            return IsUserInRoom(room, user);
        }

        private bool IsUserInARoom(ChatUser user)
        {
            return _db.Rooms.Any(room => room.Users.Any(chatUser => chatUser.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase))) || user.Rooms.Any();
        }

        private void HandleRejoin(ChatRoom room, ChatUser user)
        {
            // check if the user is in a room
            if (IsUserInRoom(room, user))
            {

                // Only support joining one room at a time for now (until we support tabs)
                HandleLeave(room, user);
            }

            JoinRoom(user, room);
        }

        private void HandleJoin(string oldRoomName, ChatUser user, string[] parts)
        {
            if (parts.Length < 2)
            {
                throw new InvalidOperationException("Join which room?");
            }

            string newRoomName = parts[1];

            if (IsUserInRoom(newRoomName, user))
            {
                throw new InvalidOperationException("You're already in that room!");
            }

            // Only support joining one room at a time for now (until we support tabs)
            ChatRoom oldRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(oldRoomName, StringComparison.OrdinalIgnoreCase));
            if (oldRoom != null)
            {
                HandleLeave(oldRoom, user);
            }

            // Create the room if it doesn't exist
            ChatRoom newRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(newRoomName, StringComparison.OrdinalIgnoreCase));
            if (newRoom == null)
            {
                newRoom = AddRoom(newRoomName);
            }

            JoinRoom(user, newRoom);
        }

        private void JoinRoom(ChatUser user, ChatRoom newRoom)
        {
            var userViewModel = new UserViewModel(user);

            // Add this room to the user's list of rooms
            user.Rooms.Add(newRoom);

            // Add this user to the list of room's users
            newRoom.Users.Add(user);

            // Tell the people in this room that you're joining
            Clients[newRoom.Name].addUser(userViewModel).Wait();

            // Set the room on the caller
            Caller.room = newRoom.Name;

            // Add the caller to the group so they receive messages
            AddToGroup(newRoom.Name).Wait();

            Caller.joinRoom(newRoom.Name);
        }

        private void HandleGravatar(ChatUser user, string[] parts)
        {
            string email = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email was not specified!");
            }

            string hash = CreateGravatarHash(email);

            SetGravatar(user, hash);
        }

        private void SetGravatar(ChatUser user, string hash)
        {
            // Set user hash
            user.Hash = hash;

            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    Clients[room.Name].changeGravatar(userViewModel);
                }
            }
            else
            {
                Caller.changeGravatar(userViewModel);
            }
        }

        private string CreateGravatarHash(string email)
        {
            return email.ToLowerInvariant().ToMD5();
        }

        private void HandleRooms()
        {
            var rooms = _db.Rooms.Select(r => new
            {
                Name = r.Name,
                Count = r.Users.Count
            });

            Caller.showRooms(rooms);
        }

        private void HandleNick(string name, string[] parts)
        {
            string newUserName = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrWhiteSpace(newUserName))
            {
                throw new InvalidOperationException("No username specified!");
            }

            ChatUser user = _db.Users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                AddUser(newUserName);
            }
            else
            {
                ChangeUserName(user, newUserName);
            }
        }

        private void HandleNudge(ChatUser user, string[] parts)
        {
            if (_db.Users.Count == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            var toUserName = NormalizeUserName(parts[1]);
            ChatUser toUser = _db.Users.FirstOrDefault(u => u.Name.Equals(toUserName, StringComparison.OrdinalIgnoreCase));

            if (toUser == null)
            {
                throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", toUserName));
            }

            if (toUser == user)
            {
                throw new InvalidOperationException("You can't nudge yourself!");
            }

            string messageText = String.Format("{0} nudged you", user);

            var betweenNudges = TimeSpan.FromSeconds(60);
            if (toUser.LastNudged.HasValue && toUser.LastNudged > DateTime.Now.Subtract(betweenNudges))
            {
                throw new InvalidOperationException(String.Format("User can only be nudged once every {0} seconds", betweenNudges.TotalSeconds));
            }

            toUser.LastNudged = DateTime.Now;
            // Send a nudge message to the sender and the sendee                        
            Clients[toUser.ClientId].nudge(user.Name, toUser.Name);
            Caller.sendPrivateMessage(user.Name, toUser.Name, "nudged " + toUser.Name);
        }

        private void HandleNudge(ChatRoom room, ChatUser user, string[] parts)
        {
            var betweenNudges = TimeSpan.FromMinutes(1);
            if (room.LastNudged == null || room.LastNudged < DateTime.Now.Subtract(betweenNudges))
            {
                room.LastNudged = DateTime.Now;
                Clients[room.Name].nudge(user.Name);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Room can only be nudged once every {0} seconds", betweenNudges.TotalSeconds));
            }
        }

        private void ChangeUserName(ChatUser user, string newUserName)
        {
            if (user.Name.Equals(newUserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("That's already your username...");
            }

            EnsureUserNameIsAvailable(newUserName);

            string oldUserName = user.Name;
            user.Name = newUserName;
            Caller.name = newUserName;

            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    Clients[room.Name].changeUserName(userViewModel, oldUserName, newUserName);
                }
            }
            else
            {
                Caller.changeUserName(userViewModel, oldUserName, newUserName);
            }
        }

        private static void Sweep(ChatRepository db)
        {
            if (_sweeping)
            {
                return;
            }

            _sweeping = true;

            var clients = GetClients<Chat>();

            var inactiveUsers = new List<ChatUser>();

            foreach (var user in db.Users)
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

            _sweeping = false;
        }

        private ChatUser AddUser(string name)
        {

            EnsureUserNameIsAvailable(name);

            var user = new ChatUser
            {
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

            return user;
        }

        private ChatRoom AddRoom(string name)
        {
            var chatRoom = new ChatRoom { Name = name };

            _db.Rooms.Add(chatRoom);

            return chatRoom;
        }

        private void LeaveAllRooms(ChatUser user)
        {
            // Leave all rooms
            foreach (var room in user.Rooms.ToList())
            {
                HandleLeave(room, user);
            }
        }

        private Tuple<ChatUser, ChatRoom> EnsureUserAndRoom()
        {
            ChatUser user = EnsureUser();

            string room = Caller.room;
            string name = Caller.name;

            if (String.IsNullOrEmpty(room))
            {
                throw new InvalidOperationException("Use '/join room' to join a room.");
            }

            ChatRoom chatRoom = _db.Rooms.FirstOrDefault(r => r.Name.Equals(room, StringComparison.OrdinalIgnoreCase));

            if (chatRoom == null)
            {
                throw new InvalidOperationException(String.Format("You're in '{0}' but it doesn't exist. Use /join '{0}' to create this room.", room));
            }

            if (!chatRoom.Users.Any(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
            }

            return Tuple.Create(user, chatRoom);
        }

        private ChatUser EnsureUser()
        {
            string name = Caller.name;
            
            if (String.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("You don't have a name. Pick a name using '/nick nickname'.");
            }

            ChatUser user = _db.Users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new InvalidOperationException(String.Format("You go by the name '{0}' but the server has no idea who you are. Maybe it got reset :(.", name));
            }

            if (user.ClientId != Context.ClientId)
            {
                throw new InvalidOperationException("Nice try...");
            }

            // Keep the client id up to date
            if (String.IsNullOrEmpty(user.ClientId))
            {
                user.ClientId = Context.ClientId;
            }

            user.Active = true;
            user.LastActivity = DateTime.UtcNow;

            return user;
        }

        private void EnsureUserNameIsAvailable(string userName)
        {
            var userExists = _db.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (userExists)
            {
                throw new InvalidOperationException(String.Format("Username {0} already taken, please pick a new one using '/nick nickname'.", userName));
            }
        }

        private string Transform(string message, out HashSet<string> extractedUrls)
        {
            const string urlPattern = @"((https?|ftp)://|www\.)[\w]+(.[\w]+)([\w\-\.,@?^=%&amp;:/~\+#!]*[\w\-\@?^=%&amp;/~\+#])";

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
    }
}

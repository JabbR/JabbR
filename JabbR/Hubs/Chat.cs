using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using JabbR.ContentProviders;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.ViewModels;
using Microsoft.Security.Application;
using Newtonsoft.Json;
using SignalR.Hubs;

namespace JabbR
{
    public class Chat : Hub, IDisconnect
    {
        private readonly IJabbrRepository _repository;

        public Chat(IJabbrRepository repository)
        {
            _repository = repository;
        }

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
            // Set the version on the client
            Caller.version = typeof(Chat).Assembly.GetName().Version.ToString();

            // Get the client state
            ClientState clientState = GetClientState();

            // Try to get the user from the client state
            ChatUser user = _repository.GetUserById(clientState.UserId);

            // Threre's no user being tracked
            if (user == null)
            {
                // We've failed to get the user from the client's state
                return false;
            }

            // Update some user values
            user.ClientId = Context.ClientId;
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;

            // Perform the update
            _repository.Update();

            // Update the client state
            Caller.room = null;
            Caller.id = user.Id;
            Caller.name = user.Name;

            // Tell the client to re-join these rooms
            foreach (var room in user.Rooms)
            {
                if (room.Name.Equals(clientState.ActiveRoom, StringComparison.OrdinalIgnoreCase))
                {
                    // Update the active room on the client
                    Caller.activeRoom = clientState.ActiveRoom;
                }

                HandleRejoin(room, user);
            }

            return true;
        }

        private ClientState GetClientState()
        {
            // New client state
            var jabbrState = GetCookieValue("jabbr.state");

            if (!String.IsNullOrEmpty(jabbrState))
            {
                return JsonConvert.DeserializeObject<ClientState>(jabbrState);
            }

            // Legacy client state (TODO: Remove after a few releases)
            var roomsString = GetCookieValue("userroom");

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

            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom room = tuple.Item2;

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

            room.Messages.Add(chatMessage);
            room.LastActivity = DateTime.UtcNow;
            _repository.Update();

            var messageViewModel = new MessageViewModel(chatMessage, room);

            Clients[room.Name].addMessage(messageViewModel);

            if (!links.Any())
            {
                return;
            }

            ProcessUrls(links, room, chatMessage);
        }

        public void Disconnect()
        {
            Disconnect(Context.ClientId);
        }

        public IEnumerable<RoomViewModel> GetRooms()
        {
            var rooms = _repository.Rooms.Select(r => new RoomViewModel
            {
                Name = r.Name,
                Count = r.Users.Count
            });

            return rooms;
        }

        public IEnumerable<UserViewModel> GetUsers(string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                return Enumerable.Empty<UserViewModel>();
            }

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room == null)
            {
                return Enumerable.Empty<UserViewModel>();
            }

            return room.Users.Select(u => new UserViewModel(u, room));
        }

        public IEnumerable<MessageViewModel> GetRecentMessages(string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                return Enumerable.Empty<MessageViewModel>();
            }

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room == null)
            {
                return Enumerable.Empty<MessageViewModel>();
            }

            return (from m in room.Messages
                    orderby m.When descending
                    select new MessageViewModel(m, room)).Take(20).Reverse();
        }

        public ChatRoom[] GetUserRooms(ChatUser user)
        {
            return user.Rooms.ToArray();
        }

        public void Typing(bool isTyping)
        {
            if (OutOfSync)
            {
                return;
            }

            string id = Caller.id;
            if (String.IsNullOrEmpty(id))
            {
                return;
            }

            ChatUser user = _repository.Users.FirstOrDefault(u => u.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return;
            }

            ChatRoom room = EnsureRoom(user);

            var userViewModel = new UserViewModel(user, room);

            if (isTyping)
            {
                UpdateActivity(user, room);
            }

            if (room != null)
            {
                Clients[room.Name].setTyping(userViewModel, isTyping);
            }
            else
            {
                Caller.setTyping(userViewModel, isTyping);
            }
        }

        private void Disconnect(string clientId)
        {
            ChatUser user = _repository.Users.FirstOrDefault(u => u.ClientId == clientId);

            if (user == null)
            {
                return;
            }

            LeaveAllRooms(user);

            user.Status = (int)UserStatus.Offline;
            _repository.Update();
        }
        
        private void UpdateActivity()
        {
            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom room = tuple.Item2;

            UpdateActivity(user, room);
        }

        private void UpdateActivity(ChatUser user, ChatRoom room)
        {
            Clients[room.Name].updateActivity(new UserViewModel(user, room));
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
                    _repository.Update();

                    Clients[room.Name].addMessageContent(chatMessage.Id, extractedContent, room.Name);
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

            string[] parts = command.Substring(1).Split(' ');
            string commandName = parts[0];

            if (!TryHandleBaseCommand(commandName, parts) &&
                !TryHandleUserCommand(commandName, parts) &&
                !TryHandleRoomCommand(commandName, parts))
            {
                // If none of the commands are valid then throw an exception
                throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", commandName));
            }

            return true;
        }

        // Commands that require a user and room
        private bool TryHandleRoomCommand(string commandName, string[] parts)
        {
            Tuple<ChatUser, ChatRoom> tuple = EnsureUserAndRoom();

            ChatUser user = tuple.Item1;
            ChatRoom room = tuple.Item2;

            if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                HandleMe(room, user, parts);
                return true;
            }
            else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase))
            {
                HandleLeave(room, user);

                return true;
            }
            else if (commandName.Equals("nudge", StringComparison.OrdinalIgnoreCase))
            {
                HandleNudge(room, user, parts);

                return true;
            }
            else if (TryHandleOwnerCommand(user, room, commandName, parts))
            {
                return true;
            }

            return false;
        }

        // Commands that require the user to be the owner of the room
        private bool TryHandleOwnerCommand(ChatUser user, ChatRoom room, string commandName, string[] parts)
        {
            if (commandName.Equals("kick", StringComparison.OrdinalIgnoreCase))
            {
                HandleKick(user, room, parts);

                return true;
            }

            return false;
        }

        private bool TryHandleBaseCommand(string commandName, string[] parts)
        {
            string userName = Caller.name;

            if (commandName.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                HandleHelp();

                return true;
            }
            else if (commandName.Equals("nick", StringComparison.OrdinalIgnoreCase))
            {
                HandleNick(parts);

                return true;
            }

            return false;
        }

        // Commands that require a user name
        private bool TryHandleUserCommand(string commandName, string[] parts)
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
            else if (commandName.Equals("who", StringComparison.OrdinalIgnoreCase))
            {
                HandleWho(parts);
                return true;
            }
            else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase))
            {
                HandleJoin(user, parts);

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

            return false;
        }

        private void HandleKick(ChatUser user, ChatRoom room, string[] parts)
        {
            if (room.Owner != user)
            {
                throw new InvalidOperationException("You are not the owner of " + room.Name);
            }

            if (parts.Length == 1)
            {
                throw new InvalidOperationException("Who are you trying to kick?");
            }

            if (room.Users.Count == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            string targetUserName = parts[1];
            var targetUser = _repository.GetUserByName(targetUserName);

            if (targetUser == null)
            {
                throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", targetUserName));
            }

            if (targetUser == user)
            {
                throw new InvalidOperationException("Why would you want to kick yourself?");
            }

            // Kick the user
            HandleLeave(room, targetUser);

            // Tell the user that they were kicked
            Clients[targetUser.ClientId].kick(room.Name);
        }

        private void HandleWho(string[] parts)
        {
            if (parts.Length == 1)
            {
                Caller.listUsers(_repository.Users.Select(s => s.Name));
                return;
            }

            var name = NormalizeUserName(parts[1]);

            ChatUser user = _repository.GetUserByName(name);

            if (user != null)
            {
                Caller.showUsersRoomList(user.Name, user.Rooms.Select(r => r.Name));
                return;
            }

            var users = _repository.Users.Where(s => s.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);

            if (users.Count() == 1)
            {
                user = users.First();
                Caller.showUsersRoomList(user.Name, user.Rooms.Select(r => r.Name));
            }
            else
            {
                Caller.listUsers(users.Select(s => s.Name));
            }
        }

        private void HandleList(string[] parts)
        {
            if (parts.Length < 2)
            {
                throw new InvalidOperationException("List users in which room?");
            }

            string roomName = parts[1];
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _repository.Rooms.FirstOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

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
                new { Name = "who", Description = "Type /who to show a list of all users, /who [name] to the rooms that user is in" },
                new { Name = "list", Description = "Type /list (room) to show a list of users in the room" },
                new { Name = "gravatar", Description = "Type \"/gravatar email\" to set your gravatar." },
                new { Name = "nudge", Description = "Type \"/nudge\" to send a nudge to the whole room, or \"/nudge @nickname\" to nudge a particular user. @ is optional." }
            });
        }

        private void HandleLeave(ChatUser user, string[] parts)
        {
            string roomName = parts[1];
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _repository.Rooms.FirstOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

            if (room == null)
            {
                throw new InvalidOperationException("No room with that name!");
            }

            HandleLeave(room, user);
        }

        private void HandleLeave(ChatRoom room, ChatUser user, bool leaveRoom = true)
        {
            if (leaveRoom)
            {
                // Remove the user from the room
                room.Users.Remove(user);
                user.Rooms.Remove(room);
            }

            room.LastActivity = DateTime.UtcNow;

            // Update the store
            _repository.Update();

            var userViewModel = new UserViewModel(user, room);
            Clients[room.Name].leave(userViewModel).Wait();

            GroupManager.RemoveFromGroup(user.ClientId, room.Name).Wait();
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
            if (_repository.Users.Count() == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            if (parts.Length < 2 || String.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException("Who are you trying send a private message to?");
            }
            var toUserName = NormalizeUserName(parts[1]);
            ChatUser toUser = _repository.Users.FirstOrDefault(u => u.Name.Equals(toUserName, StringComparison.OrdinalIgnoreCase));

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
            return room.Users.Any(r => r.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsUserInRoom(string roomName, ChatUser user)
        {
            var room = _repository.Rooms.FirstOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

            if (room == null)
            {
                return false;
            }

            return IsUserInRoom(room, user);
        }

        private void HandleRejoin(ChatRoom room, ChatUser user)
        {
            JoinRoom(user, room);
        }

        private void HandleJoin(ChatUser user, string[] parts)
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

            // Create the room if it doesn't exist
            ChatRoom newRoom = _repository.Rooms.FirstOrDefault(r => r.Name.Equals(newRoomName, StringComparison.OrdinalIgnoreCase));
            if (newRoom == null)
            {
                newRoom = AddRoom(user, newRoomName);
            }

            JoinRoom(user, newRoom, isActive: true);
        }

        private void JoinRoom(ChatUser user, ChatRoom newRoom, bool isActive = false)
        {
            var userViewModel = new UserViewModel(user, newRoom);

            // Add this room to the user's list of rooms
            user.Rooms.Add(newRoom);

            // Add this user to the list of room's users
            newRoom.Users.Add(user);
            newRoom.LastActivity = DateTime.UtcNow;

            _repository.Update();

            // Set the room on the caller
            Caller.room = newRoom.Name;
            Caller.joinRoom(newRoom.Name, isActive);

            // Tell the people in this room that you've joined
            userViewModel.Room = newRoom.Name;
            Clients[newRoom.Name].addUser(userViewModel).Wait();

            // Add the caller to the group so they receive messages
            AddToGroup(newRoom.Name).Wait();
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
            _repository.Update();

            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    userViewModel = new UserViewModel(user, room);
                    Clients[room.Name].changeGravatar(userViewModel);
                }
            }
            else
            {
                Caller.changeGravatar(userViewModel);
            }

            Caller.gravatarChanged(userViewModel);
        }

        private string CreateGravatarHash(string email)
        {
            return email.ToLowerInvariant().ToMD5();
        }

        private void HandleRooms()
        {
            var rooms = _repository.Rooms.Select(r => new
            {
                Name = r.Name,
                Count = r.Users.Count
            });

            Caller.showRooms(rooms);
        }

        private void HandleNick(string[] parts)
        {
            string newUserName = String.Join(" ", parts.Skip(1));

            if (String.IsNullOrWhiteSpace(newUserName))
            {
                throw new InvalidOperationException("No nick specified!");
            }

            // See if there is a current user
            string id = Caller.id;
            ChatUser user = _repository.GetUserById(id);

            if (user == null)
            {
                // If there's no user add a new one
                AddUser(newUserName);
            }
            else
            {
                // Change the user's name
                ChangeUserName(user, newUserName);
            }
        }

        private void HandleNudge(ChatUser user, string[] parts)
        {
            if (_repository.Users.Count() == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            var toUserName = NormalizeUserName(parts[1]);
            ChatUser toUser = _repository.Users.FirstOrDefault(u => u.Name.Equals(toUserName, StringComparison.OrdinalIgnoreCase));

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
            _repository.Update();
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
                _repository.Update();
                Clients[room.Name].nudge(user.Name);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Room can only be nudged once every {0} seconds", betweenNudges.TotalSeconds));
            }
        }

        private void ChangeUserName(ChatUser user, string newUserName)
        {
            if (!IsValidUserName(newUserName))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", newUserName));
            }

            if (user.Name.Equals(newUserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("That's already your username...");
            }

            EnsureUserNameIsAvailable(newUserName);

            string oldUserName = user.Name;

            // Update the user name
            user.Name = newUserName;
            _repository.Update();

            // Update the client side state
            Caller.name = newUserName;

            var userViewModel = new UserViewModel(user);

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    userViewModel = new UserViewModel(user, room);
                    Clients[room.Name].changeUserName(userViewModel, oldUserName, newUserName);
                }
            }
            else
            {
                Caller.changeUserName(userViewModel, oldUserName, newUserName);
            }

            Caller.userNameChanged(newUserName);
        }

        private void AddUser(string name)
        {
            if (!IsValidUserName(name))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", name));
            }

            EnsureUserNameIsAvailable(name);
            
            var user = new ChatUser
            {
                Name = name,
                Status = (int)UserStatus.Active,
                Id = Guid.NewGuid().ToString("d"),
                LastActivity = DateTime.UtcNow,
                ClientId = Context.ClientId
            };

            _repository.Add(user);

            Caller.name = user.Name;
            Caller.id = user.Id;

            var userViewModel = new UserViewModel(user);
            Caller.addUser(userViewModel);
        }

        private ChatRoom AddRoom(ChatUser owner, string name)
        {
            if (name.Equals("Lobby", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Lobby is not a valid chat room.");
            }

            if (!IsValidRoomName(name))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid room name.", name));
            }

            var room = new ChatRoom
            {
                Name = name,
                Owner = owner
            };

            _repository.Add(room);

            return room;
        }

        private bool IsValidUserName(string name)
        {
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]+$");
        }

        private bool IsValidRoomName(string name)
        {
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]+$");
        }

        private void LeaveAllRooms(ChatUser user)
        {
            // Leave all rooms
            foreach (var room in user.Rooms.ToList())
            {
                HandleLeave(room, user, leaveRoom: false);
            }
        }

        private Tuple<ChatUser, ChatRoom> EnsureUserAndRoom()
        {
            ChatUser user = EnsureUser();
            ChatRoom room = EnsureRoom(user);

            return Tuple.Create(user, room);
        }

        private ChatRoom EnsureRoom(ChatUser user)
        {
            string roomName = Caller.room;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new InvalidOperationException("Use '/join room' to join a room.");
            }

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room == null)
            {
                throw new InvalidOperationException(String.Format("You're in '{0}' but it doesn't exist. Use /join '{0}' to create this room.", roomName));
            }

            if (!room.Users.Any(u => u.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", roomName));
            }

            return room;
        }

        private ChatUser EnsureUser()
        {
            string id = Caller.id;

            ChatUser user = _repository.GetUserById(id);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid user id");
            }

            // Update the user's activity
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;
            _repository.Update();

            return user;
        }

        private void EnsureUserNameIsAvailable(string userName)
        {
            var userExists = _repository.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));

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

        private class ClientState
        {
            public string UserId { get; set; }
            public string ActiveRoom { get; set; }
        }
    }
}

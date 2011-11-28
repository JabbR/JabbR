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
                return false;
            }

            // Update some user values
            user.ClientId = Context.ClientId;
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;

            // Perform the update
            _repository.Update();

            // Update the client state
            Caller.id = user.Id;
            Caller.name = user.Name;

            // Update the active room on the client (only if it's still a valid room)
            if (user.Rooms.Any(room => room.Name.Equals(clientState.ActiveRoom, StringComparison.OrdinalIgnoreCase)))
            {
                // Update the active room on the client (only if it's still a valid room)
                Caller.activeRoom = clientState.ActiveRoom;
            }

            RejoinRooms(user, user.Rooms);

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
            UpdateActivity(user, room);

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
            _repository.Update();

            var messageViewModel = new MessageViewModel(chatMessage);

            Clients[room.Name].addMessage(messageViewModel, room.Name);

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
            if (String.IsNullOrEmpty(id))
            {
                return;
            }

            ChatUser user = _repository.GetUserById(id);

            if (user == null)
            {
                return;
            }

            ChatRoom room = EnsureActiveRoom(user);

            var userViewModel = new UserViewModel(user);

            if (isTyping)
            {
                UpdateActivity(user, room);
            }

            Clients[room.Name].setTyping(userViewModel, room.Name, isTyping);
        }

        private void Disconnect(string clientId)
        {
            ChatUser user = _repository.GetUserByClientId(clientId);

            if (user == null)
            {
                return;
            }

            LeaveAllRooms(user);

            user.Status = (int)UserStatus.Offline;
            _repository.Update();
        }

        private void UpdateActivity(ChatUser user, ChatRoom room)
        {
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;
            _repository.Update();

            Clients[room.Name].updateActivity(new UserViewModel(user), room.Name);
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
            ChatUser user = EnsureActiveUser();
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
            else if (commandName.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                HandleCreate(user, parts);

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
            else if (commandName.EndsWith("addowner", StringComparison.OrdinalIgnoreCase))
            {
                HandleAddOwner(user, parts);

                return true;
            }

            return false;
        }

        private void HandleAddOwner(ChatUser user, string[] parts)
        {
            if (parts.Length == 1)
            {
                throw new InvalidOperationException("Who do you want to make an owner?");
            }

            string targetUserName = parts[1];

            ChatUser targetUser = EnsureUser(targetUserName);

            if (parts.Length == 2)
            {
                throw new InvalidOperationException("Which room?");
            }

            string roomName = parts[2];
            ChatRoom targetRoom = EnsureRoom(roomName);

            // Ensure the user is owner of the target room
            EnsureOwner(user, targetRoom);

            if (targetUser == user || targetRoom.Owners.Contains(targetUser))
            {
                // If the target user is already an owner, then throw
                throw new InvalidOperationException(String.Format("'{0}' is already and owner of '{1}'.", targetUser.Name, targetRoom.Name));
            }

            // Make the user an owner
            targetRoom.Owners.Add(targetUser);
            targetUser.OwnedRooms.Add(targetRoom);
            _repository.Update();

            // Tell this client it's an owner
            Clients[targetUser.ClientId].makeOwner(targetRoom.Name);

            // If the target user is in the target room.
            // Tell everyone in the target room that a new owner was added
            if (IsUserInRoom(targetRoom, targetUser))
            {
                Clients[targetRoom.Name].addOwner(new UserViewModel(targetUser), targetRoom.Name);
            }

            // Tell the calling client the granting of ownership was successful
            Caller.ownerMade(targetUser.Name, targetRoom.Name);
        }

        private void HandleKick(ChatUser user, ChatRoom room, string[] parts)
        {
            EnsureOwner(user, room);

            if (parts.Length == 1)
            {
                throw new InvalidOperationException("Who are you trying to kick?");
            }

            if (room.Users.Count == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            ChatUser targetUser = EnsureUser(parts[1]);

            if (targetUser == user)
            {
                throw new InvalidOperationException("Why would you want to kick yourself?");
            }

            // If this user isnt' the creator and the target user is an owner then throw
            if (room.Creator != user && room.Owners.Contains(targetUser))
            {
                throw new InvalidOperationException("Owners cannot kick other owners. Only the room creator and kick an owner.");
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
                Caller.listUsers(_repository.Users.Online().Select(s => s.Name));
                return;
            }

            var name = NormalizeUserName(parts[1]);

            ChatUser user = _repository.GetUserByName(name);

            if (user != null)
            {
                Caller.showUsersRoomList(user.Name, user.Rooms.Select(r => r.Name));
                return;
            }

            var users = _repository.SearchUsers(name);

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
            ChatRoom room = EnsureRoom(roomName);

            var names = room.Users.Online().Select(s => s.Name);

            Caller.showUsersInRoom(room.Name, names);
        }

        private void HandleHelp()
        {
            Caller.showCommands(new[] { 
                new { Name = "help", Description = "Shows the list of commands" },
                new { Name = "nick", Description = "/nick changes your nickname" },
                new { Name = "join", Description = "Type /join [room] -- to join a channel of your choice" },
                new { Name = "create", Description = "Type /create [room] to create a room" },
                new { Name = "me", Description = "Type /me 'does anything'" },
                new { Name = "msg", Description = "Type /msg @nickname (message) to send a private message to nickname. @ is optional." },
                new { Name = "leave", Description = "Type /leave to leave the current room. Type /leave [room name] to leave a specific room." },
                new { Name = "rooms", Description = "Type /rooms to show the list of rooms" },
                new { Name = "who", Description = "Type /who to show a list of all users, /who [name] to the rooms that user is in" },
                new { Name = "list", Description = "Type /list (room) to show a list of users in the room" },
                new { Name = "gravatar", Description = "Type /gravatar [email] to set your gravatar." },
                new { Name = "nudge", Description = "Type /nudge to send a nudge to the whole room, or \"/nudge @nickname\" to nudge a particular user. @ is optional." },
                new { Name = "kick", Description = "Type /kick [user] to kick a user from the room. Note, this is only valid for owners of the room." }
            });
        }

        private void HandleLeave(ChatUser user, string[] parts)
        {
            string roomName = parts[1];
            ChatRoom room = EnsureRoom(roomName);

            HandleLeave(room, user);
        }

        private void HandleLeave(ChatRoom room, ChatUser user, bool leaveRoom = true)
        {
            if (leaveRoom)
            {
                // Remove the user from the room
                room.Users.Remove(user);
                user.Rooms.Remove(room);

                // Update the store
                _repository.Update();
            }

            var userViewModel = new UserViewModel(user);
            Clients[room.Name].leave(userViewModel, room.Name).Wait();

            GroupManager.RemoveFromGroup(user.ClientId, room.Name).Wait();

            UpdateRoomCount(room);
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
            ChatUser toUser = EnsureUser(parts[1]);

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

        private void HandleCreate(ChatUser user, string[] parts)
        {
            if (parts.Length == 1)
            {
                throw new InvalidOperationException("No room specified.");
            }

            string roomName = parts[1];
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new InvalidOperationException("No room specified.");
            }

            ChatRoom room = _repository.GetRoomByName(roomName);

            if (room != null)
            {
                throw new InvalidOperationException(String.Format("The room '{0}' already exists", roomName));
            }

            // Create the room, then join it
            room = AddRoom(user, roomName);

            JoinRoom(user, room);
        }

        private void HandleJoin(ChatUser user, string[] parts)
        {
            if (parts.Length < 2)
            {
                throw new InvalidOperationException("Join which room?");
            }

            // Create the room if it doesn't exist
            string roomName = parts[1];
            ChatRoom room = EnsureRoom(roomName);

            if (IsUserInRoom(room, user))
            {
                throw new InvalidOperationException("You're already in that room!");
            }

            JoinRoom(user, room);
        }

        private void RejoinRooms(ChatUser user, IEnumerable<ChatRoom> rooms)
        {
            var userViewModel = new UserViewModel(user);
            var roomNames = new List<string>();

            foreach (var room in rooms)
            {
                var isOwner = user.OwnedRooms.Contains(room);

                // Tell the people in this room that you've joined
                Clients[room.Name].addUser(userViewModel, room.Name, isOwner).Wait();

                // Update the room count
                UpdateRoomCount(room);

                // Update activity
                UpdateActivity(user, room);

                // Add the caller to the group so they receive messages
                AddToGroup(room.Name).Wait();

                // Add to the list of room names
                roomNames.Add(room.Name);
            }

            // Initialize the chat with the rooms the user is in
            Caller.initialize(roomNames);
        }

        private void JoinRoom(ChatUser user, ChatRoom room)
        {
            var userViewModel = new UserViewModel(user);
            var isOwner = user.OwnedRooms.Contains(room);

            // Add this room to the user's list of rooms
            user.Rooms.Add(room);

            // Add this user to the list of room's users
            room.Users.Add(user);

            _repository.Update();

            // Set the room on the caller
            Caller.activeRoom = room.Name;
            Caller.joinRoom(room.Name);

            // Tell the people in this room that you've joined
            Clients[room.Name].addUser(userViewModel, room.Name, isOwner).Wait();

            UpdateRoomCount(room);

            // Add the caller to the group so they receive messages
            AddToGroup(room.Name).Wait();
        }

        private void UpdateRoomCount(ChatRoom room)
        {
            // Update the room count
            Clients.updateRoomCount(room.Name, room.Users.Online().Count());
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

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Update the calling client
            Caller.gravatarChanged();

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    Clients[room.Name].changeGravatar(userViewModel, room.Name);
                }
            }
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

            ChatUser toUser = EnsureUser(parts[1]);

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

            // Create the view model
            var userViewModel = new UserViewModel(user);

            // Update the client side state
            Caller.name = newUserName;
            Caller.userNameChanged(userViewModel);

            if (user.Rooms.Any())
            {
                foreach (var room in user.Rooms)
                {
                    Clients[room.Name].changeUserName(oldUserName, userViewModel, room.Name);
                }
            }
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

            Caller.userCreated();
        }

        private ChatRoom AddRoom(ChatUser user, string name)
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
                Creator = user
            };

            room.Owners.Add(user);

            _repository.Add(room);

            user.OwnedRooms.Add(room);

            return room;
        }

        private bool IsValidUserName(string name)
        {
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
        }

        private bool IsValidRoomName(string name)
        {
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
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
            ChatUser user = EnsureActiveUser();
            ChatRoom room = EnsureActiveRoom(user);

            return Tuple.Create(user, room);
        }

        private static void EnsureOwner(ChatUser user, ChatRoom room)
        {
            if (!room.Owners.Contains(user))
            {
                throw new InvalidOperationException("You are not an owner of " + room.Name);
            }
        }

        private ChatRoom EnsureActiveRoom(ChatUser user)
        {
            string roomName = Caller.activeRoom;

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

        private ChatUser EnsureActiveUser()
        {
            string id = Caller.id;

            ChatUser user = _repository.GetUserById(id);

            if (user == null)
            {
                throw new InvalidOperationException("You don't have a name. Pick a name using '/nick nickname'.");
            }

            // Update the user's activity
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;
            _repository.Update();

            return user;
        }

        private ChatRoom EnsureRoom(string roomName)
        {
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new InvalidOperationException("Room name cannot be blank!");
            }

            var room = _repository.GetRoomByName(roomName);

            if (room == null)
            {
                throw new InvalidOperationException(String.Format("Unable to find room '{0}'.", roomName));
            }
            return room;
        }

        private ChatUser EnsureUser(string userName)
        {
            userName = NormalizeUserName(userName);
            ChatUser user = _repository.GetUserByName(userName);

            if (user == null)
            {
                throw new InvalidOperationException(String.Format("Unable to find user '{0}'.", userName));
            }

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

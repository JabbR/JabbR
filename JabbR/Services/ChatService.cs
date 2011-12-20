using System;
using System.Linq;
using System.Text.RegularExpressions;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Services
{
    public class ChatService : IChatService
    {
        private readonly IJabbrRepository _repository;
        private readonly ICryptoService _crypto;

        public ChatService(IJabbrRepository repository, ICryptoService crypto)
        {
            _repository = repository;
            _crypto = crypto;
        }

        public ChatUser AddUser(string userName, string clientId, string password)
        {
            if (!IsValidUserName(userName))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", userName));
            }

            if (String.IsNullOrEmpty(password))
            {
                ThrowPasswordIsRequired();
            }

            EnsureUserNameIsAvailable(userName);

            var user = new ChatUser
            {
                Name = userName,
                Status = (int)UserStatus.Active,
                Id = Guid.NewGuid().ToString("d"),
                Salt = _crypto.CreateSalt(),
                LastActivity = DateTime.UtcNow
            };

            ValidatePassword(password);
            user.HashedPassword = password.ToSha256(user.Salt);

            _repository.Add(user);

            AddClient(user, clientId);

            return user;
        }

        public void AuthenticateUser(string userName, string password)
        {
            ChatUser user = _repository.VerifyUser(userName);

            if (user.HashedPassword == null)
            {
                throw new InvalidOperationException(String.Format("The nick '{0}' is unclaimable", userName));
            }

            if (user.HashedPassword != password.ToSha256(user.Salt))
            {
                throw new InvalidOperationException(String.Format("Unable to claim '{0}'.", userName));
            }

            EnsureSaltedPassword(user, password);
        }

        public void ChangeUserName(ChatUser user, string newUserName)
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

            // Update the user name
            user.Name = newUserName;
        }

        public void SetUserPassword(ChatUser user, string password)
        {
            ValidatePassword(password);
            user.HashedPassword = password.ToSha256(user.Salt);
        }

        public void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword)
        {
            if (user.HashedPassword != oldPassword.ToSha256(user.Salt))
            {
                throw new InvalidOperationException("Passwords don't match.");
            }

            ValidatePassword(newPassword);

            EnsureSaltedPassword(user, newPassword);
        }

        public ChatRoom AddRoom(ChatUser user, string name)
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

        public void JoinRoom(ChatUser user, ChatRoom room, string inviteCode)
        {
            // Throw if the room is private but the user isn't allowed
            if (room.Private)
            {
                // First, check if the invite code is correct
                if (!String.IsNullOrEmpty(inviteCode) && String.Equals(inviteCode, room.InviteCode, StringComparison.OrdinalIgnoreCase))
                {
                    // It is, add the user to the allowed users so that future joins will work
                    room.AllowedUsers.Add(user);
                }
                if (!IsUserAllowed(room, user))
                {
                    throw new InvalidOperationException(String.Format("Unable to join {0}. This room is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command", room.Name));
                }
            }

            // Add this room to the user's list of rooms
            user.Rooms.Add(room);

            // Add this user to the list of room's users
            room.Users.Add(user);
        }

        public void SetInviteCode(ChatUser user, ChatRoom room, string inviteCode)
        {
            EnsureOwner(user, room);
            if (!room.Private)
            {
                throw new InvalidOperationException("Only private rooms can have invite codes");
            }

            // Set the invite code and save
            room.InviteCode = inviteCode;
            _repository.CommitChanges();
        }
        
        public void UpdateActivity(ChatUser user)
        {
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;

            // Remove any Afk notes.
            if (user.IsAfk)
            {
                user.Note = null;
            }
        }

        public void LeaveRoom(ChatUser user, ChatRoom room)
        {
            // Remove the user from the room
            room.Users.Remove(user);

            // Remove this room from the users' list
            user.Rooms.Remove(room);
        }

        public ChatMessage AddMessage(ChatUser user, ChatRoom room, string content)
        {
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString("d"),
                User = user,
                Content = content,
                When = DateTimeOffset.UtcNow,
                Room = room
            };

            _repository.Add(chatMessage);

            return chatMessage;
        }

        public void AddOwner(ChatUser ownerOrCreator, ChatUser targetUser, ChatRoom targetRoom)
        {
            // Ensure the user is owner of the target room
            EnsureOwner(ownerOrCreator, targetRoom);

            if (targetRoom.Owners.Contains(targetUser))
            {
                // If the target user is already an owner, then throw
                throw new InvalidOperationException(String.Format("'{0}' is already and owner of '{1}'.", targetUser.Name, targetRoom.Name));
            }

            // Make the user an owner
            targetRoom.Owners.Add(targetUser);
            targetUser.OwnedRooms.Add(targetRoom);

            if (targetRoom.Private)
            {
                if (!targetRoom.AllowedUsers.Contains(targetUser))
                {
                    // If the room is private make this user allowed
                    targetRoom.AllowedUsers.Add(targetUser);
                    targetUser.AllowedRooms.Add(targetRoom);
                }
            }
        }

        public void RemoveOwner(ChatUser creator, ChatUser targetUser, ChatRoom targetRoom)
        {
            // Ensure the user is creator of the target room
            EnsureCreator(creator, targetRoom);

            if (!targetRoom.Owners.Contains(targetUser))
            {
                // If the target user is not an owner, then throw
                throw new InvalidOperationException(String.Format("'{0}' is not an owner of '{1}'.", targetUser.Name, targetRoom.Name));
            }

            // Remove user as owner of room
            targetRoom.Owners.Remove(targetUser);
            targetUser.OwnedRooms.Remove(targetRoom);
        }

        public void KickUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwner(user, targetRoom);

            if (targetUser == user)
            {
                throw new InvalidOperationException("Why would you want to kick yourself?");
            }

            if (!IsUserInRoom(targetRoom, targetUser))
            {
                throw new InvalidOperationException(String.Format("'{0}' isn't in '{1}'.", targetUser.Name, targetRoom.Name));
            }

            // If this user isnt' the creator and the target user is an owner then throw
            if (targetRoom.Creator != user && targetRoom.Owners.Contains(targetUser))
            {
                throw new InvalidOperationException("Owners cannot kick other owners. Only the room creator and kick an owner.");
            }

            LeaveRoom(targetUser, targetRoom);
        }

        public void AddClient(ChatUser user, string clientId)
        {
            var client = new ChatClient
            {
                Id = clientId,
                User = user
            };

            _repository.Add(client);
            _repository.CommitChanges();
        }

        public ChatUser DisconnectClient(string clientId)
        {
            // Remove this client from the list of user's clients
            ChatClient client = _repository.GetClientById(clientId);

            // No client tracking this user
            if (client == null)
            {
                return null;
            }

            // Get the user for this client
            ChatUser user = client.User;

            if (user != null)
            {
                user.ConnectedClients.Remove(client);

                if (!user.ConnectedClients.Any())
                {
                    // If no more clients mark the user as offline
                    user.Status = (int)UserStatus.Offline;
                }

                _repository.Remove(client);
                _repository.CommitChanges();
            }

            return user;
        }

        private void EnsureUserNameIsAvailable(string userName)
        {
            var userExists = _repository.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (userExists)
            {
                ThrowUserExists(userName);
            }
        }

        internal static string NormalizeUserName(string userName)
        {
            return userName.StartsWith("@") ? userName.Substring(1) : userName;
        }

        internal static string NormalizeRoomName(string roomName)
        {
            return roomName.StartsWith("#") ? roomName.Substring(1) : roomName;
        }

        internal static void ThrowUserExists(string userName)
        {
            throw new InvalidOperationException(String.Format("Username {0} already taken, please pick a new one using '/nick nickname'.", userName));
        }

        internal static void ThrowPasswordIsRequired()
        {
            throw new InvalidOperationException("A password is required.");
        }

        internal static bool IsUserInRoom(ChatRoom room, ChatUser user)
        {
            return room.Users.Any(r => r.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsUserAllowed(ChatRoom room, ChatUser user)
        {
            return room.AllowedUsers.Contains(user);
        }

        private static void ValidatePassword(string password)
        {
            if (String.IsNullOrEmpty(password) || password.Length < 6)
            {
                throw new InvalidOperationException("Your password must be at least 6 characters.");
            }
        }

        private static bool IsValidUserName(string name)
        {
            return !String.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
        }

        private static bool IsValidRoomName(string name)
        {
            return !String.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
        }

        private static void EnsureOwner(ChatUser user, ChatRoom room)
        {
            if (!room.Owners.Contains(user))
            {
                throw new InvalidOperationException("You are not an owner of " + room.Name);
            }
        }

        private static void EnsureCreator(ChatUser user, ChatRoom room)
        {
            if (user != room.Creator)
            {
                throw new InvalidOperationException("You are not the creator of " + room.Name);
            }
        }

        private void EnsureSaltedPassword(ChatUser user, string password)
        {
            if (String.IsNullOrEmpty(user.Salt))
            {
                user.Salt = _crypto.CreateSalt();
            }
            user.HashedPassword = password.ToSha256(user.Salt);
        }

        public void AllowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwner(user, targetRoom);

            if (!targetRoom.Private)
            {
                throw new InvalidOperationException(String.Format("{0} is not a private room.", targetRoom.Name));
            }

            if (targetUser.AllowedRooms.Contains(targetRoom))
            {
                throw new InvalidOperationException(String.Format("{0} is already allowed for {1}.", targetUser.Name, targetRoom.Name));
            }

            targetRoom.AllowedUsers.Add(targetUser);
            targetUser.AllowedRooms.Add(targetRoom);

            _repository.CommitChanges();
        }

        public void UnallowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwner(user, targetRoom);

            if (targetUser == user)
            {
                throw new InvalidOperationException("Why would you want to unallow yourself?");
            }

            if (!targetRoom.Private)
            {
                throw new InvalidOperationException(String.Format("{0} is not a private room.", targetRoom.Name));
            }

            if (!targetUser.AllowedRooms.Contains(targetRoom))
            {
                throw new InvalidOperationException(String.Format("{0} isn't allowed to access {1}.", targetUser.Name, targetRoom.Name));
            }

            // If this user isn't the creator and the target user is an owner then throw
            if (targetRoom.Creator != user && targetRoom.Owners.Contains(targetUser))
            {
                throw new InvalidOperationException("Owners cannot unallow other owners. Only the room creator and unallow an owner.");
            }

            targetRoom.AllowedUsers.Remove(targetUser);
            targetUser.AllowedRooms.Remove(targetRoom);

            // Make the user leave the room
            LeaveRoom(targetUser, targetRoom);

            _repository.CommitChanges();
        }

        public void LockRoom(ChatUser user, ChatRoom targetRoom)
        {
            EnsureOwner(user, targetRoom);

            if (targetRoom.Private)
            {
                throw new InvalidOperationException(String.Format("{0} is already locked.", targetRoom.Name));
            }

            // Make the room private
            targetRoom.Private = true;

            // Add the creator to the allowed list
            targetRoom.AllowedUsers.Add(user);

            // Add the room to the users' list
            user.AllowedRooms.Add(targetRoom);

            // Make all users in the current room allowed
            foreach (var u in targetRoom.Users.Online())
            {
                u.AllowedRooms.Add(targetRoom);
                targetRoom.AllowedUsers.Add(u);
            }

            _repository.CommitChanges();
        }
    }
}
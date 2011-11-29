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

        public ChatService(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public ChatUser AddUser(string userName, string clientId, string password)
        {
            if (!IsValidUserName(userName))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", userName));
            }

            EnsureUserNameIsAvailable(userName);

            if (!String.IsNullOrEmpty(password))
            {
                ValidatePassword(password);
            }

            var user = new ChatUser
            {
                Name = userName,
                Status = (int)UserStatus.Active,
                Id = Guid.NewGuid().ToString("d"),
                LastActivity = DateTime.UtcNow,
                ClientId = clientId,
                HashedPassword = password.ToSha256()
            };

            _repository.Add(user);

            return user;
        }

        public ChatUser AuthenticateUser(string userName, string password)
        {
            ChatUser user = _repository.VerifyUser(userName);

            if (user.HashedPassword == null)
            {
                throw new InvalidOperationException(String.Format("The nick '{0}' is unclaimable", userName));
            }

            if (user.HashedPassword != password.ToSha256())
            {
                throw new InvalidOperationException(String.Format("Unable to authorize '{0}'.", userName));
            }

            return user;
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
            _repository.Update();
        }

        public void SetUserPassword(ChatUser user, string password)
        {
            ValidatePassword(password);
            user.HashedPassword = password.ToSha256();

            _repository.Update();
        }

        public void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword)
        {
            if (user.HashedPassword != oldPassword.ToSha256())
            {
                throw new InvalidOperationException("Passwords don't match.");
            }

            ValidatePassword(newPassword);
            user.HashedPassword = newPassword.ToSha256();

            _repository.Update();
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

        public void JoinRoom(ChatUser user, ChatRoom room)
        {
            // Add this room to the user's list of rooms
            user.Rooms.Add(room);

            // Add this user to the list of room's users
            room.Users.Add(user);

            // Update the repo
            _repository.Update();
        }

        public void UpdateActivity(ChatUser user)
        {
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;

            // Perform the update
            _repository.Update();
        }

        public void LeaveRoom(ChatUser user, ChatRoom room)
        {
            // Remove the user from the room
            room.Users.Remove(user);
            user.Rooms.Remove(room);

            // Update the store
            _repository.Update();
        }

        public ChatMessage AddMessage(ChatUser user, ChatRoom room, string content)
        {
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString("d"),
                User = user,
                Content = content,
                When = DateTimeOffset.UtcNow
            };

            room.Messages.Add(chatMessage);
            _repository.Update();

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

            _repository.Update();
        }

        private void EnsureUserNameIsAvailable(string userName)
        {
            var userExists = _repository.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (userExists)
            {
                throw new InvalidOperationException(String.Format("Username {0} already taken, please pick a new one using '/nick nickname'.", userName));
            }
        }

        public static bool IsUserInRoom(ChatRoom room, ChatUser user)
        {
            return room.Users.Any(r => r.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase));
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
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
        }

        private static bool IsValidRoomName(string name)
        {
            return Regex.IsMatch(name, "^[A-Za-z0-9-_.]{1,30}$");
        }

        public static void EnsureOwner(ChatUser user, ChatRoom room)
        {
            if (!room.Owners.Contains(user))
            {
                throw new InvalidOperationException("You are not an owner of " + room.Name);
            }
        }
    }
}
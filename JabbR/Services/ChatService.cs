using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JabbR.Models;
using JabbR.UploadHandlers;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace JabbR.Services
{
    public class ChatService : IChatService
    {
        private readonly IJabbrRepository _repository;
        private readonly ICache _cache;
        private readonly IRecentMessageCache _recentMessageCache;
        private readonly ApplicationSettings _settings;

        private const int NoteMaximumLength = 140;
        private const int TopicMaximumLength = 80;
        private const int WelcomeMaximumLength = 200;

        public ChatService(ICache cache, IRecentMessageCache recentMessageCache, IJabbrRepository repository)
            : this(cache,
                   recentMessageCache,
                   repository,  
                   ApplicationSettings.GetDefaultSettings())
        {
        }

        public ChatService(ICache cache,
                           IRecentMessageCache recentMessageCache,
                           IJabbrRepository repository,
                           ApplicationSettings settings)
        {
            _cache = cache;
            _recentMessageCache = recentMessageCache;
            _repository = repository;
            _settings = settings;
        }

        public ChatRoom AddRoom(ChatUser user, string name)
        {
            if (!_settings.AllowRoomCreation && !user.IsAdmin)
            {
                throw new HubException(LanguageResources.RoomCreationDisabled);
            }

            if (name.Equals("Lobby", StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException(LanguageResources.RoomCannotBeNamedLobby);
            }

            if (!IsValidRoomName(name))
            {
                throw new HubException(String.Format(LanguageResources.RoomInvalidName, name));
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
                if (!room.IsUserAllowed(user))
                {
                    throw new HubException(String.Format(LanguageResources.Join_LockedAccessPermission, room.Name));
                }
            }

            // Add this user to the room
            _repository.AddUserRoom(user, room);

            ChatUserPreferences userPreferences = user.Preferences;
            userPreferences.TabOrder.Add(room.Name);
            user.Preferences = userPreferences;

            // Clear the cache
            _cache.RemoveUserInRoom(user, room);
        }

        public void SetInviteCode(ChatUser user, ChatRoom room, string inviteCode)
        {
            EnsureOwnerOrAdmin(user, room);
            if (!room.Private)
            {
                throw new HubException(LanguageResources.InviteCode_PrivateRoomRequired);
            }

            // Set the invite code and save
            room.InviteCode = inviteCode;
            _repository.CommitChanges();
        }

        public void UpdateActivity(ChatUser user, string clientId, string userAgent)
        {
            user.Status = (int)UserStatus.Active;
            user.LastActivity = DateTime.UtcNow;

            ChatClient client = AddClient(user, clientId, userAgent);
            client.UserAgent = userAgent;
            client.LastActivity = DateTimeOffset.UtcNow;
            client.LastClientActivity = DateTimeOffset.UtcNow;

            // Remove any Afk notes.
            if (user.IsAfk)
            {
                user.AfkNote = null;
                user.IsAfk = false;
            }
        }

        public void LeaveRoom(ChatUser user, ChatRoom room)
        {
            // Update the cache
            _cache.RemoveUserInRoom(user, room);

            // Remove the user from this room
            _repository.RemoveUserRoom(user, room);

            ChatUserPreferences userPreferences = user.Preferences;
            userPreferences.TabOrder.Remove(room.Name);
            user.Preferences = userPreferences;

            _repository.CommitChanges();
        }

        public void AddAttachment(ChatMessage message, string fileName, string contentType, long size, UploadResult result)
        {
            var attachment = new Attachment
            {
                Id = result.Identifier,
                Url = result.Url,
                FileName = fileName,
                ContentType = contentType,
                Size = size,
                Room = message.Room,
                Owner = message.User,
                When = DateTimeOffset.UtcNow
            };

            _repository.Add(attachment);
        }

        public ChatMessage AddMessage(ChatUser user, ChatRoom room, string id, string content)
        {
            var chatMessage = new ChatMessage
            {
                Id = id,
                User = user,
                Content = content,
                When = DateTimeOffset.UtcNow,
                Room = room,
                HtmlEncoded = false
            };

            _recentMessageCache.Add(chatMessage);

            _repository.Add(chatMessage);

            return chatMessage;
        }

        public ChatMessage AddMessage(string userId, string roomName, string content)
        {
            ChatUser user = _repository.VerifyUserId(userId);
            ChatRoom room = _repository.VerifyUserRoom(_cache, user, roomName);

            // REVIEW: Is it better to use room.EnsureOpen() here?
            if (room.Closed)
            {
                throw new HubException(String.Format(LanguageResources.SendMessageRoomClosed, roomName));
            }

            var message = AddMessage(user, room, Guid.NewGuid().ToString("d"), content);

            _repository.CommitChanges();

            return message;
        }

        public void AddNotification(ChatUser mentionedUser, ChatMessage message, ChatRoom room, bool markAsRead)
        {
            // We need to use the key here since messages might be a new entity
            var notification = new Notification
            {
                User = mentionedUser,
                Message = message,
                Read = markAsRead,
                Room = room
            };

            _repository.Add(notification);
        }

        public void AppendMessage(string id, string content)
        {
            ChatMessage message = _repository.GetMessageById(id);

            message.Content += content;

            _repository.CommitChanges();
        }

        public void AddOwner(ChatUser ownerOrCreator, ChatUser targetUser, ChatRoom targetRoom)
        {
            // Ensure the user is owner of the target room
            EnsureOwnerOrAdmin(ownerOrCreator, targetRoom);

            if (targetRoom.Owners.Contains(targetUser))
            {
                // If the target user is already an owner, then throw
                throw new HubException(String.Format(LanguageResources.RoomUserAlreadyOwner, targetUser.Name, targetRoom.Name));
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
            // must be admin OR creator
            EnsureCreatorOrAdmin(creator, targetRoom);

            // ensure acting user is owner
            EnsureOwnerOrAdmin(creator, targetRoom);

            if (!targetRoom.Owners.Contains(targetUser))
            {
                // If the target user is not an owner, then throw
                throw new HubException(String.Format(LanguageResources.UserNotRoomOwner, targetUser.Name, targetRoom.Name));
            }

            // Remove user as owner of room
            targetRoom.Owners.Remove(targetUser);
            targetUser.OwnedRooms.Remove(targetRoom);
        }

        public void KickUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (targetUser == user)
            {
                throw new HubException(LanguageResources.Kick_CannotKickSelf);
            }

            if (!_repository.IsUserInRoom(_cache, targetUser, targetRoom))
            {
                throw new HubException(String.Format(LanguageResources.UserNotInRoom, targetUser.Name, targetRoom.Name));
            }

            // only admin can kick admin
            if (!user.IsAdmin && targetUser.IsAdmin)
            {
                throw new HubException(LanguageResources.Kick_AdminRequiredToKickAdmin);
            }

            // If this user isn't the creator/admin AND the target user is an owner then throw
            if (targetRoom.Creator != user && targetRoom.Owners.Contains(targetUser) && !user.IsAdmin)
            {
                throw new HubException(LanguageResources.Kick_CreatorRequiredToKickOwner);
            }

            LeaveRoom(targetUser, targetRoom);
        }

        public ChatClient AddClient(ChatUser user, string clientId, string userAgent)
        {
            ChatClient client = _repository.GetClientById(clientId);
            if (client != null)
            {
                return client;
            }

            client = new ChatClient
            {
                Id = clientId,
                User = user,
                UserAgent = userAgent,
                LastActivity = DateTimeOffset.UtcNow,
                LastClientActivity = user.LastActivity
            };

            _repository.Add(client);
            _repository.CommitChanges();

            return client;
        }

        public string DisconnectClient(string clientId)
        {
            // Remove this client from the list of user's clients
            ChatClient client = _repository.GetClientById(clientId, includeUser: true);

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

            return user.Id;
        }

        internal static string NormalizeRoomName(string roomName)
        {
            return roomName.StartsWith("#") ? roomName.Substring(1) : roomName;
        }

        private static bool IsValidRoomName(string name)
        {
            return !String.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[\\w-_]{1,30}$");
        }

        private static void EnsureAdmin(ChatUser user)
        {
            if (!user.IsAdmin)
            {
                throw new HubException(LanguageResources.AdminRequired);
            }
        }

        private static void EnsureOwnerOrAdmin(ChatUser user, ChatRoom room)
        {
            if (!room.Owners.Contains(user) && !user.IsAdmin)
            {
                throw new HubException(String.Format(LanguageResources.RoomOwnerRequired, room.Name));
            }
        }

        private static void EnsureOwner(ChatUser user, ChatRoom room)
        {
            if (!room.Owners.Contains(user))
            {
                throw new HubException(String.Format(LanguageResources.RoomOwnerRequired, room.Name));
            }
        }

        private static void EnsureCreator(ChatUser user, ChatRoom room)
        {
            if (user != room.Creator)
            {
                throw new HubException(String.Format(LanguageResources.RoomCreatorRequired, room.Name));
            }
        }

        private static void EnsureCreatorOrAdmin(ChatUser user, ChatRoom room)
        {
            if (user != room.Creator && !user.IsAdmin)
            {
                throw new HubException(String.Format(LanguageResources.RoomCreatorRequired, room.Name));
            }
        }

        public void AllowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (!targetRoom.Private)
            {
                throw new HubException(String.Format(LanguageResources.RoomNotPrivate, targetRoom.Name));
            }

            if (targetUser.AllowedRooms.Contains(targetRoom))
            {
                throw new HubException(String.Format(LanguageResources.RoomUserAlreadyAllowed, targetUser.Name, targetRoom.Name));
            }

            targetRoom.AllowedUsers.Add(targetUser);
            targetUser.AllowedRooms.Add(targetRoom);

            _repository.CommitChanges();
        }

        public void UnallowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (targetUser == user)
            {
                throw new HubException(LanguageResources.UnAllow_CannotUnallowSelf);
            }

            if (!targetRoom.Private)
            {
                throw new HubException(String.Format(LanguageResources.RoomNotPrivate, targetRoom.Name));
            }

            if (!targetUser.AllowedRooms.Contains(targetRoom))
            {
                throw new HubException(String.Format(LanguageResources.RoomAccessPermissionUser, targetUser.Name, targetRoom.Name));
            }

            // only admin can unallow admin
            if (!user.IsAdmin && targetUser.IsAdmin)
            {
                throw new HubException(LanguageResources.UnAllow_AdminRequired);
            }

            // If this user isn't the creator and the target user is an owner then throw
            if (targetRoom.Creator != user && targetRoom.Owners.Contains(targetUser) && !user.IsAdmin)
            {
                throw new HubException(LanguageResources.UnAllow_CreatorRequiredToUnallowOwner);
            }

            targetRoom.AllowedUsers.Remove(targetUser);
            targetUser.AllowedRooms.Remove(targetRoom);

            // Make the user leave the room
            LeaveRoom(targetUser, targetRoom);

            _repository.CommitChanges();
        }

        public void LockRoom(ChatUser user, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (targetRoom.Private)
            {
                throw new HubException(String.Format(LanguageResources.RoomAlreadyLocked, targetRoom.Name));
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

        public void CloseRoom(ChatUser user, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (targetRoom.Closed)
            {
                throw new HubException(String.Format(LanguageResources.RoomAlreadyClosed, targetRoom.Name));
            }

            // Make the room closed.
            targetRoom.Closed = true;

            _repository.CommitChanges();
        }

        public void OpenRoom(ChatUser user, ChatRoom targetRoom)
        {
            EnsureOwnerOrAdmin(user, targetRoom);

            if (!targetRoom.Closed)
            {
                throw new HubException(String.Format(LanguageResources.RoomAlreadyOpen, targetRoom.Name));
            }

            // Open the room
            targetRoom.Closed = false;
            _repository.CommitChanges();
        }

        public void ChangeTopic(ChatUser user, ChatRoom room, string newTopic)
        {
            EnsureOwnerOrAdmin(user, room);
            room.Topic = newTopic;
            _repository.CommitChanges();
        }

        public void ChangeWelcome(ChatUser user, ChatRoom room, string newWelcome)
        {
            EnsureOwnerOrAdmin(user, room);
            room.Welcome = newWelcome;
            _repository.CommitChanges();
        }

        public void AddAdmin(ChatUser admin, ChatUser targetUser)
        {
            // Ensure the user is admin
            EnsureAdmin(admin);

            if (targetUser.IsAdmin)
            {
                // If the target user is already an admin, then throw
                throw new HubException(String.Format(LanguageResources.UserAlreadyAdmin, targetUser.Name));
            }

            // Make the user an admin
            targetUser.IsAdmin = true;
            _repository.CommitChanges();
        }

        public void RemoveAdmin(ChatUser admin, ChatUser targetUser)
        {
            // Ensure the user is admin
            EnsureAdmin(admin);

            if (!targetUser.IsAdmin)
            {
                // If the target user is NOT an admin, then throw
                throw new HubException(String.Format(LanguageResources.UserNotAdmin, targetUser.Name));
            }

            // Make the user an admin
            targetUser.IsAdmin = false;
            _repository.CommitChanges();
        }

        public void BanUser(ChatUser admin, ChatUser targetUser)
        {
            EnsureAdmin(admin);

            if (targetUser == admin)
            {
                throw new HubException(LanguageResources.Ban_CannotBanSelf);
            }

            if (targetUser.IsAdmin)
            {
                throw new HubException(LanguageResources.Ban_CannotBanAdmin);
            }

            targetUser.IsBanned = true;

            _repository.CommitChanges();
        }

        public void UnbanUser(ChatUser admin, ChatUser targetUser)
        {
            // Ensure the user is admin
            EnsureAdmin(admin);

            if (targetUser.IsAdmin)
            {
                // If the target user is an admin, then throw
                throw new HubException(LanguageResources.Unban_CannotUnbanAdmin);
            }

            //Unban the user
            targetUser.IsBanned = false;

            _repository.CommitChanges();
        }

        internal static void ValidateNote(string note, string noteTypeName = "note", int? maxLength = null)
        {
            var lengthToValidateFor = (maxLength ?? NoteMaximumLength);
            if (!String.IsNullOrWhiteSpace(note) &&
                note.Length > lengthToValidateFor)
            {
                throw new HubException(
                    String.Format(LanguageResources.NoteTooLong,
                        lengthToValidateFor, noteTypeName));
            }
        }

        internal static void ValidateTopic(string topic)
        {
            ValidateNote(topic, noteTypeName: "topic", maxLength: TopicMaximumLength);
        }

        internal static void ValidateWelcome(string message)
        {
            ValidateNote(message, noteTypeName: "welcome", maxLength: WelcomeMaximumLength);
        }

        internal static string GetUserRoomPresence(ChatUser user, ChatRoom room)
        {
            return user.Rooms.Contains(room) ? "present" : "absent";
        }
    }
}

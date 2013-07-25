using System;
using System.Linq;
using System.Collections.Generic;

namespace JabbR.Models
{
    public static class ModelExtensions
    {
        public static IList<string> GetConnections(this ChatUser user)
        {
            return user.ConnectedClients.Select(c => c.Id).ToList();
        }

        public static IList<string> GetRoomNames(this ChatUser user)
        {
            return user.Rooms.Select(r => r.Name).ToList();
        }

        public static void EnsureAllowed(this ChatUser user, ChatRoom room)
        {
            if (room.RoomType != RoomType.Public && !room.IsUserAllowed(user))
            {
                throw new InvalidOperationException(String.Format(LanguageResources.RoomAccessPermission, room.Name));
            }
        }

        public static bool IsUserAllowed(this ChatRoom room, ChatUser user)
        {
            return room.AllowedUsers.Contains(user) || room.Owners.Contains(user) || user.IsAdmin;
        }

        public static void EnsureOpen(this ChatRoom room)
        {
            if (room.Closed)
            {
                throw new InvalidOperationException(String.Format(LanguageResources.RoomClosed, room.Name));
            }
        }

        public static void EnsureUserCanAllow(this ChatRoom room, ChatUser user)
        {
            if ((room.Owners.Contains(user) && room.OwnersCanAllow) || room.UsersCanAllow)
            {
                throw new InvalidOperationException(String.Format(LanguageResources.RoomCannotAllow, room.Name));
            }
        }

        public static string BuildRoomTopic(this ChatRoom room, ChatUser caller)
        {
            if (!string.IsNullOrEmpty(room.Topic) || room.RoomType != RoomType.PrivateMessage)
            {
                return room.Topic;
            }

            return string.Join(", ", room.AllowedUsers.Where(u => u != caller).Select(u => u.Name));
        }
    }
}
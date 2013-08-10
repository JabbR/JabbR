using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;

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
            if (room.Private && !room.IsUserAllowed(user))
            {
                throw new HubException(String.Format(LanguageResources.RoomAccessPermission, room.Name));
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
                throw new HubException(String.Format(LanguageResources.RoomClosed, room.Name));
            }
        }
    }
}
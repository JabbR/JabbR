using System;

namespace JabbR.Models
{
    public static class ModelExtensions
    {

        public static void EnsureAllowed(this ChatUser user, ChatRoom room)
        {
            if (room.Private && !room.IsUserAllowed(user))
            {
                throw new InvalidOperationException(String.Format("You do not have access to {0}.", room.Name));
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
                throw new InvalidOperationException(String.Format("The room '{0}' is closed.", room.Name));
            }
        }
    }
}
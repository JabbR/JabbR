using System;

namespace JabbR.Models
{
    public static class CacheExtensions
    {
        public static bool? IsUserInRoom(this ICache cache, ChatUser user, ChatRoom room)
        {
            string key = CacheKeys.GetUserInRoom(user, room);

            return (bool?)cache.Get(key);
        }

        public static void SetUserInRoom(this ICache cache, ChatUser user, ChatRoom room, bool value)
        {
            string key = CacheKeys.GetUserInRoom(user, room);

            // Cache this forever since people don't leave rooms often
            cache.Set(key, value, TimeSpan.FromDays(365));
        }

        public static void RemoveUserInRoom(this ICache cache, ChatUser user, ChatRoom room)
        {
            cache.Remove(CacheKeys.GetUserInRoom(user, room));
        }

        private static class CacheKeys
        {
            public static string GetUserInRoom(ChatUser user, ChatRoom room)
            {
                return "UserInRoom" + user.Key + "_" + room.Key;
            }
        }
    }
}
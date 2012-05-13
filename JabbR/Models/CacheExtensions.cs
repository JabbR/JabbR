using System;

namespace JabbR.Models
{
    public static class CacheExtensions
    {
        private const string UserInRoomKeyPrefix = "UserInRoom_";

        public static bool? IsUserInRoom(this ICache cache, ChatUser user, ChatRoom room)
        {
            string key = UserInRoomKeyPrefix + user.Key + room.Key;

            return (bool?)cache.Get(key);
        }

        public static void SetUserInRoom(this ICache cache, ChatUser user, ChatRoom room, bool value)
        {
            string key = UserInRoomKeyPrefix + user.Key + "_" + room.Key;

            // Cache this forever since people don't leave rooms often
            cache.Set(key, value, TimeSpan.FromDays(365));
        }
    }
}
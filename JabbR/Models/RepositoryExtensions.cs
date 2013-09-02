using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using JabbR.Services;
using JabbR.Infrastructure;
using Microsoft.AspNet.SignalR;

namespace JabbR.Models
{
    public static class RepositoryExtensions
    {
        public static ChatUser GetLoggedInUser(this IJabbrRepository repository, ClaimsPrincipal principal)
        {
            return repository.GetUserById(principal.GetUserId());
        }

        public static ChatUser GetUser(this IJabbrRepository repository, ClaimsPrincipal principal)
        {
            string identity = principal.GetClaimValue(ClaimTypes.NameIdentifier);
            string providerName = principal.GetIdentityProvider();

            return repository.GetUserByIdentity(providerName, identity);
        }

        public static IQueryable<ChatUser> Online(this IQueryable<ChatUser> source)
        {
            return source.Where(u => u.Status != (int)UserStatus.Offline);
        }

        public static IEnumerable<ChatUser> Online(this IEnumerable<ChatUser> source)
        {
            return source.Where(u => u.Status != (int)UserStatus.Offline);
        }

        public static IEnumerable<ChatRoom> Allowed(this IEnumerable<ChatRoom> rooms, string userId)
        {
            return from r in rooms
                   where !r.Private ||
                         r.Private && r.AllowedUsers.Any(u => u.Id == userId)
                   select r;
        }

        public static ChatRoom VerifyUserRoom(this IJabbrRepository repository, ICache cache, ChatUser user, string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                throw new HubException(LanguageResources.RoomJoinMessage);
            }

            roomName = ChatService.NormalizeRoomName(roomName);

            ChatRoom room = repository.GetRoomByName(roomName);

            if (room == null)
            {
                throw new HubException(String.Format(LanguageResources.RoomMemberButNotExists, roomName));
            }

            if (!repository.IsUserInRoom(cache, user, room))
            {
                throw new HubException(String.Format(LanguageResources.RoomNotMember, roomName));
            }

            return room;
        }

        public static bool IsUserInRoom(this IJabbrRepository repository, ICache cache, ChatUser user, ChatRoom room)
        {
            bool? cached = cache.IsUserInRoom(user, room);

            if (cached == null)
            {
                cached = repository.IsUserInRoom(user, room);
                cache.SetUserInRoom(user, room, cached.Value);
            }

            return cached.Value;
        }

        public static ChatUser VerifyUserId(this IJabbrRepository repository, string userId)
        {
            ChatUser user = repository.GetUserById(userId);

            if (user == null)
            {
                // The user isn't logged in 
                throw new HubException(LanguageResources.Authentication_NotLoggedIn);
            }

            return user;
        }

        public static ChatRoom VerifyRoom(this IJabbrRepository repository, string roomName, bool mustBeOpen = true)
        {
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new HubException(LanguageResources.RoomNameCannotBeBlank);
            }

            roomName = ChatService.NormalizeRoomName(roomName);

            var room = repository.GetRoomByName(roomName);

            if (room == null)
            {
                throw new HubException(String.Format(LanguageResources.RoomNotFound, roomName));
            }

            if (room.Closed && mustBeOpen)
            {
                throw new HubException(String.Format(LanguageResources.RoomClosed, roomName));
            }

            return room;
        }

        public static ChatUser VerifyUser(this IJabbrRepository repository, string userName)
        {
            userName = MembershipService.NormalizeUserName(userName);

            ChatUser user = repository.GetUserByName(userName);

            if (user == null)
            {
                throw new HubException(String.Format(LanguageResources.UserNotFound, userName));
            }

            return user;
        }

        public static int GetUnreadNotificationsCount(this IJabbrRepository repository, ChatUser user)
        {
            return repository.GetNotificationsByUser(user).Unread().Count();
        }

        public static IQueryable<Notification> Unread(this IQueryable<Notification> source)
        {
            return source.Where(n => !n.Read);
        }

        public static IQueryable<Notification> ByRoom(this IQueryable<Notification> source, string roomName)
        {
            return source.Where(n => n.Room.Name == roomName);
        }

        public static IList<string> GetAllowedClientIds(this IJabbrRepository repository, ChatRoom room)
        {
            int[] allowedUserKeys = room.AllowedUsers.Select(u => u.Key).ToArray();
            return repository.Clients.Where(c => allowedUserKeys.Contains(c.UserKey)).Select(c => c.Id).ToList();
        }
    }
}
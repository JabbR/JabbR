using System.Linq;
using System;

namespace JabbR.Models
{
    public interface IJabbrRepository : IDisposable
    {
        IQueryable<ChatRoom> Rooms { get; }
        IQueryable<ChatUser> Users { get; }

        IQueryable<ChatUser> GetOnlineUsers(ChatRoom room);

        IQueryable<ChatUser> SearchUsers(string name);
        IQueryable<ChatMessage> GetMessagesByRoom(ChatRoom room);
        IQueryable<ChatMessage> GetPreviousMessages(string messageId);
        IQueryable<ChatRoom> GetAllowedRooms(ChatUser user);
        ChatMessage GetMessagesById(string id);

        ChatUser GetUserById(string userId);
        ChatRoom GetRoomByName(string roomName);

        ChatUser GetUserByName(string userName);
        ChatUser GetUserByClientId(string clientId);
        ChatUser GetUserByIdentity(string userIdentity);

        ChatClient GetClientById(string clientId, bool includeUser = false);

        void AddUserRoom(ChatUser user, ChatRoom room);
        void RemoveUserRoom(ChatUser user, ChatRoom room);

        void Add(ChatClient client);
        void Add(ChatMessage message);
        void Add(ChatRoom room);
        void Add(ChatUser user);
        void Remove(ChatClient client);
        void Remove(ChatRoom room);
        void Remove(ChatUser user);
        void RemoveAllClients();
        void CommitChanges();

        bool IsUserInRoom(ChatUser user, ChatRoom room);
    }
}
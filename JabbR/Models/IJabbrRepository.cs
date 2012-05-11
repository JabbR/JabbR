using System.Linq;
using System;

namespace JabbR.Models
{
    public interface IJabbrRepository : IDisposable
    {
        IQueryable<ChatRoom> Rooms { get; }
        IQueryable<ChatUser> Users { get; }

        IQueryable<ChatUser> SearchUsers(string name);
        IQueryable<ChatMessage> GetMessagesByRoom(string roomName);
        IQueryable<ChatMessage> GetPreviousMessages(string messageId);
        IQueryable<ChatRoom> GetAllowedRooms(ChatUser user);
        ChatMessage GetMessagesById(string id);

        ChatUser GetUserById(string userId);
        ChatRoom GetRoomByName(string roomName, bool includeUsers = false, bool includeOwners = false);

        ChatUser GetUserByName(string userName);
        ChatUser GetUserByClientId(string clientId);
        ChatUser GetUserByIdentity(string userIdentity);

        ChatClient GetClientById(string clientId, bool includeUser = false);

        void Add(ChatClient client);
        void Add(ChatMessage message);
        void Add(ChatRoom room);
        void Add(ChatUser user);
        void Remove(ChatClient client);
        void Remove(ChatRoom room);
        void Remove(ChatUser user);
        void RemoveAllClients();
        void CommitChanges();
    }
}
using System.Linq;
using System;

namespace JabbR.Models
{
    public interface IJabbrRepository : IDisposable
    {        
        IQueryable<ChatRoom> Rooms { get; }
        IQueryable<ChatUser> Users { get; }
        
        IQueryable<ChatUser> SearchUsers(string name);

        ChatUser GetUserById(string userId);
        ChatRoom GetRoomByName(string roomName);
        ChatUser GetUserByName(string userName);
        ChatUser GetUserByClientId(string clientId);

        ChatClient GetClientById(string clientId);

        void Add(ChatClient client);
        void Add(ChatMessage message);
        void Add(ChatRoom room);
        void Add(ChatUser user);
        void Remove(ChatClient client);
        void Remove(ChatRoom room);
        void Remove(ChatUser user);
        void CommitChanges();
    }
}
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

        void Add(ChatRoom room);
        void Add(ChatUser user);
        void Remove(ChatRoom room);
        void Remove(ChatUser user);
        void Update();
    }
}
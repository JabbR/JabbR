using System.Linq;
using System;

namespace Chat.Models
{
    public interface IJabbrRepository : IDisposable
    {
        IQueryable<ChatRoom> Rooms { get; }
        IQueryable<ChatUser> Users { get; }
        void Add(ChatRoom room);
        void Add(ChatUser user);
        void Remove(ChatRoom room);
        void Remove(ChatUser user);
        void Update();
    }
}
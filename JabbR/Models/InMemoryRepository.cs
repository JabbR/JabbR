using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using Chat.ViewModels;

namespace Chat.Models
{
    public class InMemoryRepository : IJabbrRepository
    {
        readonly HashSet<ChatUser> _users;
        readonly HashSet<ChatRoom> _rooms;

        public InMemoryRepository()
        {
            _users = new HashSet<ChatUser>();
            _rooms = new HashSet<ChatRoom>();
        }

        public IQueryable<ChatRoom> Rooms { get { return _rooms.AsQueryable(); } }

        public IQueryable<ChatUser> Users { get { return _users.AsQueryable(); } }

        public void Add(ChatRoom room)
        {
            _rooms.Add(room);
        }

        public void Add(ChatUser user)
        {
            _users.Add(user);
        }

        public void Remove(ChatRoom room)
        {
            _rooms.Remove(room);
        }
        
        public void Remove(ChatUser user)
        {
            _users.Remove(user);
        }

        public void Update()
        {
            // no-op since this is an in-memory impl' of the repo
        }

        public void Dispose()
        {
        }
    }
}
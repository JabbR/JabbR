using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using JabbR.ViewModels;

namespace JabbR.Models
{
    public class InMemoryRepository : IJabbrRepository
    {
        private readonly HashSet<ChatUser> _users;
        private readonly HashSet<ChatRoom> _rooms;

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

        public ChatUser GetUserById(string userId)
        {
            return _users.FirstOrDefault(u => u.Id.Equals(userId, StringComparison.OrdinalIgnoreCase));
        }

        public ChatUser GetUserByName(string userName)
        {
            return _users.FirstOrDefault(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }

        public ChatRoom GetRoomByName(string roomName)
        {
            return _rooms.FirstOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));
        }

        public IQueryable<ChatUser> SearchUsers(string name)
        {
            return _users.Online()
                         .Where(u => u.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                         .AsQueryable();
        }
    }
}
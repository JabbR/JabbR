using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Infrastructure;

namespace JabbR.Models
{
    public class InMemoryRepository : IJabbrRepository
    {
        private readonly ICollection<ChatUser> _users;
        private readonly ICollection<ChatRoom> _rooms;

        public InMemoryRepository()
        {
            _users = new SafeCollection<ChatUser>();
            _rooms = new SafeCollection<ChatRoom>();
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

        public void Add(ChatMessage message)
        {
            // There's no need to keep a collection of messages outside of a room
            var room = _rooms.First(r => r == message.Room);
            room.Messages.Add(message);
        }

        public void Add(ChatClient client)
        {
            var user = _users.FirstOrDefault(u => client.User == u);
            user.ConnectedClients.Add(client);
        }

        public void Remove(ChatClient client)
        {
            var user = _users.FirstOrDefault(u => client.User == u);
            user.ConnectedClients.Remove(client);
        }

        public void Remove(ChatRoom room)
        {
            _rooms.Remove(room);
        }

        public void Remove(ChatUser user)
        {
            _users.Remove(user);
        }

        public void CommitChanges()
        {
            // no-op since this is an in-memory impl' of the repo
        }

        public void Dispose()
        {
        }

        public ChatUser GetUserById(string userId)
        {
            return _users.FirstOrDefault(u => u.Id != null && u.Id.Equals(userId, StringComparison.OrdinalIgnoreCase));
        }

        public ChatUser GetUserByName(string userName)
        {
            return _users.FirstOrDefault(u => u.Name != null && u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
        }

        public ChatRoom GetRoomByName(string roomName)
        {
            return _rooms.FirstOrDefault(r => r.Name != null && r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));
        }

        public ChatRoom GetRoomByName(string roomName, bool includeUsers = false, bool includeOwners = false)
        {
            return GetRoomByName(roomName);
        }

        public ChatRoom GetRoomAndUsersByName(string roomName)
        {
            return GetRoomByName(roomName);
        }

        public IQueryable<ChatRoom> GetAllowedRooms(ChatUser user)
        {
            return _rooms
                .Where(r =>
                    (!r.Private && !r.Closed) ||
                    (r.Private && !r.Closed && r.AllowedUsers.Contains(user)))
                .AsQueryable();
        }

        public IQueryable<ChatMessage> GetMessagesByRoom(ChatRoom room)
        {
            return room.Messages.AsQueryable();
        }

        public IQueryable<ChatUser> GetOnlineUsers(ChatRoom room)
        {
            return room.Users.Online().AsQueryable();
        }

        public IQueryable<ChatUser> SearchUsers(string name)
        {
            return _users.Online()
                         .Where(u => u.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                         .AsQueryable();
        }

        public ChatUser GetUserByClientId(string clientId)
        {
            return _users.FirstOrDefault(u => u.ConnectedClients.Any(c => c.Id == clientId));
        }

        public ChatUser GetUserByIdentity(string userIdentity)
        {
            return _users.FirstOrDefault(u => u.Identity == userIdentity);
        }

        public ChatClient GetClientById(string clientId, bool includeUser = false)
        {
            return _users.SelectMany(u => u.ConnectedClients).FirstOrDefault(c => c.Id == clientId);
        }

        public IQueryable<ChatMessage> GetPreviousMessages(string messageId)
        {
            // Ineffcient since we don't have a messages collection

            return (from r in _rooms
                    let message = r.Messages.FirstOrDefault(m => m.Id == messageId)
                    where message != null
                    from m in r.Messages
                    where m.When < message.When
                    select m).AsQueryable();
        }

        public void RemoveAllClients()
        {
            // No need to do anything here since this is only called on App_Start
            // if we're using the in memory repository all the data has been purged anyways
        }

        public ChatMessage GetMessagesById(string id)
        {
            return (from r in _rooms
                    let message = r.Messages.FirstOrDefault(m => m.Id == id)
                    where message != null
                    select message).FirstOrDefault();
        }

        public bool IsUserInRoom(ChatUser user, ChatRoom room)
        {
            // REVIEW: Inefficient, bu only users for unit tests right now
            return room.Users.Any(u => u.Name == user.Name);
        }

        public void AddUserRoom(ChatUser user, ChatRoom room)
        {
            user.Rooms.Add(room);

            room.Users.Add(user);
        }

        public void RemoveUserRoom(ChatUser user, ChatRoom room)
        {
            user.Rooms.Remove(room);

            room.Users.Remove(user);
        }
    }
}
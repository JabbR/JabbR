using System.Data.Entity;
using System.Linq;

namespace JabbR.Models
{
    public class PersistedRepository : IJabbrRepository
    {
        private readonly JabbrContext _db;

        public PersistedRepository(JabbrContext db)
        {
            _db = db;
        }

        public IQueryable<ChatRoom> Rooms
        {
            get { return _db.Rooms; }
        }

        public IQueryable<ChatUser> Users
        {
            get { return _db.Users; }
        }

        public void Add(ChatRoom room)
        {
            _db.Rooms.Add(room);
            _db.SaveChanges();
        }

        public void Add(ChatUser user)
        {
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        public void Add(ChatMessage message)
        {
            _db.Messages.Add(message);
        }

        public void Remove(ChatRoom room)
        {
            _db.Rooms.Remove(room);
            _db.SaveChanges();
        }

        public void Remove(ChatUser user)
        {
            _db.Users.Remove(user);
            _db.SaveChanges();
        }

        public void CommitChanges()
        {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public ChatUser GetUserById(string userId)
        {
            return _db.Users.FirstOrDefault(u => u.Id == userId);
        }

        public ChatUser GetUserByName(string userName)
        {
            return _db.Users.FirstOrDefault(u => u.Name == userName);
        }

        public ChatRoom GetRoomByName(string roomName)
        {
            return _db.Rooms.Include(r => r.Owners)
                            .Include(r => r.Users)
                            .FirstOrDefault(r => r.Name == roomName);
        }

        public ChatRoom GetRoomByName(string roomName, bool includeUsers = false, bool includeOwners = false)
        {
            IQueryable<ChatRoom> rooms = _db.Rooms;
            if (includeUsers)
            {
                rooms = rooms.Include(r => r.Users);
            }

            if (includeOwners)
            {
                rooms = rooms.Include(r => r.Owners);
            }


            return rooms.FirstOrDefault(r => r.Name == roomName);
        }

        public ChatRoom GetRoomAndUsersByName(string roomName)
        {
            return _db.Rooms.Include(r => r.Users).FirstOrDefault(r => r.Name == roomName);
        }

        public ChatMessage GetMessagesById(string id)
        {
            return _db.Messages.FirstOrDefault(m => m.Id == id);
        }

        public IQueryable<ChatRoom> GetAllowedRooms(ChatUser user)
        {
            // All *open* public and private rooms the user can see.
            return _db.Rooms
                .Where(r =>
                       (!r.Private && !r.Closed) ||
                       (r.Private && !r.Closed && r.AllowedUsers.Any(u => u.Key == user.Key)));
        }

        public IQueryable<ChatMessage> GetMessagesByRoom(string roomName)
        {
            return _db.Messages.Include(r => r.Room).Where(r => r.Room.Name == roomName);
        }

        public IQueryable<ChatMessage> GetPreviousMessages(string messageId)
        {
            var info = (from m in _db.Messages.Include(m => m.Room)
                        where m.Id == messageId
                        select new
                        {
                            m.When,
                            RoomName = m.Room.Name
                        }).FirstOrDefault();

            return from m in GetMessagesByRoom(info.RoomName)
                   where m.When < info.When
                   select m;
        }

        public IQueryable<ChatUser> SearchUsers(string name)
        {
            return _db.Users.Online().Where(u => u.Name.Contains(name));
        }

        public void Add(ChatClient client)
        {
            _db.Clients.Add(client);
            _db.SaveChanges();
        }

        public void Remove(ChatClient client)
        {
            _db.Clients.Remove(client);
            _db.SaveChanges();
        }

        public ChatUser GetUserByClientId(string clientId)
        {
            var client = GetClientById(clientId, includeUser: true);
            if (client != null)
            {
                return client.User;
            }
            return null;
        }

        public ChatUser GetUserByIdentity(string userIdentity)
        {
            return _db.Users.FirstOrDefault(u => u.Identity == userIdentity);
        }

        public ChatClient GetClientById(string clientId, bool includeUser = false)
        {
            IQueryable<ChatClient> clients = _db.Clients;

            if (includeUser)
            {
                clients = clients.Include(c => c.User);
            }

            return clients.FirstOrDefault(c => c.Id == clientId);
        }

        public void RemoveAllClients()
        {
            foreach (var c in _db.Clients)
            {
                _db.Clients.Remove(c);
            }
        }
    }
}
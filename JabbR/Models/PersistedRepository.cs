using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chat.Models
{
    public class PersistedRepository : IJabbrRepository
    {
        readonly JabbrContext _db;
        
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

        public void Update()
        {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
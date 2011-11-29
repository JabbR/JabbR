using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatUser
    {
        [Key]
        public int Key { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
        // MD5 email hash for gravatar
        public string Hash { get; set; }
        public string HashedPassword { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime? LastNudged { get; set; }
        public string ClientId { get; set; }
        public int Status { get; set; }

        public virtual ICollection<ChatRoom> OwnedRooms { get; set; }
        public virtual ICollection<ChatRoom> Rooms { get; set; }

        public ChatUser()
        {
            OwnedRooms = new HashSet<ChatRoom>();
            Rooms = new HashSet<ChatRoom>();
        }
    }
}
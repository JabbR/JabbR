using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatRoom
    {
        [Key]
        public int Key { get; set; }

        public DateTime? LastNudged { get; set; }
        public string Name { get; set; }
        public bool Closed { get; set; }

        // Private rooms
        public bool Private { get; set; }
        public virtual ICollection<ChatUser> AllowedUsers { get; set; }
        public string InviteCode { get; set; }

        // Creator of the room
        public virtual ChatUser Creator { get; set; }

        // Creator and owners
        public virtual ICollection<ChatUser> Owners { get; set; } 

        public virtual ICollection<ChatMessage> Messages { get; set; }
        public virtual ICollection<ChatUser> Users { get; set; }

        public ChatRoom()
        {
            Owners = new HashSet<ChatUser>();
            Messages = new HashSet<ChatMessage>();
            Users = new HashSet<ChatUser>();
            AllowedUsers = new HashSet<ChatUser>();
        }
    }
}
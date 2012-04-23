using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JabbR.Infrastructure;

namespace JabbR.Models
{
    public class ChatRoom
    {
        [Key]
        public int Key { get; set; }

        public DateTime? LastNudged { get; set; }

        [MaxLength(200)]
        public string Name { get; set; }
        public bool Closed { get; set; }
        [StringLength(80)]
        public string Topic { get; set; }

        // Private rooms
        public bool Private { get; set; }
        public virtual ICollection<ChatUser> AllowedUsers { get; set; }
        public string InviteCode { get; set; }

        // Creator of the room
        public virtual ChatUser Creator { get; set; }
        public int? CreatorKey { get; set; }

        // Creator and owners
        public virtual ICollection<ChatUser> Owners { get; set; } 

        public virtual ICollection<ChatMessage> Messages { get; set; }
        public virtual ICollection<ChatUser> Users { get; set; }

        public ChatRoom()
        {
            Owners = new SafeCollection<ChatUser>();
            Messages = new SafeCollection<ChatMessage>();
            Users = new SafeCollection<ChatUser>();
            AllowedUsers = new SafeCollection<ChatUser>();
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Chat.Models
{
    public class ChatRoom
    {
        [Key]
        public int Key { get; set; }

        public DateTime LastActivity { get; set; }
        public DateTime? LastNudged { get; set; }
        public string Name { get; set; }
        
        public virtual ICollection<ChatMessage> Messages { get; set; }
        public virtual ICollection<ChatUser> Users { get; set; }

        public ChatRoom()
        {
            LastActivity = DateTime.UtcNow;
            Messages = new HashSet<ChatMessage>();
            Users = new HashSet<ChatUser>();
        }
    }
}
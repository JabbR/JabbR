using System;
using System.Collections.Generic;

namespace Chat.Models
{
    public class ChatRoom
    {
        public string Name { get; set; }
        public DateTime? LastNudged { get; set; }
        public DateTime LastActivity { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; }
        public virtual ICollection<ChatUser> Users { get; set; }

        public ChatRoom()
        {
            Messages = new HashSet<ChatMessage>();
            Users = new HashSet<ChatUser>();
            LastActivity = DateTime.UtcNow;
        }
    }
}
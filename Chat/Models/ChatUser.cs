using System.Collections.Generic;
using System;

namespace Chat.Models {
    public class ChatUser {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public bool Active { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime? LastNudged { get; set; }
        public string ClientId { get; set; }

        public virtual ICollection<ChatRoom> Rooms { get; set; }

        public ChatUser() {
            Rooms = new HashSet<ChatRoom>();
        }
    }
}
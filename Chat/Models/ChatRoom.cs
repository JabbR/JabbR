using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chat.Models {
    public class ChatRoom {
        public List<ChatMessage> Messages { get; set; }
        public HashSet<string> Users { get; set; }

        public ChatRoom() {
            Messages = new List<ChatMessage>();
            Users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chat.Models {
    public class ChatMessage {
        public string Id { get; private set; }
        public ChatUser User { get; set; }
        public string Content { get; set; }
        public DateTimeOffset When { get; set; }
        
        public ChatMessage(ChatUser user, string content) {
            User = user;
            Content = content;
            Id = Guid.NewGuid().ToString("d");
            When = DateTimeOffset.UtcNow;
        }
    }
}
using System;

namespace Chat.Models {
    public class ChatMessage {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTimeOffset When { get; set; }

        public virtual ChatUser User { get; set; }
    }
}
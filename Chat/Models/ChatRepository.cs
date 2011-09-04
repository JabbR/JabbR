using System.Collections.Generic;

namespace Chat.Models {
    public class ChatRepository {
        public HashSet<ChatUser> Users { get; set; }
        public HashSet<ChatMessage> Messages { get; set; }
        public HashSet<ChatRoom> Rooms { get; set; }

        public ChatRepository() {
            Users = new HashSet<ChatUser>();
            Messages = new HashSet<ChatMessage>();
            Rooms = new HashSet<ChatRoom>();
        }
    }
}
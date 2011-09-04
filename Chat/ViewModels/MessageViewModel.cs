using System;
using Chat.Models;

namespace Chat.ViewModels {
    public class MessageViewModel {
        public MessageViewModel(ChatMessage message) {
            Id = message.Id;
            Content = message.Content;
            User = new UserViewModel(message.User);
            When = message.When;
        }

        public string Id { get; set; }
        public string Content { get; set; }
        public DateTimeOffset When { get; set; }
        public UserViewModel User { get; set; }
    }
}
using System;
using JabbR.Models;

namespace JabbR.ViewModels
{
    public class MessageViewModel
    {
        public MessageViewModel(ChatMessage message)
        {
            Id = message.Id;
            Content = message.Content;
            HtmlContent = message.HtmlContent;
            User = new UserViewModel(message.User);
            When = message.When;
            HtmlEncoded = message.HtmlEncoded;
        }

        public bool HtmlEncoded { get; set; }
        public string Id { get; set; }
        public string Content { get; set; }
        public string HtmlContent { get; set; }
        public DateTimeOffset When { get; set; }
        public UserViewModel User { get; set; }
    }
}
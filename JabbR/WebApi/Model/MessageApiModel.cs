using System;

namespace JabbR.WebApi.Model
{
    public class MessageApiModel
    {
        public string Content { get; set; }
        public string Username { get; set; }
        public DateTimeOffset When { get; set; }
    }
}
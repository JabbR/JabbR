using System;
using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatClient
    {
        [Key]
        public int Key { get; set; }

        public string Id { get; set; }
        public ChatUser User { get; set; }
        public string UserAgent { get; set; }
        public DateTimeOffset LastActivity { get; set; }

        public int UserKey { get; set; }
    }
}
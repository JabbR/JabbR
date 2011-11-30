using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatClient
    {
        [Key]
        public int Key { get; set; }

        public string Id { get; set; }
        public ChatUser User { get; set; }
    }
}
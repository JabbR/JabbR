using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatUserIdentity
    {
        [Key]
        public int Key { get; set; }

        public int UserKey { get; set; }
        public ChatUser User { get; set; }

        public string Email { get; set; }
        public string Identity { get; set; }
    }
}
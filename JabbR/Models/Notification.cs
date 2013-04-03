using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class Notification
    {
        [Key]
        public int Key { get; set; }

        public int UserKey { get; set; }
        public virtual ChatUser User { get; set; }

        public int MessageKey { get; set; }
        public virtual ChatMessage Message { get; set; }

        public bool Read { get; set; }
    }
}
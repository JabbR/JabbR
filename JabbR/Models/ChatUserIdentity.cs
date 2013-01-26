using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatUserIdentity
    {
        [Key]
        public int Key { get; set; }

        public int UserKey { get; set; }
        public virtual ChatUser User { get; set; }

        public string Email { get; set; }
        public string Identity { get; set; }
        public string ProviderName { get; set; }
    }
}
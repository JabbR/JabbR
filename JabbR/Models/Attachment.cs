using System;
using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class Attachment
    {
        [Key]
        public int Key { get; set; }
        
        public string Url { get; set; }
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }

        public int RoomKey { get; set; }
        public int OwnerKey { get; set; }
        public DateTimeOffset When { get; set; }

        public virtual ChatRoom Room { get; set; }
        public virtual ChatUser Owner { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace JabbR.Models
{
    public class ChatUserPreference
    {
        public const string AudibleNotificationsKey = "hasSound";
        public const string RichContentKey = "blockRichness";
        public const string PopupNotificationsKey = "canToast";

        public int ChatUserId { get; set; }

        public int RoomId { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public virtual ChatUser User { get; set; }

        public virtual ChatRoom Room { get; set; }
    }
}
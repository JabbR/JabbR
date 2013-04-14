using System;
using PagedList;

namespace JabbR.ViewModels
{
    public class NotificationsViewModel
    {
        public bool ShowAll { get; set; }
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public IPagedList<NotificationViewModel> Notifications { get; set; }
    }

    public class NotificationViewModel
    {
        public int NotificationKey { get; set; }
        public string Message { get; set; }
        public string FromUserName { get; set; }
        public string FromUserImage { get; set; }
        public string RoomName { get; set; }
        public bool Read { get; set; }
        public bool HtmlEncoded { get; set; }
        public DateTimeOffset When { get; set; }
    }
}
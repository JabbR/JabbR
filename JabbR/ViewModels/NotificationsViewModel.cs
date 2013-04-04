using System;
using JabbR.Models;
using PagedList;

namespace JabbR.ViewModels
{
    public class NotificationsViewModel
    {
        public bool ShowAll { get; set; }
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public IPagedList<Notification> Notifications { get; set; }
    }
}
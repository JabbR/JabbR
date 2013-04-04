using System;
using JabbR.Models;
using PagedList;

namespace JabbR.ViewModels
{
    public class NotificationsViewModel
    {
        public int TotalCount { get; set; }
        public IPagedList<Notification> Notifications { get; set; }
    }
}
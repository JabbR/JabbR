using System;
using System.Linq;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.Helpers;
using Nancy.ModelBinding;
using PagedList;

namespace JabbR.Nancy
{
    public class NotificationsModule : JabbRModule
    {
        public NotificationsModule(IJabbrRepository repository)
            : base("/notifications")
        {
            Get["/"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return Response.AsRedirect(String.Format("~/account/login?returnUrl={0}", HttpUtility.UrlEncode(Request.Path)));
                }

                var request = this.Bind<NotificationRequestModel>();

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);
                IPagedList<Notification> notifications = GetNotifications(repository, user, all: request.All, page: request.Page, roomName: request.Room);

                var viewModel = new NotificationsViewModel
                {
                    TotalCount = notifications.TotalItemCount,
                    Notifications = notifications,
                };

                return View["index", viewModel];
            };
        }

        private static IPagedList<Notification> GetNotifications(IJabbrRepository repository, ChatUser user, bool all = false, int page = 1, int take = 20,
                                                                 string roomName = null)
        {
            IQueryable<Notification> notificationsQuery = repository.GetNotificationsByUser(user);

            if (!all)
            {
                notificationsQuery = notificationsQuery.Unread();
            }

            if (!String.IsNullOrWhiteSpace(roomName))
            {
                var room = repository.VerifyRoom(roomName);

                if (room != null)
                {
                    notificationsQuery = notificationsQuery.ByRoom(roomName);
                }
            }

            return notificationsQuery.OrderByDescending(n => n.Message.When).ToPagedList(page, take);
        }

        private class NotificationRequestModel
        {
            public NotificationRequestModel()
            {
                All = false;
                Page = 1;
                Room = null;
            }

            public bool All { get; set; }
            public int Page { get; set; }
            public string Room { get; set; }
        }
    }
}
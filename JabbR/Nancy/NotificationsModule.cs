using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Helpers;
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

                bool showAll = (Request.Query.all as bool?) ?? false;
                string roomName = Request.Query.room;
                int currentPage = (Request.Query.page as int?) ?? 1;

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);
                IPagedList<Notification> notifications = GetNotifications(repository, user, all: showAll, page: currentPage, roomName: roomName);

                var viewModel = new
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

            return notificationsQuery.ToPagedList(page, take);
        }
    }
}
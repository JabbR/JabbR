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
        public NotificationsModule(IJabbrRepository repository, IChatService chatService)
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
                int unreadCount = repository.GetNotificationsByUser(user).Count(n => !n.Read);
                IPagedList<NotificationViewModel> notifications = GetNotifications(repository, user, all: request.All, page: request.Page, roomName: request.Room);

                var viewModel = new NotificationsViewModel
                {
                    ShowAll = request.All,
                    UnreadCount = unreadCount,
                    Notifications = notifications,
                };

                return View["index", viewModel];
            };

            Post["/markasread"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                int notificationId = Request.Form.notificationId;

                Notification notification = repository.GetNotificationById(notificationId);

                if (notification == null)
                {
                    return HttpStatusCode.NotFound;
                }

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                if (notification.UserKey != user.Key)
                {
                    return HttpStatusCode.Forbidden;
                }

                notification.Read = true;
                repository.CommitChanges();

                var response = Response.AsJson(new { success = true });

                return response;
            };
        }

        private static IPagedList<NotificationViewModel> GetNotifications(IJabbrRepository repository, ChatUser user, bool all = false, int page = 1, int take = 20,
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

            return notificationsQuery.OrderByDescending(n => n.Message.When)
                                    .Select(n => new NotificationViewModel()
                                    {
                                        NotificationKey = n.Key,
                                        FromUserName = n.User.Name,
                                        FromUserImage = n.Message.User.Hash,
                                        Message = n.Message.Content,
                                        RoomName = n.Message.Room.Name,
                                        Read = n.Read
                                    })
                                    .ToPagedList(page, take);
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
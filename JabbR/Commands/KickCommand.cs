using System;
using System.Linq;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("kick", "")]
    public class KickCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, Models.ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who are you trying to kick?");
            }

            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);

            if (context.Repository.GetOnlineUsers(room).Count() == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.KickUser(callingUser, targetUser, room);

            context.NotificationService.KickUser(targetUser, room);

            context.Repository.CommitChanges();
        }
    }
}
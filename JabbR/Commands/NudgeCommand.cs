using System;
using System.Linq;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("nudge", "")]
    public class NudgeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                NudgeRoom(context, callerContext, callingUser);
            }
            else
            {
                NudgeUser(context, callingUser, args);
            }
        }

        private static void NudgeRoom(CommandContext context, CallerContext callerContext, ChatUser callingUser)
        {
            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);

            var betweenNudges = TimeSpan.FromMinutes(1);
            if (room.LastNudged == null || room.LastNudged < DateTime.Now.Subtract(betweenNudges))
            {
                room.LastNudged = DateTime.Now;
                context.Repository.CommitChanges();

                context.NotificationService.NudgeRoom(room, callingUser);
            }
            else
            {
                throw new InvalidOperationException(String.Format("Room can only be nudged once every {0} seconds", betweenNudges.TotalSeconds));
            }
        }

        private static void NudgeUser(CommandContext context, ChatUser callingUser, string[] args)
        {
            if (context.Repository.Users.Count() == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            var toUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser toUser = context.Repository.VerifyUser(toUserName);

            if (toUser == callingUser)
            {
                throw new InvalidOperationException("You can't nudge yourself!");
            }

            string messageText = String.Format("{0} nudged you", callingUser);

            var betweenNudges = TimeSpan.FromSeconds(60);
            if (toUser.LastNudged.HasValue && toUser.LastNudged > DateTime.Now.Subtract(betweenNudges))
            {
                throw new InvalidOperationException(String.Format("User can only be nudged once every {0} seconds", betweenNudges.TotalSeconds));
            }

            toUser.LastNudged = DateTime.Now;
            context.Repository.CommitChanges();

            context.NotificationService.NugeUser(callingUser, toUser);
        }
    }
}
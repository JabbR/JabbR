using System;
using System.Linq;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("nudge", "Nudge_CommandInfo", "[@user]", "global")]
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

            room.EnsureOpen();

            var betweenNudges = TimeSpan.FromMinutes(1);
            if (room.LastNudged == null || room.LastNudged < DateTime.Now.Subtract(betweenNudges))
            {
                room.LastNudged = DateTime.Now;
                context.Repository.CommitChanges();

                context.NotificationService.NudgeRoom(room, callingUser);
            }
            else
            {
                throw new HubException(String.Format(LanguageResources.NudgeRoom_Throttled, betweenNudges.TotalSeconds));
            }
        }

        private static void NudgeUser(CommandContext context, ChatUser callingUser, string[] args)
        {
            var toUserName = args[0];

            ChatUser toUser = context.Repository.VerifyUser(toUserName);

            if (toUser == callingUser)
            {
                throw new HubException(LanguageResources.NudgeUser_CannotNudgeSelf);
            }

            var betweenNudges = TimeSpan.FromSeconds(60);
            if (toUser.LastNudged.HasValue && toUser.LastNudged > DateTime.Now.Subtract(betweenNudges))
            {
                throw new HubException(String.Format(LanguageResources.NudgeUser_Throttled, betweenNudges.TotalSeconds));
            }

            toUser.LastNudged = DateTime.Now;
            context.Repository.CommitChanges();

            context.NotificationService.NudgeUser(callingUser, toUser);
        }
    }
}
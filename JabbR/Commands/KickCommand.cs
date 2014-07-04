using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    using System.Linq;

    [Command("kick", "Kick_CommandInfo", "user [room] [reason]", "user")]
    public class KickCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Kick_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            string targetRoomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(targetRoomName))
            {
                throw new HubException(LanguageResources.Kick_RoomRequired);
            }

            ChatRoom room = context.Repository.VerifyRoom(targetRoomName);

            context.Service.KickUser(callingUser, targetUser, room);

            // try to extract the reason
            string reason = null;
            if (args.Length > 2)
            {
                reason = String.Join(" ", args.Skip(2)).Trim();
            }

            context.NotificationService.KickUser(targetUser, room, callingUser, reason);

            context.Repository.CommitChanges();
        }
    }
}
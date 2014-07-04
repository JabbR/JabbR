using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("unallow", "Unallow_CommandInfo", "user [room]", "room")]
    public class UnAllowCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.UnAllow_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            string roomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new HubException(LanguageResources.UnAllow_RoomRequired);
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName, mustBeOpen: false);

            context.Service.UnallowUser(callingUser, targetUser, targetRoom);

            context.NotificationService.UnallowUser(targetUser, targetRoom, callingUser);

            context.Repository.CommitChanges();
        }
    }
}
using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("invite", "Invite_CommandInfo", "user [room]", "room")]
    public class InviteCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Invite_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (targetUser == callingUser)
            {
                throw new HubException(LanguageResources.Invite_CannotInviteSelf);
            }

            string roomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new HubException(LanguageResources.Invite_RoomRequired);
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName, mustBeOpen: false);

            context.NotificationService.Invite(callingUser, targetUser, targetRoom);
        }
    }
}
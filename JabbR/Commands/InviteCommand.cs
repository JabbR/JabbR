using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("invite", "Invite_CommandInfo", "user [room]", "room")]
    public class InviteCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException(LanguageResources.Invite_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (targetUser == callingUser)
            {
                throw new InvalidOperationException(LanguageResources.Invite_CannotInviteSelf);
            }

            string roomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new InvalidOperationException(LanguageResources.Invite_RoomRequired);
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName, mustBeOpen: false);

            // if the user isn't in the allowed user list, check that the user can invite people, then add the target user to the allowed list
            if (targetRoom.Private && !targetRoom.IsUserAllowed(targetUser))
            {
                context.Service.AllowUser(callingUser, targetUser, targetRoom);

                context.NotificationService.AllowUser(targetUser, targetRoom);

                context.Repository.CommitChanges();
            }

            context.NotificationService.Invite(callingUser, targetUser, targetRoom);
        }
    }
}
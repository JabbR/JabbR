using System;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("invitecode", "")]
    public class InviteCodeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);

            if (String.IsNullOrEmpty(room.InviteCode))
            {
                context.Service.SetInviteCode(callingUser, room, RandomUtils.NextInviteCode());
            }

            context.NotificationService.PostNotification(room, callingUser, String.Format("Invite Code for this room: {0}", room.InviteCode));
        }
    }
}

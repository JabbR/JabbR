using System;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("invitecode", "Show the current invite code.", "[room]", "room")]
    public class InviteCodeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (String.IsNullOrEmpty(callerContext.RoomName))
            {
                throw new InvalidOperationException("This command cannot be invoked from the Lobby.");
            }

            string targetRoomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(targetRoomName))
            {
                throw new InvalidOperationException("Which room?");
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(targetRoomName, mustBeOpen: false);

            // ensure the user could join the room if they wanted to
            callingUser.EnsureAllowed(targetRoom);

            if (String.IsNullOrEmpty(targetRoom.InviteCode))
            {
                context.Service.SetInviteCode(callingUser, targetRoom, RandomUtils.NextInviteCode());
            }

            ChatRoom callingRoom = context.Repository.GetRoomByName(callerContext.RoomName);
            context.NotificationService.PostNotification(callingRoom, callingUser, String.Format("Invite Code for {0}: {1}", targetRoomName, targetRoom.InviteCode));
        }
    }
}

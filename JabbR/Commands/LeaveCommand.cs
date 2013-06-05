using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("leave", "Leave the current room. Use [room] to leave a specific room.", "[room]", "room")]
    public class LeaveCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string targetRoomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(targetRoomName))
            {
                throw new InvalidOperationException("Which room?");
            }

            ChatRoom room = context.Repository.VerifyRoom(targetRoomName, mustBeOpen: false);

            context.Service.LeaveRoom(callingUser, room);

            context.NotificationService.LeaveRoom(callingUser, room);

            context.Repository.CommitChanges();
        }
    }
}
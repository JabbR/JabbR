using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("lock", "Make a room private. Only works if you're the creator of that room.", "[room]", "room")]
    public class LockCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string roomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new InvalidOperationException("Which room do you want to lock?");
            }

            ChatRoom room = context.Repository.VerifyRoom(roomName);

            context.Service.LockRoom(callingUser, room);

            context.NotificationService.LockRoom(callingUser, room);
        }
    }
}
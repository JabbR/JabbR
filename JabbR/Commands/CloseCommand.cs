using System;
using System.Linq;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("close", "Close a room. Only works if you're an owner of that room.", "[room]", "room")]
    public class CloseCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string roomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new InvalidOperationException("Which room do you want to close?");
            }

            ChatRoom room = context.Repository.VerifyRoom(roomName);

            // Before I close the room, I need to grab a copy of -all- the users in that room.
            // Otherwise, I can't send any notifications to the room users, because they
            // have already been kicked.
            var users = room.Users.ToList();
            context.Service.CloseRoom(callingUser, room);

            context.NotificationService.CloseRoom(users, room);
        }
    }
}
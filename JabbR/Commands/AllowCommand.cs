using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("allow", "Give a user permission to a private room. Only works if you're an owner of that room.", "user [room]", "room")]
    public class AllowCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to allow?");
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            string roomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new InvalidOperationException("Which room?");
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.Service.AllowUser(callingUser, targetUser, targetRoom);

            context.NotificationService.AllowUser(targetUser, targetRoom);

            context.Repository.CommitChanges();
        }
    }
}
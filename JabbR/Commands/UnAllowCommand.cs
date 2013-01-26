using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("unallow", "Revoke a user's permission to a private room. Only works if you're an owner of that room.", "user room", "room")]
    public class UnAllowCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Which user to you want to revoke persmissions from?");
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (args.Length == 1)
            {
                throw new InvalidOperationException("Which room?");
            }

            string roomName = args[1];
            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.Service.UnallowUser(callingUser, targetUser, targetRoom);

            context.NotificationService.UnallowUser(targetUser, targetRoom);

            context.Repository.CommitChanges();
        }
    }
}
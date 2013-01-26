using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("addowner", "Add an owner a user as an owner to the specified room. Only works if you're an owner of that room.", "user room", "room")]
    public class AddOwnerCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to make an owner?");
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (args.Length == 1)
            {
                throw new InvalidOperationException("Which room?");
            }

            string roomName = args[1];
            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.Service.AddOwner(callingUser, targetUser, targetRoom);

            context.NotificationService.AddOwner(targetUser, targetRoom);

            context.Repository.CommitChanges();
        }
    }
}
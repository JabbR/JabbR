using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("invite", "Invite a user to join a room.", "user room", "room")]
    public class InviteCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to invite?");
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (targetUser == callingUser)
            {
                throw new InvalidOperationException("You can't invite yourself!");
            }

            if (args.Length == 1)
            {
                throw new InvalidOperationException("Invite them to which room?");
            }

            string roomName = args[1];
            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.NotificationService.Invite(callingUser, targetUser, targetRoom);
        }
    }
}
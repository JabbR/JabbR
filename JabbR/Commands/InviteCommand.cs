using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("invite", "")]
    public class InviteCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to invite?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (targetUser == callingUser)
            {
                throw new InvalidOperationException("You can't invite yourself!");
            }

            if (args.Length == 1)
            {
                throw new InvalidOperationException("Invite them to which room?");
            }

            string roomName = HttpUtility.HtmlDecode(args[1]);
            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.NotificationService.Invite(callingUser, targetUser, targetRoom);
        }
    }
}
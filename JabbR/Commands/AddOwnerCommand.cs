using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("addowner", "")]
    public class AddOwnerCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to make an owner?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            if (args.Length == 1)
            {
                throw new InvalidOperationException("Which room?");
            }

            string roomName = HttpUtility.HtmlDecode(args[1]);
            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.Service.AddOwner(callingUser, targetUser, targetRoom);

            context.NotificationService.AddOwner(targetUser, targetRoom);

            context.Repository.CommitChanges();
        }
    }
}
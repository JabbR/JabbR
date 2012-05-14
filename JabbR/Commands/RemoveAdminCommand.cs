using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("removeadmin", "")]
    public class RemoveAdminCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 1)
            {
                throw new InvalidOperationException("Which admin do you want to remove?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[1]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.RemoveAdmin(callingUser, targetUser);

            context.NotificationService.RemoveAdmin(targetUser);

            context.Repository.CommitChanges();
        }
    }
}
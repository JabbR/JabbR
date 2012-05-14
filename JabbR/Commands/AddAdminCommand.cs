using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("addadmin", "")]
    public class AddAdminCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who do you want to make an admin?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.AddAdmin(callingUser, targetUser);

            context.NotificationService.AddAdmin(targetUser);

            context.Repository.CommitChanges();
        }
    }
}
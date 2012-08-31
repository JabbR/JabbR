using System;
using System.Linq;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("ban", "Ban a user from JabbR!", "user", "admin")]
    public class BanCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who are you trying to ban?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.BanUser(callingUser, targetUser);
            context.NotificationService.BanUser(targetUser);
            context.Repository.CommitChanges();
        }
    }
}
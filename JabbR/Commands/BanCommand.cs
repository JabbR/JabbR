using System;
using System.Linq;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("ban", "Ban_CommandInfo", "user [reason]", "admin")]
    public class BanCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Ban_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            // try to extract the reason
            string reason = null;
            if (args.Length > 1)
            {
                reason = String.Join(" ", args.Skip(1)).Trim();
            }

            context.Service.BanUser(callingUser, targetUser);
            context.NotificationService.BanUser(targetUser, callingUser, reason);
            context.Repository.CommitChanges();
        }
    }
}
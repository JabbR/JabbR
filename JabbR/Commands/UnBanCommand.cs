using System;
using System.Linq;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("unban", "Unban_CommandInfo", "user", "admin")]
    public class UnbanCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Unban_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.UnbanUser(callingUser, targetUser);
            context.NotificationService.UnbanUser(targetUser);
            context.Repository.CommitChanges();
        }
    }
}
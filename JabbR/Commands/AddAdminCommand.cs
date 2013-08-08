using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("addadmin", "", "", "")]
    public class AddAdminCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.AddAdmin_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            context.Service.AddAdmin(callingUser, targetUser);

            context.NotificationService.AddAdmin(targetUser);

            context.Repository.CommitChanges();
        }
    }
}
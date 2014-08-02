using System;

using JabbR.Models;
using JabbR.Services;

using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("resetpassword", "ResetPassword_CommandInfo", "username [password]", "admin")]
    public class ResetPasswordCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Authentication_NameRequired);
            }

            string targetUserName = MembershipService.NormalizeUserName(args[0]);

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            string password;

            if (args.Length == 1)
            {
                password = context.MembershipService.CreatePassword(8);
            }
            else
            {
                password = args[1];
            }

            try
            {
                context.MembershipService.ResetUserPassword(targetUser, password);
                context.Repository.CommitChanges();
            }
            catch (InvalidOperationException e)
            {
                throw new HubException(e.Message);
            }

            context.NotificationService.ResetUserPassword(targetUser, callingUser, password);
        }
    }
}
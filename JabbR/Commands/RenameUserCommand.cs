using System;

using JabbR.Models;
using JabbR.Services;

using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("renameuser", "RenameUser_CommandInfo", "currentUsername newUsername", "admin")]
    public class RenameUserCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Authentication_CurrentNameRequired);
            }

            if (args.Length == 1)
            {
                throw new HubException(LanguageResources.Authentication_NameRequired);
            }

            string targetCurrentUserName = MembershipService.NormalizeUserName(args[0]);
            string targetChangedUserName = MembershipService.NormalizeUserName(args[1]);

            ChatUser targetUser = context.Repository.VerifyUser(targetCurrentUserName);

            try
            {
                context.MembershipService.ChangeUserName(targetUser, targetChangedUserName);
                context.Repository.CommitChanges();
            }
            catch (InvalidOperationException e)
            {
                throw new HubException(e.Message);
            }

            context.NotificationService.OnUserNameChanged(targetUser, callingUser, targetCurrentUserName, targetChangedUserName);
        }
    }
}
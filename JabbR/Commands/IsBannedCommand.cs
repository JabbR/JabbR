using System;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("isbanned", "IsBanned_CommandInfo", "[nickname]", "admin")]
    public class IsBannedCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                context.NotificationService.IsBanned();
                return;
            }

            var name = MembershipService.NormalizeUserName(args[0]);

            ChatUser user = context.Repository.GetUserByName(name);

            if (user == null)
            {
                throw new HubException(String.Format(LanguageResources.UserNotFound, name));
            }

            context.NotificationService.IsBanned(user);
        }
    }
}
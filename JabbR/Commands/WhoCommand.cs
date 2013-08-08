using System;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("who", "Who_CommandInfo", "[nickname]", "global")]
    public class WhoCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                context.NotificationService.ListUsers();
                return;
            }

            var name = MembershipService.NormalizeUserName(args[0]);

            ChatUser user = context.Repository.GetUserByName(name);

            if (user == null)
            {
                throw new HubException(String.Format(LanguageResources.UserNotFound, name));
            }

            context.NotificationService.ShowUserInfo(user);
        }
    }
}
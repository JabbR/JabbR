using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("who", "")]
    public class WhoCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                context.NotificationService.ListUsers();
                return;
            }

            var name = ChatService.NormalizeUserName(args[0]);

            ChatUser user = context.Repository.GetUserByName(name);

            if (user == null)
            {
                throw new InvalidOperationException(String.Format("We didn't find anyone with the username {0}", name));
            }

            context.NotificationService.ShowUserInfo(user);
        }
    }
}
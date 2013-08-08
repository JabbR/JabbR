using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("where", "Where_CommandInfo", "nickname", "user")]
    public class WhereCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Where_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser user = context.Repository.VerifyUser(targetUserName);
            context.NotificationService.ListRooms(user);
        }
    }
}
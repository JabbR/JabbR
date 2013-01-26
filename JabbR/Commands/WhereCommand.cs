using System;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("where", "List the rooms that user is in.", "nickname", "user")]
    public class WhereCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who are you trying to locate?");
            }

            string targetUserName = args[0];

            ChatUser user = context.Repository.VerifyUser(targetUserName);
            context.NotificationService.ListRooms(user);
        }
    }
}
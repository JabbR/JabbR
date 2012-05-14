using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("where", "")]
    public class WhereCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Who are you trying to locate?");
            }

            string targetUserName = HttpUtility.HtmlDecode(args[0]);

            ChatUser user = context.Repository.VerifyUser(targetUserName);
            context.NotificationService.ListRooms(user);
        }
    }
}
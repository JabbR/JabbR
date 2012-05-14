using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("lock", "")]
    public class LockCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length < 2)
            {
                throw new InvalidOperationException("Which room do you want to lock?");
            }

            string roomName = HttpUtility.HtmlDecode(args[1]);
            ChatRoom room = context.Repository.VerifyRoom(roomName);

            context.Service.LockRoom(callingUser, room);

            context.NotificationService.LockRoom(callingUser, room);
        }
    }
}
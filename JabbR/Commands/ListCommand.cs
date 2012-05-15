using System;
using System.Linq;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("list", "")]
    public class ListCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length  == 0)
            {
                throw new InvalidOperationException("List users in which room?");
            }

            string roomName = HttpUtility.HtmlDecode(args[0]);
            ChatRoom room = context.Repository.VerifyRoom(roomName);

            var names = context.Repository.GetOnlineUsers(room).Select(s => s.Name);

            context.NotificationService.ListUsers(room, names);
        }
    }
}
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
            if (args.Length < 2)
            {
                throw new InvalidOperationException("List users in which room?");
            }

            string roomName = HttpUtility.HtmlDecode(args[1]);
            ChatRoom room = context.Repository.VerifyRoom(roomName);

            var names = room.Users.Online().Select(s => s.Name);

            context.NotificationService.ListUsers(room, names);
        }
    }
}
using System;
using System.Linq;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("close", "")]
    public class CloseCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Which room do you want to close?");
            }

            string roomName = HttpUtility.HtmlDecode(args[0]);
            ChatRoom room = context.Repository.VerifyRoom(roomName);

            // Before I close the room, I need to grab a copy of -all- the users in that room.
            // Otherwise, I can't send any notifications to the room users, because they
            // have already been kicked.
            var users = room.Users.ToList();
            context.Service.CloseRoom(callingUser, room);

            context.NotificationService.CloseRoom(users, room);
        }
    }
}
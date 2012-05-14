using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("join", "")]
    public class JoinCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length < 2)
            {
                throw new InvalidOperationException("Join which room?");
            }

            // Extract arguments
            string roomName = HttpUtility.HtmlDecode(args[1]);
            string inviteCode = null;
            if (args.Length > 2)
            {
                inviteCode = args[2];
            }

            // Locate the room
            ChatRoom room = context.Repository.VerifyRoom(roomName);

            if (!context.Repository.IsUserInRoom(callingUser, room))
            {
                // Join the room
                context.Service.JoinRoom(callingUser, room, inviteCode);

                context.Repository.CommitChanges();
            }

            context.NotificationService.JoinRoom(callingUser, room);
        }
    }
}
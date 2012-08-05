using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("join", "Join a channel of your choice. If it is private and you have an invite code, enter it after the room name.", "room [invitecode]", "user")]
    public class JoinCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Join which room?");
            }

            // Extract arguments
            string roomName = HttpUtility.HtmlDecode(args[0]);
            string inviteCode = null;
            if (args.Length > 1)
            {
                inviteCode = args[1];
            }

            // Locate the room, does NOT have to be open
            ChatRoom room = context.Repository.VerifyRoom(roomName, mustBeOpen: false);

            if (!context.Repository.IsUserInRoom(context.Cache, callingUser, room))
            {
                // Join the room
                context.Service.JoinRoom(callingUser, room, inviteCode);

                context.Repository.CommitChanges();
            }

            context.NotificationService.JoinRoom(callingUser, room);
        }
    }
}
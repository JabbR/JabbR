using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("join", "Join_CommandInfo", "room [invitecode]", "user")]
    public class JoinCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Join_RoomRequired);
            }

            // Extract arguments
            string roomName = args[0];
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
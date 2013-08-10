using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("allowed", "Allowed_CommandInfo", "[room]", "room")]
    public class AllowedCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string targetRoomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(targetRoomName))
            {
                throw new HubException(LanguageResources.Allowed_RoomRequired);
            }

            ChatRoom room = context.Repository.VerifyRoom(targetRoomName, mustBeOpen: false);

            // ensure the user could join the room if they wanted to
            callingUser.EnsureAllowed(room);

            context.NotificationService.ListAllowedUsers(room);
        }
    }
}
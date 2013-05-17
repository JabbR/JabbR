using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("allowed", "Show a list of all users allowed in the given room.", "[room]", "global")]
    public class AllowedCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string targetRoomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (string.IsNullOrEmpty(targetRoomName) || targetRoomName.Equals("Lobby", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("Which room?");
            }

            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, targetRoomName);

            context.NotificationService.ListAllowedUsers(room);
        }
    }
}
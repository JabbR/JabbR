using System;
using JabbR.Infrastructure;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("resetinvitecode", "ResetInviteCode_CommandInfo", "[room]", "room")]
    public class ResetInviteCodeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (String.IsNullOrEmpty(callerContext.RoomName))
            {
                throw new HubException(LanguageResources.InvokeFromRoomRequired);
            }

            string targetRoomName = args.Length > 0 ? args[0] : callerContext.RoomName;

            if (String.IsNullOrEmpty(targetRoomName))
            {
                throw new HubException(LanguageResources.ResetInviteCode_RoomRequired);
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(targetRoomName, mustBeOpen: false);

            // ensure the user could join the room if they wanted to
            callingUser.EnsureAllowed(targetRoom);

            context.Service.SetInviteCode(callingUser, targetRoom, RandomUtils.NextInviteCode());

            ChatRoom callingRoom = context.Repository.GetRoomByName(callerContext.RoomName);
            context.NotificationService.PostNotification(callingRoom, callingUser, String.Format(LanguageResources.InviteCode_Success, targetRoomName, targetRoom.InviteCode));
        }
    }
}
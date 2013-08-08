using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("removeowner", "RemoveOwner_CommandInfo", "user [room]", "room")]
    public class RemoveOwnerCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.RemoveOwner_UserRequired);
            }

            string targetUserName = args[0];

            ChatUser targetUser = context.Repository.VerifyUser(targetUserName);

            string roomName = args.Length > 1 ? args[1] : callerContext.RoomName;

            if (String.IsNullOrEmpty(roomName))
            {
                throw new HubException(LanguageResources.RemoveOwner_RoomRequired);
            }

            ChatRoom targetRoom = context.Repository.VerifyRoom(roomName);

            context.Service.RemoveOwner(callingUser, targetUser, targetRoom);

            context.NotificationService.RemoveOwner(targetUser, targetRoom);

            context.Repository.CommitChanges();
        }
    }
}
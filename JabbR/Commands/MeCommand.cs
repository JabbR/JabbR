using System;
using System.Linq;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("me", "Me_CommandInfo", "note", "user")]
    public class MeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);

            room.EnsureOpen();

            if (args.Length  == 0)
            {
                throw new HubException(LanguageResources.Me_ActionRequired);
            }

            var content = String.Join(" ", args);

            context.NotificationService.OnSelfMessage(room, callingUser, content);
        }
    }
}
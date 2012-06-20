using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("welcome", "")]
    public class WelcomeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string newWelcome = String.Join(" ", args).Trim();
            ChatService.ValidateWelcome(newWelcome);

            newWelcome = String.IsNullOrWhiteSpace(newWelcome) ? null : newWelcome;

            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);
            context.Service.ChangeWelcome(callingUser, room, newWelcome);
            context.NotificationService.ChangeWelcome(callingUser, room);
        }
    }
}
using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("meme","Meme_CommandInfo", "meme-type top-line bottom-line", "user")]
    public class MemeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (String.IsNullOrEmpty(callerContext.RoomName))
            {
                throw new HubException(LanguageResources.InvokeFromRoomRequired);
            }

            if (args == null || args.Length < 1)
            {
                throw new HubException(LanguageResources.Meme_DataRequired);
            }

            if (args.Length != 3)
            {
                throw new HubException(LanguageResources.Meme_IncorrectNumberOfArguments);
            }

            ChatRoom callingRoom = context.Repository.GetRoomByName(callerContext.RoomName);
            string message = String.Format("https://upboat.me/{0}/{1}/{2}.jpg", args[0], args[1], args[2]);

            context.NotificationService.GenerateMeme(callingUser, callingRoom, message);
        }
    }
}
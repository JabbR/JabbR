using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Infrastructure;

namespace JabbR.Commands
{
    [Command("broadcast", "")]
    public class BroadcastCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, Models.ChatUser callingUser, string[] args)
        {
            string messageText = String.Join(" ", args).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException("What did you want to broadcast?");
            }

            HashSet<string> urls;
            var transform = new TextTransform(context.Repository);
            messageText = transform.Parse(messageText);

            messageText = TextTransform.TransformAndExtractUrls(messageText, out urls);

            context.NotificationService.BroadcastMessage(callingUser, messageText);
        }
    }
}
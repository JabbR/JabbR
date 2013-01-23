using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Infrastructure;

namespace JabbR.Commands
{
    [Command("broadcast", "Sends a message to all users in all rooms. Only administrators can use this command.", "message", "global")]
    public class BroadcastCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, Models.ChatUser callingUser, string[] args)
        {
            string messageText = String.Join(" ", args).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException("What did you want to broadcast?");
            }

            context.NotificationService.BroadcastMessage(callingUser, messageText);
        }
    }
}
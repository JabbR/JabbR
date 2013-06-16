using System;

namespace JabbR.Commands
{
    [Command("broadcast", "Broadcast_CommandInfo", "message", "global")]
    public class BroadcastCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, Models.ChatUser callingUser, string[] args)
        {
            string messageText = String.Join(" ", args).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException(LanguageResources.Broadcast_MessageRequired);
            }

            context.NotificationService.BroadcastMessage(callingUser, messageText);
        }
    }
}
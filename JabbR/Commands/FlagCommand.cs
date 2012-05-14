using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("flag", "")]
    public class FlagCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                // Clear the flag.
                callingUser.Flag = null;
            }
            else
            {
                // Set the flag.
                string isoCode = String.Join(" ", args[0]).ToLowerInvariant();
                ChatService.ValidateIsoCode(isoCode);
                callingUser.Flag = isoCode;
            }

            context.NotificationService.ChangeFlag(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
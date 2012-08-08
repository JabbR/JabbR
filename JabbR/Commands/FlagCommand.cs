using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("flag", "Show a small flag which represents your nationality. Eg. /flag US for a USA flag. ISO Reference Chart: http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2 (Apologies to people with dual citizenship).",
        "Iso 3366-2 Code", "user")]
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
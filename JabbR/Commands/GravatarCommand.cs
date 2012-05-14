using System;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("gravatar", "")]
    public class GravatarCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string email = String.Join(" ", args);

            if (String.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email was not specified!");
            }

            string hash = email.ToLowerInvariant().ToMD5();

            // Set user hash
            callingUser.Hash = hash;

            context.NotificationService.ChangeGravatar(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
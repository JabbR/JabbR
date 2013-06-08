using System;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("gravatar", "Set your gravatar.", "email", "user")]
    public class GravatarCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string email = String.Join(" ", args);

            if (String.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Which email address do you want to use for the Gravatar image?");
            }

            string hash = email.ToLowerInvariant().ToMD5();

            // Set user hash
            callingUser.Hash = hash;

            context.NotificationService.ChangeGravatar(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
using System;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("gravatar", "Gravatar_CommandInfo", "email", "user")]
    public class GravatarCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string email = String.Join(" ", args);

            if (String.IsNullOrWhiteSpace(email))
            {
                throw new HubException(LanguageResources.Gravatar_EmailRequired);
            }

            string hash = email.ToLowerInvariant().ToMD5();

            // Set user hash
            callingUser.Hash = hash;

            context.NotificationService.ChangeGravatar(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
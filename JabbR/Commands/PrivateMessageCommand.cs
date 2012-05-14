using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("msg", "")]
    public class PrivateMessageCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (context.Repository.Users.Count() == 1)
            {
                throw new InvalidOperationException("You're the only person in here...");
            }

            if (args.Length == 0 || String.IsNullOrWhiteSpace(args[0]))
            {
                throw new InvalidOperationException("Who are you trying send a private message to?");
            }
            var toUserName = HttpUtility.HtmlDecode(args[0]);
            ChatUser toUser = context.Repository.VerifyUser(toUserName);

            if (toUser == callingUser)
            {
                throw new InvalidOperationException("You can't private message yourself!");
            }

            string messageText = String.Join(" ", args.Skip(1)).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException(String.Format("What did you want to say to '{0}'?", toUser.Name));
            }

            HashSet<string> urls;
            var transform = new TextTransform(context.Repository);
            messageText = transform.Parse(messageText);

            messageText = TextTransform.TransformAndExtractUrls(messageText, out urls);

            context.NotificationService.SendPrivateMessage(callingUser, toUser, messageText);
        }
    }
}
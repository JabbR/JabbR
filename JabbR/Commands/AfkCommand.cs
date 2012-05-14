using System;
using System.Linq;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("afk", "")]
    public class AfkCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string message = String.Join(" ", args).Trim();

            ChatService.ValidateNote(message);

            callingUser.AfkNote = String.IsNullOrWhiteSpace(message) ? null : message;
            callingUser.IsAfk = true;

            context.NotificationService.ChangeNote(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
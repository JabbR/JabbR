using System;
using System.Linq;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("afk", "Set a temporary (Away From Keyboard) note shown via a paperclip icon next to your name, with the message appearing when you hover over it. This note will disappear when you first resume typing.", "[note]", "user")]
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
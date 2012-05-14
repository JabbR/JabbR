using System;
using System.Linq;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    [Command("note", "")]
    public class NoteCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            // We need to determine if we're either
            // 1. Setting a new Note.
            // 2. Clearing the existing Note.
            // If we have no optional text, then we need to clear it. Otherwise, we're storing it.
            bool isNoteBeingCleared = args.Length == 0;
            callingUser.Note = isNoteBeingCleared ? null : String.Join(" ", args).Trim();

            ChatService.ValidateNote(callingUser.Note);

            context.NotificationService.ChangeNote(callingUser);

            context.Repository.CommitChanges();
        }
    }
}
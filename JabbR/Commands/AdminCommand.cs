using System;
using JabbR.Models;

namespace JabbR.Commands
{
    public abstract class AdminCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (!callingUser.IsAdmin)
            {
                throw new InvalidOperationException(LanguageResources.AdminRequired);
            }

            ExecuteAdminOperation(context, callerContext, callingUser, args);
        }

        public abstract void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args);
    }
}
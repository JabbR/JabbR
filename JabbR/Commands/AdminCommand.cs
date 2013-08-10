using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    public abstract class AdminCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (!callingUser.IsAdmin)
            {
                throw new HubException(LanguageResources.AdminRequired);
            }

            ExecuteAdminOperation(context, callerContext, callingUser, args);
        }

        public abstract void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args);
    }
}
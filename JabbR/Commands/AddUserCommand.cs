using System;

using JabbR.Models;
using JabbR.Services;

using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("adduser", "AddUser_CommandInfo", "username email [password]", "admin")]
    public class AddUserCommand : AdminCommand
    {
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.Authentication_NameRequired);
            }

            if (args.Length == 1)
            {
                throw new HubException(LanguageResources.Authentication_EmailRequired);
            }

            var name = MembershipService.NormalizeUserName(args[0]);
            var email = args[1];
            string password;

            if (args.Length == 2)
            {
                password = context.MembershipService.CreatePassword(8);
            }
            else
            {
                password = args[2];
            }

            ChatUser createdUser;
            try
            {
                createdUser = context.MembershipService.AddUser(name, email, password);
            }
            catch (InvalidOperationException e)
            {
                throw new HubException(e.Message);
            }

            context.NotificationService.AddUser(createdUser, callingUser, password);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JabbR.Services;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("nick", "")]
    public class NickCommand : ICommand
    {
        public void Execute(CommandContext context, CallerContext callerContext, string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("No nick specified!");
            }

            string userName = HttpUtility.HtmlDecode(args[0]);
            if (String.IsNullOrWhiteSpace(userName))
            {
                throw new InvalidOperationException("No nick specified!");
            }

            string password = null;
            if (args.Length > 1)
            {
                password = args[1];
            }

            string newPassword = null;
            if (args.Length > 2)
            {
                newPassword = args[2];
            }

            // See if there is a current user
            ChatUser user = context.Repository.GetUserById(callerContext.UserId);

            if (user == null && String.IsNullOrEmpty(newPassword))
            {
                user = context.Repository.GetUserByName(userName);

                // There's a user with the name specified
                if (user != null)
                {
                    if (String.IsNullOrEmpty(password))
                    {
                        ChatService.ThrowPasswordIsRequired();
                    }
                    else
                    {
                        // If there's no user but there's a password then authenticate the user
                        context.Service.AuthenticateUser(userName, password);

                        // update user's activity and add client to list of clients
                        context.Service.UpdateActivity(user, callerContext.ClientId, callerContext.UserAgent);

                        // Initialize the returning user
                        context.NotificationService.LogOn(user, callerContext.ClientId);
                    }
                }
                else
                {
                    // If there's no user add a new one
                    user = context.Service.AddUser(userName, callerContext.ClientId, callerContext.UserAgent, password);

                    // Notify the user that they're good to go!
                    context.NotificationService.OnUserCreated(user);
                }
            }
            else
            {
                if (String.IsNullOrEmpty(password))
                {
                    string oldUserName = user.Name;

                    // Change the user's name
                    context.Service.ChangeUserName(user, userName);

                    context.NotificationService.OnUserNameChanged(user, oldUserName, userName);
                }
                else
                {
                    // If the user specified a password, verify they own the nick
                    ChatUser targetUser = context.Repository.VerifyUser(userName);

                    // Make sure the current user and target user are the same
                    if (user != targetUser)
                    {
                        throw new InvalidOperationException("You can't set/change the password for a nickname you don't own.");
                    }

                    if (String.IsNullOrEmpty(newPassword))
                    {
                        if (targetUser.HashedPassword == null)
                        {
                            context.Service.SetUserPassword(user, password);

                            context.NotificationService.SetPassword();
                        }
                        else
                        {
                            throw new InvalidOperationException("Use /nick [nickname] [oldpassword] [newpassword] to change and existing password.");
                        }
                    }
                    else
                    {
                        context.Service.ChangeUserPassword(user, password, newPassword);

                        context.NotificationService.ChangePassword();
                    }
                }
            }

            // Commit the changes
            context.Repository.CommitChanges();
        }
    }
}
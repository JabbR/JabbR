using System.Security.Claims;
using System.Security.Principal;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IMembershipService
    {
        // Creates an account form a ClaimsPrincipal
        ChatUser AddUser(ClaimsPrincipal claimsPrincipal);
        void LinkIdentity(ChatUser user, ClaimsPrincipal principal);

        // User name password functions
        ChatUser AddUser(string userName, string email, string password);
        ChatUser AuthenticateUser(string userName, string password);
        void ChangeUserName(ChatUser user, string newUserName);
        void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword);
        void SetUserPassword(ChatUser user, string password);
    }
}
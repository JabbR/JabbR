using System.Security.Claims;
using System.Security.Principal;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IMembershipService
    {
        // Account creation
        ChatUser AddUser(ClaimsPrincipal claimsPrincipal);
        void LinkIdentity(ChatUser user, ClaimsPrincipal principal);
        ChatUser AddUser(string userName, string email, string password);

        void ChangeUserName(ChatUser user, string newUserName);

        // Password management
        void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword);
        void SetUserPassword(ChatUser user, string password);

        bool TryAuthenticateUser(string userName, string password, out ChatUser user);
    }
}
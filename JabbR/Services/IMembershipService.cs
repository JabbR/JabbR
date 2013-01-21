using JabbR.Models;

namespace JabbR.Services
{
    public interface IMembershipService
    {
        // Add a user without a password (identity)
        ChatUser AddUser(string userName, string identity, string email);

        // User name password functions
        ChatUser AddUser(string userName, string password);
        void AuthenticateUser(string userName, string password);
        void ChangeUserName(ChatUser user, string newUserName);
        void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword);
        void SetUserPassword(ChatUser user, string password);
    }
}
using JabbR.Models;

namespace JabbR.Services
{
    public interface IMembershipService
    {
        // Add a user without a password (identity)
        ChatUser AddUser(string userName, string provider, string identity, string email);

        // User name password functions
        ChatUser AddUser(string userName, string email, string password);
        ChatUser AuthenticateUser(string userName, string password);
        void ChangeUserName(ChatUser user, string newUserName);
        void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword);
        void SetUserPassword(ChatUser user, string password);
    }
}
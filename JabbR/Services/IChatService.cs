using JabbR.Models;

namespace JabbR.Services
{
    public interface IChatService
    {
        // Users
        ChatUser AddUser(string userName, string clientId, string password);
        void AuthenticateUser(string userName, string password);
        void ChangeUserName(ChatUser user, string newUserName);
        void ChangeUserPassword(ChatUser user, string oldPassword, string newPassword);
        void SetUserPassword(ChatUser user, string password);
        void UpdateActivity(ChatUser user);

        // Rooms
        ChatRoom AddRoom(ChatUser user, string roomName);
        void JoinRoom(ChatUser user, ChatRoom room);
        void LeaveRoom(ChatUser user, ChatRoom room);

        // Messages
        ChatMessage AddMessage(ChatUser user, ChatRoom room, string content);

        // Owner commands
        void AddOwner(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void KickUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
    }
}

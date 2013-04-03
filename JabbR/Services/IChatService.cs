using JabbR.Models;
using JabbR.UploadHandlers;

namespace JabbR.Services
{
    public interface IChatService
    {
        // Users 
        ChatClient AddClient(ChatUser user, string clientId, string userAgent);
        void UpdateActivity(ChatUser user, string clientId, string userAgent);
        string DisconnectClient(string clientId);

        // Rooms
        ChatRoom AddRoom(ChatUser user, string roomName);
        void JoinRoom(ChatUser user, ChatRoom room, string inviteCode);
        void LeaveRoom(ChatUser user, ChatRoom room);
        void SetInviteCode(ChatUser user, ChatRoom room, string inviteCode);

        // Messages
        ChatMessage AddMessage(ChatUser user, ChatRoom room, string id, string content);
        ChatMessage AddMessage(string userId, string roomName, string url);

        // Owner commands
        void AddOwner(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void RemoveOwner(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void KickUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void AllowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void UnallowUser(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void LockRoom(ChatUser user, ChatRoom targetRoom);
        void CloseRoom(ChatUser user, ChatRoom targetRoom);
        void OpenRoom(ChatUser user, ChatRoom targetRoom);
        void ChangeTopic(ChatUser user, ChatRoom room, string newTopic);
        void ChangeWelcome(ChatUser user, ChatRoom room, string newWelcome);
        void AppendMessage(string id, string content);

        // Admin commands
        void AddAdmin(ChatUser admin, ChatUser targetUser);
        void RemoveAdmin(ChatUser admin, ChatUser targetUser);
        void BanUser(ChatUser callingUser, ChatUser targetUser);

        void AddAttachment(ChatMessage message, string fileName, string contentType, long size, UploadResult result);

        // Add mention
        void AddNotification(ChatUser mentionedUser, ChatMessage message, bool markAsRead);
    }
}

using System.Collections.Generic;
using JabbR.Models;

namespace JabbR.Services
{
    public interface INotificationService
    {
        void ChangeGravatar(ChatUser user);
        void JoinRoom(ChatUser user, ChatRoom room);

        void ListUsers();
        void ListUsers(ChatRoom room, IEnumerable<string> names);
        void ListRooms(ChatUser user);
        void ListUsers(IEnumerable<ChatUser> users);

        void NudgeRoom(ChatRoom room, ChatUser user);
        void NugeUser(ChatUser user, ChatUser toUser);

        void AddMessage(ChatRoom room, ChatMessage chatMessage);
        void ChangePassword();
        void SetPassword();

        void SendPrivateMessage(ChatUser user, ChatUser toUser, string messageText);        
        void LeaveRoom(ChatUser user, ChatRoom room);

        void OnOwnerAdded(ChatUser targetUser, ChatRoom targetRoom);
        void KickUser(ChatRoom room, ChatUser targetUser);

        void OnUserCreated(ChatUser user);
        void OnUserNameChanged(ChatUser user, string newUserName, string oldUserName);

        void OnRoomCountChanged(ChatRoom room);
        void OnSelfMessage(ChatRoom room, ChatUser user, string content);
        void OnTyping(bool isTyping, ChatUser user, ChatRoom room);

        void ShowHelp();
        void ShowRooms();
    }
}

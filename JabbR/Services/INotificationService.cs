using System.Collections.Generic;
using JabbR.Models;

namespace JabbR.Services
{
    public interface INotificationService
    {
        void ChangeGravatar(ChatUser user);
        void LogOut(ChatUser user);
        void JoinRoom(ChatUser user, ChatRoom room);
        void Initialize(ChatUser user);

        void ListUsers();
        void ListUsers(ChatRoom room, IEnumerable<string> names);
        void ListRooms(ChatUser user);
        void ListUsers(IEnumerable<ChatUser> users);

        void NudgeRoom(ChatRoom room, ChatUser user);
        void NugeUser(ChatUser user, ChatUser toUser);

        void ChangePassword();
        void SetPassword();

        void SendPrivateMessage(ChatUser user, ChatUser targetUser, string messageText);        
        void LeaveRoom(ChatUser user, ChatRoom room);

        void OnOwnerAdded(ChatUser targetUser, ChatRoom targetRoom);
        void KickUser(ChatUser targetUser, ChatRoom targetRoom);

        void OnUserCreated(ChatUser user);
        void OnUserNameChanged(ChatUser user, string oldUserName, string newUserName);

        void OnSelfMessage(ChatRoom room, ChatUser user, string content);

        void ShowHelp();
        void ShowRooms();
    }
}

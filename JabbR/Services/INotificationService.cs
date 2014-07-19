using System.Collections.Generic;
using JabbR.Models;

namespace JabbR.Services
{
    public interface INotificationService
    {
        void ChangeGravatar(ChatUser user);
        void JoinRoom(ChatUser user, ChatRoom room);

        // Client actions
        void LogOn(ChatUser user, string clientId);
        void LogOut(ChatUser user, string clientId);

        void ListUsers();
        void ListUsers(ChatRoom room, IEnumerable<string> names);
        void ListRooms(ChatUser user);
        void ListUsers(IEnumerable<ChatUser> users);
        void ListAllowedUsers(ChatRoom room);
        void ListOwners(ChatRoom room);

        void Invite(ChatUser user, ChatUser targetUser, ChatRoom targetRoom);
        void NudgeRoom(ChatRoom room, ChatUser user);
        void NudgeUser(ChatUser user, ChatUser targetUser);

        void ChangeAfk(ChatUser user);
        void ChangeNote(ChatUser user);
        void ChangeFlag(ChatUser user);
        void ChangeTopic(ChatUser user, ChatRoom room);
        void ChangeWelcome(ChatUser user, ChatRoom room);
        void GenerateMeme(ChatUser user, ChatRoom room, string message);

        void PostNotification(ChatRoom room, ChatUser user, string message);
        void SendPrivateMessage(ChatUser user, ChatUser targetUser, string messageText);
        void LeaveRoom(ChatUser user, ChatRoom room);

        void AddOwner(ChatUser targetUser, ChatRoom targetRoom);
        void RemoveOwner(ChatUser targetUser, ChatRoom targetRoom);
        void KickUser(ChatUser targetUser, ChatRoom targetRoom, ChatUser callingUser, string reason);
        void AllowUser(ChatUser targetUser, ChatRoom targetRoom);
        void UnallowUser(ChatUser targetUser, ChatRoom targetRoom, ChatUser callingUser);
        void BanUser(ChatUser targetUser, ChatUser callingUser, string reason);
        void UnbanUser(ChatUser targetUser);
        void CheckBanned(ChatUser targetUser);
        void CheckBanned();

        void OnUserCreated(ChatUser user);
        void OnUserNameChanged(ChatUser user, string oldUserName, string newUserName);

        void OnSelfMessage(ChatRoom room, ChatUser user, string content);

        void ShowUserInfo(ChatUser user);
        void ShowHelp();

        void LockRoom(ChatUser targetUser, ChatRoom room);
        void CloseRoom(IEnumerable<ChatUser> users, ChatRoom room);
        void UnCloseRoom(IEnumerable<ChatUser> users, ChatRoom room);

        void AddAdmin(ChatUser targetUser);
        void RemoveAdmin(ChatUser targetUser);
        void BroadcastMessage(ChatUser user, string messageText);
        void ForceUpdate();
    }
}
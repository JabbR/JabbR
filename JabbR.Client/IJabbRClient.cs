using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JabbR.Client.Models;
using JabbR.Models;
using Microsoft.AspNet.SignalR.Client;

namespace JabbR.Client
{
    public interface IJabbRClient
    {
        event Action<Message, string> MessageReceived;
        event Action<IEnumerable<string>> LoggedOut;
        event Action<User, string, bool> UserJoined;
        event Action<User, string> UserLeft;
        event Action<string> Kicked;
        event Action<string, string, string> PrivateMessage;
        event Action<User, string> UserTyping;
        event Action<User, string> GravatarChanged;
        event Action<string, string, string> MeMessageReceived;
        event Action<string, User, string> UsernameChanged;
        event Action<User, string> NoteChanged;
        event Action<User, string> FlagChanged;
        event Action<Room> TopicChanged;
        event Action<User, string> OwnerAdded;
        event Action<User, string> OwnerRemoved;
        event Action<string, string, string> AddMessageContent;
        event Action<Room> JoinedRoom;
        event Action<Room, int> RoomCountChanged;
        event Action<User> UserActivityChanged;
        event Action<IEnumerable<User>> UsersInactive;
        event Action Disconnected;
        event Action<StateChange> StateChanged;

        string SourceUrl { get; }
        bool AutoReconnect { get; set; }
        ICredentials Credentials { get; set; }

        Task<LogOnInfo> Connect(string name, string password);
        Task<User> GetUserInfo();
        Task LogOut();
        Task<bool> Send(string message, string roomName);
        Task<bool> Send(ClientMessage message);
        Task CreateRoom(string roomName);
        Task JoinRoom(string roomName);
        Task LeaveRoom(string roomName);
        Task SetFlag(string countryCode);
        Task SetNote(string noteText);
        Task SendPrivateMessage(string userName, string message);
        Task Kick(string userName, string roomName);
        Task<bool> CheckStatus();
        Task SetTyping(string roomName);
        Task PostNotification(ClientNotification notification);
        Task PostNotification(ClientNotification notification, bool executeContentProviders);
        Task<IEnumerable<Message>> GetPreviousMessages(string fromId);
        Task<Room> GetRoomInfo(string roomName);
        Task<IEnumerable<Room>> GetRooms();
        void Disconnect();
    }
}

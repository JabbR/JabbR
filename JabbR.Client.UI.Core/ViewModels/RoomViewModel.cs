using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class RoomViewModel 
        : BaseViewModel
    {
        readonly IJabbRClient _client;
        public RoomViewModel(IJabbRClient client)
        {
            _client = client;
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged(() => Message);
                RaisePropertyChanged(() => CanSendMessage);
            }
        }

        private Room _room;
        public Room Room
        {
            get { return _room; }
            set
            {
                _room = value;
                RaisePropertyChanged(() => Room);
            }
        }

        private ObservableCollection<Message> _messages;
        public ObservableCollection<Message> Messages 
        {
            get { return _messages; }
            set
            {
                _messages = value;
                RaisePropertyChanged(() => Messages);
            }
        }

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set
            {
                _users = value;
                RaisePropertyChanged(() => Users);
            }   
        }

        private ICommand _sendMessageCommand;
        public ICommand SendMessageCommand
        {
            get { return _sendMessageCommand ?? new MvxCommand(DoSendMessage); }
        }

        public bool CanSendMessage
        {
            get { return !String.IsNullOrEmpty(Message); }
        }

        private async void DoSendMessage()
        {
            if (CanSendMessage)
            {
                try
                {
                    var result = await _client.Send(Message, Room.Name);
                    if (result)
                    {
                        Message = String.Empty;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.GetBaseException().Message;
                }
            }
        }

        public async void Init(string roomName)
        {
            var room = await _client.GetRoomInfo(roomName);
            Room = room;
            Messages = new ObservableCollection<Message>(Room.RecentMessages);
            Users = new ObservableCollection<User>(Room.Users);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;
using Newtonsoft.Json;
using JabbR.Client.UI.Core.Interfaces;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class RoomViewModel
        : BaseViewModel
    {
        public class NavigationParameter
            : NavigationParametersBase
        {
            public string RoomName { get; set; }
        }

        readonly IJabbRClient _client;
        public RoomViewModel(IJabbRClient client, IGlobalProgressIndicator progress)
            : base(progress)
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

        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                RaisePropertyChanged(() => CurrentUser);
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

        private Message _currentItem;
        public Message CurrentItem
        {
            get { return _currentItem; }
            set
            {
                _currentItem = value;
                RaisePropertyChanged(() => CurrentItem);
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

        public async void Init(NavigationParameter parameters)
        {
            try
            {
                Progress.SetStatus("Loading...", true);
                var room = await _client.GetRoomInfo(parameters.RoomName);
                Room = room;
                Messages = new ObservableCollection<Message>(Room.RecentMessages);
                Users = new ObservableCollection<User>(Room.Users);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                Progress.ClearStatus();
            }
            // Does this get registered multiple times? Is there a dispose method we can use
            // to unsubscribe
            _client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(Message message, string room)
        {
            if (Room.Name == room)
            {
                Dispatcher.RequestMainThreadAction(() => 
                {
                    Messages.Add(message);
                    CurrentItem = message;
                });
            }
        }

        public override void Deactivate()
        {
            _client.MessageReceived -= OnMessageReceived;
        }
    }
}

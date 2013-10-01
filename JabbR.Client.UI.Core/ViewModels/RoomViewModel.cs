using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;
using JabbR.Client.UI.Core.Interfaces;
using JabbR.Models;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class RoomViewModel
        : BaseViewModel
    {
        public class NavigationParameter
            : NavigationParametersBase
        {
            public User User { get; set; }
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

        private MessageViewModel _addedMessage;
        public MessageViewModel AddedMessage
        {
            get { return _addedMessage; }
            set
            {
                _addedMessage = value;
                RaisePropertyChanged(() => AddedMessage);
            }
        }

        private ObservableCollection<MessageViewModel> _messages;
        public ObservableCollection<MessageViewModel> Messages
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

        private ICommand _fetchNextMessagesCommand;
        public ICommand FetchNextMessagesCommand
        {
            get { return _fetchNextMessagesCommand ?? new MvxCommand<object>(FetchNextMessages); }
        }

        public async void FetchNextMessages(object obj)
        {
            if (Progress.IsLoading) return;

            var message = obj as Message;
            if (message != null)
            {
                Progress.SetStatus("Loading messages...", true);
                
                try
                {
                    var fetchedMessages = await _client.GetPreviousMessages(message.Id);
                    
                    Dispatcher.RequestMainThreadAction(() =>
                    {
                        foreach (var msg in fetchedMessages)
                        {
                            Messages.Insert(0, new MessageViewModel { Message = msg, State = MessageState.Complete });
                        }
                    });
                    Progress.ClearStatus();
                }
                catch (Exception ex)
                {
                    Progress.SetStatus("Failed loading messages.", false);
                }
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
                    Progress.SetStatus("Sending message...", true, new TimeSpan(0, 0, 0, 0, 500));
                    var clientMessage = new ClientMessage()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Room = Room.Name,
                        Content = Message
                    };
                    
                    await AddMessage(clientMessage);

                    //var result = await _client.Send(clientMessage);
                    //if (result)
                    //{
                    //    var msg = new Message()
                    //    {
                    //        Id = clientMessage.Id,
                    //        Content = clientMessage.Content,
                    //        When = DateTimeOffset.Now,
                    //        User = CurrentUser
                    //    };

                    //    Messages.Add(new MessageViewModel { Message = msg, State = MessageState.New });
                    //    AddedMessage = msg;
                    //    Message = String.Empty;
                    //}
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.GetBaseException().Message;
                }
                finally
                {
                    Progress.ClearStatus();
                }
            }
        }

        public async void Init(NavigationParameter parameters)
        {
            if (Progress.IsLoading) return;
            
            try
            {
                CurrentUser = parameters.User;
                Progress.SetStatus("Loading...", true);
                var room = await _client.GetRoomInfo(parameters.RoomName);
                Room = room;
                Messages = new ObservableCollection<MessageViewModel>(Room.RecentMessages.Select(m => new MessageViewModel { Message = m, State = MessageState.Complete }));
                Users = new ObservableCollection<User>(Room.Users);
            }
            catch (Exception ex)
            {
                Progress.SetStatus("Failed to initialise room...", false);
            }
            finally
            {
                Progress.ClearStatus();
            }

            _client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(Message message, string room)
        {
            if (Room.Name == room)
            {
                Dispatcher.RequestMainThreadAction(() =>
                {
                    var msg = new MessageViewModel { Message = message, State = MessageState.Complete };
                    Messages.Add(msg);
                    AddedMessage = msg;
                });
            }
        }
        private async Task AddMessage(ClientMessage clientMessage)
        {
            var message = new MessageViewModel
            {
                Message = new Message { Id = clientMessage.Id, Content = clientMessage.Content, User = CurrentUser, When = DateTimeOffset.UtcNow },
                State = MessageState.New
            };

            // Add the message locally
            Messages.Add(message);
            AddedMessage = message;

            SendMessage(clientMessage, message);

            WaitForSlowMessages(message);
        }

        private void WaitForSlowMessages(MessageViewModel message)
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                Dispatcher.RequestMainThreadAction(() =>
                {
                    lock (message)
                    {
                        if (message.State == MessageState.New)
                        {
                            message.State = MessageState.Slow;
                        }
                    }
                });
            });
        }

        private void SendMessage(ClientMessage clientMessage, MessageViewModel message)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _client.Send(clientMessage, TimeSpan.FromSeconds(5));

                    Dispatcher.RequestMainThreadAction(() =>
                    {
                        lock (message)
                        {
                            message.State = MessageState.Complete;
                        }
                        AddedMessage = message;
                        Message = String.Empty;
                    });
                }
                catch (OperationCanceledException)
                {
                    Dispatcher.RequestMainThreadAction(() =>
                    {
                        lock (message)
                        {
                            if (message.State == MessageState.New || message.State == MessageState.Slow)
                            {
                                // Failed to send the message
                                message.State = MessageState.Error;
                            }
                        }
                    });
                }
            });
        }
        public override void Deactivate()
        {
            _client.MessageReceived -= OnMessageReceived;
        }
    }
}

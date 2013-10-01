using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class MessageViewModel : MvxViewModel
    {
        public Message Message { get; set; }

        private MessageState _state;
        public MessageState State 
        {
            get { return _state; } 
            set
            {
                _state = value;
                RaisePropertyChanged(() => State);
            }
        }
    }
}

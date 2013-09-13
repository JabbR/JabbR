using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class BaseViewModel : MvxViewModel
    {
        private string _displayName = "JabbR";
        public string DisplayName
        {
            get { return _displayName; }
        }

        public string PageName { get; set; }
        
        private bool _hasError;
        public bool HasError
        {
            get { return _hasError; }
            set
            {
                _hasError = value;
                RaisePropertyChanged(() => HasError);
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                RaisePropertyChanged(() => ErrorMessage);
                HasError = !String.IsNullOrEmpty(value);
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new MvxCommand(RequestClose);
            }
        }

        protected void RequestClose()
        {
            var closer = Mvx.Resolve<IViewModelCloser>();
            closer.RequestClose(this);
        }
    }
}

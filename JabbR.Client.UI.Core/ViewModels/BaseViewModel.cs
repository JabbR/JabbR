using Cirrious.MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}

using System;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class LoginViewModel
        : BaseViewModel
    {
        readonly IJabbRClient _client;
        public LoginViewModel(IJabbRClient client)
        {
            _client = client;
            PageName = "login";
        }

        public void Init()
        {
            if (IsConnected)
            {
                _client.Disconnect();

                IsConnected = false;
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                RaisePropertyChanged(() => IsConnected);
                RaisePropertyChanged(() => CanDoSignIn);
            }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
                RaisePropertyChanged(() => CanDoSignIn);
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                RaisePropertyChanged(() => Password);
                RaisePropertyChanged(() => CanDoSignIn);
            }
        }

        private ICommand _signInCommand;
        public ICommand SignInCommand
        {
            get
            {
                _signInCommand = _signInCommand ?? new MvxCommand(DoSignIn);
                return _signInCommand;
            }
        }

        public bool CanDoSignIn
        {
            get { return (!_isConnected && !String.IsNullOrEmpty(UserName) && !String.IsNullOrEmpty(Password)); }
        }

        private async void DoSignIn()
        {
            if (!CanDoSignIn)
            {
                ErrorMessage = "Please enter a username and password";
                return;
            }

            try
            {
                IsLoading = true;

                var loginInfo = await _client.Connect(UserName, Password);

                IsConnected = true;

                var user = await _client.GetUserInfo();
                
                UserName = String.Empty;
                Password = String.Empty;

                this.ShowViewModel<MainViewModel>(new MainViewModel.NavigationParameters
                {
                    CurrentUser = user,
                    Rooms = loginInfo.Rooms
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.GetBaseException().Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

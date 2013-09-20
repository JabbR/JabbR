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
            get { return (!String.IsNullOrEmpty(UserName) && !String.IsNullOrEmpty(Password)); }
        }

        private async void DoSignIn()
        {
            try
            {
                IsLoading = true;
                LoadingText = "Logging in...";
                var loginInfo = await _client.Connect(UserName, Password);

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

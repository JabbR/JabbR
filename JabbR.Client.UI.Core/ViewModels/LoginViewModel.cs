using Akavache;
using Cirrious.MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
            Init();
        }

        public void Init()
        {
            LoginInfo loginInfo = null;
            //BlobCache.Secure.GetLoginAsync(_client.SourceUrl)
            //    .Subscribe(info => loginInfo = info);
            if (loginInfo != null)
            {
                UserName = loginInfo.UserName;
                Password = loginInfo.Password;
                DoSignIn();
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
            if (!CanDoSignIn)
            {
                ErrorMessage = "Please enter a username and password";
                return;
            }
            try
            {
                var info = await _client.Connect(UserName, Password);
                //BlobCache.Secure.SaveLogin(UserName, Password, _client.SourceUrl, TimeSpan.FromDays(7));

                var user = await _client.GetUserInfo();

                ShowViewModel<MainViewModel>(new { userName = user.Name, loginInfo = info });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.GetBaseException().Message;
                return;
            }
        }
    }
}

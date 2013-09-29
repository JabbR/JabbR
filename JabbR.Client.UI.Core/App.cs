using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.UI.Core.ViewModels;

namespace JabbR.Client.UI.Core
{
    public class App : MvxApplication
    {
        const string _serverUrl = "https://jabbr.net";

        public override void Initialize()
        {
            var jabbrClient = new JabbRClient(_serverUrl);
            Mvx.RegisterSingleton<IJabbRClient>(jabbrClient);
            
            RegisterAppStart<LoginViewModel>();
        }

        protected override IMvxViewModelLocator CreateDefaultViewModelLocator()
        {
            return new CustomViewModelLocator();
        }
    }
}

using Cirrious.CrossCore;
using Cirrious.CrossCore.IoC;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.UI.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JabbR.Client.UI.Core
{
    public class App : MvxApplication
    {
        const string _serverUrl = "https://jabbr-staging.apphb.com";

        public override void Initialize()
        {
            var jabbrClient = new JabbRClient(_serverUrl);
            Mvx.RegisterSingleton<IJabbRClient>(jabbrClient);
            
            RegisterAppStart<LoginViewModel>();
        }
    }
}

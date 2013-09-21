using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.WindowsPhone.Platform;
using JabbR.Client.UI.Core.Interfaces;
using Microsoft.Phone.Controls;

namespace JabbR.Client.UI.WP8
{
    public class Setup : MvxPhoneSetup
    {
        private readonly IViewModelCloser _closer;

        public Setup(PhoneApplicationFrame rootFrame) : base(rootFrame)
        {
            _closer = new ViewModelCloser(rootFrame);
        }

        protected override IMvxApplication CreateApp()
        {
            return new JabbR.Client.UI.Core.App();
        }

        protected override void InitializeLastChance()
        {
            Mvx.RegisterSingleton<IViewModelCloser>(_closer);
            base.InitializeLastChance();
        }
        
        protected override IMvxTrace CreateDebugTrace()
        {
            return new DebugTrace();
        }
    }
}
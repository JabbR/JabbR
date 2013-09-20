using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;
using JabbR.Client.UI.Core.Interfaces;
using JabbR.Client.UI.Core.ViewModels;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JabbR.Client.WP8.UI
{
    public class ViewModelCloser : IViewModelCloser
    {
        private readonly PhoneApplicationFrame _frame;

        public ViewModelCloser(PhoneApplicationFrame frame)
        {
            _frame = frame;
            _frame.Navigating += _frame_Navigating;
        }

        void _frame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                var viewModel = ((_frame.Content as IMvxView).ViewModel as BaseViewModel);
                if (viewModel != null)
                {
                    MvxTrace.Trace("deactivate for {0}", viewModel.GetType().Name);
                    viewModel.Deactivate();
                }
            }
        }

        public void RequestClose(IMvxViewModel viewModel)
        {
            var topPage = _frame.Content;
            var view = topPage as IMvxView;

            if (view == null)
            {
                MvxTrace.Trace("request close ignored for {0} - no current view", viewModel.GetType().Name);
                return;
            }

            if (view.ViewModel != viewModel)
            {
                MvxTrace.Trace("request close ignored for {0} - current view is registered for a different viewmodel of type {1}", viewModel.GetType().Name, view.ViewModel.GetType().Name);
                return;
            }

            MvxTrace.Trace("request close for {0} - will close current page {1}", viewModel.GetType().Name, view.GetType().Name);
            _frame.GoBack();
        }
    }
}

using Cirrious.MvvmCross.ViewModels;

namespace JabbR.Client.UI.Core.Interfaces
{
    public interface IViewModelCloser
    {
        void RequestClose(IMvxViewModel viewModel);
    }
}

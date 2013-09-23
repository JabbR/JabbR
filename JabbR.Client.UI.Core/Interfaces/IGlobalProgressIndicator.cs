using System.ComponentModel;

namespace JabbR.Client.UI.Core.Interfaces
{
    public interface IGlobalProgressIndicator : INotifyPropertyChanged
    {
        bool ActualIsLoading { get; }
        void ClearStatus();
        bool IsLoading { get; set; }
        event PropertyChangedEventHandler PropertyChanged;
        void SetStatus(string message, bool isProgress);
    }
}

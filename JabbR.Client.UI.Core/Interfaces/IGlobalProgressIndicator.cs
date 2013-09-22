using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

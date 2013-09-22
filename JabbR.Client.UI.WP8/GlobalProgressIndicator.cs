using JabbR.Client.UI.Core.Interfaces;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JabbR.Client.UI.WP8
{
    public class GlobalProgressIndicator : IGlobalProgressIndicator
    {
        private ProgressIndicator _progressIndicator;

        public GlobalProgressIndicator(PhoneApplicationFrame frame)
        {
            _progressIndicator = new ProgressIndicator();
            frame.Navigated += frame_Navigated;
        }

        void frame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var ee = e.Content;
            var pp = ee as PhoneApplicationPage;
            if(pp != null)
            {
                pp.SetValue(SystemTray.ProgressIndicatorProperty, _progressIndicator);
            }
        }

        public bool ActualIsLoading
        {
            get { return true; }
        }

        private int _loadingCount;
        public bool IsLoading
        {
            get { return _loadingCount > 0; }
            set
            {
                bool loading = IsLoading;
                if(value)
                {
                    _loadingCount++;
                }
                else
                {
                    _loadingCount--;
                }
                NotifyValueChanged();
            }
        }

        public void SetStatus(string message, bool isProgress)
        {
            if (_loadingCount > 0)
                ClearStatus();

            IsLoading = isProgress;
            _progressIndicator.Text = message;
        }

        public void ClearStatus()
        {
            IsLoading = false;
            _progressIndicator.Text = String.Empty;
        }

        private void NotifyValueChanged()
        {
            if (_progressIndicator != null)
            {
                _progressIndicator.IsIndeterminate = _loadingCount > 0;

                // for now, just make sure it's always visible.
                if (_progressIndicator.IsVisible == false)
                {
                    _progressIndicator.IsVisible = true;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

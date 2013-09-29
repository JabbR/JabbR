using System;
using System.ComponentModel;
using System.Windows.Threading;
using JabbR.Client.UI.Core.Interfaces;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace JabbR.Client.UI.WP8
{
    public class GlobalProgressIndicator : IGlobalProgressIndicator
    {
        private ProgressIndicator _progressIndicator;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

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
            SetStatus(message, isProgress, TimeSpan.Zero);
        }

        public void SetStatus(string message, bool isProgress, TimeSpan delay)
        {
            Action action = () =>
            {
                if (_loadingCount > 0)
                    ClearStatus();

                IsLoading = isProgress;
                _progressIndicator.Text = message;
            };

            if (delay != TimeSpan.Zero)
            {
                _timer.Interval = delay;
                _timer.Tick += (sender, args) =>
                {
                    action();
                    _timer.Stop();
                };
                _timer.Start();
            }
            else
            {
                action();
            }
        }

        public void ClearStatus()
        {
            if(_timer.IsEnabled)
            {
                _timer.Stop();
            }
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

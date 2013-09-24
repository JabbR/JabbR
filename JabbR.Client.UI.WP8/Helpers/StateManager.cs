using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace JabbR.Client.UI.WP8.Helpers
{
    public static class StateManager
    {
        #region Methods

        public static void SaveState<T>(string key, T item)
        {
            PhoneApplicationService.Current.State[key] = item;
        }

        public static void SaveScrollViewerOffset(DependencyObject dependencyObject)
        {
            try
            {
                ScrollViewer scrollViewer = GetScrollViewer(dependencyObject);

                if (scrollViewer != null)
                {
                    string key = GetUniqueKey(dependencyObject);

                    PhoneApplicationService.Current.State[key] = scrollViewer.VerticalOffset;
                }
            }
            catch
            {
            }
        }

        public static void RestoreScrollViewerOffset(DependencyObject dependencyObject)
        {
            try
            {
                ScrollViewer scrollViewer = GetScrollViewer(dependencyObject);

                if (scrollViewer != null)
                {
                    string key = GetUniqueKey(dependencyObject);

                    if (PhoneApplicationService.Current.State.ContainsKey(key))
                    {
                        scrollViewer.ScrollToVerticalOffset((double)PhoneApplicationService.Current.State[key]);

                        PhoneApplicationService.Current.State.Remove(key);
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Private Methods

        private static ScrollViewer GetScrollViewer(DependencyObject dependencyObject)
        {
            ScrollViewer scrollViewer = null;

            if (dependencyObject is ScrollViewer)
            {
                scrollViewer = dependencyObject as ScrollViewer;
            }
            else
            {
                FrameworkElement frameworkElement = VisualTreeHelper.GetChild(dependencyObject, 0) as FrameworkElement;

                if (frameworkElement != null)
                {
                    scrollViewer = frameworkElement.FindName("ScrollViewer") as ScrollViewer;
                }
            }

            return scrollViewer;
        }

        private static PhoneApplicationPage GetPage(FrameworkElement frameworkElement)
        {
            PhoneApplicationPage phoneApplicationPage = null;

            while (frameworkElement != null)
            {
                if (frameworkElement.Parent is PhoneApplicationPage)
                {
                    phoneApplicationPage = frameworkElement.Parent as PhoneApplicationPage;
                    break;
                }

                frameworkElement = frameworkElement.Parent as FrameworkElement;
            }

            return phoneApplicationPage;
        }

        private static string GetUniqueKey(DependencyObject dependencyObject)
        {
            string key = "ScrollOffset";

            FrameworkElement frameworkElement = dependencyObject as FrameworkElement;

            if (frameworkElement != null)
            {
                PhoneApplicationPage page = GetPage(frameworkElement);

                key = page != null ? page.GetType().Name + frameworkElement.Name + "ScrollOffset" : frameworkElement.Name + "ScrollOffset";
            }

            return key;
        }

        #endregion
    }
}

using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using JabbR.Client.UI.WP8.Helpers;

namespace JabbR.Client.UI.WP8.Extensions
{
    public class ExtendedListBox : ListBox
    {
        // Compression states: Thanks to http://blogs.msdn.com/b/slmperf/archive/2011/06/30/windows-phone-mango-change-listbox-how-to-detect-compression-end-of-scroll-states.aspx
        public static DependencyProperty AtEndCommandProperty =
            DependencyProperty.RegisterAttached("AtEndCommand", typeof(ICommand), typeof(ExtendedListBox),
            new PropertyMetadata(default(ICommand)));

        public static readonly DependencyProperty IsBouncyProperty =
            DependencyProperty.Register("IsBouncy", typeof(bool), typeof(ExtendedListBox), new PropertyMetadata(default(bool)));

        public bool IsBouncy
        {
            get { return (bool)GetValue(IsBouncyProperty); }
            set { SetValue(IsBouncyProperty, value); }
        }

        public static ICommand GetAtEndCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(AtEndCommandProperty);
        }

        public static void SetAtEndCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(AtEndCommandProperty, value);
        }

        private bool alreadyHookedScrollEvents = false;

        public ExtendedListBox()
        {
            Loaded += ListBox_Loaded;
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollBar sb = null;
            ScrollViewer sv = null;
            if (alreadyHookedScrollEvents)
                return;

            alreadyHookedScrollEvents = true;
            AddHandler(ManipulationCompletedEvent, (EventHandler<ManipulationCompletedEventArgs>)LB_ManipulationCompleted, true);
            sb = (ScrollBar)FindElementRecursive(this, typeof(ScrollBar));
            sv = (ScrollViewer)FindElementRecursive(this, typeof(ScrollViewer));

            if (sv != null)
            {
                // Visual States are always on the first child of the control template 
                FrameworkElement element = VisualTreeHelper.GetChild(sv, 0) as FrameworkElement;
                if (element != null)
                {
                    VisualStateGroup vgroup = FindVisualState(element, "VerticalCompression");
                    VisualStateGroup hgroup = FindVisualState(element, "HorizontalCompression");
                    if (vgroup != null)
                        vgroup.CurrentStateChanging += vgroup_CurrentStateChanging;
                    if (hgroup != null)
                        hgroup.CurrentStateChanging += hgroup_CurrentStateChanging;
                }
            }

        }

        public delegate void OnCompression(object sender, CompressionEventArgs e);
        public event OnCompression Compression;

        private void hgroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionLeft")
            {
                if (Compression != null)
                    Compression(this, new CompressionEventArgs(CompressionType.Left));
            }

            if (e.NewState.Name == "CompressionRight")
            {
                if (Compression != null)
                    Compression(this, new CompressionEventArgs(CompressionType.Right));
            }
            if (e.NewState.Name == "NoHorizontalCompression")
            {
            }
        }

        private void vgroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionTop")
            {
                var atEnd = GetAtEndCommand(this);
                if (atEnd != null)
                {
                    StateManager.SaveScrollViewerOffset(this);
                    var item = Items.First();
                    atEnd.Execute(item);
                }
                if (Compression != null)
                    Compression(this, new CompressionEventArgs(CompressionType.Top));
            }
            if (e.NewState.Name == "CompressionBottom")
            {
                if (Compression != null)
                    Compression(this, new CompressionEventArgs(CompressionType.Bottom));
            }
            //if (e.NewState.Name == "NoVerticalCompression")
        }

        private void LB_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            //if (_isBouncy)
            //    _isBouncy = false;
        }

        private UIElement FindElementRecursive(FrameworkElement parent, Type targetType)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            UIElement returnElement = null;
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Object element = VisualTreeHelper.GetChild(parent, i);
                    if (element.GetType() == targetType)
                    {
                        return element as UIElement;
                    }
                    else
                    {
                        returnElement = FindElementRecursive(VisualTreeHelper.GetChild(parent, i) as FrameworkElement, targetType);
                    }
                }
            }
            return returnElement;
        }

        private VisualStateGroup FindVisualState(FrameworkElement element, string name)
        {
            if (element == null)
                return null;

            IList groups = VisualStateManager.GetVisualStateGroups(element);
            foreach (VisualStateGroup group in groups)
                if (group.Name == name)
                    return group;

            return null;
        }
    }

    public class CompressionEventArgs : System.EventArgs
    {
        public CompressionType Type { get; protected set; }

        public CompressionEventArgs(CompressionType type)
        {
            Type = type;
        }
    }

    public enum CompressionType { Top, Bottom, Left, Right };
}

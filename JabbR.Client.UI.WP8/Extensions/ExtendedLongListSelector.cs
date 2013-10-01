using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JabbR.Client.UI.WP8.Helpers;
using Microsoft.Phone.Controls;

namespace JabbR.Client.UI.WP8.Extensions
{
    public class ExtendedLongListSelector : LongListSelector
    {
        public ExtendedLongListSelector()
        {
            SelectionChanged += LongListSelector_SelectionChanged;
        }

        void LongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = base.SelectedItem;
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(ExtendedLongListSelector),
                new PropertyMetadata(null, OnSelectedItemChanged)
            );

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (LongListSelector)d;
            selector.SelectedItem = e.NewValue;
            selector.ScrollTo(e.NewValue);
        }

        public new object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
    }
}

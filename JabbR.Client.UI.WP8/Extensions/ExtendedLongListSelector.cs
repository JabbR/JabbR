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
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(ExtendedLongListSelector), new PropertyMetadata(default(object)));

        public ExtendedLongListSelector()
        {
            //SelectionChanged += (sender, args) =>
            //{
            //    SelectedItem = args.AddedItems[0];
            //};
        }
    }
}

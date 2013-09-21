using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Cirrious.MvvmCross.WindowsPhone.Views;
using System.Windows.Data;

namespace JabbR.Client.UI.WP8.Views
{
    public partial class RoomView : MvxPhonePage
    {
        public RoomView()
        {
            InitializeComponent();
        }

        private void Messages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                Messages.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            BindingExpression binding = textbox.GetBindingExpression(TextBox.TextProperty);
            binding.UpdateSource();
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((Pivot)sender).SelectedIndex)
            {
                case 0:
                    AppBar.IsVisible = true;
                    break;
                default:
                    AppBar.IsVisible = false;
                    break;
            }
        }
    }
}
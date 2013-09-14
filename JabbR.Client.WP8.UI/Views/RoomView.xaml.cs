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

namespace JabbR.Client.WP8.UI.Views
{
    public partial class RoomView : MvxPhonePage
    {
        public RoomView()
        {
            InitializeComponent();
        }

        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            BindingExpression binding = textbox.GetBindingExpression(TextBox.TextProperty);
            binding.UpdateSource();
        }
    }
}
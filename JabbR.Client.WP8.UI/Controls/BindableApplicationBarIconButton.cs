using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JabbR.Client.WP8.UI.Controls
{
    public class BindableApplicationBarIconButton : BindableApplicationBarMenuItem, IApplicationBarIconButton
    {
        public static readonly DependencyProperty IconUriProperty = DependencyProperty.RegisterAttached("IconUri", typeof(Uri), typeof(BindableApplicationBarMenuItem), new PropertyMetadata(OnIconUriChanged));

        private static void OnIconUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                ((BindableApplicationBarIconButton)d).Button.IconUri = (Uri)e.NewValue;
        }

        public BindableApplicationBarIconButton()
            : base()
        {
        }

        public ApplicationBarIconButton Button
        {
            get
            {
                return (ApplicationBarIconButton)Item;
            }
        }

        protected override IApplicationBarMenuItem CreateItem()
        {
            return new ApplicationBarIconButton();
        }

        public Uri IconUri
        {
            get
            {
                return (Uri)GetValue(IconUriProperty);
            }
            set
            {
                SetValue(IconUriProperty, value);
            }
        }
    }
}

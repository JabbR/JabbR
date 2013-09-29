using System;
using System.Windows.Data;

namespace JabbR.Client.UI.WP8.Converters
{
    public class HastToGravatarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new Uri("https://secure.gravatar.com/avatar/7bc4ab95147100a4ff7f92596c818b6c?s=100&d=mm");
            }
            return new Uri(String.Format("https://secure.gravatar.com/avatar/{0}?d=identicon", value.ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

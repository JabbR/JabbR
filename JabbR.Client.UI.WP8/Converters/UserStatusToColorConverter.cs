using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace JabbR.Client.UI.WP8.Converters
{
    public class UserStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new SolidColorBrush(Colors.Gray);
            }

            Color color;
            switch(value.ToString().ToLower())
            {
                case "active":
                    color = Colors.Green;
                    break;
                case "inactive":
                    color = Colors.Orange;
                    break;
                case "offline":
                default:
                    color = Colors.Gray;
                    break;
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

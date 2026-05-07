using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PeerChat.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (status == "Online")
                return new SolidColorBrush(Color.FromRgb(0, 200, 83)); // Green
            else
                return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
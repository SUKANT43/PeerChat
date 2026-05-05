using PeerChat.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PeerChat.Converters
{
    public class MessageAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var direction = (MessageDirection)value;
            return direction == MessageDirection.Sent ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using PeerChat.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PeerChat.Converters
{
    public class MessageTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var direction = (MessageDirection)value;

            return direction == MessageDirection.Sent
                ? System.Windows.Application.Current.Resources["ChatSentTextBrush"]
                : System.Windows.Application.Current.Resources["ChatReceivedTextBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
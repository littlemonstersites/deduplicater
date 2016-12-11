using System;
using System.Linq;
using System.Windows.Data;

namespace DeDuplicate.Ui.Converters
{
    public class ShortTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;

            if (string.IsNullOrEmpty(text))
                return text;

            int textLength = text.Count();
            if (textLength > 50)
            {
                return "..." + text.Substring(textLength - 50);
            }
            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

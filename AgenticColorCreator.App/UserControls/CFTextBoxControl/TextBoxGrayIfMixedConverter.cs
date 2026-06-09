using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFTextBoxControl
{
    public class TextBoxGrayIfMixedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMixed && isMixed)
            {
                return Brushes.Gray;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

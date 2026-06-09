using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AgenticColorCreator.App.UserControls.CFTextBoxControl
{
    // Shows the placeholder if IsMixedState == true && Text == ""
    public class MixedVisualVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;
// values[0]: IsMixedState (bool), values[1]: IsEditing (bool)
if (values[0] is bool mixed && mixed && values[1] is bool editing && !editing)
    return Visibility.Visible;
return Visibility.Collapsed;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

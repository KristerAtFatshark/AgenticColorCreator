using System;
using System.Globalization;
using System.Windows.Data;

namespace AgenticColorCreator.App.UserControls.CFColorControl;

public sealed class CFColorHexToRgbConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is string hexValue && CFColor.TryConvertHexToRgb(hexValue, out var rgb)
			? rgb
			: default(CFColorRgb);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is CFColorRgb rgbValue
			? CFColor.ConvertRgbToHex(rgbValue)
			: Binding.DoNothing;
	}
}

public sealed class CFColorHexToHsvConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is string hexValue && CFColor.TryConvertHexToHsv(hexValue, out var hsv)
			? hsv
			: default(CFColorHsv);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is CFColorHsv hsvValue
			? CFColor.ConvertHsvToHex(hsvValue)
			: Binding.DoNothing;
	}
}

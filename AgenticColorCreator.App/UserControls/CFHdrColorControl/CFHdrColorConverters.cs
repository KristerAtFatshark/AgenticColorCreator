using System;
using System.Globalization;
using System.Windows.Data;

namespace AgenticColorCreator.App.UserControls.CFHdrColorControl;

public sealed class CFHdrColorHexToRgbConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var stops = parameter is double doubleStops ? doubleStops : 0d;
		return value is string hexValue && CFHdrColor.TryConvertHexToRgb(hexValue, stops, out var rgb)
			? rgb
			: default(CFHdrColorRgb);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is CFHdrColorRgb rgbValue
			? CFHdrColor.ConvertRgbToHex(rgbValue)
			: Binding.DoNothing;
	}
}

public sealed class CFHdrColorHexToHsvConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var stops = parameter is double doubleStops ? doubleStops : 0d;
		return value is string hexValue && CFHdrColor.TryConvertHexToHsv(hexValue, stops, out var hsv)
			? hsv
			: default(CFHdrColorHsv);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is CFHdrColorHsv hsvValue
			? CFHdrColor.ConvertHsvToHex(hsvValue)
			: Binding.DoNothing;
	}
}

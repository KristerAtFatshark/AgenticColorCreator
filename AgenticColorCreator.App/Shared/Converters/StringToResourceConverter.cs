using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClownFishUi.Converters
{
	public class StringToResourceConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not string key)
			{
				return null;
			}

			var app = Application.Current;
			if (app == null)
			{
				return "\ue903";
			}

			var result = app.TryFindResource(key);
			return result ?? "\ue903";
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return null;
		}
	}
}

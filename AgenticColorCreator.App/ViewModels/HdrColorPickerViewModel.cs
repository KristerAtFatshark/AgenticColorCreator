using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.App.UserControls.CFHdrColorControl;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.ViewModels;

public sealed class HdrColorPickerViewModel : ViewModelBase
{
	private const double HueWheelOuterRadius = 100d;
	private const double HueWheelInnerRadius = 80d;
	private const double HueWheelCenter = 110d;
	private const double HueWheelOuterOutlineMargin = 8.5d;
	private const double HueWheelInnerOutlineMargin = 30d;
	private const double HueSelectorRadiusOffset = -1.5d;
	private const double SaturationValueSize = 98d;
	private const double HueSelectorSize = 14d;
	private const double SaturationValueSelectorSize = 10d;

	private bool _isSynchronizing;
	private int _alpha;
	private int _red;
	private int _green;
	private int _blue;
	private double _hue;
	private double _saturation;
	private double _value;
	private string _hexValue;
	private double _stops;
	private readonly Brush _hueWheelBrush;

	public HdrColorPickerViewModel(string initialHexValue, double initialStops)
	{
		_hueWheelBrush = HueWheelBrushFactory.Create(220, HueWheelOuterRadius, HueWheelInnerRadius);
		_stops = initialStops;
		_hexValue = ColorHexParser.TryParseArgb(initialHexValue, out var color)
			? ColorHexParser.Normalize(initialHexValue)
			: "#FFFFFFFF";

		ApplyArgb(color.A, color.R, color.G, color.B, true);
	}

	public int Alpha
	{
		get => _alpha;
		set => SetRgbChannel(ref _alpha, value, nameof(Alpha), nameof(AlphaText), false);
	}

	public int Red
	{
		get => _red;
		set => SetRgbChannel(ref _red, value, nameof(Red), nameof(RedText), true);
	}

	public int Green
	{
		get => _green;
		set => SetRgbChannel(ref _green, value, nameof(Green), nameof(GreenText), true);
	}

	public int Blue
	{
		get => _blue;
		set => SetRgbChannel(ref _blue, value, nameof(Blue), nameof(BlueText), true);
	}

	public double Hue
	{
		get => _hue;
		set
		{
			var normalized = value % 360d;
			if (normalized < 0)
			{
				normalized += 360d;
			}

			if (!SetProperty(ref _hue, normalized))
			{
				return;
			}

			if (_isSynchronizing)
			{
				RaiseVisualProperties();
				return;
			}

			UpdateFromHsv();
		}
	}

	public double Saturation
	{
		get => _saturation;
		set => SetHsvChannel(ref _saturation, Math.Clamp(value, 0d, 1d), nameof(Saturation), nameof(SaturationText));
	}

	public double ValueComponent
	{
		get => _value;
		set => SetHsvChannel(ref _value, Math.Clamp(value, 0d, 1d), nameof(ValueComponent), nameof(ValueText));
	}

	public double Stops
	{
		get => _stops;
		set
		{
			if (!SetProperty(ref _stops, value))
			{
				return;
			}

			OnPropertyChanged(nameof(StopsText));
			RaiseVisualProperties();
		}
	}

	public string AlphaText
	{
		get => Alpha.ToString(CultureInfo.InvariantCulture);
		set => SetByteTextChannel(value, channel => Alpha = channel);
	}

	public string RedText
	{
		get => Red.ToString(CultureInfo.InvariantCulture);
		set => SetByteTextChannel(value, channel => Red = channel);
	}

	public string GreenText
	{
		get => Green.ToString(CultureInfo.InvariantCulture);
		set => SetByteTextChannel(value, channel => Green = channel);
	}

	public string BlueText
	{
		get => Blue.ToString(CultureInfo.InvariantCulture);
		set => SetByteTextChannel(value, channel => Blue = channel);
	}

	public string HueText
	{
		get => Hue.ToString("0.##", CultureInfo.InvariantCulture);
		set => SetDoubleTextChannel(value, parsed => Hue = parsed);
	}

	public string SaturationText
	{
		get => (Saturation * 100d).ToString("0.##", CultureInfo.InvariantCulture);
		set => SetPercentageTextChannel(value, parsed => Saturation = parsed / 100d);
	}

	public string ValueText
	{
		get => (ValueComponent * 100d).ToString("0.##", CultureInfo.InvariantCulture);
		set => SetPercentageTextChannel(value, parsed => ValueComponent = parsed / 100d);
	}

	public string StopsText
	{
		get => Stops.ToString("0.00", CultureInfo.InvariantCulture);
		set => SetDoubleTextChannel(value, parsed => Stops = parsed);
	}

	public string HexValue
	{
		get => _hexValue;
		set
		{
			if (!SetProperty(ref _hexValue, value))
			{
				return;
			}

			if (_isSynchronizing)
			{
				RaiseVisualProperties();
				return;
			}

			if (!ColorHexParser.TryParseArgb(value, out var color))
			{
				RaiseVisualProperties();
				return;
			}

			ApplyArgb(color.A, color.R, color.G, color.B, true);
		}
	}

	public Brush PreviewBrush => CreateBrushFromHex(HexValue);

	public Brush HdrPreviewBrush
	{
		get
		{
			if (!ColorHexParser.TryParseArgb(HexValue, out var color))
			{
				return Brushes.Transparent;
			}

			var hdrColor = ColorOperations.WpfColorToHdr(Color.FromArgb(color.A, color.R, color.G, color.B), Stops);
			var brush = new SolidColorBrush(hdrColor);
			brush.Freeze();
			return brush;
		}
	}

	public Brush HueWheelBrush => _hueWheelBrush;

	public Brush SaturationValueBrush
	{
		get
		{
			var rgb = ColorSpaceConverter.HsvToRgb(Hue, 1d, 1d);
			var brush = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0.5),
				EndPoint = new Point(1, 0.5),
			};
			brush.GradientStops.Add(new GradientStop(Colors.White, 0));
			brush.GradientStops.Add(new GradientStop(Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue), 1));
			brush.Freeze();
			return brush;
		}
	}

	public Point HueSelectorPoint
	{
		get
		{
			var angle = (Hue - 90d) * Math.PI / 180d;
			var outerOutlineRadius = HueWheelCenter - HueWheelOuterOutlineMargin;
			var innerOutlineRadius = HueWheelCenter - HueWheelInnerOutlineMargin;
			var radius = ((outerOutlineRadius + innerOutlineRadius) / 2d) + HueSelectorRadiusOffset;
			return new Point(
				HueWheelCenter + (Math.Cos(angle) * radius) - (HueSelectorSize / 2d),
				HueWheelCenter + (Math.Sin(angle) * radius) - (HueSelectorSize / 2d));
		}
	}

	public Point SaturationValueSelectorPoint => new(
		(Saturation * SaturationValueSize) - (SaturationValueSelectorSize / 2d),
		((1d - ValueComponent) * SaturationValueSize) - (SaturationValueSelectorSize / 2d));

	public void UpdateHueFromPoint(Point point, Size renderSize)
	{
		var center = new Point(renderSize.Width / 2d, renderSize.Height / 2d);
		var angle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180d / Math.PI;
		Hue = angle + 90d;
	}

	public void UpdateSaturationValueFromPoint(Point point, Size renderSize)
	{
		if (renderSize.Width <= 0 || renderSize.Height <= 0)
		{
			return;
		}

		Saturation = Math.Clamp(point.X / renderSize.Width, 0d, 1d);
		ValueComponent = Math.Clamp(1d - (point.Y / renderSize.Height), 0d, 1d);
	}

	private static Brush CreateBrushFromHex(string hexValue)
	{
		if (!ColorHexParser.TryParseArgb(hexValue, out var color))
		{
			return Brushes.Transparent;
		}

		var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
		brush.Freeze();
		return brush;
	}

	private void SetRgbChannel(ref int field, int value, string propertyName, string textPropertyName, bool updateHsv)
	{
		value = Math.Clamp(value, 0, 255);

		if (!SetProperty(ref field, value, propertyName))
		{
			return;
		}

		OnPropertyChanged(textPropertyName);

		if (_isSynchronizing)
		{
			RaiseVisualProperties();
			return;
		}

		if (updateHsv)
		{
			UpdateHsvFromRgb();
		}

		UpdateHexFromChannels();
		RaiseVisualProperties();
	}

	private void SetHsvChannel(ref double field, double value, string propertyName, string textPropertyName)
	{
		if (!SetProperty(ref field, value, propertyName))
		{
			return;
		}

		OnPropertyChanged(textPropertyName);

		if (_isSynchronizing)
		{
			RaiseVisualProperties();
			return;
		}

		UpdateFromHsv();
	}

	private void UpdateFromHsv()
	{
		var rgb = ColorSpaceConverter.HsvToRgb(Hue, Saturation, ValueComponent);
		_isSynchronizing = true;
		try
		{
			_red = rgb.Red;
			_green = rgb.Green;
			_blue = rgb.Blue;
			RaiseRgbProperties();
			UpdateHexFromChannels();
		}
		finally
		{
			_isSynchronizing = false;
		}

		RaiseVisualProperties();
	}

	private void ApplyArgb(byte alpha, byte red, byte green, byte blue, bool normalizeHex)
	{
		_isSynchronizing = true;
		try
		{
			_alpha = alpha;
			_red = red;
			_green = green;
			_blue = blue;
			UpdateHsvFromRgb();
			RaiseRgbProperties();
			if (normalizeHex)
			{
				_hexValue = $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
				OnPropertyChanged(nameof(HexValue));
			}
		}
		finally
		{
			_isSynchronizing = false;
		}

		RaiseVisualProperties();
	}

	private void UpdateHsvFromRgb()
	{
		var hsv = ColorSpaceConverter.RgbToHsv((byte)Red, (byte)Green, (byte)Blue);
		_hue = hsv.Hue;
		_saturation = hsv.Saturation;
		_value = hsv.Value;
		RaiseHsvProperties();
	}

	private void RaiseRgbProperties()
	{
		OnPropertyChanged(nameof(Alpha));
		OnPropertyChanged(nameof(Red));
		OnPropertyChanged(nameof(Green));
		OnPropertyChanged(nameof(Blue));
		OnPropertyChanged(nameof(AlphaText));
		OnPropertyChanged(nameof(RedText));
		OnPropertyChanged(nameof(GreenText));
		OnPropertyChanged(nameof(BlueText));
	}

	private void RaiseHsvProperties()
	{
		OnPropertyChanged(nameof(Hue));
		OnPropertyChanged(nameof(Saturation));
		OnPropertyChanged(nameof(ValueComponent));
		OnPropertyChanged(nameof(HueText));
		OnPropertyChanged(nameof(SaturationText));
		OnPropertyChanged(nameof(ValueText));
	}

	private void RaiseVisualProperties()
	{
		OnPropertyChanged(nameof(PreviewBrush));
		OnPropertyChanged(nameof(HdrPreviewBrush));
		OnPropertyChanged(nameof(HueWheelBrush));
		OnPropertyChanged(nameof(SaturationValueBrush));
		OnPropertyChanged(nameof(HueSelectorPoint));
		OnPropertyChanged(nameof(SaturationValueSelectorPoint));
	}

	private void SetByteTextChannel(string value, Action<int> setter)
	{
		if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
		{
			setter(parsed);
		}
	}

	private void SetDoubleTextChannel(string value, Action<double> setter)
	{
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
		{
			setter(parsed);
		}
	}

	private void SetPercentageTextChannel(string value, Action<double> setter)
	{
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
		{
			setter(Math.Clamp(parsed, 0d, 100d));
		}
	}

	private void UpdateHexFromChannels()
	{
		_hexValue = $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
		OnPropertyChanged(nameof(HexValue));
	}
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.UserControls.CFColorControl;

public partial class CFColor : UserControl
{
	private static readonly Brush TransparentBrush = TransparentCheckerBrushFactory.Create();

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(string),
		typeof(CFColor),
		new FrameworkPropertyMetadata("#FFFFFFFF", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

	public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
		nameof(ValueChanged),
		RoutingStrategy.Bubble,
		typeof(RoutedEventHandler),
		typeof(CFColor));

	public CFColor()
	{
		InitializeComponent();
		RefreshVisuals();
		IsEnabledChanged += OnControlIsEnabledChanged;
	}

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public event RoutedEventHandler ValueChanged
	{
		add => AddHandler(ValueChangedEvent, value);
		remove => RemoveHandler(ValueChangedEvent, value);
	}

	public static bool TryConvertHexToRgb(string? hexValue, out CFColorRgb rgb)
	{
		rgb = default;

		if (!ColorHexParser.TryParseArgb(hexValue, out var color))
		{
			return false;
		}

		rgb = new CFColorRgb(color.R, color.G, color.B, color.A);
		return true;
	}

	public static bool TryConvertHexToHsv(string? hexValue, out CFColorHsv hsv)
	{
		hsv = default;

		if (!ColorHexParser.TryParseArgb(hexValue, out var color))
		{
			return false;
		}

		var hsvColor = ColorSpaceConverter.RgbToHsv(color.R, color.G, color.B);
		hsv = new CFColorHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value, color.A);
		return true;
	}

	public static string ConvertRgbToHex(CFColorRgb rgb)
	{
		return $"#{rgb.Alpha:X2}{rgb.Red:X2}{rgb.Green:X2}{rgb.Blue:X2}";
	}

	public static string ConvertHsvToHex(CFColorHsv hsv)
	{
		var rgb = ColorSpaceConverter.HsvToRgb(hsv.Hue, hsv.Saturation, hsv.Value);
		return $"#{hsv.Alpha:X2}{rgb.Red:X2}{rgb.Green:X2}{rgb.Blue:X2}";
	}

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFColor colorControl)
		{
			return;
		}

		colorControl.OnValueChanged();
	}

	private void OnValueChanged()
	{
		RefreshVisuals();
		RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
	}

	private void RefreshVisuals()
	{
		CheckerBorder.Background = TransparentBrush;
		UpdateBorderVisuals();

		if (ColorHexParser.TryParseArgb(Value, out var color))
		{
			var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
			brush.Freeze();
			ColorOverlayBorder.Background = brush;
			return;
		}

		ColorOverlayBorder.Background = Brushes.Transparent;
	}

	private void UpdateBorderVisuals()
	{
		if (!IsEnabled)
		{
			CheckerBorder.BorderBrush = (Brush)FindResource("CF.TextBox.Disabled.Border");
			HexTextBox.Background = (Brush)FindResource("CF.CFColor.Disabled.Background");
			HexTextBox.Foreground = (Brush)FindResource("CF.CFColor.Disabled.Foreground");
			return;
		}

		HexTextBox.Background = (Brush)FindResource("CF.CFColor.Default.Background");
		HexTextBox.Foreground = (Brush)FindResource("CF.CFColor.Default.Foreground");

		if (HexTextBox.IsKeyboardFocused)
		{
			CheckerBorder.BorderBrush = (Brush)FindResource("CF.CFColor.Selected.Border");
			return;
		}

		if (HexTextBox.IsMouseOver || OpenPickerButton.IsMouseOver)
		{
			CheckerBorder.BorderBrush = (Brush)FindResource("CF.CFColor.MouseOver.Border");
			return;
		}

		CheckerBorder.BorderBrush = (Brush)FindResource("CF.CFColor.Default.Border");
	}

	private void OnControlIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		UpdateBorderVisuals();
	}

	private void OnSwatchInteractionChanged(object sender, RoutedEventArgs e)
	{
		UpdateBorderVisuals();
	}

	private void OnTextInteractionChanged(object sender, RoutedEventArgs e)
	{
		UpdateBorderVisuals();
	}

	private void OnTextInteractionChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		UpdateBorderVisuals();
	}

	private void OpenPickerButton_Click(object sender, RoutedEventArgs e)
	{
		var owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
		var dialogService = new ColorPickerDialogService();
		var selectedColor = dialogService.PickColor(Value, owner);

		if (!string.IsNullOrWhiteSpace(selectedColor))
		{
			Value = selectedColor;
		}
	}
}

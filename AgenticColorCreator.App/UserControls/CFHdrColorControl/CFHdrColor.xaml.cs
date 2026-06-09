using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.App.UserControls.CFColorControl;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.UserControls.CFHdrColorControl
{
	public partial class CFHdrColor : UserControl
	{
		private const string DefaultColorValue = "#FF000000";
		private const double DefaultStopsValue = 0d;

		public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
			name: nameof(IsMixedState),
			typeof(bool),
			typeof(CFHdrColor),
			new PropertyMetadata(false, OnIsMixedStateChanged));

		private static readonly Brush TransparentBrush = TransparentCheckerBrushFactory.Create();

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			name: nameof(Value),
			typeof(string),
			typeof(CFHdrColor),
			new FrameworkPropertyMetadata(DefaultColorValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

		public static readonly DependencyProperty StopsProperty = DependencyProperty.Register(
			name: nameof(Stops),
			typeof(double),
			typeof(CFHdrColor),
			new FrameworkPropertyMetadata(DefaultStopsValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnStopsChanged));

		public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
			name: nameof(ValueChanged),
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(CFHdrColor));

		public CFHdrColor()
		{
			InitializeComponent();
			RefreshVisuals();
			IsEnabledChanged += OnControlIsEnabledChanged;
			Loaded += (s, e) =>
			{
				if (StopsInput != null)
				{
					StopsInput.LostFocus += StopsInput_LostFocus;
				}
			};
		}

		public bool IsMixedState
		{
			get => (bool)GetValue(IsMixedStateProperty);
			set => SetValue(IsMixedStateProperty, value);
		}

		public string Value
		{
			get => (string)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public double Stops
		{
			get => (double)GetValue(StopsProperty);
			set => SetValue(StopsProperty, value);
		}

		public event RoutedEventHandler ValueChanged
		{
			add => AddHandler(ValueChangedEvent, value);
			remove => RemoveHandler(ValueChangedEvent, value);
		}

		private static void OnIsMixedStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not CFHdrColor colorControl)
			{
				return;
			}

			colorControl.RefreshVisuals();
		}

		private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not CFHdrColor colorControl) return;
			colorControl.OnValueChanged();
		}

		private static void OnStopsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not CFHdrColor colorControl) return;
			colorControl.RefreshVisuals();
			colorControl.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
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
			if (ColorHexParser.TryParseArgb(GetDisplayValue(), out var color))
			{
				var sdrColor = Color.FromArgb(color.A, color.R, color.G, color.B);
				var hdrColor = ColorOperations.WpfColorToHdr(sdrColor, GetDisplayStops());
				var brush = new SolidColorBrush(hdrColor);
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
			if (HexTextBox.IsKeyboardFocused || StopsInput.IsKeyboardFocused)
			{
				CheckerBorder.BorderBrush = (Brush)FindResource("CF.CFColor.Selected.Border");
				return;
			}
			if (HexTextBox.IsMouseOver || StopsInput.IsMouseOver || OpenPickerButton.IsMouseOver)
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
			var dialog = new HdrColorPickerWindow(GetDisplayValue(), GetDisplayStops()) { Owner = owner };
			if (dialog.ShowDialog() != true) return;

			if (IsMixedState)
			{
				var changedColor = !string.Equals(dialog.SelectedHexValue, DefaultColorValue, System.StringComparison.OrdinalIgnoreCase);

				if (changedColor)
				{
					Value = dialog.SelectedHexValue;
					Stops = dialog.SelectedStops;
					IsMixedState = false;
				}

				return;
			}

			Value = dialog.SelectedHexValue;
			Stops = dialog.SelectedStops;
		}

		public static bool TryConvertHexToRgb(string? hexValue, double stops, out CFHdrColorRgb rgb)
		{
			rgb = default;
			if (!ColorHexParser.TryParseArgb(hexValue, out var color)) return false;
			var hdrColor = ColorOperations.WpfColorToHdr(Color.FromArgb(color.A, color.R, color.G, color.B), stops);
			rgb = new CFHdrColorRgb(hdrColor.R, hdrColor.G, hdrColor.B, hdrColor.A, stops);
			return true;
		}
		public static bool TryConvertHexToHsv(string? hexValue, double stops, out CFHdrColorHsv hsv)
		{
			hsv = default;
			if (!ColorHexParser.TryParseArgb(hexValue, out var color)) return false;
			var hdrColor = ColorOperations.WpfColorToHdr(Color.FromArgb(color.A, color.R, color.G, color.B), stops);
			var hsvColor = ColorSpaceConverter.RgbToHsv(hdrColor.R, hdrColor.G, hdrColor.B);
			hsv = new CFHdrColorHsv(hsvColor.Hue, hsvColor.Saturation, hsvColor.Value, hdrColor.A, stops);
			return true;
		}
		public static string ConvertRgbToHex(CFHdrColorRgb rgb)
		{
			var hdrColor = Color.FromArgb(rgb.Alpha, rgb.Red, rgb.Green, rgb.Blue);
			var sdrColor = ColorOperations.HdrToWpfColor(hdrColor, rgb.Stops);
			return $"#{sdrColor.A:X2}{sdrColor.R:X2}{sdrColor.G:X2}{sdrColor.B:X2}";
		}
		public static string ConvertHsvToHex(CFHdrColorHsv hsv)
		{
			var hdrRgb = ColorSpaceConverter.HsvToRgb(hsv.Hue, hsv.Saturation, hsv.Value);
			var hdrColor = Color.FromArgb(hsv.Alpha, hdrRgb.Red, hdrRgb.Green, hdrRgb.Blue);
			var sdrColor = ColorOperations.HdrToWpfColor(hdrColor, hsv.Stops);
			return $"#{sdrColor.A:X2}{sdrColor.R:X2}{sdrColor.G:X2}{sdrColor.B:X2}";
		}

		private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			IsMixedState = false;
		}
			private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
			{
				// Mixed state now only set externally
			}
			private void StopsInput_LostFocus(object sender, RoutedEventArgs e)
			{
				// Mixed state now only set externally
			}

		private string GetDisplayValue()
		{
			if (IsMixedState)
			{
				return DefaultColorValue;
			}

			return ColorHexParser.TryParseArgb(Value, out _) ? Value : DefaultColorValue;
		}

		private double GetDisplayStops()
		{
			return IsMixedState ? DefaultStopsValue : Stops;
		}

	}
}

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

		if (ColorHexParser.TryParseArgb(Value, out var color))
		{
			var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
			brush.Freeze();
			ColorOverlayBorder.Background = brush;
			HexTextBlock.Text = ColorHexParser.Normalize(Value);
			return;
		}

		ColorOverlayBorder.Background = Brushes.Transparent;
		HexTextBlock.Text = Value;
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

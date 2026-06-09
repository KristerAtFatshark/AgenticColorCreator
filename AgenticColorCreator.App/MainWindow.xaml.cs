using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using AgenticColorCreator.App.UserControls.CFColorControl;
using AgenticColorCreator.App.UserControls.CFHdrColorControl;
using AgenticColorCreator.App.UserControls.CFTreeViewControl;
using AgenticColorCreator.App.ViewModels;

namespace AgenticColorCreator.App;

public partial class MainWindow : Window
{
	// Mixed state preview toggles for all custom controls
	public static readonly DependencyProperty PreviewCFTextBoxIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFTextBoxIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
	public static readonly DependencyProperty PreviewCFIntIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFIntIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
	public static readonly DependencyProperty PreviewCFFloatIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFFloatIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
	public static readonly DependencyProperty PreviewCFColorIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFColorIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
	public static readonly DependencyProperty PreviewCFHdrColorIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFHdrColorIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
	public static readonly DependencyProperty PreviewCFTreeViewIsMixedProperty =
		DependencyProperty.Register(
			nameof(PreviewCFTreeViewIsMixed), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

	public static readonly DependencyProperty PreviewTextBoxValueProperty = DependencyProperty.Register(
		nameof(PreviewTextBoxValue),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("My Text"));

	public static readonly DependencyProperty PreviewColorValueProperty = DependencyProperty.Register(
		nameof(PreviewColorValue),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("#80FF6600", OnPreviewColorValueChanged));

	public static readonly DependencyProperty PreviewColorValueRgbTextProperty = DependencyProperty.Register(
		nameof(PreviewColorValueRgbText),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("RGBA: 255, 102, 0, 128"));

	public static readonly DependencyProperty PreviewColorValueHsvTextProperty = DependencyProperty.Register(
		nameof(PreviewColorValueHsvText),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("HSVA: 24, 1, 1, 128"));

	public static readonly DependencyProperty PreviewHdrColorValueProperty = DependencyProperty.Register(
		nameof(PreviewHdrColorValue),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("#80FF6600", OnPreviewHdrColorChanged));

	public static readonly DependencyProperty PreviewHdrColorStopsProperty = DependencyProperty.Register(
		nameof(PreviewHdrColorStops),
		typeof(double),
		typeof(MainWindow),
		new PropertyMetadata(1d, OnPreviewHdrColorChanged));

	public static readonly DependencyProperty PreviewHdrColorValueRgbTextProperty = DependencyProperty.Register(
		nameof(PreviewHdrColorValueRgbText),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("HDR RGBA: 255, 204, 0, 128 | Stops: 1"));

	public static readonly DependencyProperty PreviewHdrColorValueHsvTextProperty = DependencyProperty.Register(
		nameof(PreviewHdrColorValueHsvText),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("HDR HSVA: 24, 1, 1, 128 | Stops: 1"));

	public static readonly DependencyProperty PreviewNumberValueProperty = DependencyProperty.Register(
		nameof(PreviewNumberValue),
		typeof(int),
		typeof(MainWindow),
		new PropertyMetadata(12));

	public static readonly DependencyProperty PreviewIntStepProperty = DependencyProperty.Register(
		nameof(PreviewIntStep),
		typeof(int),
		typeof(MainWindow),
		new PropertyMetadata(1));

	public static readonly DependencyProperty PreviewFloatValueProperty = DependencyProperty.Register(
		nameof(PreviewFloatValue),
		typeof(float),
		typeof(MainWindow),
		new PropertyMetadata(12.5f));

	public static readonly DependencyProperty PreviewFloatMinimumProperty = DependencyProperty.Register(
		nameof(PreviewFloatMinimum),
		typeof(float),
		typeof(MainWindow),
		new PropertyMetadata(0f));

	public static readonly DependencyProperty PreviewFloatMaximumProperty = DependencyProperty.Register(
		nameof(PreviewFloatMaximum),
		typeof(float),
		typeof(MainWindow),
		new PropertyMetadata(100f));

	public static readonly DependencyProperty PreviewFloatDecimalsProperty = DependencyProperty.Register(
		nameof(PreviewFloatDecimals),
		typeof(int),
		typeof(MainWindow),
		new PropertyMetadata(2));

	public static readonly DependencyProperty PreviewFloatStepProperty = DependencyProperty.Register(
		nameof(PreviewFloatStep),
		typeof(float),
		typeof(MainWindow),
		new PropertyMetadata(0.25f));

	public static readonly DependencyProperty SelectedPreviewTreeViewItemsProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewItems),
		typeof(IReadOnlyList<CFTreeViewItem>),
		typeof(MainWindow),
		new PropertyMetadata(null, OnSelectedPreviewTreeViewItemsChanged));

	public static readonly DependencyProperty SelectedPreviewTreeViewValuesProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewValues),
		typeof(ObservableCollection<string>),
		typeof(MainWindow),
		new PropertyMetadata(null));

	public static readonly DependencyProperty SelectedPreviewTreeViewDetailsProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewDetails),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("Selected: none"));

	public MainWindow()
	{
		SelectedPreviewTreeViewValues = [];
		InitializeComponent();
		Closing += OnClosing;
	}

	public IReadOnlyList<CFTreeViewItem>? SelectedPreviewTreeViewItems
	{
		get => (IReadOnlyList<CFTreeViewItem>?)GetValue(SelectedPreviewTreeViewItemsProperty);
		set => SetValue(SelectedPreviewTreeViewItemsProperty, value);
	}

	// CLR wrappers for mixed state preview toggles
	public bool PreviewCFTextBoxIsMixed
	{
		get => (bool)GetValue(PreviewCFTextBoxIsMixedProperty);
		set => SetValue(PreviewCFTextBoxIsMixedProperty, value);
	}
	public bool PreviewCFIntIsMixed
	{
		get => (bool)GetValue(PreviewCFIntIsMixedProperty);
		set => SetValue(PreviewCFIntIsMixedProperty, value);
	}
	public bool PreviewCFFloatIsMixed
	{
		get => (bool)GetValue(PreviewCFFloatIsMixedProperty);
		set => SetValue(PreviewCFFloatIsMixedProperty, value);
	}
	public bool PreviewCFColorIsMixed
	{
		get => (bool)GetValue(PreviewCFColorIsMixedProperty);
		set => SetValue(PreviewCFColorIsMixedProperty, value);
	}
	public bool PreviewCFHdrColorIsMixed
	{
		get => (bool)GetValue(PreviewCFHdrColorIsMixedProperty);
		set => SetValue(PreviewCFHdrColorIsMixedProperty, value);
	}
	public bool PreviewCFTreeViewIsMixed
	{
		get => (bool)GetValue(PreviewCFTreeViewIsMixedProperty);
		set => SetValue(PreviewCFTreeViewIsMixedProperty, value);
	}

	public string PreviewTextBoxValue
	{
		get => (string)GetValue(PreviewTextBoxValueProperty);
		set => SetValue(PreviewTextBoxValueProperty, value);
	}

	public string PreviewColorValue
	{
		get => (string)GetValue(PreviewColorValueProperty);
		set => SetValue(PreviewColorValueProperty, value);
	}

	public string PreviewColorValueRgbText
	{
		get => (string)GetValue(PreviewColorValueRgbTextProperty);
		set => SetValue(PreviewColorValueRgbTextProperty, value);
	}

	public string PreviewColorValueHsvText
	{
		get => (string)GetValue(PreviewColorValueHsvTextProperty);
		set => SetValue(PreviewColorValueHsvTextProperty, value);
	}

	public string PreviewHdrColorValue
	{
		get => (string)GetValue(PreviewHdrColorValueProperty);
		set => SetValue(PreviewHdrColorValueProperty, value);
	}

	public double PreviewHdrColorStops
	{
		get => (double)GetValue(PreviewHdrColorStopsProperty);
		set => SetValue(PreviewHdrColorStopsProperty, value);
	}

	public string PreviewHdrColorValueRgbText
	{
		get => (string)GetValue(PreviewHdrColorValueRgbTextProperty);
		set => SetValue(PreviewHdrColorValueRgbTextProperty, value);
	}

	public string PreviewHdrColorValueHsvText
	{
		get => (string)GetValue(PreviewHdrColorValueHsvTextProperty);
		set => SetValue(PreviewHdrColorValueHsvTextProperty, value);
	}

	public int PreviewNumberValue
	{
		get => (int)GetValue(PreviewNumberValueProperty);
		set => SetValue(PreviewNumberValueProperty, value);
	}

	public int PreviewIntStep
	{
		get => (int)GetValue(PreviewIntStepProperty);
		set => SetValue(PreviewIntStepProperty, value);
	}

	public float PreviewFloatValue
	{
		get => (float)GetValue(PreviewFloatValueProperty);
		set => SetValue(PreviewFloatValueProperty, value);
	}

	public float PreviewFloatMinimum
	{
		get => (float)GetValue(PreviewFloatMinimumProperty);
		set => SetValue(PreviewFloatMinimumProperty, value);
	}

	public float PreviewFloatMaximum
	{
		get => (float)GetValue(PreviewFloatMaximumProperty);
		set => SetValue(PreviewFloatMaximumProperty, value);
	}

	public int PreviewFloatDecimals
	{
		get => (int)GetValue(PreviewFloatDecimalsProperty);
		set => SetValue(PreviewFloatDecimalsProperty, value);
	}

	public float PreviewFloatStep
	{
		get => (float)GetValue(PreviewFloatStepProperty);
		set => SetValue(PreviewFloatStepProperty, value);
	}

	public ObservableCollection<string> SelectedPreviewTreeViewValues
	{
		get => (ObservableCollection<string>)GetValue(SelectedPreviewTreeViewValuesProperty);
		set => SetValue(SelectedPreviewTreeViewValuesProperty, value);
	}

	public string SelectedPreviewTreeViewDetails
	{
		get => (string)GetValue(SelectedPreviewTreeViewDetailsProperty);
		set => SetValue(SelectedPreviewTreeViewDetailsProperty, value);
	}

	private static void OnSelectedPreviewTreeViewItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not MainWindow window)
		{
			return;
		}

		if (e.NewValue is not IReadOnlyList<CFTreeViewItem> selectedItems || selectedItems.Count == 0)
		{
			window.SelectedPreviewTreeViewDetails = "Selected: none";
			return;
		}

		window.SelectedPreviewTreeViewDetails = string.Join(
			Environment.NewLine,
			selectedItems.Select(item => $"Text: {item.Text} | Value: {item.Value}"));
	}

	private static void OnPreviewColorValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not MainWindow window)
		{
			return;
		}

		if (CFColor.TryConvertHexToRgb(window.PreviewColorValue, out var rgb))
		{
			window.PreviewColorValueRgbText = $"RGBA: {rgb.Red}, {rgb.Green}, {rgb.Blue}, {rgb.Alpha}";
		}
		else
		{
			window.PreviewColorValueRgbText = "RGBA: invalid";
		}

		if (CFColor.TryConvertHexToHsv(window.PreviewColorValue, out var hsv))
		{
			window.PreviewColorValueHsvText = $"HSVA: {hsv.Hue:0.##}, {hsv.Saturation:0.###}, {hsv.Value:0.###}, {hsv.Alpha}";
		}
		else
		{
			window.PreviewColorValueHsvText = "HSVA: invalid";
		}
	}

	private static void OnPreviewHdrColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not MainWindow window)
		{
			return;
		}

		if (CFHdrColor.TryConvertHexToRgb(window.PreviewHdrColorValue, window.PreviewHdrColorStops, out var rgb))
		{
			window.PreviewHdrColorValueRgbText = $"HDR RGBA: {rgb.Red}, {rgb.Green}, {rgb.Blue}, {rgb.Alpha} | Stops: {rgb.Stops:0.00}";
		}
		else
		{
			window.PreviewHdrColorValueRgbText = "HDR RGBA: invalid";
		}

		if (CFHdrColor.TryConvertHexToHsv(window.PreviewHdrColorValue, window.PreviewHdrColorStops, out var hsv))
		{
			window.PreviewHdrColorValueHsvText = $"HDR HSVA: {hsv.Hue:0.##}, {hsv.Saturation:0.###}, {hsv.Value:0.###}, {hsv.Alpha} | Stops: {hsv.Stops:0.00}";
		}
		else
		{
			window.PreviewHdrColorValueHsvText = "HDR HSVA: invalid";
		}
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}

		if (!viewModel.ConfirmClose())
		{
			e.Cancel = true;
		}
	}
}

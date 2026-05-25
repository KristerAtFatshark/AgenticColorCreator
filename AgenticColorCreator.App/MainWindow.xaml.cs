using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using AgenticColorCreator.App.UserControls.CFColorControl;
using AgenticColorCreator.App.UserControls.CFTreeViewControl;
using AgenticColorCreator.App.ViewModels;

namespace AgenticColorCreator.App;

public partial class MainWindow : Window
{
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

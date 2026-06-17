using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFSliderControl;

public partial class CFSlider : UserControl
{
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(float),
		typeof(CFSlider),
		new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum),
		typeof(float),
		typeof(CFSlider),
		new PropertyMetadata(0f, OnRangePropertyChanged));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum),
		typeof(float),
		typeof(CFSlider),
		new PropertyMetadata(100f, OnRangePropertyChanged));

	public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
		nameof(Step),
		typeof(float),
		typeof(CFSlider),
		new PropertyMetadata(1f));

	public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(
		nameof(TickFrequency),
		typeof(double),
		typeof(CFSlider),
		new PropertyMetadata(0d));

	public static readonly DependencyProperty TickPlacementProperty = DependencyProperty.Register(
		nameof(TickPlacement),
		typeof(TickPlacement),
		typeof(CFSlider),
		new PropertyMetadata(TickPlacement.None));

	public static readonly DependencyProperty TicksProperty = DependencyProperty.Register(
		nameof(Ticks),
		typeof(DoubleCollection),
		typeof(CFSlider),
		new PropertyMetadata(null));

	public static readonly DependencyProperty IsSnapToTickEnabledProperty = DependencyProperty.Register(
		nameof(IsSnapToTickEnabled),
		typeof(bool),
		typeof(CFSlider),
		new PropertyMetadata(false));

	public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
		nameof(Decimals),
		typeof(int),
		typeof(CFSlider),
		new PropertyMetadata(2, OnNumberEditorSizingPropertyChanged));

	public CFSlider()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	public float Value
	{
		get => (float)GetValue(ValueProperty);
		set => SetValue(ValueProperty, CoerceValue(value));
	}

	public float Minimum
	{
		get => (float)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public float Maximum
	{
		get => (float)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public float Step
	{
		get => (float)GetValue(StepProperty);
		set => SetValue(StepProperty, value);
	}

	public int Decimals
	{
		get => (int)GetValue(DecimalsProperty);
		set => SetValue(DecimalsProperty, value);
	}

	public double TickFrequency
	{
		get => (double)GetValue(TickFrequencyProperty);
		set => SetValue(TickFrequencyProperty, value);
	}

	public TickPlacement TickPlacement
	{
		get => (TickPlacement)GetValue(TickPlacementProperty);
		set => SetValue(TickPlacementProperty, value);
	}

	public DoubleCollection Ticks
	{
		get => (DoubleCollection)GetValue(TicksProperty);
		set => SetValue(TicksProperty, value);
	}

	public bool IsSnapToTickEnabled
	{
		get => (bool)GetValue(IsSnapToTickEnabledProperty);
		set => SetValue(IsSnapToTickEnabledProperty, value);
	}

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFSlider slider)
		{
			return;
		}

		var coercedValue = slider.CoerceValue((float)e.NewValue);
		if (!coercedValue.Equals((float)e.NewValue))
		{
			slider.SetCurrentValue(ValueProperty, coercedValue);
		}
	}

	private static void OnRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFSlider slider)
		{
			return;
		}

		slider.SetCurrentValue(ValueProperty, slider.CoerceValue(slider.Value));
		slider.UpdateNumberEditorWidth();
	}

	private static void OnNumberEditorSizingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFSlider slider)
		{
			return;
		}

		slider.UpdateNumberEditorWidth();
	}

	private float CoerceValue(float value)
	{
		if (value < Minimum)
		{
			return Minimum;
		}

		if (value > Maximum)
		{
			return Maximum;
		}

		return value;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		UpdateNumberEditorWidth();
	}

	private void UpdateNumberEditorWidth()
	{
		if (ValueEditor == null)
		{
			return;
		}

		ValueEditor.Width = CalculateNumberEditorWidth();
	}

	private double CalculateNumberEditorWidth()
	{
		var sampleText = GetWidestNumericTextSample();
		var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
		var formattedText = new FormattedText(
			sampleText,
			CultureInfo.InvariantCulture,
			FlowDirection.LeftToRight,
			typeface,
			FontSize,
			Brushes.Black,
			VisualTreeHelper.GetDpi(this).PixelsPerDip);

		return Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace + 40d);
	}

	private string GetWidestNumericTextSample()
	{
		var decimals = Math.Max(0, Decimals);
		var format = decimals == 0 ? "0" : $"0.{new string('0', decimals)}";
		var minimumText = Minimum.ToString(format, CultureInfo.InvariantCulture);
		var maximumText = Maximum.ToString(format, CultureInfo.InvariantCulture);
		return minimumText.Length >= maximumText.Length ? minimumText : maximumText;
	}
}

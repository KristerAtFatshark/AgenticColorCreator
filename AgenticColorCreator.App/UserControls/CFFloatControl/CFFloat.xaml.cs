using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AgenticColorCreator.App.UserControls.CFFloatControl;

public partial class CFFloat : UserControl
{
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(float),
		typeof(CFFloat),
		new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum),
		typeof(float),
		typeof(CFFloat),
		new PropertyMetadata(float.MinValue));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum),
		typeof(float),
		typeof(CFFloat),
		new PropertyMetadata(float.MaxValue));

	public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
		nameof(Step),
		typeof(float),
		typeof(CFFloat),
		new PropertyMetadata(1f));

	public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
		nameof(Decimals),
		typeof(int),
		typeof(CFFloat),
		new PropertyMetadata(2, OnDecimalsChanged));

	public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
		nameof(TextValue),
		typeof(string),
		typeof(CFFloat),
		new PropertyMetadata("0", OnTextValueChanged));

	private bool _isApplyingValue;

	public CFFloat()
	{
		InitializeComponent();
		PreviewKeyDown += OnPreviewKeyDown;
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

	public string TextValue
	{
		get => (string)GetValue(TextValueProperty);
		set => SetValue(TextValueProperty, value);
	}

	private static void OnDecimalsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFFloat floatControl)
		{
			return;
		}

		floatControl.ApplyValue(floatControl.Value);
	}

	private static void OnTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFFloat floatControl || floatControl._isApplyingValue)
		{
			return;
		}

		floatControl.CommitTextValue();
	}

	private void OnIncreaseClick(object sender, RoutedEventArgs e)
	{
		ApplyValue(Value + Step);
	}

	private void OnDecreaseClick(object sender, RoutedEventArgs e)
	{
		ApplyValue(Value - Step);
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!IsEnabled)
		{
			return;
		}

		switch (e.Key)
		{
			case Key.Up:
				OnIncreaseClick(this, new RoutedEventArgs());
				e.Handled = true;
				break;
			case Key.Down:
				OnDecreaseClick(this, new RoutedEventArgs());
				e.Handled = true;
				break;
		}
	}

	private void CommitTextValue()
	{
		if (!float.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
		{
			TextValue = FormatValue(Value);
			return;
		}

		ApplyValue(parsedValue);
	}

	private void ApplyValue(float value)
	{
		Value = CoerceValue(RoundValue(value));
		TextValue = FormatValue(Value);
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

	private float RoundValue(float value)
	{
		var decimals = Math.Max(0, Decimals);
		var roundedValue = (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
		return roundedValue == 0f ? 0f : roundedValue;
	}

	private string FormatValue(float value)
	{
		var roundedValue = RoundValue(value);
		var decimals = Math.Max(0, Decimals);
		if (decimals == 0)
		{
			return roundedValue.ToString("0", CultureInfo.InvariantCulture);
		}

		return roundedValue.ToString($"0.{new string('#', decimals)}", CultureInfo.InvariantCulture);
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.Property != ValueProperty)
		{
			return;
		}

		if (_isApplyingValue)
		{
			return;
		}

		_isApplyingValue = true;
		try
		{
			TextValue = FormatValue(Value);
		}
		finally
		{
			_isApplyingValue = false;
		}
	}
}

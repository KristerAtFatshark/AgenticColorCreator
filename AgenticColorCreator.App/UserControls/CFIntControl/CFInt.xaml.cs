using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AgenticColorCreator.App.UserControls.CFIntControl;

public partial class CFInt : UserControl
{
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(int),
		typeof(CFInt),
		new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum),
		typeof(int),
		typeof(CFInt),
		new PropertyMetadata(int.MinValue));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum),
		typeof(int),
		typeof(CFInt),
		new PropertyMetadata(int.MaxValue));

	public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
		nameof(Step),
		typeof(int),
		typeof(CFInt),
		new PropertyMetadata(1));

	public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
		nameof(TextValue),
		typeof(string),
		typeof(CFInt),
		new PropertyMetadata("0", OnTextValueChanged));

	private bool _isApplyingValue;

	public CFInt()
	{
		InitializeComponent();
	}

	public int Value
	{
		get => (int)GetValue(ValueProperty);
		set => SetValue(ValueProperty, CoerceValue(value));
	}

	public int Minimum
	{
		get => (int)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public int Maximum
	{
		get => (int)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public int Step
	{
		get => (int)GetValue(StepProperty);
		set => SetValue(StepProperty, value);
	}

	public string TextValue
	{
		get => (string)GetValue(TextValueProperty);
		set => SetValue(TextValueProperty, value);
	}

	private static void OnTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFInt numberControl || numberControl._isApplyingValue)
		{
			return;
		}

		numberControl.CommitTextValue();
	}

	private void OnIncreaseClick(object sender, RoutedEventArgs e)
	{
		Value = CoerceValue(Value + Step);
		TextValue = Value.ToString(CultureInfo.InvariantCulture);
	}

	private void OnDecreaseClick(object sender, RoutedEventArgs e)
	{
		Value = CoerceValue(Value - Step);
		TextValue = Value.ToString(CultureInfo.InvariantCulture);
	}

	private void CommitTextValue()
	{
		if (!int.TryParse(TextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
		{
			TextValue = Value.ToString(CultureInfo.InvariantCulture);
			return;
		}

		Value = CoerceValue(parsedValue);
		TextValue = Value.ToString(CultureInfo.InvariantCulture);
	}

	private int CoerceValue(int value)
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
			TextValue = Value.ToString(CultureInfo.InvariantCulture);
		}
		finally
		{
			_isApplyingValue = false;
		}
	}
}

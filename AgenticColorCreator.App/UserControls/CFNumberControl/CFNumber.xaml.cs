using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AgenticColorCreator.App.UserControls.CFNumberControl;

public partial class CFNumber : UserControl
{
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(int),
		typeof(CFNumber),
		new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum),
		typeof(int),
		typeof(CFNumber),
		new PropertyMetadata(int.MinValue));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum),
		typeof(int),
		typeof(CFNumber),
		new PropertyMetadata(int.MaxValue));

	public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
		nameof(Step),
		typeof(int),
		typeof(CFNumber),
		new PropertyMetadata(1));

	public CFNumber()
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

	private void OnIncreaseClick(object sender, RoutedEventArgs e)
	{
		Value = CoerceValue(Value + Step);
	}

	private void OnDecreaseClick(object sender, RoutedEventArgs e)
	{
		Value = CoerceValue(Value - Step);
	}

	private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled = !IsTextAllowed(e.Text);
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Up)
		{
			Value = CoerceValue(Value + Step);
			e.Handled = true;
			return;
		}

		if (e.Key == Key.Down)
		{
			Value = CoerceValue(Value - Step);
			e.Handled = true;
		}
	}

	private void OnValueTextBoxLostFocus(object sender, RoutedEventArgs e)
	{
		if (!int.TryParse(ValueTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
		{
			ValueTextBox.Text = Value.ToString(CultureInfo.InvariantCulture);
			return;
		}

		Value = CoerceValue(parsedValue);
		ValueTextBox.Text = Value.ToString(CultureInfo.InvariantCulture);
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

	private static bool IsTextAllowed(string text)
	{
		foreach (var character in text)
		{
			if (!char.IsDigit(character) && character != '-')
			{
				return false;
			}
		}

		return true;
	}
}

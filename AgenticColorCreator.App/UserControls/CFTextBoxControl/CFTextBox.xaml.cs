using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AgenticColorCreator.App.UserControls.CFTextBoxControl;

public partial class CFTextBox : UserControl
{
	public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
		nameof(IsMixedState),
		typeof(bool),
		typeof(CFTextBox),
		new PropertyMetadata(false, OnIsMixedStateChanged));

	public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
		nameof(IsEditing),
		typeof(bool),
		typeof(CFTextBox),
		new PropertyMetadata(false));

	public static readonly DependencyProperty DisplayedTextProperty = DependencyProperty.Register(
		nameof(DisplayedText),
		typeof(string),
		typeof(CFTextBox),
		new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDisplayedTextChanged));

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value),
		typeof(string),
		typeof(CFTextBox),
		new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(CFTextBox),
		new PropertyMetadata(string.Empty, OnTextChanged));

	public static readonly RoutedEvent ValueCommittedEvent = EventManager.RegisterRoutedEvent(
		nameof(ValueCommitted),
		RoutingStrategy.Bubble,
		typeof(RoutedEventHandler),
		typeof(CFTextBox));

	public static readonly DependencyProperty ValidationModeProperty = DependencyProperty.Register(
		nameof(ValidationMode),
		typeof(CFTextBoxValidationMode),
		typeof(CFTextBox),
		new PropertyMetadata(CFTextBoxValidationMode.AlphaNumericPath, OnValidationModeChanged));

	public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
		nameof(DecimalPlaces),
		typeof(int),
		typeof(CFTextBox),
		new PropertyMetadata(2, OnDecimalPlacesChanged));

	private static readonly Regex GeneralValidationRegex = new("^[A-Za-z0-9/._-]*$", RegexOptions.Compiled);
	private static readonly Regex FloatValidationRegex = new("^-?\\d*\\.?\\d*$", RegexOptions.Compiled);
	private static readonly string DefaultTextValue = string.Empty;

	private readonly DispatcherTimer _commitTimer;
	private bool _isApplyingExternalValue;

	public CFTextBox()
	{
		InitializeComponent();

		_commitTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(1000),
		};
		_commitTimer.Tick += OnCommitTimerTick;

		InnerTextBox.PreviewKeyDown += OnInnerTextBoxPreviewKeyDown;
		InnerTextBox.LostKeyboardFocus += OnInnerTextBoxLostKeyboardFocus;
		Loaded += OnLoaded;

		UpdateValidationVisual();
	}

	public bool IsMixedState
	{
		get => (bool)GetValue(IsMixedStateProperty);
		set => SetValue(IsMixedStateProperty, value);
	}

	public bool IsEditing
	{
		get => (bool)GetValue(IsEditingProperty);
		set => SetValue(IsEditingProperty, value);
	}

	public string DisplayedText
	{
		get => (string)GetValue(DisplayedTextProperty);
		set => SetValue(DisplayedTextProperty, value);
	}

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public CFTextBoxValidationMode ValidationMode
	{
		get => (CFTextBoxValidationMode)GetValue(ValidationModeProperty);
		set => SetValue(ValidationModeProperty, value);
	}

	public int DecimalPlaces
	{
		get => (int)GetValue(DecimalPlacesProperty);
		set => SetValue(DecimalPlacesProperty, value);
	}

	public event RoutedEventHandler ValueCommitted
	{
		add => AddHandler(ValueCommittedEvent, value);
		remove => RemoveHandler(ValueCommittedEvent, value);
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (!IsMixedState && DisplayedText != Value)
		{
			DisplayedText = Value;
		}
	}

	private static void OnDisplayedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox)
		{
			return;
		}

		textBox.Text = e.NewValue as string ?? string.Empty;
	}

	private static void OnIsMixedStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox)
		{
			return;
		}

		var nextMixed = (bool)e.NewValue;
		if (nextMixed)
		{
			if (textBox.DisplayedText == textBox.Value)
			{
				textBox.DisplayedText = DefaultTextValue;
			}

			return;
		}

		textBox.DisplayedText = textBox.Value;
	}

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox || textBox._isApplyingExternalValue)
		{
			return;
		}

		var newValue = e.NewValue as string ?? string.Empty;
		if (!textBox.IsMixedState)
		{
			textBox.DisplayedText = newValue;
		}

		textBox.ApplyExternalText(newValue);
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox || textBox._isApplyingExternalValue)
		{
			return;
		}

		textBox.UpdateValidationVisual();
		textBox.RestartCommitTimer();
	}

	private static void OnValidationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox)
		{
			return;
		}

		textBox.UpdateValidationVisual();
	}

	private static void OnDecimalPlacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox)
		{
			return;
		}

		textBox.UpdateValidationVisual();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (!IsMixedState && DisplayedText != Value)
		{
			DisplayedText = Value;
		}
	}

	private void OnInnerTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!IsEditing)
		{
			IsEditing = true;
		}

		_commitTimer.Stop();

		if (e.Key == Key.Enter)
		{
			CommitText();
			e.Handled = true;
		}
	}

	private void OnInnerTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		_commitTimer.Stop();
		IsEditing = false;

		if (!IsMixedState)
		{
			CommitText();
			return;
		}

		if (DisplayedText != DefaultTextValue)
		{
			CommitText();
			return;
		}

		if (IsMixedState)
		{
			DisplayedText = DefaultTextValue;
		}
	}

	private void OnCommitTimerTick(object? sender, EventArgs e)
	{
		_commitTimer.Stop();
		CommitText();
	}

	private void RestartCommitTimer()
	{
		_commitTimer.Stop();
		_commitTimer.Start();
	}

	private void CommitText()
	{
		_commitTimer.Stop();

		if (!IsTextValid(DisplayedText))
		{
			return;
		}

		if (IsMixedState)
		{
			if (DisplayedText != DefaultTextValue)
			{
				Value = DisplayedText;
				IsMixedState = false;
			}
			else
			{
				DisplayedText = DefaultTextValue;
			}
		}
		else if (DisplayedText != Value)
		{
			Value = DisplayedText;
		}

		RaiseEvent(new RoutedEventArgs(ValueCommittedEvent));
	}

	private void ApplyExternalText(string text)
	{
		_commitTimer.Stop();

		_isApplyingExternalValue = true;
		try
		{
			Text = text;
		}
		finally
		{
			_isApplyingExternalValue = false;
		}

		UpdateValidationVisual();
	}

	private void UpdateValidationVisual()
	{
		if (IsTextValid(Text))
		{
			InnerTextBox.ClearValue(TextBox.ForegroundProperty);
			return;
		}

		InnerTextBox.Foreground = System.Windows.Media.Brushes.Red;
	}

	private bool IsTextValid(string text)
	{
		return ValidationMode switch
		{
			CFTextBoxValidationMode.NumberOnly => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
			CFTextBoxValidationMode.FloatNumber => IsFloatTextValid(text),
			_ => GeneralValidationRegex.IsMatch(text),
		};
	}

	private bool IsFloatTextValid(string text)
	{
		if (string.IsNullOrWhiteSpace(text) || !FloatValidationRegex.IsMatch(text))
		{
			return false;
		}

		if (text is "-" or "." or "-.")
		{
			return false;
		}

		if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
		{
			return false;
		}

		var decimalSeparatorIndex = text.IndexOf('.');
		if (decimalSeparatorIndex < 0)
		{
			return true;
		}

		var allowedDecimalPlaces = Math.Max(0, DecimalPlaces);
		var decimalDigits = text.Length - decimalSeparatorIndex - 1;
		return decimalDigits <= allowedDecimalPlaces;
	}
}

public enum CFTextBoxValidationMode
{
	AlphaNumericPath,
	NumberOnly,
	FloatNumber,
}

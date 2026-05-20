using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AgenticColorCreator.App.UserControls.CFTextBoxControl;

public partial class CFTextBox : UserControl
{
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

	public event RoutedEventHandler ValueCommitted
	{
		add => AddHandler(ValueCommittedEvent, value);
		remove => RemoveHandler(ValueCommittedEvent, value);
	}

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox || textBox._isApplyingExternalValue)
		{
			return;
		}

		textBox.Text = e.NewValue as string ?? string.Empty;
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTextBox textBox || textBox._isApplyingExternalValue)
		{
			return;
		}

		textBox.RestartCommitTimer();
	}

	private void OnInnerTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter)
		{
			return;
		}

		CommitText();
		e.Handled = true;
	}

	private void OnInnerTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		CommitText();
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

		if (string.Equals(Value, Text, StringComparison.Ordinal))
		{
			return;
		}

		_isApplyingExternalValue = true;
		try
		{
			Value = Text;
			RaiseEvent(new RoutedEventArgs(ValueCommittedEvent));
		}
		finally
		{
			_isApplyingExternalValue = false;
		}
	}
}

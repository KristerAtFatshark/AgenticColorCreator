using System.Windows;
using System.Windows.Controls;

namespace AgenticColorCreator.App.UserControls.CFRadioButtonControl;

public partial class CFRadioButton : UserControl
{
	public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
		nameof(IsChecked),
		typeof(bool),
		typeof(CFRadioButton),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

	public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
		nameof(IsMixedState),
		typeof(bool),
		typeof(CFRadioButton),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsMixedStateChanged));

	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label),
		typeof(object),
		typeof(CFRadioButton),
		new PropertyMetadata(null));

	public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
		nameof(GroupName),
		typeof(string),
		typeof(CFRadioButton),
		new PropertyMetadata(string.Empty));

	private bool _isSyncingInnerState;

	public CFRadioButton()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	public bool IsChecked
	{
		get => (bool)GetValue(IsCheckedProperty);
		set => SetValue(IsCheckedProperty, value);
	}

	public bool IsMixedState
	{
		get => (bool)GetValue(IsMixedStateProperty);
		set => SetValue(IsMixedStateProperty, value);
	}

	public object Label
	{
		get => GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	public string GroupName
	{
		get => (string)GetValue(GroupNameProperty);
		set => SetValue(GroupNameProperty, value);
	}

	private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFRadioButton radioButton)
		{
			radioButton.SyncInnerState();
		}
	}

	private static void OnIsMixedStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFRadioButton radioButton)
		{
			radioButton.SyncInnerState();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SyncInnerState();
	}

	private void OnInnerChecked(object sender, RoutedEventArgs e)
	{
		CommitUserValue(true);
	}

	private void OnInnerUnchecked(object sender, RoutedEventArgs e)
	{
		CommitUserValue(false);
	}

	private void CommitUserValue(bool value)
	{
		// Ignore callbacks raised while we are pushing the current state into the inner
		// RadioButton; only user-driven toggles should commit a real value and clear mixed state.
		if (_isSyncingInnerState)
		{
			return;
		}

		if (IsChecked != value)
		{
			IsChecked = value;
		}

		if (IsMixedState)
		{
			IsMixedState = false;
		}
	}

	private void SyncInnerState()
	{
		if (InnerRadioButton == null)
		{
			return;
		}

		_isSyncingInnerState = true;
		try
		{
			// Mixed state maps to the native indeterminate value (null), which the CF.RadioButton
			// style renders as a hollow orange ring in the center. Otherwise the inner RadioButton
			// mirrors the committed boolean value.
			InnerRadioButton.IsChecked = IsMixedState ? (bool?)null : IsChecked;
		}
		finally
		{
			_isSyncingInnerState = false;
		}
	}
}

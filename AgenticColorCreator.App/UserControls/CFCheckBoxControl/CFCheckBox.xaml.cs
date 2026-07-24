using System.Windows;
using System.Windows.Controls;

namespace AgenticColorCreator.App.UserControls.CFCheckBoxControl;

public partial class CFCheckBox : UserControl
{
	public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
		nameof(IsChecked),
		typeof(bool),
		typeof(CFCheckBox),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

	public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
		nameof(IsMixedState),
		typeof(bool),
		typeof(CFCheckBox),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsMixedStateChanged));

	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label),
		typeof(object),
		typeof(CFCheckBox),
		new PropertyMetadata(null));

	private bool _isSyncingInnerState;

	public CFCheckBox()
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

	private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFCheckBox checkBox)
		{
			checkBox.SyncInnerState();
		}
	}

	private static void OnIsMixedStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFCheckBox checkBox)
		{
			checkBox.SyncInnerState();
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
		// CheckBox; only user-driven toggles should commit a real value and clear mixed state.
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
		if (InnerCheckBox == null)
		{
			return;
		}

		_isSyncingInnerState = true;
		try
		{
			// Mixed state maps to the native indeterminate visual (null). Otherwise the inner
			// CheckBox mirrors the committed boolean value.
			InnerCheckBox.IsChecked = IsMixedState ? (bool?)null : IsChecked;
		}
		finally
		{
			_isSyncingInnerState = false;
		}
	}
}

using System.Windows;
using System.Windows.Controls;

namespace AgenticColorCreator.App.UserControls.CFComboBoxControl;

public partial class CFComboBox : UserControl
{
	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
		nameof(ItemsSource),
		typeof(System.Collections.IEnumerable),
		typeof(CFComboBox),
		new PropertyMetadata(null));

	public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
		nameof(SelectedItem),
		typeof(object),
		typeof(CFComboBox),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
		nameof(SelectedIndex),
		typeof(int),
		typeof(CFComboBox),
		new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

	public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
		nameof(DisplayMemberPath),
		typeof(string),
		typeof(CFComboBox),
		new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
		nameof(IsMixedState),
		typeof(bool),
		typeof(CFComboBox),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	private bool _isSyncingSelection;

	public CFComboBox()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	public System.Collections.IEnumerable ItemsSource
	{
		get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public object SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	public int SelectedIndex
	{
		get => (int)GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}

	public string DisplayMemberPath
	{
		get => (string)GetValue(DisplayMemberPathProperty);
		set => SetValue(DisplayMemberPathProperty, value);
	}

	public bool IsMixedState
	{
		get => (bool)GetValue(IsMixedStateProperty);
		set => SetValue(IsMixedStateProperty, value);
	}

	private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFComboBox comboBox)
		{
			comboBox.PushSelectedItemToInner();
		}
	}

	private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFComboBox comboBox)
		{
			comboBox.PushSelectedIndexToInner();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		// Apply whatever the host bound before the inner ComboBox was ready.
		if (SelectedItem != null)
		{
			PushSelectedItemToInner();
		}
		else if (SelectedIndex >= 0)
		{
			PushSelectedIndexToInner();
		}
	}

	private void PushSelectedItemToInner()
	{
		if (InnerComboBox == null || _isSyncingSelection)
		{
			return;
		}

		_isSyncingSelection = true;
		try
		{
			InnerComboBox.SelectedItem = SelectedItem;
		}
		finally
		{
			_isSyncingSelection = false;
		}
	}

	private void PushSelectedIndexToInner()
	{
		if (InnerComboBox == null || _isSyncingSelection)
		{
			return;
		}

		_isSyncingSelection = true;
		try
		{
			InnerComboBox.SelectedIndex = SelectedIndex;
		}
		finally
		{
			_isSyncingSelection = false;
		}
	}

	private void OnInnerSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		// Mirror the inner selection back onto the public properties.
		SelectedItem = InnerComboBox.SelectedItem;
		SelectedIndex = InnerComboBox.SelectedIndex;

		// A selection change while we are pushing a host value into the inner ComboBox is not a
		// user commit, so it must not clear the mixed overlay. Only genuine user picks do.
		if (_isSyncingSelection)
		{
			return;
		}

		if (IsMixedState)
		{
			IsMixedState = false;
		}
	}
}

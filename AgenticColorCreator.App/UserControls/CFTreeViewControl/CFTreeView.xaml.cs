using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

#pragma warning disable CS8600, CS8603, CS8604, CS8618, CS8622, CS8625

namespace ClownFishUi.CFUserControls.CFTreeViewControl
{
	public partial class CFTreeView : UserControl
	{
	public static readonly DependencyProperty IsMixedStateProperty = DependencyProperty.Register(
		nameof(IsMixedState),
		typeof(bool),
		typeof(CFTreeView),
		new PropertyMetadata(false));
	public bool IsMixedState
	{
		get => (bool)GetValue(IsMixedStateProperty);
		set => SetValue(IsMixedStateProperty, value);
	}

	public static readonly DependencyProperty IsMultiSelectProperty = DependencyProperty.Register(
		nameof(IsMultiSelect),
		typeof(bool),
		typeof(CFTreeView),
		new PropertyMetadata(false, OnIsMultiSelectChanged));

	public static readonly DependencyProperty NodesSourceProperty = DependencyProperty.Register(
		nameof(NodesSource),
		typeof(ObservableCollection<TreeViewSourceEntry>),
		typeof(CFTreeView),
		new PropertyMetadata(null, OnNodesSourceChanged));

	public static readonly DependencyProperty SelectedTreeViewItemsProperty = DependencyProperty.Register(
		nameof(SelectedTreeViewItems),
		typeof(IReadOnlyList<CFTreeViewItem>),
		typeof(CFTreeView),
		new PropertyMetadata(null));

	public static readonly DependencyProperty SelectedValuesProperty = DependencyProperty.Register(
		nameof(SelectedValues),
		typeof(ObservableCollection<string>),
		typeof(CFTreeView),
		new PropertyMetadata(null, OnSelectedValuesChanged));

	private ObservableCollection<TreeViewSourceEntry> _subscribedSource;
	private ObservableCollection<string> _subscribedSelectedValues;
	private readonly List<CFTreeViewItem> _selectedTreeViewItems = new List<CFTreeViewItem>();
	private readonly Dictionary<string, CFTreeViewItem> _treeViewItemsByValue = new Dictionary<string, CFTreeViewItem>(StringComparer.OrdinalIgnoreCase);
	private bool _isApplyingExternalSelection;
	private bool _hasExternalSelectionState;
	private bool _isUpdatingSelectedValues;
	private bool _suppressSelectedItemChanged;

	public CFTreeView()
	{
		InitializeComponent();
	}

	public bool IsMultiSelect
	{
		get => (bool)GetValue(IsMultiSelectProperty);
		set => SetValue(IsMultiSelectProperty, value);
	}

	public ObservableCollection<TreeViewSourceEntry> NodesSource
	{
		get => (ObservableCollection<TreeViewSourceEntry>)GetValue(NodesSourceProperty);
		set => SetValue(NodesSourceProperty, value);
	}

	public IReadOnlyList<CFTreeViewItem> SelectedTreeViewItems
	{
		get => (IReadOnlyList<CFTreeViewItem>)GetValue(SelectedTreeViewItemsProperty);
		set => SetValue(SelectedTreeViewItemsProperty, value);
	}

	public ObservableCollection<string> SelectedValues
	{
		get => (ObservableCollection<string>)GetValue(SelectedValuesProperty);
		set => SetValue(SelectedValuesProperty, value);
	}

	private static void OnIsMultiSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var treeView = d as CFTreeView;
		if (treeView == null)
		{
			return;
		}

		treeView.SyncSelectionMode();
	}

	private static void OnNodesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var treeView = d as CFTreeView;
		if (treeView == null)
		{
			return;
		}

		treeView.ResetNodeSubscription();
		treeView.SubscribeToSource(e.NewValue as ObservableCollection<TreeViewSourceEntry>);
		treeView.RebuildTreeViewItems();
	}

	private static void OnSelectedValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var treeView = d as CFTreeView;
		if (treeView == null)
		{
			return;
		}

		treeView.ResetSelectedValuesSubscription();
		treeView.SubscribeToSelectedValues(e.NewValue as ObservableCollection<string>);
		treeView.ApplySelectionFromValues();
	}

	private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		if (_suppressSelectedItemChanged)
		{
			return;
		}

		IsMixedState = false;
		if (IsMultiSelect)
		{
			return;
		}

		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
		}

		_selectedTreeViewItems.Clear();

		if (e.NewValue is CFTreeViewItem selectedItem)
		{
			_selectedTreeViewItems.Add(selectedItem);
		}

		UpdateSelectedTreeViewItems();
	}

	private void OnPreviewTreeViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!IsMultiSelect)
		{
			return;
		}

		if (FindAncestor<ToggleButton>(e.OriginalSource as DependencyObject) != null)
		{
			return;
		}

		var clickedItem = FindAncestor<CFTreeViewItem>(e.OriginalSource as DependencyObject);
		if (clickedItem == null)
		{
			return;
		}

		if (_hasExternalSelectionState)
		{
			ClearForcedSelectionState();
			_hasExternalSelectionState = false;
		}

		if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		{
			ToggleItemSelection(clickedItem);
		}
		else
		{
			SelectSingleItem(clickedItem);
		}

		clickedItem.Focus();
		e.Handled = true;
	}

	private void SelectSingleItem(CFTreeViewItem clickedItem)
	{
		foreach (var item in _selectedTreeViewItems.Where(item => !ReferenceEquals(item, clickedItem)).ToList())
		{
			item.IsMultiSelected = false;
			item.IsSelected = false;
		}

		_selectedTreeViewItems.Clear();
		clickedItem.IsMultiSelected = true;
		clickedItem.IsSelected = false;
		_selectedTreeViewItems.Add(clickedItem);
		UpdateSelectedTreeViewItems();
	}

	private void ToggleItemSelection(CFTreeViewItem clickedItem)
	{
		if (_selectedTreeViewItems.Remove(clickedItem))
		{
			clickedItem.IsMultiSelected = false;
			clickedItem.IsSelected = false;
			UpdateSelectedTreeViewItems();
			return;
		}

		clickedItem.IsMultiSelected = true;
		clickedItem.IsSelected = false;
		_selectedTreeViewItems.Add(clickedItem);
		UpdateSelectedTreeViewItems();
	}

	private void UpdateSelectedTreeViewItems()
	{
		SelectedTreeViewItems = _selectedTreeViewItems.ToList();
		SyncSelectedValuesFromItems();
	}

	private void RebuildTreeViewItems()
	{
		var selectedValues = GetSelectedValueSet();
		var useMultipleSelection = IsMultiSelect && selectedValues.Count > 1;
		var treeViewItemStyle = PreviewTreeView.TryFindResource("CF.TreeViewItem") as Style;

		PreviewTreeView.Items.Clear();
		_treeViewItemsByValue.Clear();

		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
		}

		_selectedTreeViewItems.Clear();

		if (NodesSource != null)
		{
			foreach (var rootNode in BuildTreeViewNodes(NodesSource))
			{
				var treeViewItem = CreateTreeViewItem(rootNode, selectedValues, useMultipleSelection, treeViewItemStyle, _treeViewItemsByValue);
				PreviewTreeView.Items.Add(treeViewItem);
				CollectSelectedTreeViewItems(treeViewItem);
			}
		}

		UpdateSelectedTreeViewItems();
	}

	private HashSet<string> GetSelectedValueSet()
	{
		if (SelectedValues != null && (_hasExternalSelectionState || _isApplyingExternalSelection))
		{
			return SelectedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		return _selectedTreeViewItems.Select(item => item.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	private static IReadOnlyList<TreeViewNode> BuildTreeViewNodes(IEnumerable<TreeViewSourceEntry> sourceEntries)
	{
		var rootNodes = new List<TreeViewNode>();

		foreach (var sourceEntry in sourceEntries)
		{
			var segments = sourceEntry.Value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (segments.Length == 0)
			{
				continue;
			}

			ICollection<TreeViewNode> currentNodes = rootNodes;
			var currentPath = string.Empty;

			for (var index = 0; index < segments.Length; index++)
			{
				var segment = segments[index];
				var isLeaf = index == segments.Length - 1;
				currentPath = string.IsNullOrWhiteSpace(currentPath) ? segment : $"{currentPath}/{segment}";
				var nodeType = isLeaf ? sourceEntry.Type : "folder";

				var existingNode = currentNodes.FirstOrDefault(node => string.Equals(node.Value, currentPath, StringComparison.OrdinalIgnoreCase));
				if (existingNode == null)
				{
					existingNode = new TreeViewNode
					{
						Text = segment,
						Value = currentPath,
						Icon = TreeViewIconMap.GetIcon(nodeType),
					};
					currentNodes.Add(existingNode);
				}
				else if (isLeaf)
				{
					existingNode.Icon = TreeViewIconMap.GetIcon(sourceEntry.Type);
				}

				currentNodes = existingNode.Children;
			}
		}

		return rootNodes;
	}

	private void CollectSelectedTreeViewItems(CFTreeViewItem treeViewItem)
	{
		if (treeViewItem.IsMultiSelected || treeViewItem.IsSelected)
		{
			_selectedTreeViewItems.Add(treeViewItem);
		}

		foreach (var childItem in treeViewItem.Items.OfType<CFTreeViewItem>())
		{
			CollectSelectedTreeViewItems(childItem);
		}
	}

	private static CFTreeViewItem CreateTreeViewItem(TreeViewNode treeViewNode, IReadOnlyCollection<string> selectedValues, bool useMultipleSelection, Style treeViewItemStyle, IDictionary<string, CFTreeViewItem> treeViewItemsByValue)
	{
		var isSelected = selectedValues.Contains(treeViewNode.Value);
		var treeViewItem = new CFTreeViewItem
		{
			Icon = treeViewNode.Icon,
			Text = treeViewNode.Text,
			Value = treeViewNode.Value,
			IsExpanded = treeViewNode.Children.Count > 0,
			IsMultiSelected = isSelected && useMultipleSelection,
			IsSelected = isSelected && !useMultipleSelection,
			Style = treeViewItemStyle,
		};

		treeViewItemsByValue[treeViewNode.Value] = treeViewItem;

		foreach (var childNode in treeViewNode.Children)
		{
			treeViewItem.Items.Add(CreateTreeViewItem(childNode, selectedValues, useMultipleSelection, treeViewItemStyle, treeViewItemsByValue));
		}

		return treeViewItem;
	}

	private void SubscribeToSource(ObservableCollection<TreeViewSourceEntry> source)
	{
		if (source == null)
		{
			return;
		}

		_subscribedSource = source;
		source.CollectionChanged += OnSourceCollectionChanged;
	}

	private void SubscribeToSelectedValues(ObservableCollection<string> selectedValues)
	{
		if (selectedValues == null)
		{
			return;
		}

		_subscribedSelectedValues = selectedValues;
		selectedValues.CollectionChanged += OnSelectedValuesCollectionChanged;
	}

	private void ResetNodeSubscription()
	{
		if (_subscribedSource == null)
		{
			return;
		}

		_subscribedSource.CollectionChanged -= OnSourceCollectionChanged;
		_subscribedSource = null;
	}

	private void ResetSelectedValuesSubscription()
	{
		if (_subscribedSelectedValues == null)
		{
			return;
		}

		_subscribedSelectedValues.CollectionChanged -= OnSelectedValuesCollectionChanged;
		_subscribedSelectedValues = null;
	}

	private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		RebuildTreeViewItems();
	}

	private void OnSelectedValuesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (_isUpdatingSelectedValues)
		{
			return;
		}

		_isApplyingExternalSelection = true;
		ApplySelectionFromValues();
	}

	private void ApplySelectionFromValues()
	{
		if (SelectedValues == null)
		{
			_isApplyingExternalSelection = false;
			_hasExternalSelectionState = false;
			return;
		}

		_hasExternalSelectionState = true;
		ApplySelectionFromValuesInPlace();
		ScrollFirstExternallySelectedItemIntoView();

		if (SelectedValues.Count == 0)
		{
			_hasExternalSelectionState = false;
		}

		_isApplyingExternalSelection = false;
	}

	private void ApplySelectionFromValuesInPlace()
	{
		_suppressSelectedItemChanged = true;
		try
		{
			foreach (var item in _selectedTreeViewItems)
			{
				item.IsMultiSelected = false;
				item.IsSelected = false;
			}

			_selectedTreeViewItems.Clear();

			if (SelectedValues == null)
			{
				UpdateSelectedTreeViewItems();
				return;
			}

			var matchingItems = new List<CFTreeViewItem>();
			foreach (var selectedValue in SelectedValues)
			{
				CFTreeViewItem matchingItem;
				if (_treeViewItemsByValue.TryGetValue(selectedValue, out matchingItem) && matchingItem != null)
				{
					matchingItems.Add(matchingItem);
				}
			}

			if (IsMultiSelect && matchingItems.Count > 1)
			{
				foreach (var matchingItem in matchingItems)
				{
					matchingItem.IsMultiSelected = true;
					matchingItem.IsSelected = false;
					_selectedTreeViewItems.Add(matchingItem);
				}
			}
			else if (matchingItems.Count > 0)
			{
				var selectedItem = matchingItems[0];
				selectedItem.IsMultiSelected = false;
				selectedItem.IsSelected = true;
				_selectedTreeViewItems.Add(selectedItem);
			}

			UpdateSelectedTreeViewItems();
		}
		finally
		{
			_suppressSelectedItemChanged = false;
		}
	}

	private void ScrollFirstExternallySelectedItemIntoView()
	{
		if (SelectedValues == null || SelectedValues.Count == 0)
		{
			return;
		}

		var selectedItem = SelectedValues
			.Select(FindSelectedTreeViewItem)
			.FirstOrDefault(item => item != null);

		if (selectedItem == null)
		{
			return;
		}

		Dispatcher.BeginInvoke(() =>
		{
			selectedItem.BringIntoView();
		}, DispatcherPriority.Loaded);
	}

	private CFTreeViewItem FindSelectedTreeViewItem(string selectedValue)
	{
		CFTreeViewItem treeViewItem;
		return _treeViewItemsByValue.TryGetValue(selectedValue, out treeViewItem) ? treeViewItem : null;
	}

	private void SyncSelectedValuesFromItems()
	{
		if (SelectedValues == null)
		{
			return;
		}

		_hasExternalSelectionState = false;

		var selectedItemValues = _selectedTreeViewItems.Select(item => item.Value).ToList();
		if (SelectedValues.SequenceEqual(selectedItemValues, StringComparer.OrdinalIgnoreCase))
		{
			return;
		}

		_isUpdatingSelectedValues = true;
		try
		{
			SelectedValues.Clear();
			foreach (var selectedItemValue in selectedItemValues)
			{
				SelectedValues.Add(selectedItemValue);
			}
		}
		finally
		{
			_isUpdatingSelectedValues = false;
		}
	}

	private void ClearForcedSelectionState()
	{
		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
			item.IsSelected = false;
		}

		_selectedTreeViewItems.Clear();

		if (SelectedValues == null)
		{
			return;
		}

		_isUpdatingSelectedValues = true;
		try
		{
			SelectedValues.Clear();
		}
		finally
		{
			_isUpdatingSelectedValues = false;
		}
	}

	private void SyncSelectionMode()
	{
		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
		}

		_selectedTreeViewItems.Clear();

		var selectedItem = PreviewTreeView.SelectedItem as CFTreeViewItem;
		if (selectedItem != null)
		{
			if (IsMultiSelect)
			{
				selectedItem.IsMultiSelected = true;
			}

			_selectedTreeViewItems.Add(selectedItem);
		}

		UpdateSelectedTreeViewItems();
	}

	private static T FindAncestor<T>(DependencyObject dependencyObject)
		where T : DependencyObject
	{
		var current = dependencyObject;

		while (current != null)
		{
			var match = current as T;
			if (match != null)
			{
				return match;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return null;
	}

 	}
}

#pragma warning restore CS8600, CS8603, CS8604, CS8618, CS8622, CS8625

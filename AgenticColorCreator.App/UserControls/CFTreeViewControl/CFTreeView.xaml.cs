using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

	public static readonly DependencyProperty CollapseAllThresholdItemCountProperty = DependencyProperty.Register(
		nameof(CollapseAllThresholdItemCount),
		typeof(int),
		typeof(CFTreeView),
		new PropertyMetadata(100, OnCollapseAllThresholdItemCountChanged));

	private ObservableCollection<TreeViewSourceEntry> _subscribedSource;
	private ObservableCollection<string> _subscribedSelectedValues;
	private readonly List<CFTreeViewItem> _selectedTreeViewItems = new List<CFTreeViewItem>();
	private Dictionary<string, CFTreeViewItem> _treeViewItemsByValue = new Dictionary<string, CFTreeViewItem>(StringComparer.OrdinalIgnoreCase);
	private bool _isApplyingExternalSelection;
	private bool _hasExternalSelectionState;
	private bool _isUpdatingSelectedValues;
	private bool _suppressSelectedItemChanged;
	private int _updateSuppressionCount;
	private bool _hasPendingRebuild;
	private long _currentRebuildGeneration;
	private bool _isCoalescingSourceChanges;

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

	public int CollapseAllThresholdItemCount
	{
		get => (int)GetValue(CollapseAllThresholdItemCountProperty);
		set => SetValue(CollapseAllThresholdItemCountProperty, value);
	}

	/// <summary>
	/// Raised on the UI thread after each rebuild completes, including when the tree is cleared
	/// because <see cref="NodesSource"/> was set to <c>null</c>. Useful for hosts and tests that
	/// need to await the async rebuild pipeline before inspecting the visual tree.
	/// </summary>
	public event EventHandler RebuildCompleted;

	/// <summary>
	/// Suspends automatic rebuilds triggered by <see cref="NodesSource"/> replacements or source
	/// collection changes. Every call must be paired with a matching <see cref="EndUpdate"/>.
	/// Callers can perform multiple mutations to <see cref="NodesSource"/> (or replace it entirely)
	/// and only a single rebuild will occur once the outermost <see cref="EndUpdate"/> returns.
	/// </summary>
	public void BeginUpdate()
	{
		_updateSuppressionCount++;
	}

	/// <summary>
	/// Ends a batch started by <see cref="BeginUpdate"/>. When the outermost pair completes and
	/// at least one rebuild was requested while suppressed, a single rebuild is scheduled.
	/// </summary>
	public void EndUpdate()
	{
		if (_updateSuppressionCount == 0)
		{
			return;
		}

		_updateSuppressionCount--;
		if (_updateSuppressionCount > 0)
		{
			return;
		}

		if (!_hasPendingRebuild)
		{
			return;
		}

		_hasPendingRebuild = false;
		RequestRebuildTreeViewItems();
	}

	public void CollapseAllExceptSelectedItemParents()
	{
		var expandedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var selectedItem in _selectedTreeViewItems)
		{
			AddParentPaths(expandedValues, selectedItem.Value);
		}

		foreach (var rootItem in GetRootTreeViewItems())
		{
			ApplyExpansionState(rootItem, expandedValues);
		}
	}

	public void CollapseAll()
	{
		foreach (var rootItem in GetRootTreeViewItems())
		{
			CollapseItemAndChildren(rootItem);
		}
	}

	private IEnumerable<CFTreeViewItem> GetRootTreeViewItems()
	{
		if (PreviewTreeView.ItemsSource is IEnumerable<CFTreeViewItem> typedSource)
		{
			return typedSource;
		}

		return PreviewTreeView.Items.OfType<CFTreeViewItem>();
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
		treeView.RequestRebuildTreeViewItems();
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

	private static void OnCollapseAllThresholdItemCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var treeView = d as CFTreeView;
		if (treeView == null)
		{
			return;
		}

		treeView.ApplyCollapseAllThreshold();
	}

	private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		if (_suppressSelectedItemChanged)
		{
			return;
		}

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

	private static readonly char[] PathSeparators = new[] { '/' };

	private void RequestRebuildTreeViewItems()
	{
		if (_updateSuppressionCount > 0)
		{
			_hasPendingRebuild = true;
			return;
		}

		var generation = Interlocked.Increment(ref _currentRebuildGeneration);

		var selectedValues = GetSelectedValueSet();
		var useMultipleSelection = IsMultiSelect && selectedValues.Count > 1;
		var treeViewItemStyle = PreviewTreeView.TryFindResource("CF.TreeViewItem") as Style;

		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
		}

		_selectedTreeViewItems.Clear();
		_treeViewItemsByValue.Clear();

		if (NodesSource == null)
		{
			PreviewTreeView.ItemsSource = null;
			PreviewTreeView.Items.Clear();
			UpdateSelectedTreeViewItems();
			RebuildCompleted?.Invoke(this, EventArgs.Empty);
			return;
		}

		var threshold = CollapseAllThresholdItemCount;
		var startCollapsed = threshold > 0 && NodesSource.Count > threshold;

		var snapshot = new TreeViewSourceEntry[NodesSource.Count];
		for (var index = 0; index < snapshot.Length; index++)
		{
			var entry = NodesSource[index];
			snapshot[index] = new TreeViewSourceEntry
			{
				Value = entry.Value,
				Type = entry.Type,
			};
		}

		var dispatcher = Dispatcher;

		Task.Run(() =>
		{
			var rootNodes = BuildTreeViewNodes(snapshot);

			dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action(() =>
				{
					if (generation != Interlocked.Read(ref _currentRebuildGeneration))
					{
						return;
					}

					FinishRebuildTreeViewItems(rootNodes, selectedValues, useMultipleSelection, treeViewItemStyle, startCollapsed, snapshot.Length);
				}));
		});
	}

	private void FinishRebuildTreeViewItems(List<TreeViewNode> rootNodes, HashSet<string> selectedValues, bool useMultipleSelection, Style treeViewItemStyle, bool startCollapsed, int sourceEntryCount)
	{
		var estimatedItemCount = sourceEntryCount * 2;
		if (_treeViewItemsByValue.Count == 0 && estimatedItemCount > 0)
		{
			// Dictionary<TKey,TValue>.EnsureCapacity is a .NET Core / .NET 5+ API and is not
			// available on the older framework versions this control needs to stay compatible
			// with. Reallocating the dictionary with the estimated capacity produces the same
			// grow-avoidance benefit without depending on the newer API.
			_treeViewItemsByValue = new Dictionary<string, CFTreeViewItem>(estimatedItemCount, StringComparer.OrdinalIgnoreCase);
		}

		var rootItems = new List<CFTreeViewItem>(rootNodes.Count);
		var collectSelections = selectedValues.Count > 0;

		foreach (var rootNode in rootNodes)
		{
			var treeViewItem = CreateTreeViewItem(rootNode, selectedValues, useMultipleSelection, treeViewItemStyle, _treeViewItemsByValue, startCollapsed, collectSelections, _selectedTreeViewItems);
			rootItems.Add(treeViewItem);
		}

		PreviewTreeView.ItemsSource = null;
		PreviewTreeView.Items.Clear();
		PreviewTreeView.ItemsSource = rootItems;

		UpdateSelectedTreeViewItems();
		RebuildCompleted?.Invoke(this, EventArgs.Empty);
	}

	private void ApplyCollapseAllThreshold()
	{
		var threshold = CollapseAllThresholdItemCount;
		if (threshold <= 0 || NodesSource == null)
		{
			return;
		}

		if (NodesSource.Count <= threshold)
		{
			return;
		}

		foreach (var rootItem in GetRootTreeViewItems())
		{
			CollapseItemAndChildren(rootItem);
		}
	}

	private static void AddParentPaths(ISet<string> expandedValues, string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		var currentValue = value;
		while (true)
		{
			var separatorIndex = currentValue.LastIndexOf('/');
			if (separatorIndex <= 0)
			{
				break;
			}

			currentValue = currentValue.Substring(0, separatorIndex);
			expandedValues.Add(currentValue);
		}
	}

	private static void ApplyExpansionState(CFTreeViewItem treeViewItem, ISet<string> expandedValues)
	{
		treeViewItem.IsExpanded = expandedValues.Contains(treeViewItem.Value);

		foreach (var childItem in treeViewItem.Items.OfType<CFTreeViewItem>())
		{
			ApplyExpansionState(childItem, expandedValues);
		}
	}

	private static void CollapseItemAndChildren(CFTreeViewItem treeViewItem)
	{
		treeViewItem.IsExpanded = false;

		foreach (var childItem in treeViewItem.Items.OfType<CFTreeViewItem>())
		{
			CollapseItemAndChildren(childItem);
		}
	}

	private HashSet<string> GetSelectedValueSet()
	{
		if (SelectedValues != null && (_hasExternalSelectionState || _isApplyingExternalSelection))
		{
			if (SelectedValues.Count == 0)
			{
				return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}

			return new HashSet<string>(SelectedValues, StringComparer.OrdinalIgnoreCase);
		}

		if (_selectedTreeViewItems.Count == 0)
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in _selectedTreeViewItems)
		{
			result.Add(item.Value);
		}

		return result;
	}

	private static List<TreeViewNode> BuildTreeViewNodes(IList<TreeViewSourceEntry> sourceEntries)
	{
		var rootNodes = new List<TreeViewNode>();
		var rootIndex = new Dictionary<string, TreeViewNode>(StringComparer.OrdinalIgnoreCase);

		for (var sourceIndex = 0; sourceIndex < sourceEntries.Count; sourceIndex++)
		{
			var sourceEntry = sourceEntries[sourceIndex];
			var rawValue = sourceEntry.Value;
			if (string.IsNullOrEmpty(rawValue))
			{
				continue;
			}

			var segments = rawValue.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length == 0)
			{
				continue;
			}

			List<TreeViewNode> currentList = rootNodes;
			Dictionary<string, TreeViewNode> currentIndex = rootIndex;
			string currentPath = null;

			for (var index = 0; index < segments.Length; index++)
			{
				var segment = segments[index];
				var isLeaf = index == segments.Length - 1;
				currentPath = currentPath == null ? segment : string.Concat(currentPath, "/", segment);

				TreeViewNode existingNode;
				if (!currentIndex.TryGetValue(currentPath, out existingNode))
				{
					var nodeType = isLeaf ? sourceEntry.Type : "folder";
					existingNode = new TreeViewNode
					{
						Text = segment,
						Value = currentPath,
						Icon = TreeViewIconMap.GetIcon(nodeType),
					};
					currentList.Add(existingNode);
					currentIndex.Add(currentPath, existingNode);
				}
				else if (isLeaf)
				{
					existingNode.Icon = TreeViewIconMap.GetIcon(sourceEntry.Type);
				}

				currentList = existingNode.Children;
				currentIndex = existingNode.ChildIndex;
			}
		}

		return rootNodes;
	}

	private static CFTreeViewItem CreateTreeViewItem(TreeViewNode treeViewNode, IReadOnlyCollection<string> selectedValues, bool useMultipleSelection, Style treeViewItemStyle, IDictionary<string, CFTreeViewItem> treeViewItemsByValue, bool startCollapsed, bool collectSelections, List<CFTreeViewItem> selectionSink)
	{
		var childCount = treeViewNode.Children.Count;
		var hasChildren = childCount > 0;
		var isSelected = collectSelections && selectedValues.Contains(treeViewNode.Value);
		var isMultiSelected = isSelected && useMultipleSelection;
		var isSingleSelected = isSelected && !useMultipleSelection;
		var shouldBeExpanded = !startCollapsed && hasChildren;

		var treeViewItem = new CFTreeViewItem
		{
			Icon = treeViewNode.Icon,
			Text = treeViewNode.Text,
			Value = treeViewNode.Value,
			Style = treeViewItemStyle,
		};

		if (shouldBeExpanded)
		{
			treeViewItem.IsExpanded = true;
		}

		if (isMultiSelected)
		{
			treeViewItem.IsMultiSelected = true;
		}

		if (isSingleSelected)
		{
			treeViewItem.IsSelected = true;
		}

		if (isSelected)
		{
			selectionSink.Add(treeViewItem);
		}

		treeViewItemsByValue[treeViewNode.Value] = treeViewItem;

		if (hasChildren)
		{
			var itemsCollection = treeViewItem.Items;
			for (var childIndex = 0; childIndex < childCount; childIndex++)
			{
				var childItem = CreateTreeViewItem(treeViewNode.Children[childIndex], selectedValues, useMultipleSelection, treeViewItemStyle, treeViewItemsByValue, startCollapsed, collectSelections, selectionSink);
				itemsCollection.Add(childItem);
			}
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
		// Sources for this control are always full-replace (Clear + N Adds, or a single Reset),
		// so a single notification burst should coalesce into exactly one rebuild.
		// In short all calls coming in in the same frame will automatically be batched.
		// We automatically open a batch on the first change we see and close it on a background-
		// priority dispatcher tick, which runs after the current burst of CollectionChanged
		// callbacks but before the next render pass. Any manual BeginUpdate/EndUpdate calls made
		// by the host still nest correctly through the same suppression counter.
		if (!_isCoalescingSourceChanges)
		{
			_isCoalescingSourceChanges = true;
			BeginUpdate();
			Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action(EndCoalescedSourceChangeBatch));
		}

		RequestRebuildTreeViewItems();
	}

	private void EndCoalescedSourceChangeBatch()
	{
		if (!_isCoalescingSourceChanges)
		{
			return;
		}

		_isCoalescingSourceChanges = false;
		EndUpdate();
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

		Dispatcher.BeginInvoke(
			DispatcherPriority.Loaded,
			new Action(() =>
			{
				selectedItem.BringIntoView();
			}));
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

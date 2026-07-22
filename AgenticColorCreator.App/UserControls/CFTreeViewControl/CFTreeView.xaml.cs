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

	public static readonly DependencyProperty EnableLazyChildMaterializationProperty = DependencyProperty.Register(
		nameof(EnableLazyChildMaterialization),
		typeof(bool),
		typeof(CFTreeView),
		new PropertyMetadata(false));

	public static readonly DependencyProperty LazyChildMaterializationDepthProperty = DependencyProperty.Register(
		nameof(LazyChildMaterializationDepth),
		typeof(int),
		typeof(CFTreeView),
		new PropertyMetadata(2));

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
	/// When <c>true</c>, subtrees at or below <see cref="LazyChildMaterializationDepth"/> are not
	/// materialized until the parent item is expanded. A single sentinel child is inserted so the
	/// expand chevron still renders. This significantly reduces initial rebuild cost for very
	/// large sources at the price of a small delay the first time each subtree is expanded.
	/// <para>
	/// Lazy materialization is skipped entirely when the current rebuild would otherwise start
	/// with everything expanded — that is, when the source item count is at or below
	/// <see cref="CollapseAllThresholdItemCount"/> — because there is no visible collapse state
	/// to defer work behind in that scenario.
	/// </para>
	/// </summary>
	public bool EnableLazyChildMaterialization
	{
		get => (bool)GetValue(EnableLazyChildMaterializationProperty);
		set => SetValue(EnableLazyChildMaterializationProperty, value);
	}

	/// <summary>
	/// Zero-based depth threshold at which lazy child materialization becomes active. Root items
	/// are depth <c>0</c>. A value of <c>2</c> means depths <c>0</c> and <c>1</c> are always
	/// materialized eagerly, and every subtree rooted at depth <c>2</c> or deeper defers its
	/// children until first expansion. Only meaningful when
	/// <see cref="EnableLazyChildMaterialization"/> is <c>true</c>.
	/// </summary>
	public int LazyChildMaterializationDepth
	{
		get => (int)GetValue(LazyChildMaterializationDepthProperty);
		set => SetValue(LazyChildMaterializationDepthProperty, value);
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

	/// <summary>
	/// Selects the first root item currently in the tree, brings it into view, moves keyboard
	/// focus to the control, and returns the selected item (or <c>null</c> if the tree is empty).
	/// Selection is applied through the same code path used by manual clicks, so
	/// <see cref="SelectedValues"/> and <see cref="SelectedTreeViewItems"/> are both updated and
	/// any bound host will observe the change.
	/// </summary>
	public CFTreeViewItem SelectFirstItemAndFocus()
	{
		CFTreeViewItem firstItem = null;
		foreach (var rootItem in GetRootTreeViewItems())
		{
			firstItem = rootItem;
			break;
		}

		if (firstItem == null)
		{
			return null;
		}

		// Clear any prior forced/manual selection state before applying the new one so
		// SelectedValues does not accumulate stale entries.
		if (_hasExternalSelectionState)
		{
			ClearForcedSelectionState();
			_hasExternalSelectionState = false;
		}

		if (IsMultiSelect)
		{
			SelectSingleItem(firstItem);
		}
		else
		{
			// Non-multi-select mode uses the built-in TreeView selection. Setting IsSelected
			// routes through OnSelectedItemChanged, which updates SelectedTreeViewItems and
			// SelectedValues.
			foreach (var item in _selectedTreeViewItems)
			{
				item.IsMultiSelected = false;
			}

			_selectedTreeViewItems.Clear();
			firstItem.IsSelected = true;
		}

		firstItem.BringIntoView();

		// Focus has to happen after the item container is fully in the visual tree. When the
		// tree was just rebuilt the container may not yet have completed layout, so defer the
		// focus call one dispatcher tick at Loaded priority. Both Focus() and Keyboard.Focus()
		// can throw InvalidOperationException if the target is not currently focusable (item not
		// yet realized, not loaded, IsEnabled=false, etc.), so we guard the call.
		Dispatcher.BeginInvoke(
			DispatcherPriority.Loaded,
			new Action(() =>
			{
				try
				{
					if (!firstItem.IsLoaded || !firstItem.Focusable || !firstItem.IsVisible)
					{
						return;
					}

					firstItem.Focus();
				}
				catch (InvalidOperationException)
				{
					// Focus was rejected by the framework (usually because the container was
					// virtualized away or torn down between BringIntoView and this callback).
					// Swallow: the visual selection is still correct, the user can click.
				}
			}));

		return firstItem;
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

	private void OnPreviewTreeViewPreviewKeyDown(object sender, KeyEventArgs e)
	{		if (e.Key != Key.Enter)
		{
			return;
		}

		// Only react when a CFTreeViewItem currently has keyboard focus. This is normally the
		// case after the user navigates with the arrow keys because TreeView moves focus to the
		// container on each arrow press.
		var focusedItem = Keyboard.FocusedElement as CFTreeViewItem
			?? FindAncestor<CFTreeViewItem>(Keyboard.FocusedElement as DependencyObject);
		if (focusedItem == null)
		{
			return;
		}

		if (_hasExternalSelectionState)
		{
			ClearForcedSelectionState();
			_hasExternalSelectionState = false;
		}

		if (IsMultiSelect && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		{
			// Ctrl+Enter toggles multi-selection the same way Ctrl+Click does. We intentionally
			// do not force IsSelected on this branch because a toggled-off item should not remain
			// the TreeView's current-selection anchor either.
			ToggleItemSelection(focusedItem);
		}
		else
		{
			CommitKeyboardSingleSelection(focusedItem);
		}

		// The focused item already has keyboard focus (that is how we found it), so we do not
		// need to call Focus() again. Doing so during a selection-change reentry can trip
		// InvalidOperationException from PresentationCore because focus routing is temporarily
		// locked while the built-in TreeView is updating its own selection anchor.
		e.Handled = true;
	}

	private void OnPreviewTreeViewKeyDown(object sender, KeyEventArgs e)
	{
		// The inner TreeView handles arrow keys, PageUp/PageDown, Home and End in its own
		// OnKeyDown override to move the focused item. When there is nowhere left to move
		// (e.g. the user hit Down on the last visible row) the framework leaves the event
		// unhandled and it bubbles up to any ancestor ScrollViewer, which then scrolls the
		// whole preview page. From this control's perspective navigation keys always belong to
		// the tree, so we swallow them here (on the bubbling KeyDown, after TreeView had its
		// turn) to prevent the outer ScrollViewer from ever seeing them.
		switch (e.Key)
		{
			case Key.Up:
			case Key.Down:
			case Key.Left:
			case Key.Right:
			case Key.PageUp:
			case Key.PageDown:
			case Key.Home:
			case Key.End:
				e.Handled = true;
				break;
		}
	}

	private void CommitKeyboardSingleSelection(CFTreeViewItem focusedItem)
	{
		// SelectSingleItem intentionally leaves IsSelected = false so the built-in TreeView
		// blue-highlight does not paint on top of the CF multi-selection visual. That is fine
		// for mouse clicks, but for keyboard commit it breaks arrow-key navigation: WPF's
		// TreeView uses IsSelected as the anchor for the next arrow keystroke, so with no
		// selected item present the arrow keys jump back to the first root.
		//
		// The CF style already renders identical visuals for IsSelected and IsMultiSelected, so
		// leaving both true after a keyboard commit is safe. We clear the visuals on the
		// previous items ourselves, then set both flags on the focused item and record it in the
		// selection sink.
		foreach (var item in _selectedTreeViewItems)
		{
			if (ReferenceEquals(item, focusedItem))
			{
				continue;
			}

			item.IsMultiSelected = false;
			item.IsSelected = false;
		}

		_selectedTreeViewItems.Clear();

		if (IsMultiSelect)
		{
			focusedItem.IsMultiSelected = true;
		}

		focusedItem.IsSelected = true;
		_selectedTreeViewItems.Add(focusedItem);
		UpdateSelectedTreeViewItems();
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

		// Lazy child materialization is only active when the source is large enough that we're
		// already starting collapsed. If everything would be visible-expanded anyway there is no
		// visible collapse state to defer work behind, so lazy would just add a first-expand
		// stutter with zero benefit.
		var lazyEnabled = EnableLazyChildMaterialization && startCollapsed;
		var lazyDepth = lazyEnabled ? Math.Max(0, LazyChildMaterializationDepth) : -1;

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

					FinishRebuildTreeViewItems(rootNodes, selectedValues, useMultipleSelection, treeViewItemStyle, startCollapsed, snapshot.Length, lazyDepth);
				}));
		});
	}

	private void FinishRebuildTreeViewItems(List<TreeViewNode> rootNodes, HashSet<string> selectedValues, bool useMultipleSelection, Style treeViewItemStyle, bool startCollapsed, int sourceEntryCount, int lazyDepth)
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

		// When lazy is active we still need to materialize every ancestor of any selected value,
		// otherwise external selection would silently do nothing (the item does not yet exist in
		// the lookup dictionary). This set is intentionally empty when there is no selection.
		HashSet<string> forcedMaterializationValues = null;
		if (lazyDepth >= 0 && selectedValues.Count > 0)
		{
			forcedMaterializationValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var selectedValue in selectedValues)
			{
				var current = selectedValue;
				while (!string.IsNullOrEmpty(current))
				{
					forcedMaterializationValues.Add(current);
					var separatorIndex = current.LastIndexOf('/');
					if (separatorIndex <= 0)
					{
						break;
					}

					current = current.Substring(0, separatorIndex);
				}
			}
		}

		var rootItems = new List<CFTreeViewItem>(rootNodes.Count);
		var collectSelections = selectedValues.Count > 0;

		foreach (var rootNode in rootNodes)
		{
			var treeViewItem = CreateTreeViewItem(rootNode, selectedValues, useMultipleSelection, treeViewItemStyle, _treeViewItemsByValue, startCollapsed, collectSelections, _selectedTreeViewItems, 0, lazyDepth, forcedMaterializationValues);
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

	private CFTreeViewItem CreateTreeViewItem(TreeViewNode treeViewNode, IReadOnlyCollection<string> selectedValues, bool useMultipleSelection, Style treeViewItemStyle, IDictionary<string, CFTreeViewItem> treeViewItemsByValue, bool startCollapsed, bool collectSelections, List<CFTreeViewItem> selectionSink, int depth, int lazyDepth, HashSet<string> forcedMaterializationValues)
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
			SourceNode = treeViewNode,
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

		if (!hasChildren)
		{
			return treeViewItem;
		}

		// Decide whether to materialize this subtree now or defer it until first expansion.
		// Rules:
		//   * lazyDepth < 0 means the master switch is off (or start-collapsed didn't fire), so
		//     always materialize eagerly to preserve the pre-lazy behavior.
		//   * Subtrees whose value is on the ancestor path of a selected item must always be
		//     materialized eagerly so external selection can find the target CFTreeViewItem.
		//   * Otherwise, subtrees rooted at depth >= lazyDepth defer their children.
		var isForced = forcedMaterializationValues != null && forcedMaterializationValues.Contains(treeViewNode.Value);
		var deferChildren = lazyDepth >= 0 && depth >= lazyDepth && !isForced;

		if (deferChildren)
		{
			InsertLazyPlaceholder(treeViewItem);
			return treeViewItem;
		}

		var itemsCollection = treeViewItem.Items;
		for (var childIndex = 0; childIndex < childCount; childIndex++)
		{
			var childItem = CreateTreeViewItem(treeViewNode.Children[childIndex], selectedValues, useMultipleSelection, treeViewItemStyle, treeViewItemsByValue, startCollapsed, collectSelections, selectionSink, depth + 1, lazyDepth, forcedMaterializationValues);
			itemsCollection.Add(childItem);
		}

		return treeViewItem;
	}

	private void InsertLazyPlaceholder(CFTreeViewItem parentItem)
	{
		// A single sentinel child is enough for WPF to render the expand chevron on the parent
		// TreeViewItem. It is a plain TreeViewItem (not a CFTreeViewItem) with Visibility set to
		// Collapsed so it takes no visible space and does not render as "System.Object" while the
		// parent is briefly expanded. Using a real container also avoids the WPF default
		// item-container fallback that displayed the string form of the previous raw-object
		// sentinel before materialization completed.
		var placeholder = new TreeViewItem
		{
			Visibility = Visibility.Collapsed,
			Focusable = false,
			IsEnabled = false,
		};

		parentItem.Items.Add(placeholder);
		parentItem.HasLazyPlaceholder = true;
		parentItem.Expanded += OnLazyItemExpanded;
	}

	private void OnLazyItemExpanded(object sender, RoutedEventArgs e)
	{
		if (!(sender is CFTreeViewItem treeViewItem))
		{
			return;
		}

		if (!treeViewItem.HasLazyPlaceholder)
		{
			return;
		}

		// The Expanded event bubbles from descendants; ignore anything that is not the item we
		// actually hooked, otherwise we would try to materialize the wrong subtree.
		if (!ReferenceEquals(e.OriginalSource, sender))
		{
			return;
		}

		treeViewItem.HasLazyPlaceholder = false;
		treeViewItem.Expanded -= OnLazyItemExpanded;

		MaterializeLazyChildren(treeViewItem);
	}

	private void MaterializeLazyChildren(CFTreeViewItem parentItem)
	{
		var sourceNode = parentItem.SourceNode;
		if (sourceNode == null)
		{
			return;
		}

		var treeViewItemStyle = PreviewTreeView.TryFindResource("CF.TreeViewItem") as Style;
		var selectedValues = GetSelectedValueSet();
		var useMultipleSelection = IsMultiSelect && selectedValues.Count > 1;
		var collectSelections = selectedValues.Count > 0;
		var lazyDepth = EnableLazyChildMaterialization ? Math.Max(0, LazyChildMaterializationDepth) : -1;

		// Compute depth of the item being expanded by walking up its parent chain of
		// CFTreeViewItem containers. That way we correctly propagate the deferred-materialization
		// rule to grandchildren of the just-expanded node.
		var depth = 0;
		DependencyObject walker = ItemsControl.ItemsControlFromItemContainer(parentItem);
		while (walker is CFTreeViewItem)
		{
			depth++;
			walker = ItemsControl.ItemsControlFromItemContainer((CFTreeViewItem)walker);
		}

		parentItem.Items.Clear();

		var itemsCollection = parentItem.Items;
		var childNodes = sourceNode.Children;
		for (var childIndex = 0; childIndex < childNodes.Count; childIndex++)
		{
			// startCollapsed = true here is deliberate: when the user expands a lazy parent, we
			// only want that one level to appear. Setting startCollapsed = false would cause
			// every newly-materialized child to auto-expand, which in turn triggers their own
			// lazy sentinels to materialize, cascading the whole subtree in a single click.
			var childItem = CreateTreeViewItem(childNodes[childIndex], selectedValues, useMultipleSelection, treeViewItemStyle, _treeViewItemsByValue, true, collectSelections, _selectedTreeViewItems, depth + 1, lazyDepth, null);
			itemsCollection.Add(childItem);
		}

		if (_selectedTreeViewItems.Count > 0)
		{
			UpdateSelectedTreeViewItems();
		}
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

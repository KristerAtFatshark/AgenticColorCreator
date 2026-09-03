using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Presents an <see cref="ICFTreeViewItem"/> hierarchy through one recycling ListView. Source changes
/// rebuild the persistent graph; match, expansion, and selection changes update only its flat row view.
/// </summary>
public partial class CFListTreeView : UserControl
{
	public static readonly DependencyProperty SourceItemsProperty = DependencyProperty.Register(
		nameof(SourceItems), typeof(IEnumerable), typeof(CFListTreeView), new PropertyMetadata(null, OnSourceItemsChanged));
	public static readonly DependencyProperty MatchedItemsProperty = DependencyProperty.Register(
		nameof(MatchedItems), typeof(IEnumerable), typeof(CFListTreeView), new PropertyMetadata(null, OnMatchedItemsChanged));
	public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
		nameof(SelectedItems), typeof(IList), typeof(CFListTreeView), new PropertyMetadata(null, OnSelectedItemsChanged));
	public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
		nameof(ItemTemplate), typeof(DataTemplate), typeof(CFListTreeView), new PropertyMetadata(null));
	public static readonly DependencyProperty IsMultiSelectProperty = DependencyProperty.Register(
		nameof(IsMultiSelect), typeof(bool), typeof(CFListTreeView), new PropertyMetadata(false, OnIsMultiSelectChanged));
	public static readonly DependencyProperty CollapseAllThresholdProperty = DependencyProperty.Register(
		nameof(CollapseAllThreshold), typeof(int), typeof(CFListTreeView), new PropertyMetadata(100, OnCollapseAllThresholdChanged));
	public static readonly DependencyProperty ShowResourceTypeProperty = DependencyProperty.Register(
		nameof(ShowResourceType), typeof(bool), typeof(CFListTreeView), new PropertyMetadata(true));
	private static readonly DependencyPropertyKey VisibleRowCountPropertyKey = DependencyProperty.RegisterReadOnly(
		nameof(VisibleRowCount), typeof(int), typeof(CFListTreeView), new PropertyMetadata(0));
	public static readonly DependencyProperty VisibleRowCountProperty = VisibleRowCountPropertyKey.DependencyProperty;
	private static readonly DependencyPropertyKey IsFilterActivePropertyKey = DependencyProperty.RegisterReadOnly(
		nameof(IsFilterActive), typeof(bool), typeof(CFListTreeView), new PropertyMetadata(false));
	public static readonly DependencyProperty IsFilterActiveProperty = IsFilterActivePropertyKey.DependencyProperty;

	private readonly List<CFListTreeNode> _roots = new();
	private readonly List<CFListTreeNode> _allNodes = new();
	private readonly List<CFListTreeNode> _filterVisibleNodes = new();
	private readonly Dictionary<object, CFListTreeNode> _nodesByItem = new(ReferenceEqualityComparer.Instance);
	private readonly List<CFListTreeViewRow> _selectedRows = new();
	private CFListTreeViewRow? _focusedRow;
	private INotifyCollectionChanged? _sourceNotifier;
	private INotifyCollectionChanged? _matchedNotifier;
	private INotifyCollectionChanged? _selectedNotifier;
	private bool _isUpdatingSelectedItems;
	private bool _sourceRefreshQueued;
	private bool _matchedRefreshQueued;
	private int _sourceRefreshGeneration;
	private int _matchedRefreshGeneration;
	private readonly Dictionary<string, CFListTreeViewIconImages> _iconImages = new(StringComparer.Ordinal);

	public CFListTreeView()
	{
		InitializeComponent();
	}

	public event EventHandler? StructureLoadCompleted;

	public IEnumerable? SourceItems
	{
		get => (IEnumerable?)GetValue(SourceItemsProperty);
		set => SetValue(SourceItemsProperty, value);
	}

	public IEnumerable? MatchedItems
	{
		get => (IEnumerable?)GetValue(MatchedItemsProperty);
		set => SetValue(MatchedItemsProperty, value);
	}

	public IList? SelectedItems
	{
		get => (IList?)GetValue(SelectedItemsProperty);
		set => SetValue(SelectedItemsProperty, value);
	}

	public DataTemplate? ItemTemplate
	{
		get => (DataTemplate?)GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public bool IsMultiSelect
	{
		get => (bool)GetValue(IsMultiSelectProperty);
		set => SetValue(IsMultiSelectProperty, value);
	}

	public int CollapseAllThreshold
	{
		get => (int)GetValue(CollapseAllThresholdProperty);
		set => SetValue(CollapseAllThresholdProperty, value);
	}

	public bool ShowResourceType
	{
		get => (bool)GetValue(ShowResourceTypeProperty);
		set => SetValue(ShowResourceTypeProperty, value);
	}

	public int VisibleRowCount => (int)GetValue(VisibleRowCountProperty);

	public bool IsFilterActive => (bool)GetValue(IsFilterActiveProperty);

	public void CollapseAll()
	{
		foreach (var node in _allNodes)
		{
			if (node.IsFolder)
			{
				node.Row.IsExpanded = false;
			}
		}

		RefreshVisibleRows();
	}

	public void CollapseAllExceptSelectedItemParents()
	{
		var expanded = new HashSet<CFListTreeNode>();
		foreach (var row in _selectedRows)
		{
			var parent = row.Node.Parent;
			while (parent != null)
			{
				expanded.Add(parent);
				parent = parent.Parent;
			}
		}

		foreach (var node in _allNodes)
		{
			if (node.IsFolder)
			{
				node.Row.IsExpanded = expanded.Contains(node);
			}
		}

		RefreshVisibleRows();
	}

	public CFListTreeViewRow? SelectFirstItemAndFocus()
	{
		var node = MatchedItems == null
			? FindFirstLeaf(_roots)
			: GetVisibleRows().FirstOrDefault(row => !row.IsFolder)?.Node;
		if (node == null)
		{
			return null;
		}

		var parent = node.Parent;
		while (parent != null)
		{
			parent.Row.IsExpanded = true;
			parent = parent.Parent;
		}
		RefreshVisibleRows();
		var row = node.Row;
		SelectOnly(row);
		FocusRow(row);
		return row;
	}

	private static void OnSourceItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (CFListTreeView)d;
		control._sourceRefreshGeneration++;
		control._sourceRefreshQueued = false;
		control.ReplaceSubscription(ref control._sourceNotifier, e.NewValue as INotifyCollectionChanged, control.OnSourceCollectionChanged);
		control.RebuildStructure();
	}

	private static void OnMatchedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (CFListTreeView)d;
		control._matchedRefreshGeneration++;
		control._matchedRefreshQueued = false;
		control.ReplaceSubscription(ref control._matchedNotifier, e.NewValue as INotifyCollectionChanged, control.OnMatchedCollectionChanged);
		control.ApplyMatchMask();
	}

	private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (CFListTreeView)d;
		if (e.NewValue is IList selectedItems && (selectedItems.IsReadOnly || selectedItems.IsFixedSize))
		{
			throw new ArgumentException("SelectedItems must be a mutable IList.", nameof(SelectedItems));
		}
		control.ReplaceSubscription(ref control._selectedNotifier, e.NewValue as INotifyCollectionChanged, control.OnSelectedCollectionChanged);
		control.ApplyExternalSelection();
	}

	private static void OnIsMultiSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (CFListTreeView)d;
		if (!(bool)e.NewValue && control._selectedRows.Count > 1)
		{
			control.SelectOnly(control._selectedRows[0]);
		}
	}

	private static void OnCollapseAllThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((CFListTreeView)d).ApplyCollapseAllThreshold();
	}

	private void ReplaceSubscription(ref INotifyCollectionChanged? current, INotifyCollectionChanged? next, NotifyCollectionChangedEventHandler handler)
	{
		if (current != null)
		{
			current.CollectionChanged -= handler;
		}

		current = next;
		if (current != null)
		{
			current.CollectionChanged += handler;
		}
	}

	private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_sourceRefreshQueued)
		{
			return;
		}

		_sourceRefreshQueued = true;
		var generation = _sourceRefreshGeneration;
		Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
		{
			if (generation != _sourceRefreshGeneration)
			{
				return;
			}
			_sourceRefreshQueued = false;
			RebuildStructure();
		}));
	}

	private void OnMatchedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_matchedRefreshQueued)
		{
			return;
		}

		_matchedRefreshQueued = true;
		var generation = _matchedRefreshGeneration;
		Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
		{
			if (generation != _matchedRefreshGeneration)
			{
				return;
			}
			_matchedRefreshQueued = false;
			ApplyMatchMask();
		}));
	}

	private void OnSelectedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (!_isUpdatingSelectedItems)
		{
			ApplyExternalSelection();
		}
	}

	private void RebuildStructure()
	{
		_roots.Clear();
		_allNodes.Clear();
		_filterVisibleNodes.Clear();
		_nodesByItem.Clear();
		_selectedRows.Clear();
		_focusedRow = null;
		var folders = new Dictionary<string, CFListTreeNode>(StringComparer.OrdinalIgnoreCase);
		var terminalFolders = new Dictionary<string, CFListTreeNode>(StringComparer.OrdinalIgnoreCase);
		var sourceIndex = 0;

		if (SourceItems != null)
		{
			foreach (var sourceItem in SourceItems)
			{
				if (sourceItem is not ICFTreeViewItem item)
				{
					sourceIndex++;
					continue;
				}
				if (_nodesByItem.ContainsKey(sourceItem))
				{
					sourceIndex++;
					continue;
				}

				var parent = BuildFolderPath(item.TreeFolderPath, folders, terminalFolders, ref sourceIndex);
				var node = new CFListTreeNode
				{
					IsFolder = false,
					SortKey = NormalizeSortKey(item.TreeSortKey),
					SourceIndex = sourceIndex++,
					Parent = parent,
					Depth = parent == null ? 0 : parent.Depth + 1,
				};
				node.Row = new CFListTreeViewRow(
					false,
					item.ResourceName,
					item.ResourceType,
					sourceItem,
					GetIconImages(CFListTreeViewIconMap.GetIconResourceKey(item.ResourceType)),
					node.Depth)
				{
					Node = node,
				};
				(parent?.Children ?? _roots).Add(node);
				_allNodes.Add(node);
				_nodesByItem[sourceItem] = node;
			}
		}

		SortNodes(_roots);
		ApplyCollapseAllThreshold(false);
		ApplyMatchMask();
		ApplyExternalSelection();
		SyncSelectedItems();
		StructureLoadCompleted?.Invoke(this, EventArgs.Empty);
	}

	private CFListTreeNode? BuildFolderPath(
		string? folderPath,
		IDictionary<string, CFListTreeNode> folders,
		IDictionary<string, CFListTreeNode> terminalFolders,
		ref int sourceIndex)
	{
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			return null;
		}
		if (terminalFolders.TryGetValue(folderPath, out var terminalFolder))
		{
			return terminalFolder;
		}

		CFListTreeNode? parent = null;
		var fullPath = string.Empty;
		foreach (var rawSegment in folderPath.Split('/'))
		{
			var segment = rawSegment.Trim();
			if (segment.Length == 0)
			{
				continue;
			}

			fullPath = fullPath.Length == 0 ? segment : fullPath + "/" + segment;
			if (!folders.TryGetValue(fullPath, out var folder))
			{
				folder = new CFListTreeNode
				{
					IsFolder = true,
					SortKey = segment,
					SourceIndex = sourceIndex++,
					Parent = parent,
					Depth = parent == null ? 0 : parent.Depth + 1,
				};
				folder.Row = new CFListTreeViewRow(true, segment, string.Empty, null, GetIconImages("icon-folder"), folder.Depth) { Node = folder };
				(parent?.Children ?? _roots).Add(folder);
				_allNodes.Add(folder);
				folders.Add(fullPath, folder);
			}

			parent = folder;
		}

		if (parent != null)
		{
			terminalFolders[folderPath] = parent;
		}

		return parent;
	}

	private CFListTreeViewIconImages GetIconImages(string resourceKey)
	{
		if (_iconImages.TryGetValue(resourceKey, out var images))
		{
			return images;
		}

		var glyph = TryFindResource(resourceKey) as string ?? "\ue903";
		images = new CFListTreeViewIconImages(
			glyph,
			FindBrush("CF.TreeViewItem.Default.Icon"),
			FindBrush("CF.TreeViewItem.MouseOver.Icon"),
			FindBrush("CF.TreeViewItem.Selected.Icon"));
		_iconImages.Add(resourceKey, images);
		return images;
	}

	private Brush FindBrush(string resourceKey)
	{
		return TryFindResource(resourceKey) as Brush ?? Brushes.White;
	}

	private static string NormalizeSortKey(string? value)
	{
		return value?.Trim() ?? string.Empty;
	}

	private static void SortNodes(List<CFListTreeNode> nodes)
	{
		if (nodes.Count > 1)
		{
			nodes.Sort((left, right) =>
			{
				var kind = right.IsFolder.CompareTo(left.IsFolder);
				if (kind != 0)
				{
					return kind;
				}

				var key = StringComparer.OrdinalIgnoreCase.Compare(left.SortKey, right.SortKey);
				return key != 0 ? key : left.SourceIndex.CompareTo(right.SourceIndex);
			});
		}

		foreach (var node in nodes)
		{
			if (node.IsFolder)
			{
				SortNodes(node.Children);
			}
		}
	}

	private void ApplyCollapseAllThreshold(bool refreshVisibleRows = true)
	{
		if (CollapseAllThreshold <= 0 || _nodesByItem.Count <= CollapseAllThreshold)
		{
			return;
		}

		foreach (var node in _allNodes)
		{
			if (node.IsFolder)
			{
				node.Row.IsExpanded = false;
			}
		}

		if (refreshVisibleRows)
		{
			RefreshVisibleRows();
		}
	}

	private void ApplyMatchMask()
	{
		SetValue(IsFilterActivePropertyKey, MatchedItems != null);
		foreach (var node in _filterVisibleNodes)
		{
			node.IsFilterVisible = false;
		}
		_filterVisibleNodes.Clear();

		if (MatchedItems != null)
		{
			foreach (var item in MatchedItems)
			{
				if (item == null || !_nodesByItem.TryGetValue(item, out var node))
				{
					continue;
				}

				while (node != null)
				{
					if (node.IsFilterVisible)
					{
						break;
					}

					node.IsFilterVisible = true;
					_filterVisibleNodes.Add(node);
					node = node.Parent;
				}
			}
		}

		RefreshVisibleRows();
	}

	private void RefreshVisibleRows()
	{
		var rows = new List<CFListTreeViewRow>();
		AppendVisibleRows(_roots, rows, MatchedItems != null);
		RowsListView.ItemsSource = rows;
		if (_focusedRow != null && IsNodeVisible(_focusedRow.Node))
		{
			RowsListView.SelectedItem = _focusedRow;
		}
		else
		{
			_focusedRow = null;
		}
		SetValue(VisibleRowCountPropertyKey, rows.Count);
	}

	private static void AppendVisibleRows(IEnumerable<CFListTreeNode> nodes, ICollection<CFListTreeViewRow> rows, bool filterActive)
	{
		foreach (var node in nodes)
		{
			if (filterActive && !node.IsFilterVisible)
			{
				continue;
			}

			rows.Add(node.Row);
			if (node.IsFolder && (filterActive || node.Row.IsExpanded))
			{
				AppendVisibleRows(node.Children, rows, filterActive);
			}
		}
	}

	private static CFListTreeNode? FindFirstLeaf(IEnumerable<CFListTreeNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (!node.IsFolder)
			{
				return node;
			}

			var leaf = FindFirstLeaf(node.Children);
			if (leaf != null)
			{
				return leaf;
			}
		}

		return null;
	}

	private IReadOnlyList<CFListTreeViewRow> GetVisibleRows()
	{
		return RowsListView.ItemsSource as IReadOnlyList<CFListTreeViewRow> ?? Array.Empty<CFListTreeViewRow>();
	}

	private void OnExpanderClick(object sender, RoutedEventArgs e)
	{
		if ((sender as FrameworkElement)?.DataContext is CFListTreeViewRow row && row.IsFolder)
		{
			row.IsExpanded = !row.IsExpanded;
			RefreshVisibleRows();
			e.Handled = true;
		}
	}

	private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (FindAncestor<ToggleButton>(e.OriginalSource as DependencyObject) != null)
		{
			return;
		}

		var container = FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject);
		if (container?.DataContext is not CFListTreeViewRow row)
		{
			return;
		}

		RowsListView.SelectedItem = row;
		_focusedRow = row;
		container.Focus();
		if (row.IsFolder)
		{
			row.IsExpanded = !row.IsExpanded;
			RefreshVisibleRows();
		}
		else if (IsMultiSelect && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			ToggleSelection(row);
		}
		else
		{
			SelectOnly(row);
		}

		e.Handled = true;
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		var rows = GetVisibleRows();
		if (rows.Count == 0)
		{
			return;
		}

		var index = Math.Max(0, RowsListView.SelectedIndex);
		switch (e.Key)
		{
			case Key.Up:
				FocusRow(rows[Math.Max(0, index - 1)]);
				break;
			case Key.Down:
				FocusRow(rows[Math.Min(rows.Count - 1, index + 1)]);
				break;
			case Key.Home:
				FocusRow(rows[0]);
				break;
			case Key.End:
				FocusRow(rows[rows.Count - 1]);
				break;
			case Key.Left:
				HandleLeft(rows[index]);
				break;
			case Key.Right:
				HandleRight(rows, index);
				break;
			case Key.Enter:
				CommitFocusedRow(rows[index]);
				break;
			case Key.PageUp:
				FocusRow(rows[Math.Max(0, index - 10)]);
				break;
			case Key.PageDown:
				FocusRow(rows[Math.Min(rows.Count - 1, index + 10)]);
				break;
			default:
				return;
		}

		e.Handled = true;
	}

	private void HandleLeft(CFListTreeViewRow row)
	{
		if (row.IsFolder && row.IsExpanded)
		{
			row.IsExpanded = false;
			RefreshVisibleRows();
			return;
		}

		if (row.Node.Parent != null)
		{
			FocusRow(row.Node.Parent.Row);
		}
	}

	private void HandleRight(IReadOnlyList<CFListTreeViewRow> rows, int index)
	{
		var row = rows[index];
		if (!row.IsFolder)
		{
			return;
		}

		if (!row.IsExpanded)
		{
			row.IsExpanded = true;
			RefreshVisibleRows();
			return;
		}

		if (index + 1 < rows.Count && rows[index + 1].Depth > row.Depth)
		{
			FocusRow(rows[index + 1]);
		}
	}

	private void CommitFocusedRow(CFListTreeViewRow row)
	{
		if (row.IsFolder)
		{
			row.IsExpanded = !row.IsExpanded;
			RefreshVisibleRows();
			return;
		}

		if (IsMultiSelect && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			ToggleSelection(row);
		}
		else
		{
			SelectOnly(row);
		}
	}

	private void FocusRow(CFListTreeViewRow row)
	{
		RowsListView.SelectedItem = row;
		_focusedRow = row;
		RowsListView.ScrollIntoView(row);
		Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
		{
			if (RowsListView.ItemContainerGenerator.ContainerFromItem(row) is ListViewItem container)
			{
				container.Focus();
			}
		}));
	}

	private void SelectOnly(CFListTreeViewRow row)
	{
		foreach (var selected in _selectedRows)
		{
			selected.IsSelected = false;
		}

		_selectedRows.Clear();
		row.IsSelected = true;
		_selectedRows.Add(row);
		SyncSelectedItems();
	}

	private void ToggleSelection(CFListTreeViewRow row)
	{
		if (_selectedRows.Remove(row))
		{
			row.IsSelected = false;
		}
		else
		{
			row.IsSelected = true;
			_selectedRows.Add(row);
		}

		SyncSelectedItems();
	}

	private void ApplyExternalSelection()
	{
		foreach (var row in _selectedRows)
		{
			row.IsSelected = false;
		}
		_selectedRows.Clear();

		if (SelectedItems == null)
		{
			return;
		}

		var selectedRows = new HashSet<CFListTreeViewRow>();
		foreach (var item in SelectedItems)
		{
			if (item != null && _nodesByItem.TryGetValue(item, out var node) && selectedRows.Add(node.Row))
			{
				node.Row.IsSelected = true;
				_selectedRows.Add(node.Row);
				if (!IsMultiSelect)
				{
					break;
				}
			}
		}

		var firstVisible = _selectedRows.FirstOrDefault(row => IsNodeVisible(row.Node));
		if (firstVisible != null)
		{
			_focusedRow = firstVisible;
			RowsListView.SelectedItem = firstVisible;
			RowsListView.ScrollIntoView(firstVisible);
		}
	}

	private bool IsNodeVisible(CFListTreeNode node)
	{
		if (MatchedItems != null)
		{
			return node.IsFilterVisible;
		}

		var parent = node.Parent;
		while (parent != null)
		{
			if (!parent.Row.IsExpanded)
			{
				return false;
			}
			parent = parent.Parent;
		}

		return true;
	}

	private void SyncSelectedItems()
	{
		if (SelectedItems == null)
		{
			return;
		}

		_isUpdatingSelectedItems = true;
		try
		{
			SelectedItems.Clear();
			foreach (var row in _selectedRows)
			{
				SelectedItems.Add(row.Item!);
			}
		}
		finally
		{
			_isUpdatingSelectedItems = false;
		}
	}

	private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
	{
		var current = source;
		while (current != null)
		{
			if (current is T match)
			{
				return match;
			}
			current = VisualTreeHelper.GetParent(current);
		}

		return null;
	}
}

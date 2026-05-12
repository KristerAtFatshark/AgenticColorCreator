using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.UserControls;

public partial class CFTreeView : UserControl
{
	private const string TreeViewItemsFileName = "TreeViewItems.json";
	private const string TreeViewTypeIconsFileName = "TreeViewTypeIcons.json";
	private readonly JsonFileSerializer _jsonFileSerializer = new();
	private readonly TreeViewNodeBuilder _treeViewNodeBuilder = new();
	public static readonly DependencyProperty IsMultiSelectProperty = DependencyProperty.Register(
		nameof(IsMultiSelect),
		typeof(bool),
		typeof(CFTreeView),
		new PropertyMetadata(false, OnIsMultiSelectChanged));

	public static readonly DependencyProperty SelectedTreeViewItemsProperty = DependencyProperty.Register(
		nameof(SelectedTreeViewItems),
		typeof(IReadOnlyList<CFTreeViewItem>),
		typeof(CFTreeView),
		new PropertyMetadata(null));

	private readonly List<CFTreeViewItem> _selectedTreeViewItems = [];

	public CFTreeView()
	{
		InitializeComponent();

		if (!DesignerProperties.GetIsInDesignMode(this))
		{
			LoadTreeViewItems();
		}
	}

	public IReadOnlyList<CFTreeViewItem>? SelectedTreeViewItems
	{
		get => (IReadOnlyList<CFTreeViewItem>?)GetValue(SelectedTreeViewItemsProperty);
		set => SetValue(SelectedTreeViewItemsProperty, value);
	}

	public bool IsMultiSelect
	{
		get => (bool)GetValue(IsMultiSelectProperty);
		set => SetValue(IsMultiSelectProperty, value);
	}

	private static void OnIsMultiSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not CFTreeView treeView)
		{
			return;
		}

		treeView.SyncSelectionMode();
	}

	private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
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

		if (FindAncestor<ToggleButton>(e.OriginalSource as DependencyObject) is not null)
		{
			return;
		}

		var clickedItem = FindAncestor<CFTreeViewItem>(e.OriginalSource as DependencyObject);
		if (clickedItem is null)
		{
			return;
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
		}

		_selectedTreeViewItems.Clear();
		clickedItem.IsMultiSelected = true;
		_selectedTreeViewItems.Add(clickedItem);
		UpdateSelectedTreeViewItems();
	}

	private void ToggleItemSelection(CFTreeViewItem clickedItem)
	{
		if (_selectedTreeViewItems.Remove(clickedItem))
		{
			clickedItem.IsMultiSelected = false;
			UpdateSelectedTreeViewItems();
			return;
		}

		clickedItem.IsMultiSelected = true;
		_selectedTreeViewItems.Add(clickedItem);
		UpdateSelectedTreeViewItems();
	}

	private void UpdateSelectedTreeViewItems()
	{
		SelectedTreeViewItems = _selectedTreeViewItems.ToList();
	}

	private void LoadTreeViewItems()
	{
		var dataDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Data");
		var itemsPath = Path.Combine(dataDirectoryPath, TreeViewItemsFileName);
		var typeIconsPath = Path.Combine(dataDirectoryPath, TreeViewTypeIconsFileName);

		if (!File.Exists(itemsPath) || !File.Exists(typeIconsPath))
		{
			return;
		}

		var sourceEntries = _jsonFileSerializer.DeserializeFile<List<TreeViewSourceEntry>>(itemsPath);
		var typeIconEntries = _jsonFileSerializer.DeserializeFile<List<TreeViewTypeIconEntry>>(typeIconsPath);
		var rootNodes = _treeViewNodeBuilder.Build(sourceEntries, typeIconEntries);

		PreviewTreeView.Items.Clear();
		foreach (var rootNode in rootNodes)
		{
			PreviewTreeView.Items.Add(CreateTreeViewItem(rootNode));
		}

		UpdateSelectedTreeViewItems();
	}

	private static CFTreeViewItem CreateTreeViewItem(TreeViewNode treeViewNode)
	{
		var treeViewItem = new CFTreeViewItem
		{
			Icon = treeViewNode.Icon,
			Text = treeViewNode.Text,
			Value = treeViewNode.Value,
			IsExpanded = treeViewNode.Children.Count > 0,
		};

		foreach (var childNode in treeViewNode.Children)
		{
			treeViewItem.Items.Add(CreateTreeViewItem(childNode));
		}

		return treeViewItem;
	}

	private void SyncSelectionMode()
	{
		foreach (var item in _selectedTreeViewItems)
		{
			item.IsMultiSelected = false;
		}

		_selectedTreeViewItems.Clear();

		if (PreviewTreeView.SelectedItem is CFTreeViewItem selectedItem)
		{
			if (IsMultiSelect)
			{
				selectedItem.IsMultiSelected = true;
			}

			_selectedTreeViewItems.Add(selectedItem);
		}

		UpdateSelectedTreeViewItems();
	}

	private static T? FindAncestor<T>(DependencyObject? dependencyObject)
		where T : DependencyObject
	{
		var current = dependencyObject;

		while (current is not null)
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

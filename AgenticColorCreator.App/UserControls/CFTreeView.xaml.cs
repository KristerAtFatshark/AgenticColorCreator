using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls;

public partial class CFTreeView : UserControl
{
	public static readonly DependencyProperty SelectedTreeViewItemsProperty = DependencyProperty.Register(
		nameof(SelectedTreeViewItems),
		typeof(IReadOnlyList<CFTreeViewItem>),
		typeof(CFTreeView),
		new PropertyMetadata(null));

	private readonly List<CFTreeViewItem> _selectedTreeViewItems = [];

	public CFTreeView()
	{
		InitializeComponent();
	}

	public IReadOnlyList<CFTreeViewItem>? SelectedTreeViewItems
	{
		get => (IReadOnlyList<CFTreeViewItem>?)GetValue(SelectedTreeViewItemsProperty);
		set => SetValue(SelectedTreeViewItemsProperty, value);
	}

	private void OnPreviewTreeViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
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

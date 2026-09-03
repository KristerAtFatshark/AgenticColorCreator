using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AgenticColorCreator.App.UserControls.CFListTreeViewControl;

namespace AgenticColorCreator.Tests;

public sealed class CFListTreeViewTests
{
	[Fact]
	public void SourceItems_BuildsFoldersAndRootLeavesInSortedOrder()
	{
		RunOnStaThread(() =>
		{
			var rootLeaf = new TestItem("Root", null, "root");
			var second = new TestItem("Second", "Folder/Sub", "b");
			var first = new TestItem("First", "Folder/Sub", "a");
			var control = CreateControl(new[] { rootLeaf, second, first });

			var rows = GetVisibleRows(control);
			Assert.Equal(new[] { "Folder", "Sub", "First", "Second", "Root" }, rows.Select(GetDisplayName));
			Assert.Equal(5, control.VisibleRowCount);
		});
	}

	[Fact]
	public void MatchedItems_MasksExistingRowsAndIncludesAncestors()
	{
		RunOnStaThread(() =>
		{
			var match = new TestItem("Match", "A/B", "a");
			var other = new TestItem("Other", "C", "b");
			var control = CreateControl(new[] { match, other });
			var originalRows = GetVisibleRows(control).ToArray();

			control.MatchedItems = new[] { match };

			var filteredRows = GetVisibleRows(control);
			Assert.Equal(new[] { "A", "B", "Match" }, filteredRows.Select(GetDisplayName));
			Assert.Same(originalRows.Single(row => row.Item == match), filteredRows.Last());
		});
	}

	[Fact]
	public void EmptyMatchedItems_ShowsNoRows_AndNullRestoresExpansion()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("Leaf", "A/B", "leaf");
			var control = CreateControl(new[] { item });
			control.CollapseAll();
			Assert.Single(GetVisibleRows(control));

			control.MatchedItems = Array.Empty<TestItem>();
			Assert.Empty(GetVisibleRows(control));

			control.MatchedItems = new[] { item };
			Assert.Equal(3, GetVisibleRows(control).Count);

			control.MatchedItems = null;
			Assert.Single(GetVisibleRows(control));
		});
	}

	[Fact]
	public void MatchedItems_ChangesDoNotReloadStructure()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("Leaf", "A", "leaf");
			var control = CreateControl(new[] { item });
			var loadCount = 0;
			control.StructureLoadCompleted += (_, _) => loadCount++;

			control.MatchedItems = new[] { item };
			control.MatchedItems = Array.Empty<TestItem>();
			control.MatchedItems = null;

			Assert.Equal(0, loadCount);
		});
	}

	[Fact]
	public void ListCollectionViewRefresh_UpdatesMaskWithoutReloadingStructure()
	{
		RunOnStaThread(() =>
		{
			var first = new TestItem("First", "A", "a");
			var second = new TestItem("Second", "B", "b");
			var items = new List<TestItem> { first, second };
			var control = CreateControl(items);
			var filterText = "First";
			var view = new ListCollectionView(items)
			{
				Filter = value => ((TestItem)value).Name.Contains(filterText, StringComparison.OrdinalIgnoreCase),
			};
			control.MatchedItems = view;
			var loadCount = 0;
			control.StructureLoadCompleted += (_, _) => loadCount++;

			filterText = "Second";
			view.Refresh();
			DrainDispatcher();

			Assert.Equal(new[] { "B", "Second" }, GetVisibleRows(control).Select(GetDisplayName));
			Assert.Equal(0, loadCount);
		});
	}

	[Fact]
	public void SelectedItems_UsesOriginalLeafObjectsAndSurvivesFiltering()
	{
		RunOnStaThread(() =>
		{
			var selected = new TestItem("Selected", "A", "a");
			var visible = new TestItem("Visible", "B", "b");
			var selectedItems = new ObservableCollection<object> { selected };
			var control = CreateControl(new[] { selected, visible });
			control.IsMultiSelect = true;
			control.SelectedItems = selectedItems;

			control.MatchedItems = new[] { visible };

			Assert.Same(selected, Assert.Single(selectedItems));
			Assert.True(GetAllRows(control).Single(row => row.Item == selected).IsSelected);
		});
	}

	[Fact]
	public void SelectedItems_AppliesMultipleExternalSelections()
	{
		RunOnStaThread(() =>
		{
			var first = new TestItem("First", "A", "a");
			var second = new TestItem("Second", "B", "b");
			var control = CreateControl(new[] { first, second });
			control.IsMultiSelect = true;

			control.SelectedItems = new ObservableCollection<object> { first, second };

			Assert.Equal(2, GetAllRows(control).Count(row => row.IsSelected));
		});
	}

	[Fact]
	public void CollapseAllExceptSelectedItemParents_KeepsSelectedPathVisible()
	{
		RunOnStaThread(() =>
		{
			var selected = new TestItem("Selected", "A/B", "a");
			var other = new TestItem("Other", "C/D", "b");
			var control = CreateControl(new[] { selected, other });
			control.SelectedItems = new ObservableCollection<object> { selected };

			control.CollapseAllExceptSelectedItemParents();

			Assert.Equal(new[] { "A", "B", "Selected", "C" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void SourceCollectionBurst_CoalescesToOneStructureLoad()
	{
		RunOnStaThread(() =>
		{
			var items = new ObservableCollection<TestItem>();
			var control = CreateControl(items);
			var loadCount = 0;
			control.StructureLoadCompleted += (_, _) => loadCount++;

			items.Add(new TestItem("First", "A", "a"));
			items.Add(new TestItem("Second", "B", "b"));
			DrainDispatcher();

			Assert.Equal(1, loadCount);
			Assert.Equal(4, control.VisibleRowCount);
		});
	}

	[Fact]
	public void ReplacingSourceItems_DiscardsQueuedNotificationFromPreviousSource()
	{
		RunOnStaThread(() =>
		{
			var oldItems = new ObservableCollection<TestItem>();
			var control = CreateControl(oldItems);
			var loadCount = 0;
			control.StructureLoadCompleted += (_, _) => loadCount++;

			oldItems.Add(new TestItem("Old", "Old", "old"));
			control.SourceItems = new[] { new TestItem("New", "New", "new") };
			DrainDispatcher();

			Assert.Equal(1, loadCount);
			Assert.Equal(new[] { "New", "New" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void ReplacingMatchedItems_DiscardsQueuedNotificationFromPreviousView()
	{
		RunOnStaThread(() =>
		{
			var first = new TestItem("First", "A", "a");
			var second = new TestItem("Second", "B", "b");
			var oldMatches = new ObservableCollection<TestItem>();
			var control = CreateControl(new[] { first, second });
			control.MatchedItems = oldMatches;

			oldMatches.Add(first);
			control.MatchedItems = new[] { second };
			DrainDispatcher();

			Assert.Equal(new[] { "B", "Second" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void CollapseAllThreshold_CollapsesWhenSourceAssetCountExceedsThreshold()
	{
		RunOnStaThread(() =>
		{
			var items = new[]
			{
				new TestItem("First", "A/B", "a"),
				new TestItem("Second", "C/D", "b"),
			};
			var control = CreateControl(items, collapseAllThreshold: 1);

			Assert.Equal(new[] { "A", "C" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void CollapseAllThreshold_DoesNotCollapseAtExactThresholdOrWhenDisabled()
	{
		RunOnStaThread(() =>
		{
			var items = new[]
			{
				new TestItem("First", "A", "a"),
				new TestItem("Second", "B", "b"),
			};

			Assert.Equal(4, CreateControl(items, collapseAllThreshold: 2).VisibleRowCount);
			Assert.Equal(4, CreateControl(items, collapseAllThreshold: 0).VisibleRowCount);
		});
	}

	[Fact]
	public void CollapseAllThreshold_RuntimeChangeCollapsesWithoutReloadingStructure()
	{
		RunOnStaThread(() =>
		{
			var items = new[]
			{
				new TestItem("First", "A", "a"),
				new TestItem("Second", "B", "b"),
			};
			var control = CreateControl(items, collapseAllThreshold: 0);
			var loadCount = 0;
			control.StructureLoadCompleted += (_, _) => loadCount++;

			control.CollapseAllThreshold = 1;

			Assert.Equal(new[] { "A", "B" }, GetVisibleRows(control).Select(GetDisplayName));
			Assert.Equal(0, loadCount);
		});
	}

	[Fact]
	public void CollapseAllThreshold_UsesSourceCountRatherThanFilteredCount()
	{
		RunOnStaThread(() =>
		{
			var first = new TestItem("First", "A/B", "a");
			var second = new TestItem("Second", "C/D", "b");
			var control = CreateControl(new[] { first, second }, collapseAllThreshold: 1);

			control.MatchedItems = new[] { first };
			Assert.Equal(new[] { "A", "B", "First" }, GetVisibleRows(control).Select(GetDisplayName));

			control.MatchedItems = null;
			Assert.Equal(new[] { "A", "C" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void SelectFirstItemAndFocus_SkipsFolderRows()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("Leaf", "A", "leaf");
			var selectedItems = new ObservableCollection<object>();
			var control = CreateControl(new[] { item });
			control.SelectedItems = selectedItems;

			var selectedRow = control.SelectFirstItemAndFocus();

			Assert.NotNull(selectedRow);
			Assert.False(selectedRow!.IsFolder);
			Assert.Same(item, Assert.Single(selectedItems));
		});
	}

	[Fact]
	public void SelectFirstItemAndFocus_ExpandsCollapsedAncestors()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("Leaf", "A/B", "leaf");
			var selectedItems = new ObservableCollection<object>();
			var control = CreateControl(new[] { item });
			control.SelectedItems = selectedItems;
			control.CollapseAll();

			var selectedRow = control.SelectFirstItemAndFocus();

			Assert.NotNull(selectedRow);
			Assert.Equal(3, control.VisibleRowCount);
			Assert.Same(item, Assert.Single(selectedItems));
		});
	}

	[Fact]
	public void SelectFirstItemAndFocus_UsesSortedTreeOrder()
	{
		RunOnStaThread(() =>
		{
			var later = new TestItem("Later", "Z", "z");
			var first = new TestItem("First", "A", "a");
			var selectedItems = new ObservableCollection<object>();
			var control = CreateControl(new[] { later, first });
			control.SelectedItems = selectedItems;

			control.SelectFirstItemAndFocus();

			Assert.Same(first, Assert.Single(selectedItems));
		});
	}

	[Fact]
	public void SelectFirstItemAndFocus_WithEmptyFilterReturnsNull()
	{
		RunOnStaThread(() =>
		{
			var control = CreateControl(new[] { new TestItem("Leaf", "A", "a") });
			control.MatchedItems = Array.Empty<TestItem>();

			Assert.Null(control.SelectFirstItemAndFocus());
		});
	}

	[Fact]
	public void SourceReplacement_RemovesStaleSelectedItems()
	{
		RunOnStaThread(() =>
		{
			var oldItem = new TestItem("Old", "A", "a");
			var selectedItems = new ObservableCollection<object> { oldItem };
			var control = CreateControl(new[] { oldItem });
			control.SelectedItems = selectedItems;

			control.SourceItems = new[] { new TestItem("New", "B", "b") };

			Assert.Empty(selectedItems);
		});
	}

	[Fact]
	public void SelectedItems_RejectsFixedSizeLists()
	{
		RunOnStaThread(() =>
		{
			var control = CreateControl(Array.Empty<TestItem>());

			Assert.Throws<ArgumentException>(() => control.SelectedItems = Array.Empty<object>());
		});
	}

	[Fact]
	public void DuplicateSourceReference_IsLoadedOnce()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("Leaf", "A", "leaf");
			var control = CreateControl(new[] { item, item });

			Assert.Equal(new[] { "A", "Leaf" }, GetVisibleRows(control).Select(GetDisplayName));
		});
	}

	[Fact]
	public void SourceItems_MapsResourceTypesToIconsWithDefaultFallback()
	{
		RunOnStaThread(() =>
		{
			var explicitIcon = new TestItem("Material", null, "a", "MATERIAL");
			var defaultIcon = new TestItem("Default", null, "b", "unknown_type");
			var control = CreateControl(new[] { explicitIcon, defaultIcon });

			var rows = GetVisibleRows(control);
			Assert.Equal("icon-resource_material", CFListTreeViewIconMap.GetIconResourceKey(explicitIcon.ResourceType));
			Assert.Equal("icon-resource_default", CFListTreeViewIconMap.GetIconResourceKey(defaultIcon.ResourceType));
			var materialIcon = rows.Single(row => row.Item == explicitIcon).DefaultIcon;
			var defaultIconImage = rows.Single(row => row.Item == defaultIcon).DefaultIcon;
			Assert.True(materialIcon.IsFrozen);
			Assert.True(defaultIconImage.IsFrozen);
			Assert.NotSame(materialIcon, defaultIconImage);
		});
	}

	[Fact]
	public void SourceItems_SortsSameNameResourcesByExtensionBearingSortKey()
	{
		RunOnStaThread(() =>
		{
			var unit = new TestItem("cryptic_color_decals", "content/debug/cryptic_colors", "cryptic_color_decals.unit", "unit");
			var level = new TestItem("cryptic_color_decals", "content/debug/cryptic_colors", "cryptic_color_decals.level", "level");
			var control = CreateControl(new[] { unit, level }, collapseAllThreshold: 0);

			var leaves = GetVisibleRows(control).Where(row => !row.IsFolder).ToArray();
			Assert.Same(level, leaves[0].Item);
			Assert.Same(unit, leaves[1].Item);
			Assert.All(leaves, row => Assert.Equal("cryptic_color_decals", row.Name));
		});
	}

	[Fact]
	public void ShowResourceType_DefaultsTrueAndCanBeDisabled()
	{
		RunOnStaThread(() =>
		{
			var item = new TestItem("cryptic_color_decals", null, "cryptic_color_decals.level", "level");
			var control = CreateControl(new[] { item });

			Assert.True(control.ShowResourceType);
			Assert.Equal("level", Assert.Single(GetVisibleRows(control)).ResourceType);

			control.ShowResourceType = false;

			Assert.False(control.ShowResourceType);
		});
	}

	[Fact]
	public void KeyboardFocusedRow_UsesFullSizeLightGrayDashedFocusVisual()
	{
		RunOnStaThread(() =>
		{
			var control = CreateControl(new[] { new TestItem("Leaf", null, "leaf") });
			var window = new Window
			{
				Width = 400,
				Height = 200,
				Content = control,
				ShowInTaskbar = false,
			};
			window.Show();
			window.Activate();
			control.UpdateLayout();
			var listView = GetRowsListView(control);
			var container = Assert.IsType<ListViewItem>(listView.ItemContainerGenerator.ContainerFromIndex(0));

			Keyboard.Focus(container);
			DrainDispatcher();
			control.UpdateLayout();
			var focusVisualStyle = container.FocusVisualStyle;
			Assert.NotNull(focusVisualStyle);
			var templateSetter = Assert.Single(focusVisualStyle!.Setters.OfType<Setter>(), setter => setter.Property == Control.TemplateProperty);
			var template = Assert.IsType<ControlTemplate>(templateSetter.Value);
			var focusVisual = new Control
			{
				Template = template,
			};
			focusVisual.ApplyTemplate();
			var rectangle = Assert.IsType<System.Windows.Shapes.Rectangle>(VisualTreeHelper.GetChild(focusVisual, 0));

			Assert.True(container.IsKeyboardFocusWithin);
			Assert.Equal(new Thickness(0), rectangle.Margin);
			Assert.Equal(1, rectangle.StrokeThickness);
			Assert.Equal(new DoubleCollection { 1, 2 }, rectangle.StrokeDashArray);
			Assert.Equal("#FFC8C8C8", rectangle.Stroke.ToString().ToUpperInvariant());
			window.Close();
		});
	}

	[Fact]
	public void NonContractSourceItems_AreIgnored()
	{
		RunOnStaThread(() =>
		{
			var valid = new TestItem("Leaf", null, "leaf");
			var control = CreateControl(new object[] { new object(), valid });

			Assert.Single(GetVisibleRows(control));
			Assert.Same(valid, GetVisibleRows(control)[0].Item);
		});
	}

	private static CFListTreeView CreateControl(IEnumerable source, int collapseAllThreshold = 100)
	{
		WpfTestApplication.Ensure();
		var control = new CFListTreeView
		{
			CollapseAllThreshold = collapseAllThreshold,
		};
		control.BeginInit();
		control.EndInit();
		control.ApplyTemplate();
		control.SourceItems = source;
		return control;
	}

	private static IReadOnlyList<CFListTreeViewRow> GetVisibleRows(CFListTreeView control)
	{
		return GetRowsListView(control).ItemsSource.Cast<CFListTreeViewRow>().ToArray();
	}

	private static IReadOnlyList<CFListTreeViewRow> GetAllRows(CFListTreeView control)
	{
		var field = typeof(CFListTreeView).GetField("_nodesByItem", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		var dictionary = Assert.IsAssignableFrom<IDictionary>(field!.GetValue(control));
		return dictionary.Values.Cast<object>()
			.Select(node => (CFListTreeViewRow)node.GetType().GetProperty("Row")!.GetValue(node)!)
			.ToArray();
	}

	private static ListView GetRowsListView(CFListTreeView control)
	{
		var field = typeof(CFListTreeView).GetField("RowsListView", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		return Assert.IsType<ListView>(field!.GetValue(control));
	}

	private static string GetDisplayName(CFListTreeViewRow row)
	{
		return row.IsFolder ? row.Name : ((TestItem)row.Item!).Name;
	}

	private static void RunOnStaThread(Action action)
	{
		Exception? exception = null;
		var thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		thread.Join();
		if (exception != null)
		{
			throw exception;
		}
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
	}

	private sealed class TestItem : ICFTreeViewItem
	{
		public TestItem(string name, string? folder, string? sortKey, string resourceType = "item")
		{
			Name = name;
			TreeFolderPath = folder;
			TreeSortKey = sortKey;
			ResourceType = resourceType;
		}

		public string Name { get; }

		public string? TreeFolderPath { get; }

		public string? TreeSortKey { get; }

		public string ResourceName => Name;

		public string ResourceType { get; }
	}
}

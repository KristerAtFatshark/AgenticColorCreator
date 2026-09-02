using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AgenticColorCreator.App.UserControls.CFListTreeViewControl;
using Xunit.Abstractions;

namespace AgenticColorCreator.Tests;

public sealed class CFListTreeViewPerformanceTests
{
	private readonly ITestOutputHelper _output;

	public CFListTreeViewPerformanceTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	[Trait("Category", "Performance")]
	public void LargeSourceAndRepeatedMasks_DoNotRebuildStructure()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			const int itemCount = 50_000;
			var items = new List<PerformanceItem>(itemCount);
			for (var index = 0; index < itemCount; index++)
			{
				items.Add(new PerformanceItem("Root" + index % 20 + "/Branch" + index % 100, index.ToString("D8")));
			}

			var control = new CFListTreeView();
			var loadTimer = Stopwatch.StartNew();
			control.SourceItems = items;
			loadTimer.Stop();

			var structureLoadCount = 0;
			control.StructureLoadCompleted += (_, _) => structureLoadCount++;
			var maskTimer = Stopwatch.StartNew();
			for (var pass = 0; pass < 20; pass++)
			{
				control.MatchedItems = items.GetRange(pass * 25, 25);
			}
			maskTimer.Stop();

			_output.WriteLine($"Structure: {loadTimer.Elapsed.TotalMilliseconds:N2} ms for {itemCount:N0} items");
			_output.WriteLine($"Masks: {maskTimer.Elapsed.TotalMilliseconds:N2} ms for 20 x 25 matches");
			Assert.Equal(0, structureLoadCount);
			Assert.True(loadTimer.Elapsed < TimeSpan.FromSeconds(15));
			Assert.True(maskTimer.Elapsed < TimeSpan.FromSeconds(5));
		});
	}

	[Fact]
	[Trait("Category", "Performance")]
	public void ScrollingLargeFlatSource_KeepsRealizedContainersVirtualized()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			const int itemCount = 10_000;
			var items = new List<PerformanceItem>(itemCount);
			for (var index = 0; index < itemCount; index++)
			{
				items.Add(new PerformanceItem(string.Empty, index.ToString("D8")));
			}

			var control = new CFListTreeView
			{
				CollapseAllThreshold = 0,
				SourceItems = items,
			};
			var window = new Window
			{
				Width = 500,
				Height = 260,
				Content = control,
				ShowInTaskbar = false,
			};
			window.Show();
			control.ApplyTemplate();
			control.UpdateLayout();
			DrainDispatcher();
			var listView = GetRowsListView(control);

			var timer = Stopwatch.StartNew();
			for (var index = 0; index < itemCount; index += 500)
			{
				listView.ScrollIntoView(listView.Items[index]);
				DrainDispatcher();
			}
			timer.Stop();

			var realizedCount = 0;
			for (var index = 0; index < itemCount; index++)
			{
				if (listView.ItemContainerGenerator.ContainerFromIndex(index) != null)
				{
					realizedCount++;
				}
			}

			_output.WriteLine($"Scroll: {timer.Elapsed.TotalMilliseconds:N2} ms across {itemCount:N0} rows");
			_output.WriteLine($"Realized containers: {realizedCount:N0}");
			Assert.True(realizedCount < 200, $"Expected virtualization, but {realizedCount} containers were realized.");
			window.Close();
		});
	}

	[Fact]
	[Trait("Category", "Performance")]
	public void ScrollingExpandedTreeWithMixedIcons_RemainsVirtualized()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			const int itemCount = 10_000;
			var resourceTypes = new[] { "level", "unit", "material", "texture", "particles", "wwise_event" };
			var items = new List<PerformanceItem>(itemCount);
			for (var index = 0; index < itemCount; index++)
			{
				items.Add(new PerformanceItem(
					"Content/Root" + index % 10 + "/Branch" + index % 50,
					index.ToString("D8"),
					resourceTypes[index % resourceTypes.Length]));
			}

			var control = new CFListTreeView
			{
				CollapseAllThreshold = 0,
				SourceItems = items,
			};
			var window = new Window
			{
				Width = 500,
				Height = 260,
				Content = control,
				ShowInTaskbar = false,
			};
			window.Show();
			control.ApplyTemplate();
			control.UpdateLayout();
			DrainDispatcher();
			var listView = GetRowsListView(control);

			var timer = Stopwatch.StartNew();
			for (var index = 0; index < listView!.Items.Count; index += 250)
			{
				listView.ScrollIntoView(listView.Items[index]);
				DrainDispatcher();
			}
			timer.Stop();

			var realizedCount = 0;
			for (var index = 0; index < listView.Items.Count; index++)
			{
				if (listView.ItemContainerGenerator.ContainerFromIndex(index) != null)
				{
					realizedCount++;
				}
			}

			_output.WriteLine($"Expanded mixed-icon scroll: {timer.Elapsed.TotalMilliseconds:N2} ms across {listView.Items.Count:N0} rows");
			_output.WriteLine($"Realized containers: {realizedCount:N0}");
			Assert.True(realizedCount > 0, "Expected the loaded ListView to realize viewport containers.");
			Assert.True(realizedCount < 200, $"Expected virtualization, but {realizedCount} containers were realized.");
			Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5));
			window.Close();
		});
	}

	private static ListView GetRowsListView(CFListTreeView control)
	{
		var field = typeof(CFListTreeView).GetField("RowsListView", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		return Assert.IsType<ListView>(field!.GetValue(control));
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
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

	private sealed class PerformanceItem : ICFTreeViewItem
	{
		public PerformanceItem(string folderPath, string sortKey, string resourceType = "item")
		{
			TreeFolderPath = folderPath;
			TreeSortKey = sortKey;
			ResourceType = resourceType;
		}

		public string? TreeFolderPath { get; }

		public string? TreeSortKey { get; }

		public string ResourceName => TreeSortKey ?? string.Empty;

		public string ResourceType { get; }
	}
}

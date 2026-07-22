using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ClownFishUi.CFUserControls.CFTreeViewControl;
using Xunit.Abstractions;

namespace AgenticColorCreator.Tests;

public sealed class CFTreeViewLoadPerformanceTests
{
	private readonly ITestOutputHelper _output;

	public CFTreeViewLoadPerformanceTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void CFTreeView_LoadingLargeSource_CompletesInReasonableTime()
	{
		const int entryCount = 350_000;

		var buildTimer = Stopwatch.StartNew();
		var entries = BuildEntries(entryCount);
		buildTimer.Stop();

		var rebuildElapsed = TimeSpan.Zero;
		Exception? capturedException = null;

		RunOnStaThread(() =>
		{
			try
			{
				EnsureApplication();

				var treeView = new CFTreeView
				{
					CollapseAllThresholdItemCount = entryCount + 1,
				};

				// Force template application so the internal TreeView exists before we time.
				treeView.BeginInit();
				treeView.EndInit();
				treeView.ApplyTemplate();

				var completed = new ManualResetEventSlim(false);
				EventHandler handler = (_, _) => completed.Set();
				treeView.RebuildCompleted += handler;

				var sw = Stopwatch.StartNew();
				treeView.NodesSource = new ObservableCollection<TreeViewSourceEntry>(entries);
				PumpDispatcherUntil(completed, TimeSpan.FromMinutes(2));
				sw.Stop();

				treeView.RebuildCompleted -= handler;
				rebuildElapsed = sw.Elapsed;

				Assert.True(completed.IsSet, "Rebuild did not complete within the allotted time.");
			}
			catch (Exception ex)
			{
				capturedException = ex;
			}
			finally
			{
				Dispatcher.CurrentDispatcher.InvokeShutdown();
			}
		});

		if (capturedException != null)
		{
			throw new Xunit.Sdk.XunitException($"STA test body threw: {capturedException}");
		}

		_output.WriteLine($"Entry generation:    {buildTimer.Elapsed.TotalMilliseconds,10:N2} ms for {entryCount:N0} entries.");
		_output.WriteLine($"CFTreeView rebuild:  {rebuildElapsed.TotalMilliseconds,10:N2} ms for {entryCount:N0} entries.");
	}

	private static List<TreeViewSourceEntry> BuildEntries(int count)
	{
		var random = new Random(12345);
		var roots = new[] { "library", "project", "themes", "assets", "plugins", "modules", "vendors", "engine" };
		var branches = new[] { "core", "editor", "preview", "runtime", "shared", "layout", "inputs", "colors", "meshes", "audio", "physics", "netcode" };
		var leaves = new[] { "panel", "button", "textbox", "combobox", "treeview", "slider", "badge", "dialog", "accent", "surface", "toggle", "chip" };
		var types = new[] { "control", "palette", "folder" };
		var entries = new List<TreeViewSourceEntry>(count);
		var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		while (entries.Count < count)
		{
			var depth = random.Next(3, 7);
			var segments = new List<string>(depth)
			{
				roots[random.Next(roots.Length)],
			};

			for (var index = 1; index < depth - 1; index++)
			{
				segments.Add($"{branches[random.Next(branches.Length)]}-{random.Next(1, 200)}");
			}

			segments.Add($"{leaves[random.Next(leaves.Length)]}-{entries.Count + 1:0000000}");

			var value = string.Join("/", segments);
			if (!used.Add(value))
			{
				continue;
			}

			entries.Add(new TreeViewSourceEntry
			{
				Value = value,
				Type = types[random.Next(types.Length)],
			});
		}

		return entries;
	}

	private static void EnsureApplication()
	{
		if (Application.Current == null)
		{
			_ = new Application();
		}

		const string stylesUri = "pack://application:,,,/AgenticColorCreator.App;component/Styles/CFDarkStyles.xaml";
		var appResources = Application.Current!.Resources;
		var alreadyMerged = false;
		foreach (var dict in appResources.MergedDictionaries)
		{
			if (dict.Source != null && string.Equals(dict.Source.ToString(), stylesUri, StringComparison.OrdinalIgnoreCase))
			{
				alreadyMerged = true;
				break;
			}
		}

		if (!alreadyMerged)
		{
			appResources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(stylesUri, UriKind.Absolute),
			});
		}
	}

	private static void RunOnStaThread(Action action)
	{
		Exception? threadException = null;
		var thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				threadException = ex;
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		thread.Join();

		if (threadException != null)
		{
			throw threadException;
		}
	}

	private static void PumpDispatcherUntil(ManualResetEventSlim signal, TimeSpan timeout)
	{
		var dispatcher = Dispatcher.CurrentDispatcher;
		var deadline = DateTime.UtcNow + timeout;

		while (!signal.IsSet)
		{
			if (DateTime.UtcNow > deadline)
			{
				return;
			}

			// Drain any queued dispatcher work at Background priority or higher.
			var frame = new DispatcherFrame();
			dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action(() => frame.Continue = false));
			Dispatcher.PushFrame(frame);

			if (!signal.IsSet)
			{
				Thread.Sleep(1);
			}
		}
	}
}

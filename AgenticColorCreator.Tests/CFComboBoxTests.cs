using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AgenticColorCreator.App.UserControls.CFComboBoxControl;

namespace AgenticColorCreator.Tests;

public sealed class CFComboBoxTests
{
	[Fact]
	public void MixedState_ShowsOverlay_WithoutChangingSelection()
	{
		RunOnStaThread(() =>
		{
			var combo = CreateLoadedComboBox(out _);
			combo.SelectedIndex = 1;

			combo.IsMixedState = true;

			// The overlay is a pure visual; the underlying selection is untouched.
			Assert.True(combo.IsMixedState);
			Assert.Equal(1, combo.SelectedIndex);
		});
	}

	[Fact]
	public void ExternalSelectionChange_DoesNotClearMixedState()
	{
		RunOnStaThread(() =>
		{
			var combo = CreateLoadedComboBox(out _);
			combo.IsMixedState = true;

			// Host pushes a value through the bound property; mixed must stay set.
			combo.SelectedIndex = 2;

			Assert.True(combo.IsMixedState);
			Assert.Equal(2, combo.SelectedIndex);
		});
	}

	[Fact]
	public void UserSelection_ClearsMixedState()
	{
		RunOnStaThread(() =>
		{
			var combo = CreateLoadedComboBox(out var inner);
			combo.IsMixedState = true;

			// Simulate the user picking an item directly on the inner ComboBox.
			inner.SelectedIndex = 2;
			Pump(combo);

			Assert.False(combo.IsMixedState);
			Assert.Equal(2, combo.SelectedIndex);
		});
	}

	[Fact]
	public void SelectedItem_RoundTripsThroughWrapper()
	{
		RunOnStaThread(() =>
		{
			var combo = CreateLoadedComboBox(out var inner);

			combo.SelectedItem = "Two";
			Pump(combo);

			Assert.Equal("Two", inner.SelectedItem);
			Assert.Equal("Two", combo.SelectedItem);
		});
	}

	private static CFComboBox CreateLoadedComboBox(out ComboBox inner)
	{
		WpfTestApplication.Ensure();

		var combo = new CFComboBox
		{
			ItemsSource = new List<string> { "One", "Two", "Three" },
		};

		var window = new Window { Content = combo, Width = 200, Height = 80, ShowActivated = false };
		window.Show();
		combo.ApplyTemplate();
		Pump(combo);

		inner = (ComboBox)typeof(CFComboBox)
			.GetField("InnerComboBox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
			.GetValue(combo)!;

		return combo;
	}

	private static void Pump(DependencyObject d)
	{
		((FrameworkElement)d).Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
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
}

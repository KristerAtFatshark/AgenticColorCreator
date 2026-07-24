using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AgenticColorCreator.App.UserControls.CFCheckBoxControl;

namespace AgenticColorCreator.Tests;

public sealed class CFCheckBoxTests
{
	[Fact]
	public void MixedState_SetsInnerCheckBoxToIndeterminate()
	{
		RunOnStaThread(() =>
		{
			var checkBox = CreateLoadedCheckBox();

			checkBox.IsMixedState = true;

			Assert.Null(GetInnerIsChecked(checkBox));
		});
	}

	[Fact]
	public void ClearingMixedState_RestoresBooleanValue()
	{
		RunOnStaThread(() =>
		{
			var checkBox = CreateLoadedCheckBox();
			checkBox.IsChecked = true;
			checkBox.IsMixedState = true;

			checkBox.IsMixedState = false;

			Assert.True(GetInnerIsChecked(checkBox));
		});
	}

	[Fact]
	public void UserToggle_WhileMixed_CommitsValueAndClearsMixedState()
	{
		RunOnStaThread(() =>
		{
			var checkBox = CreateLoadedCheckBox();
			checkBox.IsMixedState = true;

			// Simulate the user clicking the indeterminate checkbox: a three-state=false
			// CheckBox moves from indeterminate to checked, raising the Checked event.
			var inner = GetInnerCheckBox(checkBox);
			inner.IsChecked = true;

			Assert.False(checkBox.IsMixedState);
			Assert.True(checkBox.IsChecked);
		});
	}

	[Fact]
	public void BooleanValue_RoundTripsToInnerCheckBox()
	{
		RunOnStaThread(() =>
		{
			var checkBox = CreateLoadedCheckBox();

			checkBox.IsChecked = true;
			Assert.True(GetInnerIsChecked(checkBox));

			checkBox.IsChecked = false;
			Assert.False(GetInnerIsChecked(checkBox));
		});
	}

	private static CFCheckBox CreateLoadedCheckBox()
	{
		EnsureApplication();

		var checkBox = new CFCheckBox();

		// Force template/loaded so the inner CheckBox field is wired and the initial state sync
		// has run, mirroring how the control behaves when hosted in a window.
		checkBox.BeginInit();
		checkBox.EndInit();
		checkBox.ApplyTemplate();
		checkBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

		return checkBox;
	}

	private static CheckBox GetInnerCheckBox(CFCheckBox checkBox)
	{
		var field = typeof(CFCheckBox).GetField("InnerCheckBox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Assert.NotNull(field);
		var inner = field!.GetValue(checkBox) as CheckBox;
		Assert.NotNull(inner);
		return inner!;
	}

	private static bool? GetInnerIsChecked(CFCheckBox checkBox)
	{
		return GetInnerCheckBox(checkBox).IsChecked;
	}

	private static void EnsureApplication()
	{
		if (Application.Current == null)
		{
			_ = new Application();
		}

		const string stylesUri = "pack://application:,,,/AgenticColorCreator.App;component/CFStyles/CFDarkStyles.xaml";
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
}

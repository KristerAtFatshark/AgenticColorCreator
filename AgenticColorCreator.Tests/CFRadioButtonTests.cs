using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AgenticColorCreator.App.UserControls.CFRadioButtonControl;

namespace AgenticColorCreator.Tests;

public sealed class CFRadioButtonTests
{
	[Fact]
	public void MixedState_SetsInnerRadioButtonToIndeterminate()
	{
		RunOnStaThread(() =>
		{
			var radioButton = CreateLoadedRadioButton();

			radioButton.IsMixedState = true;

			Assert.Null(GetInnerIsChecked(radioButton));
		});
	}

	[Fact]
	public void ClearingMixedState_RestoresBooleanValue()
	{
		RunOnStaThread(() =>
		{
			var radioButton = CreateLoadedRadioButton();
			radioButton.IsChecked = true;
			radioButton.IsMixedState = true;

			radioButton.IsMixedState = false;

			Assert.True(GetInnerIsChecked(radioButton));
		});
	}

	[Fact]
	public void UserToggle_WhileMixed_CommitsValueAndClearsMixedState()
	{
		RunOnStaThread(() =>
		{
			var radioButton = CreateLoadedRadioButton();
			radioButton.IsMixedState = true;

			// Simulate the user selecting the indeterminate radio button: a three-state=false
			// RadioButton moves from indeterminate to checked, raising the Checked event.
			var inner = GetInnerRadioButton(radioButton);
			inner.IsChecked = true;

			Assert.False(radioButton.IsMixedState);
			Assert.True(radioButton.IsChecked);
		});
	}

	[Fact]
	public void BooleanValue_RoundTripsToInnerRadioButton()
	{
		RunOnStaThread(() =>
		{
			var radioButton = CreateLoadedRadioButton();

			radioButton.IsChecked = true;
			Assert.True(GetInnerIsChecked(radioButton));

			radioButton.IsChecked = false;
			Assert.False(GetInnerIsChecked(radioButton));
		});
	}

	private static CFRadioButton CreateLoadedRadioButton()
	{
		WpfTestApplication.Ensure();

		var radioButton = new CFRadioButton
		{
			// A unique group name isolates each test's radio button from every other radio button
			// created in the shared WPF Application. WPF radio grouping is process-global per
			// GroupName, so leaving it at the default empty string would let sibling toggles from
			// other tests uncheck this one mid-test.
			GroupName = "CFRadioButtonTests_" + Guid.NewGuid().ToString("N"),
		};

		// Force template/loaded so the inner RadioButton field is wired and the initial state
		// sync has run, mirroring how the control behaves when hosted in a window.
		radioButton.BeginInit();
		radioButton.EndInit();
		radioButton.ApplyTemplate();
		radioButton.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

		return radioButton;
	}

	private static RadioButton GetInnerRadioButton(CFRadioButton radioButton)
	{
		var field = typeof(CFRadioButton).GetField("InnerRadioButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Assert.NotNull(field);
		var inner = field!.GetValue(radioButton) as RadioButton;
		Assert.NotNull(inner);
		return inner!;
	}

	private static bool? GetInnerIsChecked(CFRadioButton radioButton)
	{
		return GetInnerRadioButton(radioButton).IsChecked;
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

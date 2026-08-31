using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgenticColorCreator.App.UserControls.CFWindowControl;

namespace AgenticColorCreator.Tests;

public sealed class CFWindowTests
{
	[Fact]
	public void Template_ProvidesAllCaptionButtons()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var window = new CFWindow
			{
				Style = (Style)Application.Current.FindResource("CF.CustomWindow"),
			};

			window.ApplyTemplate();

			Assert.IsType<Button>(window.Template.FindName("PART_MinimizeButton", window));
			Assert.IsType<Button>(window.Template.FindName("PART_MaximizeButton", window));
			Assert.IsType<Button>(window.Template.FindName("PART_RestoreButton", window));
			Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
			window.Close();
		});
	}

	[Fact]
	public void MaximizedState_ShowsRestoreInsteadOfMaximize()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var window = new CFWindow
			{
				Style = (Style)Application.Current.FindResource("CF.CustomWindow"),
			};
			window.ApplyTemplate();

			window.WindowState = WindowState.Maximized;

			var maximize = (Button)window.Template.FindName("PART_MaximizeButton", window);
			var restore = (Button)window.Template.FindName("PART_RestoreButton", window);
			Assert.Equal(Visibility.Collapsed, maximize.Visibility);
			Assert.Equal(Visibility.Visible, restore.Visibility);
			window.Close();
		});
	}

	[Fact]
	public void Constructor_RegistersCaptionCommandBindingsOnce()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var window = new CFWindow();

			window.ConfigureCFWindowBehavior();

			Assert.Equal(4, window.CommandBindings.Count);
			Assert.Contains(window.CommandBindings.Cast<CommandBinding>(), binding => binding.Command == SystemCommands.CloseWindowCommand);
			Assert.Contains(window.CommandBindings.Cast<CommandBinding>(), binding => binding.Command == SystemCommands.MinimizeWindowCommand);
			Assert.Contains(window.CommandBindings.Cast<CommandBinding>(), binding => binding.Command == SystemCommands.MaximizeWindowCommand);
			Assert.Contains(window.CommandBindings.Cast<CommandBinding>(), binding => binding.Command == SystemCommands.RestoreWindowCommand);
			window.Close();
		});
	}

	[Fact]
	public void TitleBarGlyph_IsOptionalAndDisplaysWhenSet()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var window = new CFWindow
			{
				Style = (Style)Application.Current.FindResource("CF.CustomWindow"),
			};
			window.ApplyTemplate();
			var glyph = (TextBlock)window.Template.FindName("WindowGlyphIcon", window);

			Assert.Equal(Visibility.Collapsed, glyph.Visibility);

			window.TitleBarIconGlyph = (string)Application.Current.FindResource("icon-app_level_editor_icon");

			Assert.Equal(Visibility.Visible, glyph.Visibility);
			Assert.Equal(window.TitleBarIconGlyph, glyph.Text);
			window.Close();
		});
	}

	[Fact]
	public void OwnerWindow_CanBeSetFromCodeInitialization()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var owner = new Window();
			var window = new CFWindow
			{
				OwnerWindow = owner,
			};

			Assert.Same(owner, window.OwnerWindow);
			window.Close();
			owner.Close();
		});
	}

	[Fact]
	public void OwnerWindow_DefaultValueIsNullForMainWindowFallback()
	{
		RunOnStaThread(() =>
		{
			WpfTestApplication.Ensure();
			var window = new CFWindow();

			Assert.Null(window.OwnerWindow);
			window.Close();
		});
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

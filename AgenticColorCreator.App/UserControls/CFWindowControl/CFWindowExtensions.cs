using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFWindowControl;

public static class CFWindowExtensions
{
	private const int MonitorDefaultToNearest = 0x00000002;
	private const int WmGetMinMaxInfo = 0x0024;

	private static readonly ConditionalWeakTable<Window, WindowSetupData> SetupData = new();

	public static void ConfigureCFWindowBehavior(this Window window)
	{
		if (SetupData.TryGetValue(window, out _))
		{
			return;
		}

		var data = new WindowSetupData(window);
		SetupData.Add(window, data);
		window.SourceInitialized += data.OnSourceInitialized;
		window.Closed += data.OnClosed;

		window.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, data.OnCloseWindow));
		window.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, data.OnMinimizeWindow));
		window.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, data.OnMaximizeWindow));
		window.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, data.OnRestoreWindow));

		var handle = new WindowInteropHelper(window).Handle;
		if (handle != IntPtr.Zero)
		{
			data.AddHook(handle);
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

	[StructLayout(LayoutKind.Sequential)]
	private struct Point
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MinMaxInfo
	{
		public Point Reserved;
		public Point MaxSize;
		public Point MaxPosition;
		public Point MinTrackSize;
		public Point MaxTrackSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MonitorInfo
	{
		public int Size;
		public Rect Monitor;
		public Rect WorkArea;
		public int Flags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	private sealed class WindowSetupData
	{
		private readonly Window _window;
		private HwndSource? _source;

		public WindowSetupData(Window window)
		{
			_window = window;
		}

		public void AddHook(IntPtr handle)
		{
			if (_source != null)
			{
				return;
			}

			_source = HwndSource.FromHwnd(handle);
			_source?.AddHook(WindowProc);
		}

		public void OnSourceInitialized(object? sender, EventArgs e)
		{
			AddHook(new WindowInteropHelper(_window).Handle);
		}

		public void OnClosed(object? sender, EventArgs e)
		{
			if (_source != null)
			{
				_source.RemoveHook(WindowProc);
				_source = null;
			}

			SetupData.Remove(_window);
		}

		public void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
		{
			_window.Close();
		}

		public void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
		{
			_window.WindowState = WindowState.Minimized;
		}

		public void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
		{
			_window.WindowState = WindowState.Maximized;
		}

		public void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
		{
			_window.WindowState = WindowState.Normal;
		}

		private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (message == WmGetMinMaxInfo)
			{
				ApplyMinMaxInfo(hwnd, lParam);
				handled = true;
			}

			return IntPtr.Zero;
		}

		private void ApplyMinMaxInfo(IntPtr handle, IntPtr parameter)
		{
			var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
			if (monitor == IntPtr.Zero)
			{
				return;
			}

			var monitorInfo = new MonitorInfo
			{
				Size = Marshal.SizeOf<MonitorInfo>(),
			};
			if (!GetMonitorInfo(monitor, ref monitorInfo))
			{
				return;
			}

			var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(parameter);
			minMaxInfo.MaxPosition.X = monitorInfo.WorkArea.Left - monitorInfo.Monitor.Left;
			minMaxInfo.MaxPosition.Y = monitorInfo.WorkArea.Top - monitorInfo.Monitor.Top;
			minMaxInfo.MaxSize.X = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
			minMaxInfo.MaxSize.Y = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;

			var source = PresentationSource.FromVisual(_window);
			var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
			minMaxInfo.MinTrackSize.X = (int)Math.Ceiling(_window.MinWidth * transform.M11);
			minMaxInfo.MinTrackSize.Y = (int)Math.Ceiling(_window.MinHeight * transform.M22);

			Marshal.StructureToPtr(minMaxInfo, parameter, false);
		}
	}
}

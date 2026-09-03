using System;
using System.Windows;

#if NETCORE
namespace AgenticColorCreator.App.UserControls.CFWindowControl;

/// <summary>
/// Provides the themed custom-chrome window base and optional title-bar metadata.
/// </summary>
public class CFWindow : Window
{
#else
namespace AgenticColorCreator.App.UserControls.CFWindowControl
{
	/// <summary>
	/// Provides the themed custom-chrome window base and optional title-bar metadata.
	/// </summary>
	public class CFWindow : Window
	{
#endif
	public static readonly DependencyProperty TitleBarIconGlyphProperty = DependencyProperty.Register(
		nameof(TitleBarIconGlyph),
		typeof(string),
		typeof(CFWindow),
		new PropertyMetadata(null));

	public static readonly DependencyProperty OwnerWindowProperty = DependencyProperty.Register(
		nameof(OwnerWindow),
		typeof(Window),
		typeof(CFWindow),
		new PropertyMetadata(null, OnOwnerWindowChanged));

	public CFWindow()
	{
		this.ConfigureCFWindowBehavior();
	}

	public
#if NETCORE
		string?
#else
		string
#endif
		TitleBarIconGlyph
	{
#if NETCORE
		get => (string?)GetValue(TitleBarIconGlyphProperty);
		set => SetValue(TitleBarIconGlyphProperty, value);
#else
		get { return GetValue(TitleBarIconGlyphProperty) as string; }
		set { SetValue(TitleBarIconGlyphProperty, value); }
#endif
	}

	public
#if NETCORE
		Window?
#else
		Window
#endif
		OwnerWindow
	{
#if NETCORE
		get => (Window?)GetValue(OwnerWindowProperty);
		set => SetValue(OwnerWindowProperty, value);
#else
		get { return GetValue(OwnerWindowProperty) as Window; }
		set { SetValue(OwnerWindowProperty, value); }
#endif
	}

	protected override void OnInitialized(EventArgs e)
	{
		base.OnInitialized(e);
		ApplyOwnerWindow();
	}

	private static void OnOwnerWindowChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
	{
		var window = dependencyObject as CFWindow;
		if (window != null && window.IsInitialized)
		{
			window.ApplyOwnerWindow();
		}
	}

	private void ApplyOwnerWindow()
	{
		var owner = OwnerWindow;
		if (owner == null && Application.Current != null)
		{
			owner = Application.Current.MainWindow;
		}

		if (owner == null || ReferenceEquals(owner, this) || ReferenceEquals(Owner, owner))
		{
			return;
		}

		Owner = owner;
	}
}

#if !NETCORE
}
#endif

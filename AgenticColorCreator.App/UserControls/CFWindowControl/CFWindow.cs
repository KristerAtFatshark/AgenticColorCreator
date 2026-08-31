using System;
using System.Windows;

namespace AgenticColorCreator.App.UserControls.CFWindowControl;

public class CFWindow : Window
{
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

	public string? TitleBarIconGlyph
	{
		get => (string?)GetValue(TitleBarIconGlyphProperty);
		set => SetValue(TitleBarIconGlyphProperty, value);
	}

	public Window? OwnerWindow
	{
		get => (Window?)GetValue(OwnerWindowProperty);
		set => SetValue(OwnerWindowProperty, value);
	}

	protected override void OnInitialized(EventArgs e)
	{
		base.OnInitialized(e);
		ApplyOwnerWindow();
	}

	private static void OnOwnerWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CFWindow window && window.IsInitialized)
		{
			window.ApplyOwnerWindow();
		}
	}

	private void ApplyOwnerWindow()
	{
		var owner = OwnerWindow ?? Application.Current?.MainWindow;
		if (owner == null || ReferenceEquals(owner, this) || ReferenceEquals(Owner, owner))
		{
			return;
		}

		Owner = owner;
	}
}

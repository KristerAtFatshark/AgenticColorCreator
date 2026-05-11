using System.Windows;
using System.Windows.Controls;

namespace AgenticColorCreator.App.UserControls;

public class CFTreeViewItem : TreeViewItem
{
	public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
		nameof(Icon),
		typeof(string),
		typeof(CFTreeViewItem),
		new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(CFTreeViewItem),
		new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty IsMultiSelectedProperty = DependencyProperty.Register(
		nameof(IsMultiSelected),
		typeof(bool),
		typeof(CFTreeViewItem),
		new PropertyMetadata(false));

	public string Icon
	{
		get => (string)GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public bool IsMultiSelected
	{
		get => (bool)GetValue(IsMultiSelectedProperty);
		set => SetValue(IsMultiSelectedProperty, value);
	}

	public string Value { get; set; } = string.Empty;
}

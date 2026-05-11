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

	public string Value { get; set; } = string.Empty;
}

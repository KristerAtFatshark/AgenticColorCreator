using System.Windows;
using System.Windows.Controls;

namespace AgenticColorCreator.App.UserControls;

public partial class CFTreeView : UserControl
{
	public static readonly DependencyProperty SelectedTreeViewItemProperty = DependencyProperty.Register(
		nameof(SelectedTreeViewItem),
		typeof(CFTreeViewItem),
		typeof(CFTreeView),
		new PropertyMetadata(null));

	public CFTreeView()
	{
		InitializeComponent();
	}

	public CFTreeViewItem? SelectedTreeViewItem
	{
		get => (CFTreeViewItem?)GetValue(SelectedTreeViewItemProperty);
		set => SetValue(SelectedTreeViewItemProperty, value);
	}

	private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		SelectedTreeViewItem = e.NewValue as CFTreeViewItem;
	}
}

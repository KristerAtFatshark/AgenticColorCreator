using System.ComponentModel;
using System.Windows;
using AgenticColorCreator.App.ViewModels;
using AgenticColorCreator.App.UserControls;

namespace AgenticColorCreator.App;

public partial class MainWindow : Window
{
	public static readonly DependencyProperty SelectedPreviewTreeViewItemProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewItem),
		typeof(CFTreeViewItem),
		typeof(MainWindow),
		new PropertyMetadata(null, OnSelectedPreviewTreeViewItemChanged));

	public static readonly DependencyProperty SelectedPreviewTreeViewDetailsProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewDetails),
		typeof(string),
		typeof(MainWindow),
		new PropertyMetadata("Selected: none"));

	public MainWindow()
	{
		InitializeComponent();
		Closing += OnClosing;
	}

	public CFTreeViewItem? SelectedPreviewTreeViewItem
	{
		get => (CFTreeViewItem?)GetValue(SelectedPreviewTreeViewItemProperty);
		set => SetValue(SelectedPreviewTreeViewItemProperty, value);
	}

	public string SelectedPreviewTreeViewDetails
	{
		get => (string)GetValue(SelectedPreviewTreeViewDetailsProperty);
		set => SetValue(SelectedPreviewTreeViewDetailsProperty, value);
	}

	private static void OnSelectedPreviewTreeViewItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not MainWindow window)
		{
			return;
		}

		window.SelectedPreviewTreeViewDetails = e.NewValue is CFTreeViewItem selectedItem
			? $"Text: {selectedItem.Text} | Value: {selectedItem.Value}"
			: "Selected: none";
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}

		if (!viewModel.ConfirmClose())
		{
			e.Cancel = true;
		}
	}
}

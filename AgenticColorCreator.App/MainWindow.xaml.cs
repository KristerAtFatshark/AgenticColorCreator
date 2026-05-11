using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using AgenticColorCreator.App.ViewModels;
using AgenticColorCreator.App.UserControls;

namespace AgenticColorCreator.App;

public partial class MainWindow : Window
{
	public static readonly DependencyProperty SelectedPreviewTreeViewItemsProperty = DependencyProperty.Register(
		nameof(SelectedPreviewTreeViewItems),
		typeof(IReadOnlyList<CFTreeViewItem>),
		typeof(MainWindow),
		new PropertyMetadata(null, OnSelectedPreviewTreeViewItemsChanged));

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

	public IReadOnlyList<CFTreeViewItem>? SelectedPreviewTreeViewItems
	{
		get => (IReadOnlyList<CFTreeViewItem>?)GetValue(SelectedPreviewTreeViewItemsProperty);
		set => SetValue(SelectedPreviewTreeViewItemsProperty, value);
	}

	public string SelectedPreviewTreeViewDetails
	{
		get => (string)GetValue(SelectedPreviewTreeViewDetailsProperty);
		set => SetValue(SelectedPreviewTreeViewDetailsProperty, value);
	}

	private static void OnSelectedPreviewTreeViewItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not MainWindow window)
		{
			return;
		}

		if (e.NewValue is not IReadOnlyList<CFTreeViewItem> selectedItems || selectedItems.Count == 0)
		{
			window.SelectedPreviewTreeViewDetails = "Selected: none";
			return;
		}

		window.SelectedPreviewTreeViewDetails = string.Join(
			Environment.NewLine,
			selectedItems.Select(item => $"Text: {item.Text} | Value: {item.Value}"));
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

using System.ComponentModel;
using System.Windows;
using AgenticColorCreator.App.ViewModels;

namespace AgenticColorCreator.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
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

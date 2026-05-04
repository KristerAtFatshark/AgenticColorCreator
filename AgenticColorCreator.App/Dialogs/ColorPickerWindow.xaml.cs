using System.Windows;
using AgenticColorCreator.App.ViewModels;

namespace AgenticColorCreator.App.Dialogs;

public partial class ColorPickerWindow : Window
{
    private readonly ColorPickerViewModel _viewModel;

    public ColorPickerWindow(string initialHexValue)
    {
        InitializeComponent();
        _viewModel = new ColorPickerViewModel(initialHexValue);
        DataContext = _viewModel;
        SelectedHexValue = _viewModel.HexValue;
    }

    public string SelectedHexValue { get; private set; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedHexValue = _viewModel.HexValue;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

using System.Windows;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.App.Services;
using AgenticColorCreator.App.ViewModels;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var serializer = new AgenticColorsMarkdownSerializer();
		var fileDialogService = new FileDialogService();
		var messageBoxService = new MessageBoxService();
		var colorPickerDialogService = new ColorPickerDialogService();
		var mainWindowViewModel = new MainWindowViewModel(serializer, fileDialogService, messageBoxService, colorPickerDialogService);

		var window = new MainWindow
		{
			DataContext = mainWindowViewModel,
		};

		MainWindow = window;
		window.Show();
	}
}

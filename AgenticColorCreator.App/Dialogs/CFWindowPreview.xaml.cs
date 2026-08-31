using System.Windows;
using AgenticColorCreator.App.UserControls.CFWindowControl;

namespace AgenticColorCreator.App.Dialogs;

public partial class CFWindowPreview : CFWindow
{
	public CFWindowPreview()
	{
		InitializeComponent();
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}
}

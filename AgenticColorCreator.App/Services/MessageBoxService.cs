using System.Windows;

namespace AgenticColorCreator.App.Services;

public interface IMessageBoxService
{
	void ShowError(string message, string title);
	void ShowInformation(string message, string title);
	bool Confirm(string message, string title);
}

public sealed class MessageBoxService : IMessageBoxService
{
	public void ShowError(string message, string title)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public void ShowInformation(string message, string title)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
	}

	public bool Confirm(string message, string title)
	{
		return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
	}
}

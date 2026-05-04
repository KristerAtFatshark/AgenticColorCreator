using System.Windows;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.Dialogs;

public interface IColorPickerDialogService
{
	string? PickColor(string initialHexValue, Window? owner);
}

public sealed class ColorPickerDialogService : IColorPickerDialogService
{
	public string? PickColor(string initialHexValue, Window? owner)
	{
		var normalized = ColorHexParser.TryParseArgb(initialHexValue, out _)
			? ColorHexParser.Normalize(initialHexValue)
			: "#FF000000";

		var dialog = new ColorPickerWindow(normalized)
		{
			Owner = owner,
		};

		return dialog.ShowDialog() == true ? dialog.SelectedHexValue : null;
	}
}

using System.IO;
using Microsoft.Win32;

namespace AgenticColorCreator.App.Services;

public interface IFileDialogService
{
	string? OpenMarkdownFile();
	string? SaveMarkdownFile(string? initialPath);
}

public sealed class FileDialogService : IFileDialogService
{
	private const string Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*";

	public string? OpenMarkdownFile()
	{
		var dialog = new OpenFileDialog
		{
			Filter = Filter,
			CheckFileExists = true,
			DefaultExt = ".md",
		};

		return dialog.ShowDialog() == true ? dialog.FileName : null;
	}

	public string? SaveMarkdownFile(string? initialPath)
	{
		var dialog = new SaveFileDialog
		{
			Filter = Filter,
			DefaultExt = ".md",
			FileName = Path.GetFileName(initialPath) ?? "agentic_colors.md",
			InitialDirectory = string.IsNullOrWhiteSpace(initialPath)
				? null
				: Path.GetDirectoryName(initialPath),
		};

		return dialog.ShowDialog() == true ? dialog.FileName : null;
	}
}

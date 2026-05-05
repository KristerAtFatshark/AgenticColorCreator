using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class StartupPathTests : IDisposable
{
	private readonly string _rootPath;
	private readonly string _colorDirectoryPath;
	private readonly string _defaultDocumentPath;

	public StartupPathTests()
	{
		_rootPath = Path.Combine(Path.GetTempPath(), $"AgenticColorCreator.Tests.{Guid.NewGuid():N}");
		_colorDirectoryPath = Path.Combine(_rootPath, "Color");
		_defaultDocumentPath = Path.Combine(_colorDirectoryPath, "agentic_colors.md");
		Directory.CreateDirectory(_colorDirectoryPath);
		File.WriteAllText(_defaultDocumentPath, "# Agentic Colors\r\n\r\n## Metadata\r\n- format: agentic-colors/v1\r\n- name: Test Theme\r\n");
	}

	[Fact]
	public void GetDefaultDocumentPath_ReturnsColorAgenticColorsFileFromAncestor()
	{
		var nestedBaseDirectory = Path.Combine(_rootPath, "AgenticColorCreator.App", "bin", "Debug", "net8.0-windows");
		Directory.CreateDirectory(nestedBaseDirectory);

		var resolvedPath = DefaultDocumentLocator.GetDefaultDocumentPath(nestedBaseDirectory);

		Assert.Equal(_defaultDocumentPath, resolvedPath);
	}

	public void Dispose()
	{
		if (Directory.Exists(_rootPath))
		{
			Directory.Delete(_rootPath, true);
		}
	}
}

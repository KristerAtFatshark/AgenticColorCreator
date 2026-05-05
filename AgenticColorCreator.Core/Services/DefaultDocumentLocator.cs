using System;
using System.IO;

namespace AgenticColorCreator.Core.Services;

public static class DefaultDocumentLocator
{
	public static string? GetDefaultDocumentPath(string? baseDirectory = null)
	{
		var rootPath = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;

		while (!string.IsNullOrWhiteSpace(rootPath))
		{
			var candidate = Path.Combine(rootPath, "Color", "agentic_colors.md");
			if (File.Exists(candidate))
			{
				return candidate;
			}

			var parent = Directory.GetParent(rootPath);
			if (parent is null)
			{
				break;
			}

			rootPath = parent.FullName;
		}

		return null;
	}
}

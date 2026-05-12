using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AgenticColorCreator.Core.Models;

namespace AgenticColorCreator.Core.Services;

public sealed class TreeViewNodeBuilder
{
	public IReadOnlyList<TreeViewNode> Build(
		IEnumerable<TreeViewSourceEntry> sourceEntries,
		IEnumerable<TreeViewTypeIconEntry> typeIconEntries)
	{
		var iconMap = typeIconEntries.ToDictionary(entry => entry.Type, entry => entry.Icon, StringComparer.OrdinalIgnoreCase);
		var rootNodes = new List<TreeViewNode>();

		foreach (var sourceEntry in sourceEntries)
		{
			var segments = sourceEntry.Value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (segments.Length == 0)
			{
				continue;
			}

			var currentNodes = rootNodes;
			var currentPath = string.Empty;

			for (var index = 0; index < segments.Length; index++)
			{
				var segment = segments[index];
				var isLeaf = index == segments.Length - 1;
				currentPath = string.IsNullOrWhiteSpace(currentPath) ? segment : $"{currentPath}/{segment}";
				var nodeType = isLeaf ? sourceEntry.Type : "folder";

				var existingNode = currentNodes.FirstOrDefault(node => string.Equals(node.Value, currentPath, StringComparison.OrdinalIgnoreCase));
				if (existingNode is null)
				{
					existingNode = new TreeViewNode
					{
						Text = ToDisplayText(segment),
						Value = currentPath,
						Type = nodeType,
						Icon = GetIcon(iconMap, nodeType),
					};
					currentNodes.Add(existingNode);
				}
				else if (isLeaf)
				{
					existingNode.Type = sourceEntry.Type;
					existingNode.Icon = GetIcon(iconMap, sourceEntry.Type);
				}

				currentNodes = existingNode.Children;
			}
		}

		return rootNodes;
	}

	private static string GetIcon(IReadOnlyDictionary<string, string> iconMap, string type)
	{
		if (iconMap.TryGetValue(type, out var icon))
		{
			return icon;
		}

		throw new InvalidOperationException($"No icon mapping was found for TreeView type '{type}'.");
	}

	private static string ToDisplayText(string segment)
	{
		var words = segment
			.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant()));

		return string.Join(" ", words);
	}
}

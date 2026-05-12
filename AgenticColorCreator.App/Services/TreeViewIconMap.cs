using System;
using System.Collections.Generic;

namespace AgenticColorCreator.App.Services;

public static class TreeViewIconMap
{
	private static readonly IReadOnlyDictionary<string, string> TypeIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["folder"] = "📁",
		["control"] = "🧩",
		["palette"] = "🎨",
	};

	public static string GetIcon(string type)
	{
		if (TypeIconMap.TryGetValue(type, out var icon))
		{
			return icon;
		}

		throw new InvalidOperationException($"No icon mapping was found for TreeView type '{type}'.");
	}
}

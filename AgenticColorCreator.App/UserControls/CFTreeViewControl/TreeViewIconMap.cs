using System;
using System.Collections.Generic;

namespace AgenticColorCreator.App.UserControls.CFTreeViewControl;

public static class TreeViewIconMap
{
	private static readonly IReadOnlyDictionary<string, string> TypeIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["default"] = "🧩",
		["folder"] = "📁",
		["level"] = "🏠",
		["material"] = "🎨",
		["unit"] = "🧩",
		["item"] = "🎁",
		["vfx"] = "🎉",
		["control"] = "🧩",
		["palette"] = "🎨",
	};

	public static string GetIcon(string type)
	{
		if (TypeIconMap.TryGetValue(type, out var icon))
		{
			return icon;
		}

		return TypeIconMap["default"];
	}
}

using System;
using System.Collections.Generic;

#pragma warning disable CS8600


namespace ClownFishUi.CFUserControls.CFTreeViewControl

{
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
			string icon;
			if (TypeIconMap.TryGetValue(type, out icon))
			{
				return icon;
			}

			return TypeIconMap["default"];
		}
	}
}

#pragma warning restore CS8600

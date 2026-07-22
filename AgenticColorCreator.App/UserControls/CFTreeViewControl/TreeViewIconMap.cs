using System;
using System.Collections.Generic;

#pragma warning disable CS8600


namespace ClownFishUi.CFUserControls.CFTreeViewControl

{
	public static class TreeViewIconMap
	{
		private static readonly IReadOnlyDictionary<string, string> TypeIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["default"] = "icon-resource_default",
			["folder"] = "icon-folder",
			["level"] = "icon-resource_level",
			["unit"] = "icon-resource_unit",
			["wwise_bank"] = "icon-resource_wwise_bank",
			["wwise_event"] = "icon-resource_wwise_event",
			["state_machine"] = "icon-resource_state_machine",
			["material"] = "icon-resource_material",
			["texture"] = "icon-resource_texture",
			["shading_environment"] = "icon-resource_shading_environment",
			["item"] = "icon-resource_item",
			["template_definition"] = "icon-resource_template_definition",
			["particles"] = "icon-resource_particles",
			["control"] = "icon-resource_default",
			["palette"] = "icon-resource_default",
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

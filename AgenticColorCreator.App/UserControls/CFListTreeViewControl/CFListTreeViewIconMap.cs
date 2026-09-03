using System;
using System.Collections.Generic;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Maps dotless resource types to icon resource keys from the shared editor icon dictionary.
/// </summary>
public static class CFListTreeViewIconMap
{
	private const string DefaultIconResourceKey = "icon-resource_default";

	private static readonly IReadOnlyDictionary<string, string> TypeIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["animation"] = "icon-resource_animation",
		["item"] = "icon-resource_item",
		["level"] = "icon-resource_level",
		["material"] = "icon-resource_material",
		["particles"] = "icon-resource_particles",
		["shading_environment"] = "icon-resource_shading_environment",
		["state_machine"] = "icon-resource_state_machine",
		["template_definition"] = "icon-resource_template_definition",
		["texture"] = "icon-resource_texture",
		["unit"] = "icon-resource_unit",
		["wwise_bank"] = "icon-resource_wwise_bank",
		["wwise_event"] = "icon-resource_wwise_event",
	};

	public static string GetIconResourceKey(string? resourceType)
	{
		if (!string.IsNullOrWhiteSpace(resourceType) && TypeIconMap.TryGetValue(resourceType, out var iconResourceKey))
		{
			return iconResourceKey;
		}

		return DefaultIconResourceKey;
	}
}

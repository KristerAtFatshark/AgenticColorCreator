using System;
using System.Collections.Generic;

namespace AgenticColorCreator.Core.Models;

public enum InteractionState
{
	Default,
	Selected,
	Pressed,
	MouseOver,
	Disabled,
}

public static class InteractionStateCatalog
{
	public static IReadOnlyList<InteractionState> AllStates { get; } =
	[
		InteractionState.Default,
		InteractionState.Selected,
		InteractionState.Pressed,
		InteractionState.MouseOver,
		InteractionState.Disabled,
	];

	public static string ToLabel(InteractionState state)
	{
		return state switch
		{
			InteractionState.MouseOver => "MouseOver",
			_ => state.ToString(),
		};
	}

	public static bool TryParseLabel(string? value, out InteractionState state)
	{
		if (string.Equals(value, "Hovered", StringComparison.OrdinalIgnoreCase))
		{
			state = InteractionState.MouseOver;
			return true;
		}

		if (Enum.TryParse(value, true, out state))
		{
			foreach (var supportedState in AllStates)
			{
				if (supportedState == state)
				{
					return true;
				}
			}
		}

		state = InteractionState.Default;
		return false;
	}
}

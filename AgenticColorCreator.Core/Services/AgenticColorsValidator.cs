using System;
using System.Collections.Generic;
using AgenticColorCreator.Core.Models;

namespace AgenticColorCreator.Core.Services;

public static class AgenticColorsValidator
{
	public static IReadOnlyList<string> Validate(AgenticColorsDocument document)
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(document.Title))
		{
			errors.Add("Document title is required.");
		}

		if (document.Categories.Count == 0)
		{
			errors.Add("At least one category is required.");
		}

		foreach (var category in document.Categories)
		{
			if (string.IsNullOrWhiteSpace(category.Name))
			{
				errors.Add("Each category must have a name.");
			}

			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var color in category.Colors)
			{
				if (string.IsNullOrWhiteSpace(color.Name))
				{
					errors.Add($"Category '{category.Name}' contains a color without a name.");
				}
				else if (!seenNames.Add(color.Name.Trim()))
				{
					errors.Add($"Category '{category.Name}' contains duplicate color name '{color.Name.Trim()}'.");
				}

				if (string.IsNullOrWhiteSpace(color.HexValue))
				{
					errors.Add($"Color '{color.Name}' in category '{category.Name}' is missing a hex value.");
				}
				else if (!ColorHexParser.TryParseArgb(color.HexValue, out _))
				{
					errors.Add($"Color '{color.Name}' in category '{category.Name}' must use '#AARRGGBB'.");
				}
			}
		}

		return errors;
	}
}

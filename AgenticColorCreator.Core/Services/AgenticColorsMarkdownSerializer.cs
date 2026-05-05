using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgenticColorCreator.Core.Models;

namespace AgenticColorCreator.Core.Services;

public sealed class AgenticColorsMarkdownSerializer
{
	public const string CurrentFormatVersion = "agentic-colors/v1";

	public AgenticColorsDocument Deserialize(string markdown)
	{
		var lines = markdown.Replace("\r\n", "\n").Split('\n');
		string? title = null;
		string formatVersion = CurrentFormatVersion;
		var categories = new List<ColorCategory>();
		var currentCategoryName = string.Empty;
		var currentColors = new List<AgenticColorItem>();
		string? pendingColorName = null;
		string? pendingColorValue = null;
		var pendingDescriptionLines = new List<string>();

		void FlushPendingColor()
		{
			if (pendingColorName is null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(pendingColorValue))
			{
				throw new FormatException($"Color '{pendingColorName}' is missing a value.");
			}

			var (colorName, state) = ExtractColorNameAndState(currentCategoryName, pendingColorName);

			currentColors.Add(new AgenticColorItem(
				colorName,
				ColorHexParser.Normalize(pendingColorValue),
				string.Join("\n", pendingDescriptionLines).Trim(),
				state));

			pendingColorName = null;
			pendingColorValue = null;
			pendingDescriptionLines.Clear();
		}

		void FlushCategory()
		{
			FlushPendingColor();

			if (!string.IsNullOrWhiteSpace(currentCategoryName))
			{
				categories.Add(new ColorCategory(currentCategoryName, currentColors.ToList(), true));
			}

			currentCategoryName = string.Empty;
			currentColors.Clear();
		}

		foreach (var rawLine in lines)
		{
			var line = rawLine.TrimEnd();
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (line.StartsWith("# "))
			{
				title = line[2..].Trim();
				continue;
			}

			if (line.StartsWith("## Metadata", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (line.StartsWith("- format:", StringComparison.OrdinalIgnoreCase))
			{
				formatVersion = line[9..].Trim();
				continue;
			}

			if (line.StartsWith("- name:", StringComparison.OrdinalIgnoreCase))
			{
				title = line[7..].Trim();
				continue;
			}

			if (line.StartsWith("## Category:", StringComparison.OrdinalIgnoreCase))
			{
				FlushCategory();
				currentCategoryName = line[12..].Trim();
				continue;
			}

			if (line.StartsWith("### "))
			{
				FlushPendingColor();
				pendingColorName = line[4..].Trim();
				continue;
			}

			if (line.StartsWith("- value:", StringComparison.OrdinalIgnoreCase))
			{
				pendingColorValue = line[8..].Trim();
				continue;
			}

			if (line.StartsWith("- description:", StringComparison.OrdinalIgnoreCase))
			{
				pendingDescriptionLines.Add(line[14..].Trim());
				continue;
			}

			if (pendingColorName is not null)
			{
				pendingDescriptionLines.Add(line.Trim());
			}
		}

		FlushCategory();

		if (string.IsNullOrWhiteSpace(title))
		{
			throw new FormatException("Document title is missing.");
		}

		return new AgenticColorsDocument(title, string.IsNullOrWhiteSpace(formatVersion) ? CurrentFormatVersion : formatVersion, categories);
	}

	public string Serialize(AgenticColorsDocument document)
	{
		var validationErrors = AgenticColorsValidator.Validate(document);
		if (validationErrors.Count > 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
		}

		var builder = new StringBuilder();
		builder.AppendLine("# Agentic Colors");
		builder.AppendLine();
		builder.AppendLine("## Metadata");
		builder.AppendLine($"- format: {CurrentFormatVersion}");
		builder.AppendLine($"- name: {document.Title.Trim()}");

		foreach (var category in document.Categories)
		{
			builder.AppendLine();
			builder.AppendLine($"## Category: {category.Name.Trim()}");

			foreach (var color in category.Colors)
			{
				builder.AppendLine();
				builder.AppendLine($"### {category.Name.Trim()} / {color.Name.Trim()} {InteractionStateCatalog.ToLabel(color.State)}");
				builder.AppendLine($"- value: {ColorHexParser.Normalize(color.HexValue)}");
				builder.AppendLine($"- description: {NormalizeDescription(color.Description)}");
			}
		}

		return builder.ToString().TrimEnd() + Environment.NewLine;
	}

	private static (string Name, InteractionState State) ExtractColorNameAndState(string categoryName, string fullHeading)
	{
		var prefix = $"{categoryName} / ";
		var strippedHeading = fullHeading.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			? fullHeading[prefix.Length..].Trim()
			: fullHeading.Trim();

		foreach (var state in InteractionStateCatalog.AllStates)
		{
			var suffix = $" {InteractionStateCatalog.ToLabel(state)}";
			if (strippedHeading.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			{
				return (strippedHeading[..^suffix.Length].Trim(), state);
			}
		}

		return (strippedHeading, InteractionState.Default);
	}

	private static string NormalizeDescription(string description)
	{
		return string.IsNullOrWhiteSpace(description)
			? string.Empty
			: description.Replace("\r\n", "\n").Replace('\n', ' ').Trim();
	}
}

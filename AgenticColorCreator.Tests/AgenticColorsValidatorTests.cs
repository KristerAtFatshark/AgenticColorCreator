using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class AgenticColorsValidatorTests
{
	[Fact]
	public void Validate_ReturnsErrorForDuplicateNamesWithinCategory()
	{
		var document = new AgenticColorsDocument(
			"Theme",
			AgenticColorsMarkdownSerializer.CurrentFormatVersion,
			new List<ColorCategory>
			{
				new("Surface", new List<AgenticColorItem>
				{
					new("Panel", "#FF112233", "Primary panel", InteractionState.Default),
					new("Panel", "#FF445566", "Secondary panel", InteractionState.Default),
				}),
			});

		var errors = AgenticColorsValidator.Validate(document);

		Assert.Contains(errors, error => error.Contains("duplicate color name 'Panel'", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_ReturnsErrorForInvalidHexValue()
	{
		var document = new AgenticColorsDocument(
			"Theme",
			AgenticColorsMarkdownSerializer.CurrentFormatVersion,
			new List<ColorCategory>
			{
				new("Text", new List<AgenticColorItem>
				{
					new("Primary", "#123456", "Invalid format", InteractionState.Default),
				}),
			});

		var errors = AgenticColorsValidator.Validate(document);

		Assert.Contains(errors, error => error.Contains("must use '#AARRGGBB'", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_AllowsSameNameAcrossDifferentStates()
	{
		var document = new AgenticColorsDocument(
			"Theme",
			AgenticColorsMarkdownSerializer.CurrentFormatVersion,
			new List<ColorCategory>
			{
				new("Button", new List<AgenticColorItem>
				{
					new("Text Color", "#FFFFFFFF", "Default text", InteractionState.Default),
					new("Text Color", "#FFCCCCCC", "Disabled text", InteractionState.Disabled),
				}),
			});

		var errors = AgenticColorsValidator.Validate(document);

		Assert.Empty(errors);
	}
}

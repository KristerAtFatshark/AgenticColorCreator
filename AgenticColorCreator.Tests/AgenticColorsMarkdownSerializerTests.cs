using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class AgenticColorsMarkdownSerializerTests
{
    private readonly AgenticColorsMarkdownSerializer _serializer = new();

    [Fact]
    public void Serialize_WritesCanonicalMarkdown()
    {
        var document = new AgenticColorsDocument(
            "Default Theme",
            AgenticColorsMarkdownSerializer.CurrentFormatVersion,
            new List<ColorCategory>
            {
                new("Surface", new List<AgenticColorItem>
                {
                    new("App Background", "#ff101418", "Main application background."),
                }),
            });

        var markdown = _serializer.Serialize(document);

        Assert.Contains("# Agentic Colors", markdown);
        Assert.Contains("- format: agentic-colors/v1", markdown);
        Assert.Contains("- name: Default Theme", markdown);
        Assert.Contains("## Category: Surface", markdown);
        Assert.Contains("### Surface / App Background", markdown);
        Assert.Contains("- value: #FF101418", markdown);
    }

    [Fact]
    public void Deserialize_ReadsStructuredMarkdown()
    {
        const string markdown = """
# Agentic Colors

## Metadata
- format: agentic-colors/v1
- name: Default Theme

## Category: Text

### Text / Primary
- value: #FFF3F5F7
- description: High emphasis text.
""";

        var document = _serializer.Deserialize(markdown);

        Assert.Equal("Default Theme", document.Title);
        Assert.Single(document.Categories);
        Assert.Equal("Text", document.Categories[0].Name);
        Assert.Single(document.Categories[0].Colors);
        Assert.Equal("Primary", document.Categories[0].Colors[0].Name);
        Assert.Equal("#FFF3F5F7", document.Categories[0].Colors[0].HexValue);
    }

    [Fact]
    public void Deserialize_ThrowsWhenValueMissing()
    {
        const string markdown = """
# Agentic Colors

## Metadata
- format: agentic-colors/v1
- name: Broken Theme

## Category: Surface

### Surface / App Background
- description: Missing value.
""";

        Assert.Throws<FormatException>(() => _serializer.Deserialize(markdown));
    }
}

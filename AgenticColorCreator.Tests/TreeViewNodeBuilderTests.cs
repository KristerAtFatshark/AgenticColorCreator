using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class TreeViewNodeBuilderTests
{
	[Fact]
	public void Build_CreatesHierarchyAndMapsIconsFromTypes()
	{
		var sourceEntries = new List<TreeViewSourceEntry>
		{
			new() { Value = "controls/inputs/textbox", Type = "control" },
			new() { Value = "controls/inputs/checkbox", Type = "control" },
			new() { Value = "palette/inputs/checkbox", Type = "palette" },
		};
		var typeIconEntries = new List<TreeViewTypeIconEntry>
		{
			new() { Type = "folder", Icon = "📁" },
			new() { Type = "control", Icon = "🧩" },
			new() { Type = "palette", Icon = "🎨" },
		};
		var treeViewNodeBuilder = new TreeViewNodeBuilder();

		var rootNodes = treeViewNodeBuilder.Build(sourceEntries, typeIconEntries);

		Assert.Equal(2, rootNodes.Count);
		Assert.Equal("Controls", rootNodes[0].Text);
		Assert.Equal("folder", rootNodes[0].Type);
		Assert.Equal("📁", rootNodes[0].Icon);
		Assert.Equal("Inputs", rootNodes[0].Children[0].Text);
		Assert.Equal("Textbox", rootNodes[0].Children[0].Children[0].Text);
		Assert.Equal("🧩", rootNodes[0].Children[0].Children[0].Icon);
		Assert.Equal("Palette", rootNodes[1].Text);
		Assert.Equal("🎨", rootNodes[1].Children[0].Children[0].Icon);
	}
}

using System.Collections.Generic;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Represents one persistent folder or source leaf in the non-visual tree graph.
/// </summary>
internal sealed class CFListTreeNode
{
	public bool IsFolder { get; init; }

	public string SortKey { get; init; } = string.Empty;

	public int SourceIndex { get; init; }

	public CFListTreeNode? Parent { get; init; }

	public int Depth { get; init; }

	public List<CFListTreeNode> Children { get; } = new();

	public CFListTreeViewRow Row { get; set; } = null!;

	public bool IsFilterVisible { get; set; }
}

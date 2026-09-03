namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Supplies the structural, sorting, display-name, and resource-type metadata used to place a source
/// object in a <see cref="CFListTreeView"/>.
/// </summary>
public interface ICFTreeViewItem
{
	string? TreeFolderPath { get; }

	string? TreeSortKey { get; }

	string ResourceName { get; }

	string ResourceType { get; }
}

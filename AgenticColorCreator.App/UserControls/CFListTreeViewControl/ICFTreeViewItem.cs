namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

public interface ICFTreeViewItem
{
	string? TreeFolderPath { get; }

	string? TreeSortKey { get; }

	string ResourceName { get; }

	string ResourceType { get; }
}

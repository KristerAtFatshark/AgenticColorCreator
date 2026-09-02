using AgenticColorCreator.App.UserControls.CFListTreeViewControl;

namespace AgenticColorCreator.App.ViewModels;

public sealed class PreviewCFListTreeItem : ICFTreeViewItem
{
	public PreviewCFListTreeItem(string resourceName, string resourceType, string? treeFolderPath, string? treeSortKey = null)
	{
		ResourceName = resourceName;
		ResourceType = resourceType;
		TreeFolderPath = treeFolderPath;
		TreeSortKey = treeSortKey ?? resourceName + "." + resourceType;
	}

	public string ResourceName { get; }

	public string ResourceType { get; }

	public string? TreeFolderPath { get; }

	public string? TreeSortKey { get; }

	public override string ToString()
	{
		var resource = ResourceName + "." + ResourceType;
		return string.IsNullOrEmpty(TreeFolderPath) ? resource : TreeFolderPath + "/" + resource;
	}
}

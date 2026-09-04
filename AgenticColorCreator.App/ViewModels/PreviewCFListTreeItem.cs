using AgenticColorCreator.App.UserControls.CFListTreeViewControl;

#if NETCORE
namespace AgenticColorCreator.App.ViewModels;

/// <summary>
/// Supplies production-shaped resource metadata for the CFListTreeView UI preview.
/// </summary>
public sealed class PreviewCFListTreeItem : ICFTreeViewItem
{
#else
namespace AgenticColorCreator.App.ViewModels
{
	/// <summary>
	/// Supplies production-shaped resource metadata for the CFListTreeView UI preview.
	/// </summary>
	public sealed class PreviewCFListTreeItem : ICFTreeViewItem
	{
#endif
	public PreviewCFListTreeItem(
		string resourceName,
		string resourceType,
#if NETCORE
		string? treeFolderPath,
		string? treeSortKey = null)
#else
		string treeFolderPath,
		string treeSortKey = null)
#endif
	{
		ResourceName = resourceName;
		ResourceType = resourceType;
		TreeFolderPath = treeFolderPath;
		TreeSortKey = treeSortKey ?? resourceName + "." + resourceType;
	}

	public string ResourceName { get; }

	public string ResourceType { get; }

#if NETCORE
	public string? TreeFolderPath { get; }

	public string? TreeSortKey { get; }
#else
	public string TreeFolderPath { get; private set; }

	public string TreeSortKey { get; private set; }
#endif

	public override string ToString()
	{
		var resource = ResourceName + "." + ResourceType;
		return string.IsNullOrEmpty(TreeFolderPath) ? resource : TreeFolderPath + "/" + resource;
	}
}

#if !NETCORE
}
#endif

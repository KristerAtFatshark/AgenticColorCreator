namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl
{
	/// <summary>
	/// Supplies the structural, sorting, display-name, and resource-type metadata used to place a source
	/// object in a <see cref="CFListTreeView"/>.
	/// </summary>
	public interface ICFTreeViewItem
	{
		#if NETCORE
		string? TreeFolderPath { get; }

		string? TreeSortKey { get; }
		#else
		string TreeFolderPath { get; }

		string TreeSortKey { get; }
		#endif

		string ResourceName { get; }

		string ResourceType { get; }
	}
}

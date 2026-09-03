using System.Collections.Generic;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl
{
	/// <summary>
	/// Represents one persistent folder or source leaf in the non-visual tree graph.
	/// </summary>
	internal sealed class CFListTreeNode
	{
		public bool IsFolder { get; set; }

		public string SortKey { get; set; } = string.Empty;

		public int SourceIndex { get; set; }

		#if NETCORE
		public CFListTreeNode? Parent { get; set; }
		#else
		public CFListTreeNode Parent { get; set; }
		#endif

		public int Depth { get; set; }

		public List<CFListTreeNode> Children { get; } = new List<CFListTreeNode>();

		#if NETCORE
		public CFListTreeViewRow Row { get; set; } = null!;
		#else
		public CFListTreeViewRow Row { get; set; }
		#endif

		public bool IsFilterVisible { get; set; }
	}
}

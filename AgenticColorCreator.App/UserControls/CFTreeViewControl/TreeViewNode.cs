using System;
using System.Collections.Generic;


namespace ClownFishUi.CFUserControls.CFTreeViewControl

{
	public sealed class TreeViewNode
	{
		public string Text { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;

		public string Icon { get; set; } = string.Empty;

		public List<TreeViewNode> Children { get; } = new List<TreeViewNode>();

		public Dictionary<string, TreeViewNode> ChildIndex { get; } = new Dictionary<string, TreeViewNode>(StringComparer.OrdinalIgnoreCase);
	}
}

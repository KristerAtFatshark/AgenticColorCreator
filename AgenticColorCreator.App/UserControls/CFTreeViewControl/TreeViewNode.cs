using System.Collections.ObjectModel;


namespace ClownFishUi.CFUserControls.CFTreeViewControl

{
	public sealed class TreeViewNode
	{
		public string Text { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;

		public string Icon { get; set; } = string.Empty;

		public ObservableCollection<TreeViewNode> Children { get; set; } = new ObservableCollection<TreeViewNode>();
	}
}

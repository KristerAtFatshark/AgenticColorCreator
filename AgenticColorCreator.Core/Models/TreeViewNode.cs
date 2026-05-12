using System.Collections.ObjectModel;

namespace AgenticColorCreator.Core.Models;

public sealed class TreeViewNode
{
	public string Text { get; set; } = string.Empty;

	public string Value { get; set; } = string.Empty;

	public string Type { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public ObservableCollection<TreeViewNode> Children { get; set; } = [];
}

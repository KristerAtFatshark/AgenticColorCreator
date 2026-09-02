using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

public sealed class CFListTreeViewRow : INotifyPropertyChanged
{
	private bool _isExpanded;
	private bool _isSelected;

	internal CFListTreeViewRow(bool isFolder, string name, string resourceType, object? item, CFListTreeViewIconImages iconImages, int depth)
	{
		IsFolder = isFolder;
		Name = name;
		ResourceType = resourceType;
		Item = item;
		DefaultIcon = iconImages.Default;
		MouseOverIcon = iconImages.MouseOver;
		SelectedIcon = iconImages.Selected;
		Depth = depth;
		Indent = new Thickness(depth * 18, 0, 0, 0);
		_isExpanded = isFolder;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public bool IsFolder { get; }

	public string Name { get; }

	public string ResourceType { get; }

	public object? Item { get; }

	public ImageSource DefaultIcon { get; }

	public ImageSource MouseOverIcon { get; }

	public ImageSource SelectedIcon { get; }

	public int Depth { get; }

	public Thickness Indent { get; }

	public bool HasChildren => Node.Children.Count > 0;

	public bool IsExpanded
	{
		get => _isExpanded;
		internal set
		{
			if (_isExpanded == value)
			{
				return;
			}

			_isExpanded = value;
			OnPropertyChanged();
		}
	}

	public bool IsSelected
	{
		get => _isSelected;
		internal set
		{
			if (_isSelected == value)
			{
				return;
			}

			_isSelected = value;
			OnPropertyChanged();
		}
	}

	internal CFListTreeNode Node { get; set; } = null!;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

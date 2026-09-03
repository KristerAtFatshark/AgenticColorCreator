using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl
{
	/// <summary>
	/// Exposes stable presentation state for one flattened ListView row while retaining its graph node.
	/// </summary>
	public sealed class CFListTreeViewRow : INotifyPropertyChanged
	{
		private bool _isExpanded;
		private bool _isSelected;

		internal CFListTreeViewRow(
			bool isFolder,
			string name,
			string resourceType,
			#if NETCORE
			object? item,
			#else
			object item,
			#endif
			CFListTreeViewIconImages iconImages,
			int depth)
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

		#if NETCORE
		public event PropertyChangedEventHandler? PropertyChanged;
		#else
		public event PropertyChangedEventHandler PropertyChanged;
		#endif

		public bool IsFolder { get; private set; }

		public string Name { get; private set; }

		public string ResourceType { get; private set; }

		#if NETCORE
		public object? Item { get; private set; }
		#else
		public object Item { get; private set; }
		#endif

		public ImageSource DefaultIcon { get; private set; }

		public ImageSource MouseOverIcon { get; private set; }

		public ImageSource SelectedIcon { get; private set; }

		public int Depth { get; private set; }

		public Thickness Indent { get; private set; }

		public bool IsExpanded
		{
			get { return _isExpanded; }
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
			get { return _isSelected; }
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

		#if NETCORE
		internal CFListTreeNode Node { get; set; } = null!;
		#else
		internal CFListTreeNode Node { get; set; }
		#endif

		private void OnPropertyChanged(
			#if NETCORE
			[CallerMemberName] string? propertyName = null
			#else
			[CallerMemberName] string propertyName = null
			#endif
			)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}

using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AgenticColorCreator.App.Commands;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.Core.Models;

namespace AgenticColorCreator.App.ViewModels;

public sealed class CategoryViewModel : ViewModelBase
{
	private string _name;
	private bool _isExpanded;
	private readonly Action _markDirty;
	private readonly IColorPickerDialogService _colorPickerDialogService;

	public CategoryViewModel(
		ColorCategory model,
		Action<CategoryViewModel> removeAction,
		Action markDirty,
		IColorPickerDialogService colorPickerDialogService)
	{
		_name = model.Name;
		_isExpanded = model.IsExpanded;
		_markDirty = markDirty;
		_colorPickerDialogService = colorPickerDialogService;
		Colors = new ObservableCollection<ColorItemViewModel>(model.Colors.Select(color => new ColorItemViewModel(color, RemoveColor, MarkDirty, _colorPickerDialogService)));
		AddColorCommand = new RelayCommand(AddColor);
		RemoveCategoryCommand = new RelayCommand(() => removeAction(this));
	}

	public ObservableCollection<ColorItemViewModel> Colors { get; }

	public ICommand AddColorCommand { get; }

	public ICommand RemoveCategoryCommand { get; }

	public string Name
	{
		get => _name;
		set
		{
			if (SetProperty(ref _name, value))
			{
				MarkDirty();
			}
		}
	}

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (SetProperty(ref _isExpanded, value))
			{
				MarkDirty();
			}
		}
	}

	public void AddColor()
	{
		Colors.Add(new ColorItemViewModel(new AgenticColorItem("New Color", "#FF000000", string.Empty), RemoveColor, MarkDirty, _colorPickerDialogService));
		IsExpanded = true;
		MarkDirty();
	}

	internal ColorCategory ToModel()
	{
		return new ColorCategory(Name.Trim(), Colors.Select(color => color.ToModel()).ToList(), IsExpanded);
	}

	private void RemoveColor(ColorItemViewModel color)
	{
		Colors.Remove(color);
		MarkDirty();
	}

	private void MarkDirty()
	{
		_markDirty();
	}
}

using System;
using System.Collections.Generic;
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
	private readonly Dictionary<InteractionState, StateGroupViewModel> _stateGroupsByState;

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
		_stateGroupsByState = InteractionStateCatalog.AllStates.ToDictionary(
			state => state,
			state => new StateGroupViewModel(state, () => AddColor(state)));
		StateGroups = new ObservableCollection<StateGroupViewModel>(_stateGroupsByState.Values);

		foreach (var color in model.Colors)
		{
			AddColorItem(color);
		}

		AddColorCommand = new RelayCommand(AddColor);
		RemoveCategoryCommand = new RelayCommand(() => removeAction(this));
	}

	public ObservableCollection<StateGroupViewModel> StateGroups { get; }

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
		AddColor(InteractionState.Default);
	}

	internal ColorCategory ToModel()
	{
		return new ColorCategory(Name.Trim(), StateGroups.SelectMany(group => group.Colors).Select(color => color.ToModel()).ToList(), IsExpanded);
	}

	private void RemoveColor(ColorItemViewModel color)
	{
		foreach (var group in StateGroups)
		{
			if (group.Colors.Remove(color))
			{
				break;
			}
		}

		MarkDirty();
	}

	private void AddColor(InteractionState state)
	{
		AddColorItem(new AgenticColorItem("New Color", "#FF000000", string.Empty, state));
		IsExpanded = true;
		MarkDirty();
	}

	private void AddColorItem(AgenticColorItem color)
	{
		var colorViewModel = new ColorItemViewModel(color, RemoveColor, MoveColorToState, MarkDirty, _colorPickerDialogService);
		_stateGroupsByState[color.State].Colors.Add(colorViewModel);
	}

	private void MoveColorToState(ColorItemViewModel color, InteractionState targetState)
	{
		foreach (var group in StateGroups)
		{
			if (group.Colors.Remove(color))
			{
				break;
			}
		}

		_stateGroupsByState[targetState].Colors.Add(color);
	}

	private void MarkDirty()
	{
		_markDirty();
	}
}

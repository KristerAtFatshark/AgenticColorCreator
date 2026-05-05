using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using AgenticColorCreator.App.Commands;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.ViewModels;

public sealed class ColorItemViewModel : ViewModelBase
{
	private string _name;
	private string _hexValue;
	private string _description;
	private InteractionState _state;

	private readonly IColorPickerDialogService _colorPickerDialogService;
	private readonly Action<ColorItemViewModel, InteractionState> _moveStateAction;

	public ColorItemViewModel(
		AgenticColorItem model,
		Action<ColorItemViewModel> removeAction,
		Action<ColorItemViewModel, InteractionState> moveStateAction,
		Action markDirty,
		IColorPickerDialogService colorPickerDialogService)
	{
		_name = model.Name;
		_hexValue = model.HexValue;
		_description = model.Description;
		_state = model.State;
		_colorPickerDialogService = colorPickerDialogService;
		_moveStateAction = moveStateAction;
		RemoveCommand = new RelayCommand(() => removeAction(this));
		OpenColorPickerCommand = new RelayCommand(OpenColorPicker);
		MarkDirty = markDirty;
	}

	public ICommand RemoveCommand { get; }

	public ICommand OpenColorPickerCommand { get; }

	public IReadOnlyList<InteractionState> AvailableStates => InteractionStateCatalog.AllStates;

	public Brush PreviewBrush
	{
		get
		{
			return ColorHexParser.TryParseArgb(_hexValue, out var color)
				? new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
				: new SolidColorBrush(Color.FromArgb(255, 70, 70, 70));
		}
	}

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

	public string HexValue
	{
		get => _hexValue;
		set
		{
			if (SetProperty(ref _hexValue, value))
			{
				OnPropertyChanged(nameof(PreviewBrush));
				MarkDirty();
			}
		}
	}

	public string Description
	{
		get => _description;
		set
		{
			if (SetProperty(ref _description, value))
			{
				MarkDirty();
			}
		}
	}

	public InteractionState State
	{
		get => _state;
		set
		{
			if (SetProperty(ref _state, value))
			{
				_moveStateAction(this, value);
				MarkDirty();
			}
		}
	}

	internal AgenticColorItem ToModel()
	{
		return new AgenticColorItem(Name.Trim(), HexValue.Trim(), Description.Trim(), State);
	}

	private void OpenColorPicker()
	{
		var owner = Application.Current?.MainWindow;
		var selectedColor = _colorPickerDialogService.PickColor(HexValue, owner);

		if (!string.IsNullOrWhiteSpace(selectedColor))
		{
			HexValue = selectedColor;
		}
	}

	private Action MarkDirty { get; }
}

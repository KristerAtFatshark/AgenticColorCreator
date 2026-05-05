using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AgenticColorCreator.App.Commands;
using AgenticColorCreator.Core.Models;

namespace AgenticColorCreator.App.ViewModels;

public sealed class StateGroupViewModel : ViewModelBase
{
	public StateGroupViewModel(InteractionState state, Action addColorAction)
	{
		State = state;
		Colors = new ObservableCollection<ColorItemViewModel>();
		AddColorCommand = new RelayCommand(addColorAction);
	}

	public InteractionState State { get; }

	public string DisplayName => InteractionStateCatalog.ToLabel(State);

	public ObservableCollection<ColorItemViewModel> Colors { get; }

	public ICommand AddColorCommand { get; }
}

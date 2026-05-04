using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AgenticColorCreator.App.Commands;
using AgenticColorCreator.App.Dialogs;
using AgenticColorCreator.App.Services;
using AgenticColorCreator.Core.Models;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
	private readonly AgenticColorsMarkdownSerializer _serializer;
	private readonly IFileDialogService _fileDialogService;
	private readonly IMessageBoxService _messageBoxService;
	private readonly IColorPickerDialogService _colorPickerDialogService;
	private string _title = "Untitled Theme";
	private string? _currentFilePath;
	private string _statusMessage = "Ready";
	private bool _isDirty;

	public MainWindowViewModel(
		AgenticColorsMarkdownSerializer serializer,
		IFileDialogService fileDialogService,
		IMessageBoxService messageBoxService,
		IColorPickerDialogService colorPickerDialogService)
	{
		_serializer = serializer;
		_fileDialogService = fileDialogService;
		_messageBoxService = messageBoxService;
		_colorPickerDialogService = colorPickerDialogService;

		Categories = new ObservableCollection<CategoryViewModel>();
		ValidationErrors = new ObservableCollection<string>();

		NewDocumentCommand = new RelayCommand(NewDocument);
		OpenDocumentCommand = new RelayCommand(OpenDocument);
		SaveDocumentCommand = new RelayCommand(SaveDocument);
		SaveDocumentAsCommand = new RelayCommand(SaveDocumentAs);
		AddCategoryCommand = new RelayCommand(AddCategory);

		NewDocument();
	}

	public ObservableCollection<CategoryViewModel> Categories { get; }

	public ObservableCollection<string> ValidationErrors { get; }

	public ICommand NewDocumentCommand { get; }

	public ICommand OpenDocumentCommand { get; }

	public ICommand SaveDocumentCommand { get; }

	public ICommand SaveDocumentAsCommand { get; }

	public ICommand AddCategoryCommand { get; }

	public string Title
	{
		get => _title;
		set
		{
			if (SetProperty(ref _title, value))
			{
				MarkDirty();
			}
		}
	}

	public string CurrentFileDisplay => string.IsNullOrWhiteSpace(_currentFilePath) ? "Current File: not saved yet" : $"Current File: {_currentFilePath}";

	public string StatusMessage
	{
		get => _statusMessage;
		private set => SetProperty(ref _statusMessage, value);
	}

	public bool HasValidationErrors => ValidationErrors.Count > 0;

	public void AddCategory()
	{
		Categories.Add(new CategoryViewModel(new ColorCategory($"Category {Categories.Count + 1}", new List<AgenticColorItem>(), true), RemoveCategory, MarkDirty, _colorPickerDialogService));
		MarkDirty();
	}

	public bool ConfirmClose()
	{
		if (!_isDirty)
		{
			return true;
		}

		return _messageBoxService.Confirm("You have unsaved changes. Close without saving?", "Unsaved Changes");
	}

	private void NewDocument()
	{
		if (!ConfirmAbandonUnsavedChanges())
		{
			return;
		}

		_title = "Untitled Theme";
		_currentFilePath = null;
		Categories.Clear();
		Categories.Add(new CategoryViewModel(new ColorCategory("Surface", new List<AgenticColorItem>
		{
			new("App Background", "#FF101418", "Main application window background."),
			new("Panel", "#FF1A2026", "Secondary containers such as panels and cards."),
		}, true), RemoveCategory, MarkDirty, _colorPickerDialogService));
		Categories.Add(new CategoryViewModel(new ColorCategory("Text", new List<AgenticColorItem>
		{
			new("Primary", "#FFF3F5F7", "Default high emphasis text color."),
		}, true), RemoveCategory, MarkDirty, _colorPickerDialogService));
		_isDirty = false;
		ValidateDocument();
		OnPropertyChanged(nameof(Title));
		OnPropertyChanged(nameof(CurrentFileDisplay));
		StatusMessage = "Created new document";
	}

	private void OpenDocument()
	{
		if (!ConfirmAbandonUnsavedChanges())
		{
			return;
		}

		var path = _fileDialogService.OpenMarkdownFile();
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		try
		{
			var markdown = File.ReadAllText(path);
			var document = _serializer.Deserialize(markdown);
			LoadDocument(document);
			_currentFilePath = path;
			_isDirty = false;
			OnPropertyChanged(nameof(CurrentFileDisplay));
			StatusMessage = "Document loaded";
		}
		catch (Exception exception)
		{
			_messageBoxService.ShowError($"Failed to open file.\n\n{exception.Message}", "Open Failed");
		}
	}

	private void SaveDocument()
	{
		if (string.IsNullOrWhiteSpace(_currentFilePath))
		{
			SaveDocumentAs();
			return;
		}

		SaveToPath(_currentFilePath);
	}

	private void SaveDocumentAs()
	{
		var path = _fileDialogService.SaveMarkdownFile(_currentFilePath);
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		SaveToPath(path);
	}

	private bool ConfirmAbandonUnsavedChanges()
	{
		if (!_isDirty)
		{
			return true;
		}

		return _messageBoxService.Confirm("You have unsaved changes. Continue without saving them?", "Unsaved Changes");
	}

	private void LoadDocument(AgenticColorsDocument document)
	{
		_title = document.Title;
		Categories.Clear();

		foreach (var category in document.Categories)
		{
			Categories.Add(new CategoryViewModel(category, RemoveCategory, MarkDirty, _colorPickerDialogService));
		}

		OnPropertyChanged(nameof(Title));
		ValidateDocument();
	}

	private void MarkDirty()
	{
		_isDirty = true;
		ValidateDocument();
		StatusMessage = "Unsaved changes";
	}

	private void RemoveCategory(CategoryViewModel category)
	{
		Categories.Remove(category);
		MarkDirty();
	}

	private void SaveToPath(string path)
	{
		try
		{
			var document = BuildDocument();
			var validationErrors = AgenticColorsValidator.Validate(document);
			ApplyValidation(validationErrors);

			if (validationErrors.Count > 0)
			{
				_messageBoxService.ShowError("Fix validation issues before saving.", "Validation Failed");
				return;
			}

			var markdown = _serializer.Serialize(document);
			File.WriteAllText(path, markdown);
			_currentFilePath = path;
			_isDirty = false;
			OnPropertyChanged(nameof(CurrentFileDisplay));
			StatusMessage = "Document saved";
		}
		catch (Exception exception)
		{
			_messageBoxService.ShowError($"Failed to save file.\n\n{exception.Message}", "Save Failed");
		}
	}

	private AgenticColorsDocument BuildDocument()
	{
		return new AgenticColorsDocument(
			string.IsNullOrWhiteSpace(Title) ? "Untitled Theme" : Title.Trim(),
			AgenticColorsMarkdownSerializer.CurrentFormatVersion,
			Categories.Select(category => category.ToModel()).ToList());
	}

	private void ValidateDocument()
	{
		ApplyValidation(AgenticColorsValidator.Validate(BuildDocument()));
	}

	private void ApplyValidation(IReadOnlyList<string> errors)
	{
		ValidationErrors.Clear();

		foreach (var error in errors)
		{
			ValidationErrors.Add(error);
		}

		OnPropertyChanged(nameof(HasValidationErrors));
	}
}

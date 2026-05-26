using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgenticColorCreator.App.ViewModels;

namespace AgenticColorCreator.App.Dialogs;

public partial class HdrColorPickerWindow : Window
{
	private readonly HdrColorPickerViewModel _viewModel;
	private bool _isDraggingHue;
	private bool _isDraggingSaturationValue;

	public HdrColorPickerWindow(string initialHexValue, double initialStops)
	{
		InitializeComponent();
		_viewModel = new HdrColorPickerViewModel(initialHexValue, initialStops);
		DataContext = _viewModel;
		SelectedHexValue = _viewModel.HexValue;
		SelectedStops = _viewModel.Stops;
	}

	public string SelectedHexValue { get; private set; }

	public double SelectedStops { get; private set; }

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		SelectedHexValue = _viewModel.HexValue;
		SelectedStops = _viewModel.Stops;
		DialogResult = true;
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}

	private void HueRingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_isDraggingHue = true;
		HueRingCanvas.CaptureMouse();
		_viewModel.UpdateHueFromPoint(e.GetPosition(HueRingCanvas), new Size(HueRingCanvas.ActualWidth, HueRingCanvas.ActualHeight));
	}

	private void HueRingCanvas_MouseMove(object sender, MouseEventArgs e)
	{
		if (!_isDraggingHue)
		{
			return;
		}

		_viewModel.UpdateHueFromPoint(e.GetPosition(HueRingCanvas), new Size(HueRingCanvas.ActualWidth, HueRingCanvas.ActualHeight));
	}

	private void SaturationValueCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		_isDraggingSaturationValue = true;
		SaturationValueCanvas.CaptureMouse();
		_viewModel.UpdateSaturationValueFromPoint(e.GetPosition(SaturationValueCanvas), new Size(SaturationValueCanvas.ActualWidth, SaturationValueCanvas.ActualHeight));
	}

	private void SaturationValueCanvas_MouseMove(object sender, MouseEventArgs e)
	{
		if (!_isDraggingSaturationValue)
		{
			return;
		}

		_viewModel.UpdateSaturationValueFromPoint(e.GetPosition(SaturationValueCanvas), new Size(SaturationValueCanvas.ActualWidth, SaturationValueCanvas.ActualHeight));
	}

	private void PickerSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDraggingHue)
		{
			_isDraggingHue = false;
			HueRingCanvas.ReleaseMouseCapture();
		}

		if (_isDraggingSaturationValue)
		{
			_isDraggingSaturationValue = false;
			SaturationValueCanvas.ReleaseMouseCapture();
		}
	}
}

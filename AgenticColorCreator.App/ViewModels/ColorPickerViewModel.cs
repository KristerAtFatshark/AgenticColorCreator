using System;
using System.Globalization;
using System.Windows.Media;
using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.App.ViewModels;

public sealed class ColorPickerViewModel : ViewModelBase
{
    private bool _isUpdatingFromHex;
    private int _alpha;
    private int _red;
    private int _green;
    private int _blue;
    private string _hexValue;

    public ColorPickerViewModel(string initialHexValue)
    {
        _hexValue = ColorHexParser.TryParseArgb(initialHexValue, out var color)
            ? ColorHexParser.Normalize(initialHexValue)
            : "#FFFFFFFF";

        _alpha = color.A;
        _red = color.R;
        _green = color.G;
        _blue = color.B;
    }

    public int Alpha
    {
        get => _alpha;
        set => SetChannel(ref _alpha, value, nameof(Alpha), nameof(AlphaText));
    }

    public int Red
    {
        get => _red;
        set => SetChannel(ref _red, value, nameof(Red), nameof(RedText));
    }

    public int Green
    {
        get => _green;
        set => SetChannel(ref _green, value, nameof(Green), nameof(GreenText));
    }

    public int Blue
    {
        get => _blue;
        set => SetChannel(ref _blue, value, nameof(Blue), nameof(BlueText));
    }

    public string AlphaText
    {
        get => Alpha.ToString(CultureInfo.InvariantCulture);
        set => SetTextChannel(value, channel => Alpha = channel);
    }

    public string RedText
    {
        get => Red.ToString(CultureInfo.InvariantCulture);
        set => SetTextChannel(value, channel => Red = channel);
    }

    public string GreenText
    {
        get => Green.ToString(CultureInfo.InvariantCulture);
        set => SetTextChannel(value, channel => Green = channel);
    }

    public string BlueText
    {
        get => Blue.ToString(CultureInfo.InvariantCulture);
        set => SetTextChannel(value, channel => Blue = channel);
    }

    public string HexValue
    {
        get => _hexValue;
        set
        {
            if (!SetProperty(ref _hexValue, value))
            {
                return;
            }

            if (_isUpdatingFromHex)
            {
                OnPropertyChanged(nameof(PreviewBrush));
                return;
            }

            if (ColorHexParser.TryParseArgb(value, out var color))
            {
                _isUpdatingFromHex = true;
                _alpha = color.A;
                _red = color.R;
                _green = color.G;
                _blue = color.B;
                RaiseAllChannelProperties();
                _isUpdatingFromHex = false;
            }

            OnPropertyChanged(nameof(PreviewBrush));
        }
    }

    public Brush PreviewBrush => new SolidColorBrush(Color.FromArgb((byte)Alpha, (byte)Red, (byte)Green, (byte)Blue));

    private void RaiseAllChannelProperties()
    {
        OnPropertyChanged(nameof(Alpha));
        OnPropertyChanged(nameof(Red));
        OnPropertyChanged(nameof(Green));
        OnPropertyChanged(nameof(Blue));
        OnPropertyChanged(nameof(AlphaText));
        OnPropertyChanged(nameof(RedText));
        OnPropertyChanged(nameof(GreenText));
        OnPropertyChanged(nameof(BlueText));
    }

    private void SetChannel(ref int field, int value, string propertyName, string textPropertyName)
    {
        value = Math.Clamp(value, 0, 255);

        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        OnPropertyChanged(textPropertyName);
        UpdateHexFromChannels();
    }

    private void SetTextChannel(string value, Action<int> setter)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            setter(parsed);
        }
    }

    private void UpdateHexFromChannels()
    {
        _isUpdatingFromHex = true;
        HexValue = $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
        _isUpdatingFromHex = false;
        OnPropertyChanged(nameof(PreviewBrush));
    }
}

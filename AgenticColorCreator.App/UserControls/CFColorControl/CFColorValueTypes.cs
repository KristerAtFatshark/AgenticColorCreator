namespace AgenticColorCreator.App.UserControls.CFColorControl;

public readonly record struct CFColorRgb(byte Red, byte Green, byte Blue, byte Alpha);

public readonly record struct CFColorHsv(double Hue, double Saturation, double Value, byte Alpha);

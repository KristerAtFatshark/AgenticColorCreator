namespace AgenticColorCreator.App.UserControls.CFHdrColorControl;

public readonly record struct CFHdrColorRgb(byte Red, byte Green, byte Blue, byte Alpha, double Stops);

public readonly record struct CFHdrColorHsv(double Hue, double Saturation, double Value, byte Alpha, double Stops);

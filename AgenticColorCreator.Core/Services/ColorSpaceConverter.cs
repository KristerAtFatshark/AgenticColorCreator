using System;

namespace AgenticColorCreator.Core.Services;

public static class ColorSpaceConverter
{
	public static HsvColor RgbToHsv(byte red, byte green, byte blue)
	{
		var r = red / 255d;
		var g = green / 255d;
		var b = blue / 255d;

		var max = Math.Max(r, Math.Max(g, b));
		var min = Math.Min(r, Math.Min(g, b));
		var delta = max - min;

		double hue;
		if (delta == 0)
		{
			hue = 0;
		}
		else if (max == r)
		{
			hue = 60d * (((g - b) / delta) % 6d);
		}
		else if (max == g)
		{
			hue = 60d * (((b - r) / delta) + 2d);
		}
		else
		{
			hue = 60d * (((r - g) / delta) + 4d);
		}

		if (hue < 0)
		{
			hue += 360d;
		}

		var saturation = max == 0 ? 0 : delta / max;
		return new HsvColor(hue, saturation, max);
	}

	public static RgbColor HsvToRgb(double hue, double saturation, double value)
	{
		hue = NormalizeHue(hue);
		saturation = Math.Clamp(saturation, 0d, 1d);
		value = Math.Clamp(value, 0d, 1d);

		var chroma = value * saturation;
		var segment = hue / 60d;
		var x = chroma * (1d - Math.Abs((segment % 2d) - 1d));
		var match = value - chroma;

		(double rPrime, double gPrime, double bPrime) = segment switch
		{
			>= 0 and < 1 => (chroma, x, 0d),
			>= 1 and < 2 => (x, chroma, 0d),
			>= 2 and < 3 => (0d, chroma, x),
			>= 3 and < 4 => (0d, x, chroma),
			>= 4 and < 5 => (x, 0d, chroma),
			_ => (chroma, 0d, x),
		};

		return new RgbColor(
			(byte)Math.Clamp(Math.Round((rPrime + match) * 255d), 0d, 255d),
			(byte)Math.Clamp(Math.Round((gPrime + match) * 255d), 0d, 255d),
			(byte)Math.Clamp(Math.Round((bPrime + match) * 255d), 0d, 255d));
	}

	private static double NormalizeHue(double hue)
	{
		hue %= 360d;
		if (hue < 0)
		{
			hue += 360d;
		}

		return hue;
	}
}

public readonly record struct HsvColor(double Hue, double Saturation, double Value);

public readonly record struct RgbColor(byte Red, byte Green, byte Blue);

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AgenticColorCreator.App.Dialogs;

internal static class HueWheelBrushFactory
{
	public static ImageBrush Create(int size, double outerRadius, double innerRadius)
	{
		var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Pbgra32, null);
		var stride = size * 4;
		var pixels = new byte[size * stride];
		var center = (size - 1) / 2d;

		for (var y = 0; y < size; y++)
		{
			for (var x = 0; x < size; x++)
			{
				var dx = x - center;
				var dy = y - center;
				var distance = Math.Sqrt((dx * dx) + (dy * dy));
				if (distance < innerRadius || distance > outerRadius)
				{
					continue;
				}

				var angle = Math.Atan2(dy, dx) * 180d / Math.PI;
				var hue = angle + 90d;
				if (hue < 0)
				{
					hue += 360d;
				}

				var color = ColorFromHue(hue);
				var offset = (y * stride) + (x * 4);
				pixels[offset] = color.B;
				pixels[offset + 1] = color.G;
				pixels[offset + 2] = color.R;
				pixels[offset + 3] = 255;
			}
		}

		bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);
		bitmap.Freeze();

		var brush = new ImageBrush(bitmap)
		{
			Stretch = Stretch.Fill,
		};
		brush.Freeze();
		return brush;
	}

	private static Color ColorFromHue(double hue)
	{
		var segment = hue / 60d;
		var x = 1d - Math.Abs((segment % 2d) - 1d);

		(double r, double g, double b) = segment switch
		{
			>= 0 and < 1 => (1d, x, 0d),
			>= 1 and < 2 => (x, 1d, 0d),
			>= 2 and < 3 => (0d, 1d, x),
			>= 3 and < 4 => (0d, x, 1d),
			>= 4 and < 5 => (x, 0d, 1d),
			_ => (1d, 0d, x),
		};

		return Color.FromRgb(
			(byte)Math.Round(r * 255d),
			(byte)Math.Round(g * 255d),
			(byte)Math.Round(b * 255d));
	}
}

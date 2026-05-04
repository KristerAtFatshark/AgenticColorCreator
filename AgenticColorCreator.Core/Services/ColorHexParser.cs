using System.Globalization;

namespace AgenticColorCreator.Core.Services;

public static class ColorHexParser
{
    public static bool TryParseArgb(string? value, out ArgbColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length != 9 || trimmed[0] != '#')
        {
            return false;
        }

        if (!byte.TryParse(trimmed.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a)
            || !byte.TryParse(trimmed.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            || !byte.TryParse(trimmed.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            || !byte.TryParse(trimmed.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        color = new ArgbColor(a, r, g, b);
        return true;
    }

    public static string Normalize(string value)
    {
        if (!TryParseArgb(value, out var color))
        {
            throw new FormatException($"Invalid ARGB color value '{value}'. Expected format '#AARRGGBB'.");
        }

        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

public readonly record struct ArgbColor(byte A, byte R, byte G, byte B);

using AgenticColorCreator.Core.Services;

namespace AgenticColorCreator.Tests;

public sealed class ColorSpaceConverterTests
{
	[Fact]
	public void RgbToHsv_ReturnsExpectedHueForPrimaryColors()
	{
		Assert.Equal(0d, ColorSpaceConverter.RgbToHsv(255, 0, 0).Hue, 6);
		Assert.Equal(120d, ColorSpaceConverter.RgbToHsv(0, 255, 0).Hue, 6);
		Assert.Equal(240d, ColorSpaceConverter.RgbToHsv(0, 0, 255).Hue, 6);
	}

	[Fact]
	public void RgbToHsv_ReturnsZeroSaturationForGray()
	{
		var hsv = ColorSpaceConverter.RgbToHsv(128, 128, 128);

		Assert.Equal(0d, hsv.Saturation, 6);
		Assert.Equal(128d / 255d, hsv.Value, 6);
	}

	[Fact]
	public void HsvToRgb_ReturnsExpectedRgbForKnownValues()
	{
		Assert.Equal(new RgbColor(255, 0, 0), ColorSpaceConverter.HsvToRgb(0d, 1d, 1d));
		Assert.Equal(new RgbColor(0, 255, 0), ColorSpaceConverter.HsvToRgb(120d, 1d, 1d));
		Assert.Equal(new RgbColor(0, 0, 255), ColorSpaceConverter.HsvToRgb(240d, 1d, 1d));
	}

	[Fact]
	public void HsvToRgb_And_RgbToHsv_RoundTripRepresentativeColor()
	{
		var hsv = ColorSpaceConverter.RgbToHsv(102, 64, 191);
		var rgb = ColorSpaceConverter.HsvToRgb(hsv.Hue, hsv.Saturation, hsv.Value);

		Assert.Equal(new RgbColor(102, 64, 191), rgb);
	}
}

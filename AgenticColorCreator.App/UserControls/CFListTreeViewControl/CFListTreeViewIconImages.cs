using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFListTreeViewControl;

/// <summary>
/// Holds shared frozen vector images for one icon glyph in each row interaction state.
/// </summary>
internal sealed class CFListTreeViewIconImages
{
	private static readonly Typeface IconTypeface = new(
		new FontFamily(
			new Uri("pack://application:,,,/AgenticColorCreator.App;component/", UriKind.Absolute),
			"./Shared/Fonts/#fs-editor-icons"),
		FontStyles.Normal,
		FontWeights.Normal,
		FontStretches.Normal);

	public CFListTreeViewIconImages(string glyph, Brush defaultBrush, Brush mouseOverBrush, Brush selectedBrush)
	{
		Default = CreateImage(glyph, defaultBrush);
		MouseOver = CreateImage(glyph, mouseOverBrush);
		Selected = CreateImage(glyph, selectedBrush);
	}

	public DrawingImage Default { get; }

	public DrawingImage MouseOver { get; }

	public DrawingImage Selected { get; }

	private static DrawingImage CreateImage(string glyph, Brush brush)
	{
		var frozenBrush = brush.IsFrozen ? brush : brush.CloneCurrentValue();
		if (frozenBrush.CanFreeze && !frozenBrush.IsFrozen)
		{
			frozenBrush.Freeze();
		}

		var formattedText = new FormattedText(
			glyph,
			CultureInfo.InvariantCulture,
			FlowDirection.LeftToRight,
			IconTypeface,
			14,
			frozenBrush,
			1);
		var geometry = formattedText.BuildGeometry(new Point());
		if (geometry.CanFreeze)
		{
			geometry.Freeze();
		}

		var drawing = new GeometryDrawing(frozenBrush, null, geometry);
		if (drawing.CanFreeze)
		{
			drawing.Freeze();
		}

		var image = new DrawingImage(drawing);
		if (image.CanFreeze)
		{
			image.Freeze();
		}
		return image;
	}
}

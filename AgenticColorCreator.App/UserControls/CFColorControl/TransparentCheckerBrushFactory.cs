using System.Windows;
using System.Windows.Media;

namespace AgenticColorCreator.App.UserControls.CFColorControl;

internal static class TransparentCheckerBrushFactory
{
	public static Brush Create()
	{
		var lightBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210));
		lightBrush.Freeze();

		var darkBrush = new SolidColorBrush(Color.FromRgb(140, 140, 140));
		darkBrush.Freeze();

		var lightPen = new Pen(lightBrush, 1);
		lightPen.Freeze();

		var darkPen = new Pen(darkBrush, 1);
		darkPen.Freeze();

		var checkerDrawing = new DrawingGroup();
		checkerDrawing.Children.Add(new GeometryDrawing(lightBrush, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
		checkerDrawing.Children.Add(new GeometryDrawing(darkBrush, darkPen, new RectangleGeometry(new Rect(0, 0, 8, 8))));
		checkerDrawing.Children.Add(new GeometryDrawing(darkBrush, darkPen, new RectangleGeometry(new Rect(8, 8, 8, 8))));
		checkerDrawing.Freeze();

		var brush = new DrawingBrush(checkerDrawing)
		{
			TileMode = TileMode.Tile,
			Viewport = new Rect(0, 0, 16, 16),
			ViewportUnits = BrushMappingMode.Absolute,
			Viewbox = new Rect(0, 0, 16, 16),
			ViewboxUnits = BrushMappingMode.Absolute,
			Stretch = Stretch.None,
		};
		brush.Freeze();
		return brush;
	}
}

using System;
using WpfColor = System.Windows.Media.Color;

namespace AgenticColorCreator.App.UserControls.CFColorControl
{
	internal class ColorOperations
	{
		public static WpfColor WpfColorToHdr(WpfColor color, double stops)
		{
			double p = Math.Pow(2, (double)stops);
			byte R_HDR = (byte)Math.Max(0, Math.Min((float)color.R * (float)p, 255));
			byte G_HDR = (byte)Math.Max(0, Math.Min((float)color.G * (float)p, 255));
			byte B_HDR = (byte)Math.Max(0, Math.Min((float)color.B * (float)p, 255));
			var c = WpfColor.FromArgb(color.A, R_HDR, G_HDR, B_HDR);
			return c;
		}

		public static WpfColor HdrToWpfColor(WpfColor hdr_color, double stops)
		{
			double p = Math.Pow(2, (double)stops);
			byte R_SDR = (byte)Math.Max(0, Math.Min((float)hdr_color.R / (float)p, 255));
			byte G_SDR = (byte)Math.Max(0, Math.Min((float)hdr_color.G / (float)p, 255));
			byte B_SDR = (byte)Math.Max(0, Math.Min((float)hdr_color.B / (float)p, 255));
			var c = WpfColor.FromArgb(hdr_color.A, R_SDR, G_SDR, B_SDR);
			return c;
		}
	}
}

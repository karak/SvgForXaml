﻿namespace Mntone.SvgForXaml
{
	public enum SvgPaintType : ushort
	{
		Unknown = 0,
		RgbColor,
		RgbColorIccColor,
		None = 101,
		CurrentColor,
		UriNone,
		UriCurrentColor,
		UriRgbColor,
		UriRgbColorIccColor,
		Uri,
	};
}
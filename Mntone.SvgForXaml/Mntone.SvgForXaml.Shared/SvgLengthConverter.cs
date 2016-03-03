using Mntone.SvgForXaml.Primitives;
using System;

namespace Mntone.SvgForXaml
{
	public sealed class SvgLengthConverter
	{
		public SvgPoint CanvasSize { get; internal set; }

		public float ConvertX(SvgLength length)
		{
			if (length.UnitType == SvgLength.SvgLengthType.Percentage)
			{
				return (float)this.CanvasSize.X * length.Value / 100.0F;
			}
			return this.Convert(length);
		}

		public float ConvertY(SvgLength length)
		{
			if (length.UnitType == SvgLength.SvgLengthType.Percentage)
			{
				return (float)this.CanvasSize.Y * length.Value / 100.0F;
			}
			return this.Convert(length);
		}

		public float Convert(SvgLength length)
		{
			switch (length.UnitType)
			{
				case SvgLength.SvgLengthType.Percentage:
					return (float)(length.Value * (CanvasSize.X * CanvasSize.X + CanvasSize.Y * CanvasSize.Y) * invertedSqrt2 / 100.0F);
				case SvgLength.SvgLengthType.Ems:
				case SvgLength.SvgLengthType.Exs:
					throw new NotSupportedException();
				default:
					return length.ValueAsPixel;
			}
		}

		private static readonly double invertedSqrt2 = Math.Sqrt(0.5); 
	}
}
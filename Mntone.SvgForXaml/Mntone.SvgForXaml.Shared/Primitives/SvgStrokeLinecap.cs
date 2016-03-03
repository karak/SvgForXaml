using Mntone.SvgForXaml.Interfaces;
using Mntone.SvgForXaml.Internal;

namespace Mntone.SvgForXaml.Primitives
{
	[System.Diagnostics.DebuggerDisplay("StrokeLinecap: {this.Value}")]
	public struct SvgStrokeLinecap : ICssValue
	{
		public SvgStrokeLinecap(string strokeLinecap)
		{
			switch (strokeLinecap)
			{
				case "butt":
					this.Value = SvgStrokeLinecapType.Butt;
					break;
				case "round":
					this.Value = SvgStrokeLinecapType.Round;
					break;
				case "square":
					this.Value = SvgStrokeLinecapType.Square;
					break;
				default:
					throw new System.ArgumentOutOfRangeException(nameof(strokeLinecap));
			}
		}

		public SvgStrokeLinecapType Value { get; }
	}
}

using Mntone.SvgForXaml.Interfaces;
using Mntone.SvgForXaml.Internal;

namespace Mntone.SvgForXaml.Primitives
{
	[System.Diagnostics.DebuggerDisplay("StrokeLinejoin: {this.Value}")]
	public struct SvgStrokeLinejoin : ICssValue
	{
		public SvgStrokeLinejoin(string strokeLinejoin)
		{
			switch (strokeLinejoin)
			{
				case "miter":
					Value = SvgStrokeLinejoinType.Miter;
					break;
				case "round":
					Value = SvgStrokeLinejoinType.Round;
					break;
				case "bebel":
					Value = SvgStrokeLinejoinType.Bebel;
					break;
				default:
					throw new System.ArgumentOutOfRangeException(nameof(strokeLinejoin));
			}
		}

		public SvgStrokeLinejoinType Value { get; }

		public static SvgStrokeLinejoinType Initial { get { return SvgStrokeLinejoinType.Miter; } }
	}
}

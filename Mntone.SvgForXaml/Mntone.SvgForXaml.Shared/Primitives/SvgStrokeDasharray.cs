using Mntone.SvgForXaml.Interfaces;
using Mntone.SvgForXaml.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mntone.SvgForXaml.Primitives
{
	public sealed class SvgStrokeDasharray : ICssValue, IReadOnlyCollection<SvgLength>
	{
		private readonly ICollection<SvgLength> _lengthes;

		internal SvgStrokeDasharray(ICollection<SvgLength> lengthes)
		{
			this._lengthes = lengthes ?? new SvgLength[0];
		}

		internal SvgStrokeDasharray DeepCopy()
		{
			return new SvgStrokeDasharray(this._lengthes.ToList());
		}

		internal static SvgStrokeDasharray Parse(string value)
		{
			return new SvgStrokeDasharray(StrokeDasharrayParser.Parse(value));
		}

		public int Count => this._lengthes.Count;
		public IEnumerator<SvgLength> GetEnumerator() => this._lengthes.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._lengthes).GetEnumerator();
	}
}

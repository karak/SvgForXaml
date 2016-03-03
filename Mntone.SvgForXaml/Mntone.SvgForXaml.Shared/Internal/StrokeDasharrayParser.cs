using Mntone.SvgForXaml.Primitives;
using Mntone.SvgForXaml.Internal;
using System.Collections.ObjectModel;

namespace Mntone.SvgForXaml.Internal
{
	internal sealed class StrokeDasharrayParser
	{
		public static Collection<SvgLength> Parse(string data) => new Collection<SvgLength>(new StrokeDasharrayParser(data).DashArray);

		public Collection<SvgLength> DashArray { get; }

		private StrokeDasharrayParser(string data)
		{
			if (data == "none")
			{
				this.DashArray = null;
			}
			else
			{
				this.DashArray = new Collection<SvgLength>();

				var ptr = new StringPtr(data);
				ptr.AdvanceWhiteSpace();
				if (!ptr.IsEnd)
				{
					this.ParseArray(ptr);
				}
			}
		}

		private void ParseArray(StringPtr ptr)
		{
			var length = ParseLength(ptr);
			if (length.HasValue)
			{
				this.DashArray.Add(length.Value);

				if (!ptr.IsEnd && this.ParseCommaOrWhitespace(ptr))
				{
					ParseArray(ptr);
				}
			}
		}

		private SvgLength? ParseLength(StringPtr ptr)
		{
			var begin = ptr.Index;
			ptr.AdvanceNumber();
			if (begin == ptr.Index) return null;

			var lengthText = ptr.Target.Substring(begin, ptr.Index - begin);
			SvgLength result;
			if (SvgLength.TryParse(lengthText, true, out result))
			{
				return result;
			}
			else
			{
				return null;
			}
		}

		private bool ParseCommaOrWhitespace(StringPtr ptr)
		{
			if (ptr.IsWhitespace())
			{
				++ptr;
				ptr.AdvanceWhiteSpace();
				if (ptr.Char == ',') ++ptr;
				ptr.AdvanceWhiteSpace();
				return true;
			}
			else if (ptr.Char == ',')
			{
				++ptr;
				ptr.AdvanceWhiteSpace();
				return true;
			}
			return false;
		}
	}
}

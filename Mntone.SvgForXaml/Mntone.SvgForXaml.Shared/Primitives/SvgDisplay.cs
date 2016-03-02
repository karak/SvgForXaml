using Mntone.SvgForXaml.Interfaces;
using Mntone.SvgForXaml.Internal;

namespace Mntone.SvgForXaml.Primitives
{
	[System.Diagnostics.DebuggerDisplay("SvgDisplay: {this.Value}")]
	public struct SvgDisplay : ICssValue
	{
		public SvgDisplay(string display)
		{
			switch (display)
			{
				case "inline": this.Value = SvgDisplayType.Inline; break;
				case "block": this.Value = SvgDisplayType.Block; break;
				case "list-item": this.Value = SvgDisplayType.ListItem; break;
				case "run-in": this.Value = SvgDisplayType.RunIn; break;
				case "compact": this.Value = SvgDisplayType.Compact; break;
				case "marker": this.Value = SvgDisplayType.Marker; break;
				case "table": this.Value = SvgDisplayType.Table; break;
				case "inline-table": this.Value = SvgDisplayType.InlineTable; break;
				case "table-row-group": this.Value = SvgDisplayType.TableRowGroup; break;
				case "table-header-group": this.Value = SvgDisplayType.TableHeaderGroup; break;
				case "table-footer-group": this.Value = SvgDisplayType.TableFooterGroup; break;
				case "table-row": this.Value = SvgDisplayType.TableRow; break;
				case "table-column-group": this.Value = SvgDisplayType.TableColumnGroup; break;
				case "table-column": this.Value = SvgDisplayType.TableColumn; break;
				case "table-cell": this.Value = SvgDisplayType.TableCell; break;
				case "table-caption": this.Value = SvgDisplayType.TableCaption; break;
				case "none": this.Value = SvgDisplayType.None; break;
				case "inherit": this.Value = SvgDisplayType.Inherit; break;
				default: throw new System.ArgumentOutOfRangeException(nameof(display));
			}
		}

		public SvgDisplayType Value { get; }

		public static readonly SvgDisplay Initial = new SvgDisplay("inline");
	}
}

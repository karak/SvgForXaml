﻿using Mntone.SvgForXaml.Internal;
using System.Xml;

namespace Mntone.SvgForXaml
{
	public sealed class SvgGroupElement : SvgElement, ISvgStylable
	{
		internal SvgGroupElement(INode parent, XmlElement element)
			: base(parent, element)
		{
			this._stylableHelper = new SvgStylableHelper(element);
		}

		public override string TagName => "g";

		#region ISvgStylable
		[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
		private readonly SvgStylableHelper _stylableHelper;
		public string ClassName => this._stylableHelper.ClassName;
		public CssStyleDeclaration Style => this._stylableHelper.Style;
		public ICssValue GetPresentationAttribute(string name) => this._stylableHelper.GetPresentationAttribute(name);
		#endregion
	}
}
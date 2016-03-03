﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Mntone.SvgForXaml.Gradients;
using Mntone.SvgForXaml.Maskings;
using Mntone.SvgForXaml.Path;
using Mntone.SvgForXaml.Primitives;
using Mntone.SvgForXaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI;

#if WINDOWS_UWP
using System.Numerics;
#else
using Microsoft.Graphics.Canvas.Numerics;
#endif

namespace Mntone.SvgForXaml
{
	public class Win2dRenderer : SvgRendererBase<CanvasDrawingSession>, IDisposable
	{
		protected ICanvasResourceCreator ResourceCreator { get; }
		protected Collection<IDisposable> DisposableObjects { get; }
		protected Dictionary<SvgGradientElement, ICanvasBrush> ResourceCache { get; }
		protected Dictionary<SvgPathElement, CanvasGeometry> PathCache { get; }

		private bool _disposed = false;

		public Win2dRenderer(ICanvasResourceCreator resourceCreator, SvgDocument targetDocument)
			: base(targetDocument)
		{
			this.ResourceCreator = resourceCreator;
			this.DisposableObjects = new Collection<IDisposable>();
			this.ResourceCache = new Dictionary<SvgGradientElement, ICanvasBrush>();
			this.PathCache = new Dictionary<SvgPathElement, CanvasGeometry>();
		}

		public void Dispose() => this.Dispose(true);
		protected void Dispose(bool disposing)
		{
			if (this._disposed) return;
			if (disposing)
			{
				foreach (var disposableObject in this.DisposableObjects)
				{
					disposableObject.Dispose();
				}
			}
			this._disposed = true;
		}

		protected override void RenderSvg(CanvasDrawingSession session, SvgSvgElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			this.RenderChildren(session, element.ChildNodes);
		}

		protected override void RenderGroup(CanvasDrawingSession session, SvgGroupElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			using (var t = TransformSession.CreateTransformSession(session, element.Transform.Result))
			{
				this.RenderChildren(session, element.ChildNodes);
			}
		}

		protected override void RenderUse(CanvasDrawingSession session, SvgUseElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			using (var t = TransformSession.CreateTransformSession(session, element.Transform.Result))
			{
				this.RenderChild(session, element.InstanceRoot);
			}
		}

		private void RenderGeometory(CanvasDrawingSession session, CanvasGeometry geometry, SvgMatrix transform, CssStyleDeclaration style)
		{
			bool change = false;
			var geometry2 = geometry;
			CanvasSolidColorBrush opacityBrush = null;
			try
			{
				using (var t = TransformSession.CreateTransformSession(session, transform))
				{
					var clipPath = style.ClipPath;
					if (clipPath != null)
					{
						if (clipPath.Uri[0] != '#') throw new ArgumentException();
						var clipPathElement = (SvgClipPathElement)this.TargetDocument.GetElementById(clipPath.Uri.Substring(1));
						var clipGeometory = this.CreateClipPath(session, clipPathElement);
						geometry2 = geometry.CombineWith(
							clipGeometory,
							new Matrix3x2 { M11 = 1.0F, M12 = 0.0F, M21 = 0.0F, M22 = 1.0F, M31 = 0.0F, M32 = 0.0F },
							CanvasGeometryCombine.Intersect,
							CanvasGeometry.ComputeFlatteningTolerance(session.Dpi, 1.0F, session.Transform));
						change = true;
					}

					var area = geometry2.ComputeBounds();

					var opacity = style.Opacity;
					if (opacity != null)
					{
						opacityBrush = this.CreateOpacity(session, opacity.Value);
					}

					var fill = style.Fill;
					if (fill == null || fill != null && fill.PaintType != SvgPaintType.None)
					{
						var pen = this.CreatePaint(session, area, fill, style.FillOpacity);
						if (opacityBrush == null)
						{
							session.FillGeometry(geometry2, pen);
						}
						else
						{
							session.FillGeometry(geometry2, pen, opacityBrush);
						}
					}

					var stroke = style.Stroke;
					var strokeWidth = style.StrokeWidth;
					if (stroke != null && stroke.PaintType != SvgPaintType.None)
					{
						var canvasStrokeWidth = strokeWidth.HasValue ? this.LengthConverter.Convert(strokeWidth.Value) : 1.0F;
						var pen = this.CreatePaint(session, area, stroke, style.StrokeOpacity);
						using (var strokeStyle = CreateStrokeStyle(style.StrokeDashoffset, style.StrokeLinecap, style.StrokeLinejoin, style.StrokeMiterlimit, style.StrokeDasharray, canvasStrokeWidth))
						{
							session.DrawGeometry(geometry2, pen, canvasStrokeWidth, strokeStyle);
						}
					}
				}
			}
			finally
			{
				if (change) geometry2.Dispose();
				if (opacityBrush != null) opacityBrush.Dispose();
			}
		}

		protected override void RenderPath(CanvasDrawingSession session, SvgPathElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			var geometry = this.CreatePath(session, element); // NO Dispose!
			this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
		}

		protected override void RenderRect(CanvasDrawingSession session, SvgRectElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			var x = this.LengthConverter.ConvertX(element.X);
			var y = this.LengthConverter.ConvertY(element.Y);
			var width = this.LengthConverter.ConvertX(element.Width);
			var height = this.LengthConverter.ConvertY(element.Height);
			var rx = this.LengthConverter.ConvertX(element.RoundedX);
			var ry = this.LengthConverter.ConvertX(element.RoundedY);
			using (var geometry = CanvasGeometry.CreateRoundedRectangle(this.ResourceCreator, x, y, width, height, rx, ry))
			{
				this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
			}
		}

		protected override void RenderCircle(CanvasDrawingSession session, SvgCircleElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			var centerX = this.LengthConverter.ConvertX(element.CenterX);
			var centerY = this.LengthConverter.ConvertY(element.CenterY);
			var radiusX = this.LengthConverter.ConvertX(element.Radius);
			var radiusY = this.LengthConverter.ConvertY(element.Radius);
			using (var geometry = CanvasGeometry.CreateEllipse(this.ResourceCreator, centerX, centerY, radiusX, radiusY))
			{
				this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
			}
		}

		protected override void RenderEllipse(CanvasDrawingSession session, SvgEllipseElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			var centerX = this.LengthConverter.ConvertX(element.CenterX);
			var centerY = this.LengthConverter.ConvertY(element.CenterY);
			var radiusX = this.LengthConverter.ConvertX(element.RadiusX);
			var radiusY = this.LengthConverter.ConvertY(element.RadiusY);
			using (var geometry = CanvasGeometry.CreateEllipse(this.ResourceCreator, centerX, centerY, radiusX, radiusY))
			{
				this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
			}
		}

		protected override void RenderLine(CanvasDrawingSession session, SvgLineElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			var x1 = this.LengthConverter.ConvertX(element.X1);
			var y1 = this.LengthConverter.ConvertY(element.Y1);
			var x2 = this.LengthConverter.ConvertX(element.X2);
			var y2 = this.LengthConverter.ConvertY(element.Y2);
			using (var t = TransformSession.CreateTransformSession(session, element.Transform.Result))
			{
				var area = new Rect(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(y2 - x1), Math.Abs(y2 - y1));
				var stroke = element.Style.Stroke;
				if (stroke != null && stroke.PaintType != SvgPaintType.None)
				{
					var pen = this.CreatePaint(session, area, stroke, element.Style.StrokeOpacity);
					var width = element.Style.StrokeWidth;
					session.DrawLine(x1, y1, x2, y2, pen, width.HasValue ? this.LengthConverter.Convert(width.Value) : 1.0F);
				}
			}
		}

		protected override void RenderPolyline(CanvasDrawingSession session, SvgPolylineElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			using (var builder = new CanvasPathBuilder(this.ResourceCreator))
			{
				var begin = element.Points.First();
				builder.BeginFigure(begin.X, begin.Y);
				foreach (var point in element.Points.Skip(1))
				{
					builder.AddLine(point.X, point.Y);
				}
				builder.EndFigure(CanvasFigureLoop.Open);

				using (var geometry = CanvasGeometry.CreatePath(builder))
				{
					this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
				}
			}
		}

		protected override void RenderPolygon(CanvasDrawingSession session, SvgPolygonElement element)
		{
			if (element.Style.Display.Value == SvgDisplayType.None) return;

			using (var geometry = CanvasGeometry.CreatePolygon(this.ResourceCreator, element.Points.Select(p => new Vector2 { X = p.X, Y = p.Y }).ToArray()))
			{
				this.RenderGeometory(session, geometry, element.Transform.Result, element.Style);
			}
		}

		private CanvasGeometry CreatePath(CanvasDrawingSession session, SvgPathElement element)
		{
			if (this.PathCache.ContainsKey(element)) return this.PathCache[element];

			var open = false;
			var v = new Vector2 { X = 0.0F, Y = 0.0F };
			var prevC2 = new Vector2();

			CanvasGeometry geometry;
			using (var builder = new CanvasPathBuilder(this.ResourceCreator))
			{
				var fillRule = element.Style.FillRule;
				if (fillRule.HasValue && fillRule.Value.Value != SvgFillRuleType.EvenOdd)
				{
					builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
				}
				foreach (var segment in element.Segments)
				{
					if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.ClosePath)
					{
						builder.EndFigure(CanvasFigureLoop.Closed);
						open = false;
						continue;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.MoveToAbsolute)
					{
						if (open) builder.EndFigure(CanvasFigureLoop.Open);

						var casted = (SvgPathSegmentMoveToAbsolute)segment;
						v.X = casted.X;
						v.Y = casted.Y;
						builder.BeginFigure(v);
						open = true;
						continue;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.MoveToRelative)
					{
						if (open) builder.EndFigure(CanvasFigureLoop.Open);

						var casted = (SvgPathSegmentMoveToRelative)segment;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.BeginFigure(v);
						open = true;
						continue;
					}

					if (!open)
					{
						builder.BeginFigure(v);
						open = true;
					}
					if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToAbsolute)
					{
						var casted = (SvgPathSegmentLineToAbsolute)segment;
						v.X = casted.X;
						v.Y = casted.Y;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToRelative)
					{
						var casted = (SvgPathSegmentLineToRelative)segment;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToCubicAbsolute)
					{
						var casted = (SvgPathSegmentCurveToCubicAbsolute)segment;
						v.X = casted.X;
						v.Y = casted.Y;
						builder.AddCubicBezier(new Vector2 { X = casted.X1, Y = casted.Y1 }, new Vector2 { X = casted.X2, Y = casted.Y2 }, v);
						prevC2.X = casted.X2;
						prevC2.Y = casted.Y2;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToCubicRelative)
					{
						var casted = (SvgPathSegmentCurveToCubicRelative)segment;
						var c1 = v;
						c1.X += casted.X1;
						c1.Y += casted.Y1;
						var c2 = v;
						c2.X += casted.X2;
						c2.Y += casted.Y2;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.AddCubicBezier(c1, c2, v);
						prevC2.X = c2.X;
						prevC2.Y = c2.Y;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToQuadraticAbsolute)
					{
						var casted = (SvgPathSegmentCurveToQuadraticAbsolute)segment;
						v.X = casted.X;
						v.Y = casted.Y;
						builder.AddQuadraticBezier(new Vector2 { X = casted.X1, Y = casted.Y1 }, v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToQuadraticRelative)
					{
						var casted = (SvgPathSegmentCurveToQuadraticRelative)segment;
						var c1 = v;
						c1.X += casted.X1;
						c1.Y += casted.Y1;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.AddQuadraticBezier(c1, v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.ArcAbsolute)
					{
						var casted = (SvgPathSegmentArcAbsolute)segment;
						v.X = casted.X;
						v.Y = casted.Y;
						var size = casted.LargeArcFlag ? CanvasArcSize.Large : CanvasArcSize.Small;
						var sweepDirection = casted.SweepFlag ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise;
						builder.AddArc(v, casted.RadiusX, casted.RadiusY, 180.0F * casted.Angle / (float)Math.PI, sweepDirection, size);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.ArcRelative)
					{
						var casted = (SvgPathSegmentArcRelative)segment;
						v.X += casted.X;
						v.Y += casted.Y;
						var size = casted.LargeArcFlag ? CanvasArcSize.Large : CanvasArcSize.Small;
						var sweepDirection = casted.SweepFlag ? CanvasSweepDirection.Clockwise : CanvasSweepDirection.CounterClockwise;
						builder.AddArc(v, casted.RadiusX, casted.RadiusY, 180.0F * casted.Angle / (float)Math.PI, sweepDirection, size);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToHorizontalAbsolute)
					{
						var casted = (SvgPathSegmentLineToHorizontalAbsolute)segment;
						v.X = casted.X;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToHorizontalRelative)
					{
						var casted = (SvgPathSegmentLineToHorizontalRelative)segment;
						v.X += casted.X;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToVerticalAbsolute)
					{
						var casted = (SvgPathSegmentLineToVerticalAbsolute)segment;
						v.Y = casted.Y;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.LineToVerticalRelative)
					{
						var casted = (SvgPathSegmentLineToVerticalRelative)segment;
						v.Y += casted.Y;
						builder.AddLine(v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToCubicSmoothAbsolute)
					{
						var casted = (SvgPathSegmentCurveToCubicSmoothAbsolute)segment;
						var c1 = Opposite(prevC2, v);
						v.X = casted.X;
						v.Y = casted.Y;
						builder.AddCubicBezier(c1, new Vector2 { X = casted.X2, Y = casted.Y2 }, v);
						prevC2.X = casted.X2;
						prevC2.Y = casted.Y2;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToCubicSmoothRelative)
					{
						var casted = (SvgPathSegmentCurveToCubicSmoothRelative)segment;
						var c1 = Opposite(prevC2, v);
						var c2 = v;
						c2.X += casted.X2;
						c2.Y += casted.Y2;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.AddCubicBezier(c1, c2, v);
						prevC2.X = c2.X;
						prevC2.Y = c2.Y;
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToQuadraticSmoothAbsolute)
					{
						var casted = (SvgPathSegmentCurveToQuadraticSmoothAbsolute)segment;
						var c1 = v;
						v.X = casted.X;
						v.Y = casted.Y;
						builder.AddQuadraticBezier(c1, v);
					}
					else if (segment.PathSegmentType == SvgPathSegment.SvgPathSegmentType.CurveToQuadraticSmoothRelative)
					{
						var casted = (SvgPathSegmentCurveToQuadraticSmoothRelative)segment;
						var c1 = v;
						v.X += casted.X;
						v.Y += casted.Y;
						builder.AddQuadraticBezier(c1, v);
					}
				}
				if (open)
				{
					builder.EndFigure(CanvasFigureLoop.Open);
				}

				geometry = CanvasGeometry.CreatePath(builder);
			}
			this.DisposableObjects.Add(geometry);
			this.PathCache.Add(element, geometry);
			return geometry;
		}

		private static Vector2 Opposite(Vector2 p, Vector2 basePoint)
		{
			return new Vector2 { X = 2 * basePoint.X - p.X, Y = 2 * basePoint.Y - p.Y };
		}

		private CanvasGeometry CreateClipPath(CanvasDrawingSession session, SvgClipPathElement element)
		{
			var child = element.FirstChild;
			if (child.GetType() == typeof(SvgUseElement))
			{
				child = ((SvgUseElement)child).InstanceRoot;
			}

			if (child.GetType() == typeof(SvgPathElement))
			{
				return this.CreatePath(session, (SvgPathElement)child);
			}
			else if (child is SvgRectElement)
			{
				var casted = ((SvgRectElement)child);
				return CanvasGeometry.CreateRoundedRectangle(
					session,
					this.LengthConverter.ConvertX(casted.X), this.LengthConverter.ConvertY(casted.Y),
					this.LengthConverter.ConvertX(casted.Width), this.LengthConverter.ConvertY(casted.Height),
					this.LengthConverter.ConvertX(casted.RoundedX), this.LengthConverter.ConvertY(casted.RoundedY));
			}
			else
			{
				throw new ArgumentException();
			}
		}

		private CanvasSolidColorBrush CreateColor(CanvasDrawingSession session, SvgColor color)
		{
			var brush = new CanvasSolidColorBrush(this.ResourceCreator,
				color != null
					? Color.FromArgb(0xff, color.RgbColor.Red, color.RgbColor.Green, color.RgbColor.Blue)
					: Color.FromArgb(0xff, 0, 0, 0));
			this.DisposableObjects.Add(brush);
			return brush;
		}

		private CanvasSolidColorBrush CreateOpacity(CanvasDrawingSession session, SvgNumber opacity)
		{
			return new CanvasSolidColorBrush(this.ResourceCreator, Color.FromArgb((byte)(255.0F * opacity.Value), 0, 0, 0));
		}

		private ICanvasBrush CreatePaint(CanvasDrawingSession session, Rect area, SvgPaint paint, SvgNumber? opacity)
		{
			if (paint == null || paint.PaintType == SvgPaintType.RgbColor)
			{
				var alpha = (byte)(255.0F * (opacity?.Value ?? 1.0F));
				var brush = new CanvasSolidColorBrush(
					this.ResourceCreator,
					paint != null
						? Color.FromArgb(alpha, paint.RgbColor.Red, paint.RgbColor.Green, paint.RgbColor.Blue)
						: Color.FromArgb(alpha, 0, 0, 0));
				this.DisposableObjects.Add(brush);
				return brush;
			}
			if (paint.PaintType == SvgPaintType.Uri && paint.Uri[0] == '#')
			{
				var key = paint.Uri.Substring(1);
				var grad = this.TargetDocument.GetElementById(key);
				if (grad.GetType() == typeof(SvgLinearGradientElement))
				{
					return this.CreateLinearGradient(session, area, (SvgLinearGradientElement)grad);
				}
				if (grad.GetType() == typeof(SvgRadialGradientElement))
				{
					return this.CreateRadialGradient(session, area, (SvgRadialGradientElement)grad);
				}
			}
			throw new NotImplementedException();
		}

		private CanvasLinearGradientBrush CreateLinearGradient(CanvasDrawingSession session, Rect area, SvgLinearGradientElement element)
		{
			if (this.ResourceCache.ContainsKey(element)) return (CanvasLinearGradientBrush)this.ResourceCache[element];

			var stops = element.ChildNodes.Cast<SvgStopElement>().Select(s =>
			{
				var alpha = s.Style.StopOpacity.HasValue ? (byte)(255.0F * s.Style.StopOpacity.Value) : (byte)0xff;
				var color = s.Style.StopColor != null ? Color.FromArgb(alpha, s.Style.StopColor.RgbColor.Red, s.Style.StopColor.RgbColor.Green, s.Style.StopColor.RgbColor.Blue) : Color.FromArgb(alpha, 0, 0, 0);
				var stop = new CanvasGradientStop()
				{
					Position = s.Offset,
					Color = color
				};
				return stop;
			}).ToArray();

			var m = element.GradientTransform.Result;
			if (element.GradientUnits == SvgUnitType.ObjectBoundingBox)
			{
				m = new SvgMatrix(area.Width, 0.0, 0.0, area.Height, area.X, area.Y) * m;
			}
			var transform = new Matrix3x2 { M11 = (float)m.A, M12 = (float)m.B, M21 = (float)m.C, M22 = (float)m.D, M31 = (float)m.E, M32 = (float)m.F };

			var x1 = this.LengthConverter.ConvertX(element.X1);
			var y1 = this.LengthConverter.ConvertY(element.Y1);
			var x2 = this.LengthConverter.ConvertX(element.X2);
			var y2 = this.LengthConverter.ConvertY(element.Y2);
			var spreadMethod = GetSpreadMethod(element.SpreadMethod);
			var brush = new CanvasLinearGradientBrush(this.ResourceCreator, stops, spreadMethod, CanvasAlphaMode.Straight)
			{
				StartPoint = new Vector2 { X = x1, Y = y1 },
				EndPoint = new Vector2 { X = x2, Y = y2 },
				Transform = transform,
			};
			this.DisposableObjects.Add(brush);
			this.ResourceCache.Add(element, brush);
			return brush;
		}

		private CanvasRadialGradientBrush CreateRadialGradient(CanvasDrawingSession session, Rect area, SvgRadialGradientElement element)
		{
			if (this.ResourceCache.ContainsKey(element)) return (CanvasRadialGradientBrush)this.ResourceCache[element];

			var stops = element.ChildNodes.Cast<SvgStopElement>().Select(s =>
			{
				var alpha = s.Style.StopOpacity.HasValue ? (byte)(255.0F * s.Style.StopOpacity.Value) : (byte)0xff;
				var color = s.Style.StopColor != null ? Color.FromArgb(alpha, s.Style.StopColor.RgbColor.Red, s.Style.StopColor.RgbColor.Green, s.Style.StopColor.RgbColor.Blue) : Color.FromArgb(alpha, 0, 0, 0);
				var stop = new CanvasGradientStop()
				{
					Position = s.Offset,
					Color = color
				};
				return stop;
			}).ToArray();

			var m = element.GradientTransform.Result;
			if (element.GradientUnits == SvgUnitType.ObjectBoundingBox)
			{
				m = new SvgMatrix(area.Width, 0.0, 0.0, area.Height, area.X, area.Y) * m;
			}
			var transform = new Matrix3x2 { M11 = (float)m.A, M12 = (float)m.B, M21 = (float)m.C, M22 = (float)m.D, M31 = (float)m.E, M32 = (float)m.F };

			var centerX = this.LengthConverter.ConvertX(element.CenterX);
			var centerY = this.LengthConverter.ConvertY(element.CenterY);
			var focusX = this.LengthConverter.ConvertX(element.FocusX);
			var focusY = this.LengthConverter.ConvertY(element.FocusY);
			var radiusX = this.LengthConverter.ConvertX(element.Radius);
			var radiusY = this.LengthConverter.ConvertY(element.Radius);
			var spreadMethod = GetSpreadMethod(element.SpreadMethod);
			var brush = new CanvasRadialGradientBrush(this.ResourceCreator, stops, spreadMethod, CanvasAlphaMode.Straight)
			{
				OriginOffset = new Vector2 { X = focusX - centerX, Y = focusY - centerY },
				Center = new Vector2 { X = centerX, Y = centerY },
				RadiusX = radiusX,
				RadiusY = radiusY,
				Transform = transform,
			};
			this.DisposableObjects.Add(brush);
			this.ResourceCache.Add(element, brush);
			return brush;
		}

		private CanvasStrokeStyle CreateStrokeStyle(SvgLength? strokeDashoffset, SvgStrokeLinecap? strokeLinecap, SvgStrokeLinejoin? strokeLinejoin, SvgNumber? strokeMiterLimit, SvgStrokeDasharray strokeDashArray, float strokeWidth)
		{
			return new CanvasStrokeStyle
			{
				StartCap = strokeLinecap.HasValue ? GetCanvasCapStyle(strokeLinecap.Value.Value) : CanvasCapStyle.Flat,
				EndCap = strokeLinecap.HasValue ? GetCanvasCapStyle(strokeLinecap.Value.Value) : CanvasCapStyle.Flat,
				LineJoin = GetCanvasLineJoin(strokeLinejoin.HasValue ? strokeLinejoin.Value.Value : SvgStrokeLinejoin.Initial),
				MiterLimit = strokeMiterLimit?.Value ?? 4,

				DashStyle = CanvasDashStyle.Solid,
				DashOffset = GetDashOffset(strokeDashoffset, strokeWidth),
				CustomDashStyle = GetStrokeDashArray(strokeDashArray, strokeWidth),
				DashCap = CanvasCapStyle.Flat,
				//UNDONE: DashOffset
			};
		}

		private float GetDashOffset(SvgLength? strokeDashoffset, float strokeWidth)
		{
			return (strokeDashoffset.HasValue ? this.LengthConverter.Convert(strokeDashoffset.Value) : 0F) / strokeWidth;
			//SPEC NOTE: Initial value should be 1 with no limit identifier, but is actually 0 in IE11.
		}

		private float[] GetStrokeDashArray(SvgStrokeDasharray strokeDashArray, float strokeWidth)
		{
			if (strokeDashArray == null || strokeDashArray.Count == 0) return null;

			IEnumerable<SvgLength> source;
			if (strokeDashArray.Count % 2 == 0)
			{
				//even
				source = strokeDashArray;
			}
			else
			{
				//odd => repeat
				source = strokeDashArray.Concat(strokeDashArray);
			}
			
			return source.Select(x => LengthConverter.Convert(x) / strokeWidth).ToArray();
			//SPEC NOTE: Actual length is the product of each element of CustomDashStyle and stroke-width.
		}

		private static CanvasEdgeBehavior GetSpreadMethod(SvgSpreadMethodType spreadMethod)
		{
			switch (spreadMethod)
			{
				case SvgSpreadMethodType.Reflect: return CanvasEdgeBehavior.Mirror;
				case SvgSpreadMethodType.Repeat: return CanvasEdgeBehavior.Wrap;
			}
			return CanvasEdgeBehavior.Clamp;
		}

		private static CanvasCapStyle GetCanvasCapStyle(SvgStrokeLinecapType strokeLinecap)
		{
			switch (strokeLinecap)
			{
				case SvgStrokeLinecapType.Butt:
					return CanvasCapStyle.Flat;
				case SvgStrokeLinecapType.Round:
					return CanvasCapStyle.Round;
				case SvgStrokeLinecapType.Square:
					return CanvasCapStyle.Square;
				default:
					return CanvasCapStyle.Flat;
			}
		}

		private static CanvasLineJoin GetCanvasLineJoin(SvgStrokeLinejoinType strokeLinejoin)
		{
			switch (strokeLinejoin)
			{
				case SvgStrokeLinejoinType.Miter:
					return CanvasLineJoin.MiterOrBevel;
				case SvgStrokeLinejoinType.Round:
					return CanvasLineJoin.Round;
				case SvgStrokeLinejoinType.Bebel:
					return CanvasLineJoin.Bevel;
				default:
					return CanvasLineJoin.MiterOrBevel;
			}
		}
	}
}
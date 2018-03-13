using System;
using System.Collections.Generic;
using System.IO;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Drawing;

namespace WSIMap
{
	public static class KMLFile
	{
		/// <summary>
		/// Reads and parses a KML file, converting KML objects into a Layer of WSIMap features. It uses
		/// KML styling for Curve and Polygon features.  The parsing is handled by the SharpKml library.
		/// Any exceptions thrown are eaten silently, but the exception out parameter will contain
		/// the thrown Exception object. The class only handles a subset of the KML elements (Placemark,
		/// Point, LineString, Polygon, Style, LineStyle, PolyStyle).
		/// </summary>
		/// <param name="fileName">Input KML file</param>
		/// <param name="exception">Contains any exceptions thrown or null if there are no exceptions</param>
		/// <returns>WSIMap.Layer object containing the extracted features</returns>
		public static Layer LoadKMLFile(string fileName, out Exception exception)
		{
			Layer layer = new Layer();
			exception = null;
			Dictionary<string, PolygonStyle> polyStyles = new Dictionary<string, PolygonStyle>();
			Dictionary<string, LineStyle> lineStyles = new Dictionary<string, LineStyle>();
			Color defaultPointColor = Color.Black;
			uint defaultPointSize = 5;
			Color defaultLineColor = Color.Black;
			Color defaultFillColor = Color.White;
			uint defaultLineWidth = 1;
			uint defaultOpacity = 100;

			//System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

			// Load the KML file
			TextReader reader = null;
			KmlFile kmlFile = null;
			try
			{
				reader = new StreamReader(fileName);
				kmlFile = KmlFile.Load(reader);
			}
			catch (Exception e)
			{
				exception = e;
				return layer;
			}

			// Read the KML file and convert KML objects to WSIMap features
			if (kmlFile != null && kmlFile.Root != null)
			{
				#region Convert KML objects to WSIMap features
				foreach (var element in kmlFile.Root.Flatten())
				{
					try
					{
						if (element is SharpKml.Dom.Point)
						{
							// Extract point
							SharpKml.Dom.Point obj = element as SharpKml.Dom.Point;
							PointD point = new PointD(obj.Coordinate.Longitude, obj.Coordinate.Latitude);
							point.ToolTip = true;

							// Extract Placemark elements
							var placemark = GetContainingPlacemark(obj);
							if (placemark != null)
							{
								// Extract name and description
								point.FeatureName = placemark.Name == null ? string.Empty : placemark.Name;
								point.FeatureInfo = placemark.Description == null ? string.Empty : placemark.Description.Text;
							}

							layer.Features.Add(point);
						}
						else if (element is SharpKml.Dom.LineString)
						{
							// Extract points
							SharpKml.Dom.LineString obj = element as SharpKml.Dom.LineString;
							CoordinateCollection coords = obj.Coordinates;
							Curve curve = new Curve();
							foreach (Vector pt in coords)
								curve.Add(new PointD(pt.Longitude, pt.Latitude));
							curve.ToolTip = true;

							// Extract Placemark elements
							var placemark = GetContainingPlacemark(obj);
							if (placemark != null)
							{
								// Extract name, description and style URL
								curve.FeatureName = placemark.Name == null ? string.Empty : placemark.Name;
								curve.FeatureInfo = placemark.Description == null ? string.Empty : placemark.Description.Text;
								curve.Tag = placemark.StyleUrl == null ? null : placemark.StyleUrl.ToString();

								// Extract any inline styles
								if (placemark.Styles != null)
								{
									List<ColorStyle> styles = new List<ColorStyle>();
									foreach (Style ss in placemark.Styles)
									{
										if (ss.Line != null)
											styles.Add(ss.Line.Clone());
										if (ss.Polygon != null)
											styles.Add(ss.Polygon.Clone());
									}
									if (styles.Count > 0)
										curve.Tag = styles;
								}
							}

							layer.Features.Add(curve);
						}
						else if (element is SharpKml.Dom.Polygon)
						{
							// Extract the points
							SharpKml.Dom.Polygon obj = element as SharpKml.Dom.Polygon;
							CoordinateCollection coords = obj.OuterBoundary.LinearRing.Coordinates;
							Polygon polygon = new Polygon();
							foreach (Vector pt in coords)
								polygon.Add(new PointD(pt.Longitude, pt.Latitude));
							polygon.ToolTip = true;

							// Extract Placemark elements
							var placemark = GetContainingPlacemark(obj);
							if (placemark != null)
							{
								// Extract name, description and style URL
								polygon.FeatureName = placemark.Name == null ? string.Empty : placemark.Name;
								polygon.FeatureInfo = placemark.Description == null ? string.Empty : placemark.Description.Text;
								polygon.Tag = placemark.StyleUrl == null ? null : placemark.StyleUrl.ToString();

								// Extract any inline styles
								if (placemark.Styles != null)
								{
									List<ColorStyle> styles = new List<ColorStyle>();
									foreach (Style ss in placemark.Styles)
									{
										if (ss.Line != null)
											styles.Add(ss.Line.Clone());
										if (ss.Polygon != null)
											styles.Add(ss.Polygon.Clone());
									}
									if (styles.Count > 0)
										polygon.Tag = styles;
								}
							}

							layer.Features.Add(polygon);
						}
						else if (element is SharpKml.Dom.Style)
						{
							// Extract shared styles
							SharpKml.Dom.Style obj = element as SharpKml.Dom.Style;
							if (obj.Polygon != null && !string.IsNullOrEmpty(obj.Id))
								polyStyles.Add(obj.Id, obj.Polygon.Clone());
							if (obj.Line != null && !string.IsNullOrEmpty(obj.Id))
								lineStyles.Add(obj.Id, obj.Line.Clone());
						}
					}
					catch (Exception e)
					{
						exception = e;
					}
				}
				#endregion

				#region Apply KML styles to WSIMap features
				// Apply styles to the features in the layer. Inline styles take precedence over shared styles.
				foreach (Feature feature in layer.Features)
				{
					try
					{
						// Default styles are applied to points because we do not extract IconStyle elements from the KML
						if (feature is PointD)
						{
							PointD f = feature as PointD;
							f.Color = defaultPointColor;
							f.Size = defaultPointSize;
						}

						if (feature.Tag == null) // no style information for the feature
							continue;

						// Apply styles to Curve or Polygon features
						if (feature is Curve)
						{
							Curve f = feature as Curve;

							// Provide default styles in case something doesn't get set below
							f.Color = defaultLineColor;
							f.Width = defaultLineWidth;

							if (f.Tag is List<ColorStyle>) // Tag holds inline style
							{
								foreach (ColorStyle s in (List<ColorStyle>)f.Tag)
								{
									if (s is LineStyle)
									{
										LineStyle ls = s as LineStyle;
										if (ls.Color.HasValue) f.Color = Color.FromArgb(ls.Color.Value.Argb);
										if (ls.Width.HasValue) f.Width = (uint)ls.Width.Value;
									}
								}
							}
							else // Tag holds style URL string for shared style
							{
								string key = ((string)f.Tag).Replace("#", string.Empty);
								LineStyle ls;
								bool lineStyleDefined = lineStyles.TryGetValue(key, out ls);

								if (lineStyleDefined)
								{
									if (ls.Color.HasValue) f.Color = Color.FromArgb(ls.Color.Value.Argb);
									if (ls.Width.HasValue) f.Width = (uint)ls.Width.Value;
								}
							}
						}
						else if (feature is Polygon)
						{
							Polygon f = feature as Polygon;

							// Provide default styles in case something doesn't get set below
							f.BorderColor = defaultLineColor;
							f.BorderWidth = defaultLineWidth;
							f.FillColor = defaultFillColor;
							f.Opacity = defaultOpacity;

							if (f.Tag is List<ColorStyle>) // Tag holds inline style
							{
								foreach (ColorStyle s in (List<ColorStyle>)f.Tag)
								{
									if (s is LineStyle)
									{
										LineStyle ls = s as LineStyle;
										if (ls.Color.HasValue) f.BorderColor = Color.FromArgb(ls.Color.Value.Argb);
										if (ls.Width.HasValue) f.BorderWidth = (uint)ls.Width.Value;
									}
									else if (s is PolygonStyle)
									{
										PolygonStyle ps = s as PolygonStyle;
										if (ps.Color.HasValue)
										{
											f.FillColor = Color.FromArgb(ps.Color.Value.Argb);
											f.Opacity = ConvertAlpha(ps.Color.Value.Alpha);
										}
									}
								}
							}
							else // Tag holds style URL string for shared style
							{
								string key = ((string)f.Tag).Replace("#", string.Empty);
								LineStyle ls;
								PolygonStyle ps;
								bool lineStyleDefined = lineStyles.TryGetValue(key, out ls);
								bool polygonStyleDefined = polyStyles.TryGetValue(key, out ps);

								if (lineStyleDefined)
								{
									if (ls.Color.HasValue) f.BorderColor = Color.FromArgb(ls.Color.Value.Argb);
									if (ls.Width.HasValue) f.BorderWidth = (uint)ls.Width.Value;
								}

								if (polygonStyleDefined && ps.Color.HasValue)
								{
									f.FillColor = Color.FromArgb(ps.Color.Value.Argb);
									f.Opacity = ConvertAlpha(ps.Color.Value.Alpha);
								}
							}
						}
					}
					catch (Exception e)
					{
						exception = e;
					}
				}
				#endregion
			}

			//Console.WriteLine("KML processing time: " + sw.Elapsed.ToString());

			return layer;
		}

		/// <summary>
		/// Recursive method that walks backwards through the KML until it
		/// finds the containing Placemark element for the Geometry element
		/// provided.
		/// </summary>
		/// <param name="g">SharpKml.Dom.Geometry object</param>
		/// <returns>SharpKml.Dom.Placemark that contains the Geometry object</returns>
		private static Placemark GetContainingPlacemark(Geometry g)
		{
			if (g.Parent is Geometry)
				return GetContainingPlacemark(g.Parent as Geometry);
			else if (g.Parent is Placemark)
				return g.Parent as Placemark;
			else
				return null;
		}

		/// <summary>
		/// Maps a byte alpha (opacity) value into a WSIMap opacity value.
		/// </summary>
		/// <param name="alpha">Alpha (opacity) value in the range [0..255]</param>
		/// <returns>WSIMap opacity value in the range [0..100]</returns>
		private static uint ConvertAlpha(byte alpha)
		{
			return (uint)((double)alpha / byte.MaxValue * 100);
		}
	}
}

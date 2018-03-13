using System;
using System.Drawing;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public class RadSumLabel : Label
	{
		private bool underline;
		private Color secondColor;
		private bool flipped;

		public RadSumLabel(Font font, string text, Color color, double latitude, double longitude, double xOffset, double yOffset, bool highlight, Color highlightColor, string featureName, string featureInfo)
			: base(font, text, color, latitude, longitude, xOffset, yOffset, highlight, highlightColor, featureName, featureInfo)
		{
			this.secondColor = Color.Empty;
			this.underline = false;
			this.flipped = false;
		}

		public bool UnderLine
		{
			get { return underline; }
			set { underline = value; }
		}

		public Color SecondColor
		{
			get { return secondColor; }
			set { secondColor = value; }
		}

		public bool Flipped
		{
			get { return flipped; }
			set { flipped = value; }
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RadSumLabel Draw()");
#endif

			// Was this Label filtered out by the declutter algorithm?
			if (!this.draw) return;

			// Don't draw the label if the font wasn't properly initialized
			if (!font.Initialized) return;

			if (font.PointSize != this.fontSize)
			{
				fontSize = font.PointSize;
				width = double.MinValue;
				height = double.MinValue;
			}

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Set the matrix mode
			Gl.glMatrixMode(Gl.GL_PROJECTION);

			// Calculate the rendering scale factors
			float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
			float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Calculate the position of the text (don't draw labels below the equator for azimuthal projections)
			double _x = x + (xOffset / xFactor);
			double _y = y + (yOffset / yFactor);
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && _y < Projection.MinAzimuthalLatitude) return;
			double px, py;
			Projection.ProjectPoint(mapProjection, _x, _y, centralLongitude, out px, out py);

			// Draw the text
			Gl.glPushAttrib(Gl.GL_LIST_BIT);
			Gl.glListBase(font.OpenGLDisplayListBase);
			string[] _text = text.Split('\n');
			lock (_text)
			{
				RectangleD highlightRect = GetBoundingRect(parentMap);

				// Draw a background rectangle
				if (!fastDraw && highlight)
				{
					if (highlightRect != null)
					{
						highlightRect.MoveLowerLeftTo(new PointD(px, py));
						if (highlight)
						{
							if (highlightBorderColor == Color.Empty)
								highlightBorderWidth = 0;
							if (highlightColor == Color.Empty)
								highlightOpacity = 0;
							highlightRect.BorderColor = highlightBorderColor;
							highlightRect.BorderWidth = highlightBorderWidth;
							highlightRect.FillColor = highlightColor;
							highlightRect.Opacity = highlightOpacity;
							highlightRect.StretchByPixels(4, parentMap);
							highlightRect.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
						}
						if (parentMap != null && parentLayer.Declutter)
						{
							PointD pt = FindIntersection(highlightRect);
							double p1x, p1y;
							Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out p1x, out p1y);
							if (leaderLineColor == Color.Empty)
								leaderLineColor = highlightColor;
							WSIMap.Line line = new Line(new PointD(p1x, p1y), pt, leaderLineColor, declutterLineWidth);
							line.InterpolationMethod = Curve.InterpolationMethodType.Linear;
							line.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
							line.Draw(parentMap, parentLayer);
							line.Dispose();
						}
						if (highlight)
							highlightRect.Draw(parentMap, parentLayer);
					}
				}

				if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
				{
					_x = parentMap.DenormalizeLongitude(_x);
					Projection.ProjectPoint(mapProjection, _x, _y, centralLongitude, out px, out py);
				}

				// Set the text color
				if (useFontColor)
					Gl.glColor3d(glc(font.Color.R), glc(font.Color.G), glc(font.Color.B));
				else
					Gl.glColor3d(glc(color.R), glc(color.G), glc(color.B));

				double underLinePy = py - highlightRect.Height;
				for (int i = _text.Length - 1; i >= 0; i--)
				{
					string textStr = _text[i];
					if (secondColor != Color.Empty)
					{
						int index = -1;
						string prevText = string.Empty;
						string nextText = string.Empty;
						if (flipped)
						{
							Gl.glColor3d(glc(secondColor.R), glc(secondColor.G), glc(secondColor.B));
							index = textStr.IndexOf(' ');

							prevText = textStr.Substring(0, index + 1);
							nextText = textStr.Substring(index + 1);

							double prevWidth = 0;
							foreach (char c in prevText)
								prevWidth += (font.abcf[c].abcfA + font.abcf[c].abcfB + font.abcf[c].abcfC);

							double nextWidth = 0;
							foreach (char c in nextText)
								nextWidth += (font.abcf[c].abcfA + font.abcf[c].abcfB + font.abcf[c].abcfC);

							if (fastDraw)
								Gl.glRasterPos3d(px - highlightRect.Width, py, 0.0);
							else
								SetRasterPos(px - highlightRect.Width, py);

							Gl.glCallLists(prevText.Length, Gl.GL_UNSIGNED_BYTE, prevText);

							Gl.glColor3d(glc(color.R), glc(color.G), glc(color.B));

							double nextPx = px - (highlightRect.Width * nextWidth) / (prevWidth + nextWidth);

							if (fastDraw)
								Gl.glRasterPos3d(nextPx, py, 0.0);
							else
								SetRasterPos(nextPx, py);

							Gl.glCallLists(nextText.Length, Gl.GL_UNSIGNED_BYTE, nextText);
						}
						else
						{
							index = textStr.LastIndexOf(' ');

							prevText = textStr.Substring(0, index + 1);
							nextText = textStr.Substring(index + 1);

							if (fastDraw)
								Gl.glRasterPos3d(px, py, 0.0);
							else
								SetRasterPos(px, py);

							Gl.glCallLists(prevText.Length, Gl.GL_UNSIGNED_BYTE, prevText);
							Gl.glColor3d(glc(secondColor.R), glc(secondColor.G), glc(secondColor.B));

							double prevWidth = 0;
							foreach (char c in prevText)
								prevWidth += (font.abcf[c].abcfA + font.abcf[c].abcfB + font.abcf[c].abcfC);

							double nextWidth = 0;
							foreach (char c in nextText)
								nextWidth += (font.abcf[c].abcfA + font.abcf[c].abcfB + font.abcf[c].abcfC);

							double nextPx = px + (highlightRect.Width * prevWidth) / (prevWidth + nextWidth);

							if (fastDraw)
								Gl.glRasterPos3d(nextPx, py, 0.0);
							else
								SetRasterPos(nextPx, py);

							Gl.glCallLists(nextText.Length, Gl.GL_UNSIGNED_BYTE, nextText);
						}

					}
					else
					{
						if (flipped)
						{
							if (fastDraw)
								Gl.glRasterPos3d(px - highlightRect.Width, py, 0.0);
							else
								SetRasterPos(px - highlightRect.Width, py);
						}
						else
						{
							if (fastDraw)
								Gl.glRasterPos3d(px, py, 0.0);
							else
								SetRasterPos(px, py);
						}
						Gl.glCallLists(_text[i].Length, Gl.GL_UNSIGNED_BYTE, _text[i]);
					}

					py += ((font.PointSize / f1) / yFactor) * f2; // f2 creates line spacing
				}

				// Radar summary under line
				if (underline)
				{
					double temppy = py - highlightRect.Height - yOffset / yFactor;
					Gl.glColor3f(1.0f, 1.0f, 1.0f);
					Gl.glLineWidth(2);
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Gl.glVertex2d(px, temppy);
					if (flipped)
						Gl.glVertex2d(px - highlightRect.Width, temppy);
					else
						Gl.glVertex2d(px + highlightRect.Width, temppy);
					Gl.glEnd();
					Gl.glLineWidth(1);
				}

				// Cleanup
				if (highlightRect != null)
					highlightRect.Dispose();
			}
			Gl.glPopAttrib();
		}
	}
}

using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class Ellipse
	 * \brief Represents an ellipse on the map
	 */
	public class Ellipse : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		protected PointD center;
		protected double xRadius;
		protected double yRadius;
		protected Color borderColor;
		protected Color fillColor;
		protected uint opacity;
		protected uint borderWidth;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		private const int nPoints = 360;
		private const string TRACKING_CONTEXT = "Ellipse";
		#endregion

        public static PointD[] GetVertices(PointD center, double xRadiusInMiles, double yRadiusInMiles)
        {
            PointD[] points = new PointD[nPoints];
            for (int i = 0; i < nPoints; i++)
            {
                double xRadiusInDeg = xRadiusInMiles / (Math.Cos(center.Y * deg2rad) * deg2mi);
                double yRadiusInDeg = yRadiusInMiles / deg2mi;
                double x = (Math.Cos(i * deg2rad) * xRadiusInDeg) + center.X;
                double y = (Math.Sin(i * deg2rad) * yRadiusInDeg) + center.Y;
                points[i] = new PointD(x, y);
            }
            return points;
        }

        public Ellipse(PointD center, double xRadiusInMiles, double yRadiusInMiles, Color borderColor, uint borderWidth, Color fillColor, uint opacity)
		{
			this.center = center;
			this.XRadius = xRadiusInMiles;
			this.YRadius = yRadiusInMiles;
			this.borderColor = borderColor;
			this.borderWidth = borderWidth;
			this.fillColor = fillColor;
			this.Opacity = opacity;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public PointD Center
		{
			get { return center; }
            set { center = value; Updated = true; }
		}

		public double XRadius
		{
			get { return xRadius; }
			set
			{
				if (value < 0) value = 0;
				xRadius = value;
                Updated = true;
			}
		}
			
		public double YRadius
		{
			get { return yRadius; }
			set
			{
				if (value < 0) value = 0;
				yRadius = value;
                Updated = true;
			}
		}

		public Color BorderColor
		{
			get { return borderColor; }
            set { borderColor = value; Updated = true; }
		}

		public uint BorderWidth
		{
			get { return borderWidth; }
            set { borderWidth = value; Updated = true; }
		}

		public Color FillColor
		{
			get { return fillColor; }
            set { fillColor = value; Updated = true; }
		}

		public uint Opacity
		{
			get { return opacity; }
			set
			{
				if (value < 0) value = 0;
				if (value > 100) value = 100;
				opacity = value;
                Updated = true;
			}
		}

        public PointD[] Vertices
        {
            get
            {
                PointD[] points = new PointD[nPoints];
                for (int i = 0; i < nPoints; i++)
                {
                    double xRadiusInDeg = xRadius / (Math.Cos(center.Y * deg2rad) * deg2mi);
                    double yRadiusInDeg = yRadius / deg2mi;
                    double x = (Math.Cos(i * deg2rad) * xRadiusInDeg) + center.X;
                    double y = (Math.Sin(i * deg2rad) * yRadiusInDeg) + center.Y;
                    points[i] = new PointD(x, y);
                }
                return points;
            }
        }

        public void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			SetMapProjection(mapProjection, centralLongitude);
			if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
				CreateDisplayList();
		}

		private void SetMapProjection(MapProjections mapProjection, short centralLongitude)
		{
			// Only set the map projection if it's changing. This prevents unnecessary regeneration of the display list.
			if (mapProjection != this.mapProjection || centralLongitude != this.centralLongitude)
			{
				this.mapProjection = mapProjection;
				this.centralLongitude = centralLongitude;
				Updated = true;
			}
		}

		private void CreateDisplayList()
        {
            if ((openglDisplayList == -1) || Updated)
            {
                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

                // Some OpenGL initialization
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_FLAT);

                // Render the ellipse
				MapProjectionTypes mpType = Projection.GetProjectionType(mapProjection);
                if (opacity == 0 && borderWidth == 0)
                {
                    Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;	// nothing to draw
                }
                else
                    numVertices += nPoints;
                if (opacity > 0 && fillColor != Color.Transparent)
                {
                    Gl.glColor4f(glc(fillColor.R), glc(fillColor.G), glc(fillColor.B), (float)opacity / 100);
                    Gl.glBegin(Gl.GL_POLYGON);
                    for (int i = 0; i < nPoints; i++)
                    {
                        double xRadiusInDeg = xRadius / (Math.Cos(center.Y * deg2rad) * deg2mi);
                        double yRadiusInDeg = yRadius / deg2mi;
                        double x = (Math.Cos(i * deg2rad) * xRadiusInDeg) + center.X;
                        double y = (Math.Sin(i * deg2rad) * yRadiusInDeg) + center.Y;
						if (mpType == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude)
							continue;
						double px, py;
						Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
                }
                Gl.glEnd();
                if (borderWidth > 0)
                {
                    Gl.glEnable(Gl.GL_LINE_SMOOTH);
                    Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
                    Gl.glLineWidth(borderWidth);
                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    for (int i = 0; i < nPoints; i++)
                    {
                        double xRadiusInDeg = xRadius / (Math.Cos(center.Y * deg2rad) * deg2mi);
                        double yRadiusInDeg = yRadius / deg2mi;
                        double x = (Math.Cos(i * deg2rad) * xRadiusInDeg) + center.X;
                        double y = (Math.Sin(i * deg2rad) * yRadiusInDeg) + center.Y;
						if (mpType == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude)
							continue;
						double px, py;
						Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
                    Gl.glEnd();
                    Gl.glDisable(Gl.GL_LINE_SMOOTH);
                }

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Ellipse Draw()");
#endif
			if (openglDisplayList == -1) return;

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);            
            else
                Gl.glCallList(openglDisplayList);
		}
	}
}

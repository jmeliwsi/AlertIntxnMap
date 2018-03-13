using System;
using System.Collections.Generic;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class Circle
	 * \brief Represents a circle on the map
	 */
    [Serializable] public class Circle : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		protected PointD center;
		protected double radius;
		protected Color borderColor;
		protected Color fillColor;
		protected uint opacity;
		protected uint borderWidth;
		protected Polygon.PolygonBorderType borderType;
		protected int stippleFactor;
		protected ushort stipplePattern;
		private int nPoints = 60;
		protected MapProjections mapProjection;
		protected short centralLongitude;

		private  double MinX = double.MaxValue;
		private double MaxX = double.MinValue;
		private double MinY = double.MaxValue;
		private double MaxY = double.MinValue;
		private const string TRACKING_CONTEXT = "Circle";
		#endregion

        private List<PointD> GetVertices()
        {
            double d = radius / FUL.Utils.EarthRadius_sm;
            if (center.X == -180.0) center.X = 180.0;   // fix for fill problem
            double cx = -1 * center.X * deg2rad;
            double cy = center.Y * deg2rad;
			List<PointD> vertices = new List<PointD>();
			int t = 360 / nPoints;
			for (int i = 0; i < nPoints; i++)
			{
				double y = Math.Asin(Math.Sin(cy) * Math.Cos(d) + Math.Cos(cy) * Math.Sin(d) * Math.Cos(t * i * deg2rad));
				double dlon = Math.Atan2(Math.Sin(t * i * deg2rad) * Math.Sin(d) * Math.Cos(cy), Math.Cos(d) - Math.Sin(cy) * Math.Sin(y));
				double x = ((cx - dlon + Math.PI) % (2 * Math.PI)) - Math.PI;
				x = -1 * x / deg2rad;
				y = y / deg2rad;
				vertices.Add(new PointD(x, y));

				if (x < MinX)
					MinX =x;
				if (x > MaxX)
					MaxX = x;
				if (y < MinY)
					MinY = y;
				if (y > MaxY)
					MaxY = y;
			}

			// Check for segments that cross the international date line
			int nCrossings = NumDatelineCrossings(vertices);

			// If the circle crosses the date line, adjust the longitudes
			if (nCrossings > 0)
			{
				for (int i = 0; i < nPoints; i++)
				{
					if (vertices[i].X > 0)
					{
						vertices[i].X -= 360;

						if (vertices[i].X < MinX)
							MinX = vertices[i].X;
						if (vertices[i].X > MaxX)
							MaxX = vertices[i].X;
					}
				}
			}

			return vertices;
        }

        public Circle() : this(new PointD(0,0), 0, Color.Empty, 0, Color.Empty, 0, true)
        {
        }

		public Circle(PointD center, double radiusInMiles, Color borderColor, uint borderWidth, Color fillColor, uint opacity)
			: this(center, radiusInMiles, borderColor, borderWidth, fillColor, opacity, true)
		{
		}

		public Circle(PointD center, double radiusInMiles, Color borderColor, uint borderWidth, Color fillColor, uint opacity, bool highResolution)
		{
			this.center = center;
			radius = radiusInMiles;
			this.borderColor = borderColor;
			this.borderWidth = borderWidth;
			this.borderType = Polygon.PolygonBorderType.Solid;
			this.stippleFactor = 1;
			this.stipplePattern = 0xFFFF;
			this.fillColor = fillColor;
			this.Opacity = opacity;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.mapProjection = MapProjections.CylindricalEquidistant;
			
			if (highResolution)
				nPoints = 360;
			else
				nPoints = 60;

			GetVertices();
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
			set
			{
				center = value;
				GetVertices();
			}
		}

		public double Radius
		{
			get { return radius; }
			set
			{
				if (value < 0) value = 0;
				radius = value;
				GetVertices();
				Updated = true;
			}
		}
			
		public Color BorderColor
		{
			get { return borderColor; }
            set
            {
                borderColor = value;
                Updated = true;
            }
		}

		public uint BorderWidth
		{
			get { return borderWidth; }
            set
            {
                borderWidth = value;
                Updated = true;
            }
		}

		public Polygon.PolygonBorderType BorderType
		{
			get { return borderType; }
			set { borderType = value; Updated = true; }
		}

		public int StippleFactor
		{
			get { return stippleFactor; }
			set { stippleFactor = value; Updated = true; }
		}

		public ushort StipplePattern
		{
			get { return stipplePattern; }
			set { stipplePattern = value; Updated = true; }
		}

		public Color FillColor
		{
			get { return fillColor; }
            set
            {
                fillColor = value;
                Updated = true;
            }
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
				// Generate vertices
				List<PointD> vertices = GetVertices();

                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

				// Is there anything to draw?
				if (opacity == 0 && borderWidth == 0)
                {
                    Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;	// nothing to draw
                }

                // Some OpenGL initialization
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_FLAT);

				// Generate the circle vertices
				numVertices += nPoints;

				// Render the circle fill
				MapProjectionTypes mpType = Projection.GetProjectionType(mapProjection);
				if (opacity > 0 && fillColor != Color.Transparent)
                {
                    Gl.glColor4f(glc(fillColor.R), glc(fillColor.G), glc(fillColor.B), (float)opacity / 100);
                    Gl.glBegin(Gl.GL_POLYGON);
					for (int i = 0; i < nPoints; i++)
					{
						if (mpType == MapProjectionTypes.Azimuthal && vertices[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						double px, py;
						Projection.ProjectPoint(mapProjection, vertices[i].X, vertices[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
                }

				// Render the circle border
                if (borderWidth > 0)
                {
					// Setup
                    Gl.glEnable(Gl.GL_LINE_SMOOTH);
                    Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
                    Gl.glLineWidth(borderWidth);
					if (borderType != Polygon.PolygonBorderType.Solid)
					{
						switch (borderType)
						{
							case Polygon.PolygonBorderType.LongDashed:
								stipplePattern = 0x00FF;
								break;
							case Polygon.PolygonBorderType.ShortDashed:
								stipplePattern = 0x0F0F;
								break;
							case Polygon.PolygonBorderType.Dotted:
								stipplePattern = 0xCCCC;
								break;
							case Polygon.PolygonBorderType.Custom:
								// stipple pattern set by user
								break;
							default:
								break;
						}
						Gl.glLineStipple(stippleFactor, stipplePattern);
						Gl.glEnable(Gl.GL_LINE_STIPPLE);
					}

					// Draw the border
                    Gl.glBegin(Gl.GL_LINE_LOOP);
					for (int i = 0; i < nPoints; i++)
					{
						if (mpType == MapProjectionTypes.Azimuthal && vertices[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						double px, py;
						Projection.ProjectPoint(mapProjection, vertices[i].X, vertices[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
                    Gl.glEnd();

					// Cleanup
                    Gl.glDisable(Gl.GL_LINE_SMOOTH);
					if (borderType != Polygon.PolygonBorderType.Solid)
						Gl.glDisable(Gl.GL_LINE_STIPPLE);
				}

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
		}

		protected int NumDatelineCrossings(List<PointD> vertices)
		{
			// Check for segments that cross the international dateline
			int nCrossings = 0;
			PointD temp1, temp2;
			for (int i = 1; i < nPoints; i++)
			{
				if (Curve.CrossesIDL(vertices[i - 1], vertices[i], out temp1, out temp2))
					nCrossings++;
			}
			return nCrossings;
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Circle Draw()");
#endif
			if (openglDisplayList == -1) return;

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);            
             else
                Gl.glCallList(openglDisplayList);
		}

		public void GetBoundingBox(ref double minLat, ref double maxLat, ref double minLon, ref double maxLon)
		{
			//double d = radius / FUL.Utils.EarthRadius_sm;
			//if (center.X == -180.0) center.X = 180.0;   // fix for fill problem
			//double cx = -1 * center.X * deg2rad;
			//double cy = center.Y * deg2rad;
			//List<PointD> vertices = new List<PointD>();
			//int t = 360 / nPoints;

			//for (int i = 0; i < 360; i += 90)
			//{
			//    double y = Math.Asin(Math.Sin(cy) * Math.Cos(d) + Math.Cos(cy) * Math.Sin(d) * Math.Cos(t * i * deg2rad));
			//    double dlon = Math.Atan2(Math.Sin(t * i * deg2rad) * Math.Sin(d) * Math.Cos(cy), Math.Cos(d) - Math.Sin(cy) * Math.Sin(y));
			//    double x = ((cx - dlon + Math.PI) % (2 * Math.PI)) - Math.PI;
			//    x = -1 * x / deg2rad;
			//    y = y / deg2rad;

			//    if (x < MinX)
			//        MinX = x;
			//    if (x > MaxX)
			//        MaxX = x;
			//    if (y < MinY)
			//        MinY = y;
			//    if (y > MaxY)
			//        MaxY = y;
			//}

			minLat = MinY;
			maxLat = MaxY;
			minLon = MinX;
			maxLon = MaxX;
		}
	}
}

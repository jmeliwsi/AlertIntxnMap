using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public class WindBarbSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected int height;
		protected Color color;
		protected int speed;
		protected bool highlight;
		protected Color highlightColor;
		protected double direction;
		private int numOfTriangles;
		private int numOfBars;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		
		#endregion

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

		public WindBarbSymbol(int windSpeed, double windDirection, double latitude, double longitude, int height )
		{
			this.speed = windSpeed;
			this.direction = windDirection;
			this.height = height;
			this.x = longitude;
			this.y = latitude;
			this.size = 5;
			this.color = Color.Black;
			this.highlight = false;
			this.highlightColor = Color.White;
			numOfTriangles = windSpeed / 50;
			int remaining = windSpeed % 50;
			numOfBars = remaining / 5;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public WindBarbSymbol(int windSpeed, double windDirection, double latitude, double longitude)
		{
			this.speed = windSpeed;
			this.direction = windDirection;
			this.x = longitude;
			this.y = latitude;
			this.height = 90;
			this.size = 5;
			this.color = Color.Black;
			this.highlight = false;
			this.highlightColor = Color.White;
			numOfTriangles = windSpeed / 50;
			int remaining = windSpeed % 50;
			numOfBars = remaining / 5;
			this.mapProjection = MapProjections.CylindricalEquidistant;		
		}

		public double X
		{
			get { return x; }
			set { x = value; }
		}

		public double Y
		{
			get { return y; }
			set { y = value; }
		}

		public double Longitude
		{
			get { return x; }
			set { x = value; }
		}

		public double Latitude
		{
			get { return y; }
			set { y = value; }
		}

		public uint Size
		{
			get { return size; }
			set { size = value; }
		}

		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		public bool Highlight
		{
			get { return highlight; }
			set { highlight = value; }
		}

		public Color HighlightColor
		{
			get { return highlightColor; }
			set { highlightColor = value; }
		}

		public double WindDirection
		{
			get { return direction; }
			set { direction = value; Updated = true; }
		}

		public int WindSpeed
		{
			get { return speed; }
			set { speed = value; Updated = true; }
		}

		public int Height
		{
			get { return height; }
			set { height = value; Updated = true; }
		}
		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}
		
		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WindBarbSymbol Draw()");
#endif

			double _x = x;
			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
				_x = parentMap.DenormalizeLongitude(x);

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Do not draw points below the equator for azimuthal projections
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude) return;

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Turn on anti-aliasing
			Gl.glEnable(Gl.GL_POINT_SMOOTH);
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

			// Preserve the projection matrix state
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();

			// Calculate the rendering scale factors
			double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
			double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Set the position and size of the symbol
			double px, py, pdir;
			Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
			Projection.ProjectDirection(mapProjection, _x, y, direction, centralLongitude, out pdir);
			Gl.glTranslated(px, py, 0.0);
			Gl.glRotated(360 - pdir, 0.0, 0.0, 1.0);	// OpenGL rotation is counterclockwise
			double symbolSize = (size + 1) * 0.05;
			Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);

			// Draw the symbol
			CreateWindBarbSymbol();

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_POINT_SMOOTH);
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}

		protected void CreateWindBarbSymbol()
		{
			int width = 30;
			//int height = 90;
			int height = this.height;
			int increment = 20;
			int startingHeight = height;

			if (highlight)
			{
				Gl.glColor3f(glc(highlightColor.R), glc(highlightColor.G), glc(highlightColor.B));
				Gl.glLineWidth(2.0f);

				// Line highlight
				Gl.glBegin(Gl.GL_LINES);
				Gl.glVertex2d(0, 0);
				Gl.glVertex2d(0, height);
				Gl.glEnd();

				// Triangle highlight
				for (int i = 0; i < numOfTriangles; i++)
				{
					Gl.glBegin(Gl.GL_LINES);
					Gl.glVertex2d(0, height);
					Gl.glVertex2d(width, height);
					Gl.glEnd();
					Gl.glBegin(Gl.GL_LINES);
					Gl.glVertex2d(width, height);
					height -= increment;
					Gl.glVertex2d(0, height);
					Gl.glEnd();
				}

				// Bar highlight
				increment /= 2;
				width /= 2;
				Gl.glBegin(Gl.GL_LINES);
				for (int i = 0; i < numOfBars; i++)
				{
					int x = 0;
					if (i % 2 != 0)
						x += width;

					Gl.glVertex2d(x, height);
					Gl.glVertex2d(x + width, height);
					if (i % 2 != 0)
						height -= increment;
				}
				Gl.glEnd();
			}

			Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
			Gl.glLineWidth(1.0f);
			height = startingHeight;

			// Line
			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2d(0, 0);
			Gl.glVertex2d(0, height);
			Gl.glEnd();

			// Triangle
			if (highlight)
			{
				increment *= 2;
				width *= 2;
			}
			Gl.glBegin(Gl.GL_TRIANGLES);
			for (int i = 0; i < numOfTriangles; i++)
			{
				Gl.glVertex2d(0, height);
				Gl.glVertex2d(width, height);
				height -= increment;
				Gl.glVertex2d(0, height);
			}
			Gl.glEnd();

			// Bar
			increment /= 2;
			width /= 2;
			Gl.glBegin(Gl.GL_LINES);
			for (int i = 0; i < numOfBars; i++)
			{
				int x = 0;
				if (i % 2 != 0)
					x += width;

				Gl.glVertex2d(x, height);
				Gl.glVertex2d(x + width, height);
				if (i % 2 != 0)
					height -= increment;
			}
			Gl.glEnd();
		}
	}
}

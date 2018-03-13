using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class VolcanoSymbol
	 * \brief Handles rendering of Volcano symbols
	 */
	public class VolcanoSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color innerFillColor;
		protected Color innerBorderColor;
		protected Color outerFillColor;
		protected Color outerBorderColor;
		protected static int[] openglDisplayLists;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		#endregion

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("VolcanoSymbol Draw()");
#endif

			double _x = x;
			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
				_x = parentMap.DenormalizeLongitude(x);

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Do not draw points below the equator for azimuthal projections
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude) return;

			// Preserve the projection matrix state
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();

			// Calculate the rendering scale factors
			double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
			double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Set the position and size of the symbol
			double px, py;
			Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
			Gl.glTranslated(px, py, 0.0);
			double symbolSize = (size + 1) * 0.03;
			Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);

			// Set the outer circle color
			double alpha = 1;
			if (outerFillColor == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(outerFillColor.R), glc(outerFillColor.G), glc(outerFillColor.B), alpha);

			// Draw the outer circle
			Gl.glCallList(openglDisplayLists[0]);

			// Set the outer circle border color
			alpha = 1;
			if (outerBorderColor == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(outerBorderColor.R), glc(outerBorderColor.G), glc(outerBorderColor.B), alpha);

			// Draw the outer circle border
			Gl.glCallList(openglDisplayLists[1]);

			// Set the mountain color
			alpha = 1;
			if (innerFillColor == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(innerFillColor.R), glc(innerFillColor.G), glc(innerFillColor.B), alpha);

			// Draw the mountain
			Gl.glCallList(openglDisplayLists[2]);

			// Set the mountain border color
			alpha = 1;
			if (innerBorderColor == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(innerBorderColor.R), glc(innerBorderColor.G), glc(innerBorderColor.B), alpha);

			// Draw the mountain border
			Gl.glCallList(openglDisplayLists[3]);

			// Draw the ash cloud (same color as mountain border)
			Gl.glCallList(openglDisplayLists[4]);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}

		static VolcanoSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("VolcanoSymbol VolcanoSymbol()");
#endif

			CreateVolcanoSymbol();
		}

		public VolcanoSymbol(double lat, double lon, uint size, Color innerFillColor, Color innerBorderColor, Color outerFillColor, Color outerBorderColor)
		{
			this.x = lon;
			this.y = lat;
			this.size = size;
			this.innerFillColor = innerFillColor;
			this.innerBorderColor = innerBorderColor;
			this.outerFillColor = outerFillColor;
			this.outerBorderColor = outerBorderColor;
			this.featureName = string.Empty;
			this.featureInfo = string.Empty;
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
			get { return outerFillColor; }
			set { outerFillColor = value; }
		}

		public Color InnerFillColor
		{
			get { return innerFillColor; }
			set { innerFillColor = value; }
		}

		public Color InnerBorderColor
		{
			get { return innerBorderColor; }
			set { innerBorderColor = value; }
		}

		public Color OuterFillColor
		{
			get { return outerFillColor; }
			set { outerFillColor = value; }
		}

		public Color OuterBorderColor
		{
			get { return outerBorderColor; }
			set { outerBorderColor = value; }
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

		protected static void CreateVolcanoSymbol()
		{
			const int size = 20;

			// Create OpenGL display lists
			openglDisplayLists = new int[5];
			openglDisplayLists[0] = Gl.glGenLists(5);
			openglDisplayLists[1] = openglDisplayLists[0] + 1;
			openglDisplayLists[2] = openglDisplayLists[0] + 2;
			openglDisplayLists[3] = openglDisplayLists[0] + 3;
			openglDisplayLists[4] = openglDisplayLists[0] + 4;

			#region Outer Circle
			// Start GL list #0 to contain the outer circle
			Gl.glNewList(openglDisplayLists[0], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the outer circle
			Gl.glBegin(Gl.GL_POLYGON);
			CreateCircle(size * 2 + 1);
			Gl.glEnd();
			Gl.glEndList();
			#endregion

			#region Outer Circle Border
			// Start GL list #1 to contain the outer circle border
			Gl.glNewList(openglDisplayLists[1], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the outer circle border
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			CreateCircle(size * 2 + 1);
			Gl.glEnd();
			Gl.glEndList();
			#endregion

			#region Mountain
			// Start GL list #2 to contain the mountain
			Gl.glNewList(openglDisplayLists[2], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the mountain
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(-10, size);
			Gl.glVertex2d(10, size);
			Gl.glVertex2d(30, -size);
			Gl.glVertex2d(-30, -size);
			Gl.glEnd();
			Gl.glEndList();
			#endregion

			#region Mountain Border
			// Start GL list #3 to contain the mountain border
			Gl.glNewList(openglDisplayLists[3], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the mountain border
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			Gl.glVertex2d(-10, size);
			Gl.glVertex2d(10, size);
			Gl.glVertex2d(30, -size);
			Gl.glVertex2d(-30, -size);
			Gl.glEnd();
			Gl.glEndList();
			#endregion

			#region Ash Cloud
			// Start GL list #4 to contain the ash cloud
			Gl.glNewList(openglDisplayLists[4], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the ash cloud (really more of an wimpy explosion than a cloud)
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(-15, 35);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(0, 35);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(15, 35);
			Gl.glEnd();
			Gl.glEndList();
			#endregion
		}

		private static void CreateCircle(int radius)
		{
			int nPoints = 360;
			uint centerY = 0;
			uint centerX = 0;

			double angleUnit = 360 / nPoints * Math.PI / 180;
			for (int i = 0; i < nPoints; i++)
			{
				double angle = angleUnit * i;
				double x = (Math.Cos(angle) * radius) + centerX;
				double y = (Math.Sin(angle) * radius) + centerY;

				Gl.glVertex2d(x, y);
			}
		}
	}
}

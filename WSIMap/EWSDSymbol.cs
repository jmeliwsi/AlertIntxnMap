using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class EWSDSymbol
	 * \brief Handles rendering of the EWSD symbol (hot storm index)
	 */
	[Serializable]
	public class EWSDSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color circleFillColor;
		protected Color triangleFillColor;
		protected Color circleBorderColor;
		protected Color triangleBorderColor;
		protected static int openglDisplayListTriangle;
		protected static int openglDisplayListCircle;
		protected static double[,] v;
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
			ConfirmMainThread("EWSDSymbol Draw()");
#endif

			if (openglDisplayList == -1)
				return;

			double _x = x;
			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
				_x = parentMap.DenormalizeLongitude(x);

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Do not draw points below the equator for azimuthal projections
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude)
				return;

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Turn on anti-aliasing
			Gl.glEnable(Gl.GL_LINE_SMOOTH);

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
			double symbolSize = (size + 1) * 0.05;
			Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);

			// Draw the filled circle portion of the symbol
			Gl.glColor4d(glc(circleFillColor.R), glc(circleFillColor.G), glc(circleFillColor.B), 1d);
			Gl.glCallList(openglDisplayListCircle);

			// Draw the filled triangle portion of the symbol
			Gl.glColor4d(glc(triangleFillColor.R), glc(triangleFillColor.G), glc(triangleFillColor.B), 1d);
			Gl.glCallList(openglDisplayListTriangle);

			// Draw the triangle portion of the symbol
			Gl.glColor4d(glc(triangleBorderColor.R), glc(triangleBorderColor.G), glc(triangleBorderColor.B), 1d);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			Gl.glVertex2d(12.990381, 7.500000);
			Gl.glVertex2d(0.000001, -15.000000);
			Gl.glVertex2d(-12.990381, 7.499999);
			Gl.glEnd();

			// Draw the circle border
			Gl.glColor4d(glc(circleBorderColor.R), glc(circleBorderColor.G), glc(circleBorderColor.B), 1d);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// Restore previous projection matrix
			Gl.glPopMatrix();

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
		}

		static EWSDSymbol()
		{
			CreateSymbol();
		}

		public EWSDSymbol(PointD point, uint size) : this(point.X, point.Y, size)
		{
		}

		public EWSDSymbol(double x, double y, uint size)
		{
			this.x = x;
			this.y = y;
			this.size = size;
			this.mapProjection = MapProjections.CylindricalEquidistant;
			this.openglDisplayList = openglDisplayListCircle;
		}

		public Color CircleFillColor
		{
			get { return circleFillColor; }
			set { circleFillColor = value; }
		}

		public Color TriangleFillColor
		{
			get { return triangleFillColor; }
			set { triangleFillColor = value; }
		}

		public Color CircleBorderColor
		{
			get { return circleBorderColor; }
			set { circleBorderColor = value; }
		}

		public Color TriangleBorderColor
		{
			get { return triangleBorderColor; }
			set { triangleBorderColor = value; }
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

		private static void CreateSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("EWSDSymbol CreateSymbol()");
#endif

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

            // Initialize the vertices
			v = new double[,] {
			    {15.000000, 0.000000, 0},
			    {14.488887, 3.882286,  0},
			    {12.990381, 7.500000,  0},
			    {10.606602, 10.606602, 0},
			    {7.500000,  12.990381, 0},
			    {3.882285,  14.488887, 0},
			    {-0.000000, 15.00000, 0},
			    {-3.882286, 14.488887, 0},
			    {-7.500000, 12.990381, 0},
			    {-10.606602, 10.606601,  0},
			    {-12.990381, 7.499999, 0},
			    {-14.488888, 3.882285, 0},
			    {-15.000000, -0.000001, 0},
			    {-14.488887, -3.882286, 0},
			    {-12.990381, -7.500001, 0},
			    {-10.606601, -10.606602, 0},
			    {-7.499999,  -12.990382, 0},
			    {-3.882285,  -14.488888, 0},
			    {0.000001,   -15.000000, 0},
			    {3.882287,   -14.488887, 0},
			    {7.500001,   -12.990380, 0},
			    {10.606603,  -10.606601, 0},
			    {12.990382,  -7.499999, 0},
			    {14.488888,  -3.882284, 0},
			};

			// Draw the circle portion of the symbol
			openglDisplayListCircle = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListCircle, Gl.GL_COMPILE);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < v.GetLength(0); i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();
			Gl.glEndList();

			// Draw the triangle portion of the symbol
			openglDisplayListTriangle = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTriangle, Gl.GL_COMPILE);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(12.990381, 7.500000);
			Gl.glVertex2d(0.000001, -15.000000);
			Gl.glVertex2d(-12.990381, 7.499999);
            Gl.glEnd();
			Gl.glEndList();
		}
	}
}

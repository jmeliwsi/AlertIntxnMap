using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public enum TurbulenceSymbolType {None, Mod, ModSvr, Svr };

	/**
	 * \class TurbulenceSymbol
	 * \brief Handles rendering of turbulence symbols for the high sig chart
	 */
	[Serializable]
	public class TurbulenceSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected static int openglDisplayListMod;
		protected static int openglDisplayListModSvr;
		protected static int openglDisplayListSvr;
		protected TurbulenceSymbolType type;
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
			ConfirmMainThread("TurbulenceSymbol Draw()");
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

			// Set the symbol color
			Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), 1d);

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

			// Draw the symbol
			Gl.glCallList(openglDisplayList);

			// Restore previous projection matrix
			Gl.glPopMatrix();

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
		}

		static TurbulenceSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("TurbulenceSymbol TurbulenceSymbol()");
#endif

			CreateModSymbol();
			CreateModSvrSymbol();
			CreateSvrSymbol();
		}

		public TurbulenceSymbol(TurbulenceSymbolType type, PointD point, uint size, Color color)
			: this(type, point.X, point.Y, size, color)
		{
		}

		public TurbulenceSymbol(TurbulenceSymbolType type, double x, double y, uint size, Color color)
		{
			this.type = type;
			this.x = x;
			this.y = y;
			this.size = size;
			this.color = color;
			SetDisplayList();
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

		private void SetDisplayList()
		{
			switch (type)
			{
				case TurbulenceSymbolType.Mod:
					openglDisplayList = openglDisplayListMod;
					break;
				case TurbulenceSymbolType.ModSvr:
					openglDisplayList = openglDisplayListModSvr;
					break;
				case TurbulenceSymbolType.Svr:
					openglDisplayList = openglDisplayListSvr;
					break;
				default:
					openglDisplayList = openglDisplayListSvr;
					break;
			}
		}

		private static void CreateModSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListMod = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListMod, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the symbol
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-37, -12);
			Gl.glVertex2d(-22, -12);
			Gl.glVertex2d(0, 12);
			Gl.glVertex2d(22, -12);
			Gl.glVertex2d(37, -12);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void CreateModSvrSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListModSvr = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListModSvr, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the symbol
			Gl.glLineWidth(1);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-74, -12);
			Gl.glVertex2d(-59, -12);
			Gl.glVertex2d(-37, 12);
			Gl.glVertex2d(-15, -12);
			Gl.glVertex2d(-2, -12);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(2, -12);
			Gl.glVertex2d(15, -12);
			Gl.glVertex2d(37, 12);
			Gl.glVertex2d(59, -12);
			Gl.glVertex2d(74, -12);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(15, 0);
			Gl.glVertex2d(37, 24);
			Gl.glVertex2d(59, 0);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void CreateSvrSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListSvr = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListSvr, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Draw the symbol
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-37, -12);
			Gl.glVertex2d(-22, -12);
			Gl.glVertex2d(0, 12);
			Gl.glVertex2d(22, -12);
			Gl.glVertex2d(37, -12);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-22, 0);
			Gl.glVertex2d(0, 24);
			Gl.glVertex2d(22, 0);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}
	}
}

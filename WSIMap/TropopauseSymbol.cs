using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public enum TropopauseSymbolType { High, Low, None };

	/**
	 * \class TropopauseSymbol
	 * \brief Handles rendering of tropopause symbols for the high sig chart
	 */
	[Serializable]
	public class TropopauseSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected string text;
		protected static int openglDisplayListHigh;
		protected static int openglDisplayListLow;
		protected static int openglDisplayListNone;
		protected TropopauseSymbolType type;
		protected static Font font;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		#endregion

		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("TropopauseSymbol Draw()");
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

			// Draw the text
			Gl.glPushAttrib(Gl.GL_LIST_BIT);
			Gl.glListBase(font.OpenGLDisplayListBase);
			double textX = px - (12 / parentMap.ScaleX), textY;
			if (type == TropopauseSymbolType.High)
				textY = py - (14 / parentMap.ScaleY);
			else if (type == TropopauseSymbolType.Low)
				textY = py + (5 / parentMap.ScaleY);
			else
				textY = py - (4 / parentMap.ScaleY);
			Gl.glRasterPos3d(textX, textY, 0.0);
			Gl.glCallLists(text.Length, Gl.GL_UNSIGNED_BYTE, text);
			Gl.glPopAttrib();

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
		}

		static TropopauseSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("TropopauseSymbol TropopauseSymbol()");
#endif

			font = new Font("Courier", 10, false);
			CreateHighSymbol();
			CreateLowSymbol();
			CreateNoneSymbol();
		}

		public TropopauseSymbol(TropopauseSymbolType type, PointD point, Color color, string text) : this(type, point.X, point.Y, color, text)
		{
		}

		public TropopauseSymbol(TropopauseSymbolType type, double x, double y, Color color, string text)
		{
			this.type = type;
			this.x = x;
			this.y = y;
			this.text = text;
			this.size = 20;
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

		public string Text
		{
			get { return text; }
			set { text = value; }
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public TropopauseSymbolType Type
		{
			get { return type; }
			set
			{
				type = value;
				SetDisplayList();
			}
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
				case TropopauseSymbolType.High:
					openglDisplayList = openglDisplayListHigh;
					break;
				case TropopauseSymbolType.Low:
					openglDisplayList = openglDisplayListLow;
					break;
				case TropopauseSymbolType.None:
					openglDisplayList = openglDisplayListNone;
					break;
				default:
					openglDisplayList = openglDisplayListNone;
					break;
			}
		}

		private static void CreateHighSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListHigh = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListHigh, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = 
			{
				{0, 17.5, 0},
				{15, 2.5, 0},
				{15, -17.5, 0},
				{-15, -17.5, 0},
				{-15, 2.5, 0}
			};

			// Draw the house shaped box
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// Draw the H
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-2.5, 7.5);
			Gl.glVertex2d(-2.5, -2.5);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(2.5, 7.5);
			Gl.glVertex2d(2.5, -2.5);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-2.5, 2.5);
			Gl.glVertex2d(2.5, 2.5);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void CreateLowSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListLow = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListLow, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = 
			{
				{0, -17.5, 0},
				{15, -2.5, 0},
				{15, 17.5, 0},
				{-15, 17.5, 0},
				{-15, -2.5, 0}
			};

			// Draw the inverted house shaped box
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// Draw the L
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2d(-2.5, 2.5);
			Gl.glVertex2d(-2.5, -7.5);
			Gl.glVertex2d(2.5, -7.5);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void CreateNoneSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListNone = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListNone, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = 
			{
				{15, 8, 0},
				{15, -8, 0},
				{-15, -8, 0},
				{-15, 8, 0}
			};

			// Draw the inverted house shaped box
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}
	}
}

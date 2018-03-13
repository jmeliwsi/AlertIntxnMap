using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class Symbol
	 * \brief Handles rendering of plane and other map symbols
	 */
	public class NavaidSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected static int openglDisplayListVOR;
		protected static int openglDisplayListVORDME;
		protected static int openglDisplayListTACAN;
		protected static int openglDisplayListNDB;
		protected static int openglDisplayListVORTAC;
		protected static int openglDisplayListDME;
		protected double direction;
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
			ConfirmMainThread("NavaidSymbol Draw()");
#endif

			if (openglDisplayList == -1) return;

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
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

			// Set the symbol color
			double alpha = 1;
			if (color == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), alpha);

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
			Gl.glScaled(symbolSize/xFactor, symbolSize/yFactor, 1.0);

			// Draw the symbol
            Gl.glLineWidth(1);
            Gl.glCallList(openglDisplayList);

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}

		static NavaidSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("NavaidSymbol NavaidSymbol()");
#endif

			CreateNavaidVORSymbol();
			CreateNavaidVORDMESymbol();
			CreateNavaidTACANSymbol();
			CreateNavaidNDBSymbol();
			CreateNavaidVORTACSymbol();
			CreateNavaidDMESymbol();
		}

		public NavaidSymbol(FUL.NAVaidType type, Color color, uint size) : this(type,color,size,0,0)
		{
		}

		public NavaidSymbol(FUL.NAVaidType type, Color color, uint size, PointD point) : this(type,color,size,point.Latitude,point.Longitude)
		{
		}

		public NavaidSymbol(FUL.NAVaidType type, Color color, uint size, double latitude, double longitude)
		{
			this.color = color;
			this.size = size;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.mapProjection = MapProjections.CylindricalEquidistant;

			if (type == FUL.NAVaidType.VOR)
				openglDisplayList = openglDisplayListVOR;
			else if (type == FUL.NAVaidType.VOR_DME)
				openglDisplayList = openglDisplayListVORDME;
			else if (type == FUL.NAVaidType.TACAN)
				openglDisplayList = openglDisplayListTACAN;
			else if (type == FUL.NAVaidType.NDB)
				openglDisplayList = openglDisplayListNDB;
			else if (type == FUL.NAVaidType.VORTAC)
				openglDisplayList = openglDisplayListVORTAC;
			else
				openglDisplayList = openglDisplayListDME;

			SetPosition(latitude, longitude);
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

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public double Direction
		{
			get { return direction; }
			set { direction = value; }
		}

		public void SetPosition(double latitude, double longitude)
		{
			x = longitude;
			y = latitude;
		}

		public void SetPosition(PointD point)
		{
			x = point.X;
			y = point.Y;
		}

		public void SetPosition(double latitude, double longitude, double dir)
		{
			x = longitude;
			y = latitude;
			direction = dir;
		}

		public void SetPosition(PointD point, double dir)
		{
			x = point.X;
			y = point.Y;
			direction = dir;
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		protected static void CreateNavaidVORSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListVOR = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListVOR, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			// Draw the Navaid VOR symbol
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			
			double size = 30;
			double length = Math.Sqrt(3.0)*size/2;

			// Initialize vertices
			double[,] v = new double[6,3];
			v[0,0] = size; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = size/2; v[1,1] = -length; v[1,2] = 0;
			v[2,0] = -size/2; v[2,1] = -length; v[2,2] = 0;
			v[3,0] = -size; v[3,1] = 0; v[3,2] = 0;
			v[4,0] = -size/2; v[4,1] = length; v[4,2] = 0;
			v[5,0] = size/2; v[5,1] = length; v[5,2] = 0;

			for (int i=0; i<6; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);

			Gl.glEnd();
		
			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateNavaidVORDMESymbol()
		{
			// Create an OpenGL display list
			openglDisplayListVORDME = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListVORDME, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			Gl.glLineWidth(1);

			double size = 25;
			double length = Math.Sqrt(3.0)*size/2;

			// Initialize vertices
			double[,] v = new double[10,3];
			v[0,0] = size; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = size/2; v[1,1] = -length; v[1,2] = 0;
			v[2,0] = -size/2; v[2,1] = -length; v[2,2] = 0;
			v[3,0] = -size; v[3,1] = 0; v[3,2] = 0;
			v[4,0] = -size/2; v[4,1] = length; v[4,2] = 0;
			v[5,0] = size/2; v[5,1] = length; v[5,2] = 0;

			size = 30;
			v[6,0] = size; v[6,1] = size; v[6,2] = 0;
			v[7,0] = size; v[7,1] = -size; v[7,2] = 0;
			v[8,0] = -size; v[8,1] = -size; v[8,2] = 0;
			v[9,0] = -size; v[9,1] = size; v[9,2] = 0;

			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<6; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=6; i<10; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
		
			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateNavaidDMESymbol()
		{
			// Create an OpenGL display list
			openglDisplayListDME = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListDME, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			Gl.glLineWidth(1);

			double size = 30;

			Gl.glBegin(Gl.GL_LINE_LOOP);
				Gl.glVertex2d(size, size);
				Gl.glVertex2d(size, -size);
				Gl.glVertex2d(-size, -size);
				Gl.glVertex2d(-size, size);
			Gl.glEnd();
		
			// End the GL list
			Gl.glEndList();
		}
		
		protected static void CreateNavaidTACANSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListTACAN = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTACAN, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			// Draw the Navaid TACAN symbol
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP); //GL_POLYGON);
			
			double size = 30;
			double xSize = 2*size/3;
			double ySize = Math.Sqrt(3.0)*xSize/3;
			
			double value1 = size/2;
			double value2 = Math.Sqrt(3.0)*size/4;

			// Initialize vertices
			double[,] v = new double[8,3];
			v[0,0] = value1; v[0,1] = size; v[0,2] = 0;
			v[1,0] = size; v[1,1] = value2; v[1,2] = 0;
			v[2,0] = size/3; v[2,1] = -size/3; v[2,2] = 0;
			v[3,0] = size/3; v[3,1] = -2*size/3; v[3,2] = 0;

			v[4,0] = -size/3; v[4,1] = -2*size/3; v[4,2] = 0;
			v[5,0] = -size/3; v[5,1] = -size/3; v[5,2] = 0;
			v[6,0] = -size; v[6,1] = value2; v[6,2] = 0;
			v[7,0] = -value1; v[7,1] = size; v[7,2] = 0;

			for (int i=0; i< 8; i++) 
			{
				Gl.glVertex2d(v[i,0], v[i,1]);

			}

			Gl.glEnd();
		
			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateNavaidVORTACSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListVORTAC = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListVORTAC, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			// Draw the Navaid TACAN symbol
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP); 
			
			double size = 30;
			double xSize = 2*size/3;
			double ySize = Math.Sqrt(3.0)*xSize/3;
			
			double value1 = size/2;
			double value2 = Math.Sqrt(3.0)*size/4;

			// Initialize vertices
			double[,] v = new double[8,3];
			v[0,0] = value1; v[0,1] = size; v[0,2] = 0;
			v[1,0] = size; v[1,1] = value2; v[1,2] = 0;
			v[2,0] = size/3; v[2,1] = -size/3; v[2,2] = 0;
			v[3,0] = size/3; v[3,1] = -2*size/3; v[3,2] = 0;

			v[4,0] = -size/3; v[4,1] = -2*size/3; v[4,2] = 0;
			v[5,0] = -size/3; v[5,1] = -size/3; v[5,2] = 0;
			v[6,0] = -size; v[6,1] = value2; v[6,2] = 0;
			v[7,0] = -value1; v[7,1] = size; v[7,2] = 0;

			for (int i=0; i< 8; i++) 
			{
				Gl.glVertex2d(v[i,0], v[i,1]);

			}

			Gl.glEnd();

			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=2; i< 6; i++) 
			{
				Gl.glVertex2d(v[i,0], v[i,1]);

			}
			Gl.glEnd();
			
			Gl.glLineWidth(3f);
			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2d(v[0,0], v[0,1]);
			Gl.glVertex2d(v[1,0], v[1,1]);
			Gl.glVertex2d(v[6,0], v[6,1]);
			Gl.glVertex2d(v[7,0], v[7,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateNavaidNDBSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListNDB = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListNDB, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			
			// Draw the Navaid NDB symbol
			DrawCircle(30, false);
			DrawCircle(20, false);
			DrawCircle(10, true);

            Gl.glPointSize(1);
            Gl.glBegin(Gl.GL_POINTS);
			Gl.glVertex2d(0, 0);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void DrawCircle(uint radius, bool line)
		{
            Gl.glPointSize(1);
			Gl.glLineWidth(1);
			int nPoints = 360;
			
			if (line)
			{
				Gl.glBegin(Gl.GL_LINE_LOOP);
			}
			else
			{
				Gl.glBegin(Gl.GL_POINTS);
				nPoints = 18;
			}

			uint centerY = 0;
			uint centerX = 0;

			double angleUnit = 360/nPoints*Math.PI/180;
			for (int i=0; i<nPoints; i++)
			{
				double angle = angleUnit*i;
				double x = (Math.Cos(angle) * radius) + centerX;
				double y = (Math.Sin(angle) * radius) + centerY;
				
				Gl.glVertex2d(x, y);
			}
			Gl.glEnd();
		}
	}
}

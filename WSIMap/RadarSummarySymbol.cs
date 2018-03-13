using System;
using System.Drawing;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class RadarSummarySymbol
	 * \brief Handles rendering of Radar Summary symbols
	 */
	public class RadarSummarySymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected static int[] openglDisplayLists;
		protected double speed;
		protected double direction;
        protected bool arrow;
        protected FUL.Utils.StormCellClassificationType type;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		private bool drawSymbol = true;
		#endregion

        public bool Arrow
        {
            get { return arrow; }
            set { arrow = value; }
        }

        public FUL.Utils.StormCellClassificationType Type
        {
            get { return type; }
        }

		// Radar summary text mode: only draw arrow
		public bool DrawSymbol
		{
			set { drawSymbol = value; }
		}

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RadarSummarySymbol Draw()");
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
			double px, py;
			Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
			Gl.glTranslated(px, py, 0.0);
			double symbolSize = (size + 1) * 0.05;
			Gl.glScaled(symbolSize/xFactor, symbolSize/yFactor, 1.0);

			if (drawSymbol)
				// Draw the symbol
				Gl.glCallList(openglDisplayList);
			else
				Gl.glColor3f(1.0f, 1.0f, 1.0f);

			// Rotate and draw the arrow or draw a circle
            if (arrow)
            {
                if (speed > 0)
                {
					double pdir;
					Projection.ProjectDirection(mapProjection, _x, y, direction, centralLongitude, out pdir);
					Gl.glRotated(360 - pdir, 0.0, 0.0, 1.0);	// OpenGL rotation is counterclockwise
                    Gl.glCallList(openglDisplayLists[4]);
                }
                else if (drawSymbol)
                    Gl.glCallList(openglDisplayLists[5]);
            }

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_POINT_SMOOTH);
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}

		static RadarSummarySymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RadarSummarySymbol RadarSummarySymbol()");
#endif

			// Create the display lists
			openglDisplayLists = new int[6];
			openglDisplayLists[0] = Gl.glGenLists(6);
			openglDisplayLists[1] = openglDisplayLists[0] + 1;
			openglDisplayLists[2] = openglDisplayLists[0] + 2;
			openglDisplayLists[3] = openglDisplayLists[0] + 3;
			openglDisplayLists[4] = openglDisplayLists[0] + 4;
			openglDisplayLists[5] = openglDisplayLists[0] + 5;

			// Create the symbols
			CreateTVSSymbol();
			CreateHailSymbol();
			CreateMesoSymbol();
			CreateStrongSymbol();
			CreateDirectionArrow();
			CreateUnknownSpeedCircle();
		}

		public RadarSummarySymbol(FUL.Utils.StormCellClassificationType type, double lat, double lon, double speed, double dir, string label)
		{
			this.x = lon;
			this.y = lat;
			this.speed = speed;
			this.direction = dir;
			this.size = 13;
			this.featureName = label;
			this.featureInfo = string.Empty;
            this.arrow = true;
            this.type = type;
			this.mapProjection = MapProjections.CylindricalEquidistant;

            if (type == FUL.Utils.StormCellClassificationType.Tornadic)
            {
                this.color = Color.Red;
                openglDisplayList = openglDisplayLists[0];
            }
            else if (type == FUL.Utils.StormCellClassificationType.Hail)
            {
                this.color = Color.Blue;
                openglDisplayList = openglDisplayLists[1];
            }
            else if (type == FUL.Utils.StormCellClassificationType.Rotating)
            {
                this.color = Color.Yellow;
                openglDisplayList = openglDisplayLists[2];
            }
            else if (type == FUL.Utils.StormCellClassificationType.Strong)
            {
                this.color = Color.FromArgb(25, 190, 23);
                openglDisplayList = openglDisplayLists[3];
            }
            else
            {
                this.color = Color.FromArgb(25, 190, 23);
                openglDisplayList = openglDisplayLists[3];
            }
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

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		protected static void CreateTVSSymbol()
		{
			// Start GL list #1 to contain the TVS symbol
			Gl.glNewList(openglDisplayLists[0], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices
			double[,] v = new double[3,3];
			v[0,0] = 0; v[0,1] = -13; v[0,2] = 0;
			v[1,0] = -10; v[1,1] = 7; v[1,2] = 0;
			v[2,0] = 10; v[2,1] = 7; v[2,2] = 0;

			// Draw
			Gl.glColor3f(((float)Color.Red.R)/255f, ((float)Color.Red.G)/255f, ((float)Color.Red.B)/255f);
			Gl.glPointSize(3);
			Gl.glBegin(Gl.GL_POINTS);
			Gl.glVertex2d(0,0);
			Gl.glEnd();
			Gl.glPointSize(1);
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glLineWidth(1);

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateHailSymbol()
		{
			// Start GL list #2 to contain the Hail symbol
			Gl.glNewList(openglDisplayLists[1], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices
			double[,] v = new double[4,3];
			v[0,0] = 0; v[0,1] = -10; v[0,2] = 0;
			v[1,0] = -10; v[1,1] = 0; v[1,2] = 0;
			v[2,0] = 0; v[2,1] = 10; v[2,2] = 0;
			v[3,0] = 10; v[3,1] = 0; v[3,2] = 0;

			// Draw
			Gl.glColor3f(((float)Color.Blue.R)/255f, ((float)Color.Blue.G)/255f, ((float)Color.Blue.B)/255f);
			Gl.glPointSize(3);
			Gl.glBegin(Gl.GL_POINTS);
			Gl.glVertex2d(0,0);
			Gl.glEnd();
			Gl.glPointSize(1);
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<4; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glLineWidth(1);

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateMesoSymbol()
		{
			// Start GL list #3 to contain the Meso symbol
			Gl.glNewList(openglDisplayLists[2], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices
			double[,] v = new double[4,3];
			v[0,0] = 10; v[0,1] = 10; v[0,2] = 0;
			v[1,0] = 10; v[1,1] = -10; v[1,2] = 0;
			v[2,0] = -10; v[2,1] = -10; v[2,2] = 0;
			v[3,0] = -10; v[3,1] = 10; v[3,2] = 0;

			// Draw
			Gl.glColor3f(((float)Color.Yellow.R)/255f, ((float)Color.Yellow.G)/255f, ((float)Color.Yellow.B)/255f);
			Gl.glPointSize(3);
			Gl.glBegin(Gl.GL_POINTS);
			Gl.glVertex2d(0,0);
			Gl.glEnd();
			Gl.glPointSize(1);
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<4; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glLineWidth(1);

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateStrongSymbol()
		{
			// Start GL list #4 to contain the Strong symbol
			Gl.glNewList(openglDisplayLists[3], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices
			double[,] v = new double[24,3];
			v[0,0] = 10.000000; v[0,1] = 0.000000; v[0,2] = 0;
			v[1,0] = 9.659258; v[1,1] = 2.588190; v[1,2] = 0;
			v[2,0] = 8.660254; v[2,1] = 5.000000; v[2,2] = 0;
			v[3,0] = 7.071068; v[3,1] = 7.071068; v[3,2] = 0;
			v[4,0] = 5.000000; v[4,1] = 8.660254; v[4,2] = 0;
			v[5,0] = 2.588190; v[5,1] = 9.659258; v[5,2] = 0;
			v[6,0] = 0.000000; v[6,1] = 10.000000; v[6,2] = 0;
			v[7,0] = -2.588190; v[7,1] = 9.659258; v[7,2] = 0;
			v[8,0] = -5.000000; v[8,1] = 8.660254; v[8,2] = 0;
			v[9,0] = -7.071068; v[9,1] = 7.071068; v[9,2] = 0;
			v[10,0] = -8.660254; v[10,1] = 5.000000; v[10,2] = 0;
			v[11,0] = -9.659258; v[11,1] = 2.588190; v[11,2] = 0;
			v[12,0] = -10.000000; v[12,1] = 0.000000; v[12,2] = 0;
			v[13,0] = -9.659258; v[13,1] = -2.588190; v[13,2] = 0;
			v[14,0] = -8.660254; v[14,1] = -5.000000; v[14,2] = 0;
			v[15,0] = -7.071068; v[15,1] = -7.071068; v[15,2] = 0;
			v[16,0] = -5.000000; v[16,1] = -8.660254; v[16,2] = 0;
			v[17,0] = -2.588190; v[17,1] = -9.659258; v[17,2] = 0;
			v[18,0] = 0.000000; v[18,1] = -10.000000; v[18,2] = 0;
			v[19,0] = 2.588190; v[19,1] = -9.659258; v[19,2] = 0;
			v[20,0] = 5.000000; v[20,1] = -8.660254; v[20,2] = 0;
			v[21,0] = 7.071068; v[21,1] = -7.071068; v[21,2] = 0;
			v[22,0] = 8.660254; v[22,1] = -5.000000; v[22,2] = 0;
			v[23,0] = 9.659258; v[23,1] = -2.588190; v[23,2] = 0;

			// Draw
			Gl.glColor3f(25f/255f, 190f/255f, 23f/255f);
			Gl.glPointSize(3);
			Gl.glBegin(Gl.GL_POINTS);
			Gl.glVertex2d(0,0);
			Gl.glEnd();
			Gl.glPointSize(1);
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<24; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glLineWidth(1);

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateDirectionArrow()
		{
			// Start GL list #4 to contain the direction arrow
			Gl.glNewList(openglDisplayLists[4], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices for the arrow shaft
			double[,] v = new double[2,3];
			v[0,0] = 0; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = 0; v[1,1] = 30; v[1,2] = 0;

			// Draw arrow shaft
			Gl.glLineWidth(2);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<2; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// Initialize vertices for the arrow head
			v = new double[3,3];
			v[0,0] = -6; v[0,1] = 30; v[0,2] = 0;
			v[1,0] = 0; v[1,1] = 42; v[1,2] = 0;
			v[2,0] = 6; v[2,1] = 30; v[2,2] = 0;

			// Draw arrow head
			Gl.glLineWidth(3);
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glLineWidth(1);

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateUnknownSpeedCircle()
		{
			// Start GL list #5 to contain a circle for unknown speeds
			Gl.glNewList(openglDisplayLists[5], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			DrawCircle(15,true);

			// End the GL list
			Gl.glEndList();
		}

		protected static void DrawCircle(uint radius, bool line)
		{
			int nPoints = 360;
			
			if (line)
				Gl.glBegin(Gl.GL_LINE_LOOP);
			else
				Gl.glBegin(Gl.GL_POLYGON);

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

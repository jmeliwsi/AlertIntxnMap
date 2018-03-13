using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class WxAlertSymbol
	 * \brief Handles rendering of WxAlert symbols
	 */
	public class WxAlertSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected static int[] openglDisplayLists;
		protected Color color1;
		protected Color color2;
		protected Color color3;
		protected Color color4;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		#endregion

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

        public void SetSize(uint size)
        {
            this.size = size;
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WxAlertSymbol Draw()");
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
			Gl.glScaled(symbolSize/xFactor, symbolSize/yFactor, 1.0);

			// Set the symbol color
			double alpha = 1;
			if (color1 == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color1.R), glc(color1.G), glc(color1.B), alpha);

			// Draw the symbol
			Gl.glCallList(openglDisplayLists[0]);

			// Set the symbol color
			alpha = 1;
			if (color2 == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color2.R), glc(color2.G), glc(color2.B), alpha);

			// Draw the symbol
			Gl.glCallList(openglDisplayLists[1]);

			// Set the symbol color
			alpha = 1;
			if (color3 == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color3.R), glc(color3.G), glc(color3.B), alpha);

			// Draw the symbol
			Gl.glCallList(openglDisplayLists[2]);

			// Set the symbol color
			alpha = 1;
			if (color4 == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color4.R), glc(color4.G), glc(color4.B), alpha);

			// Draw the symbol
			Gl.glCallList(openglDisplayLists[3]);

            // Draw the border
            Gl.glCallList(openglDisplayLists[4]);

			// Restore previous projection matrix
			Gl.glPopMatrix(); 
		}

		static WxAlertSymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WxAlertSymbol WxAlertSymbol()");
#endif

			CreateWxAlertSymbol();
		}

		public WxAlertSymbol(Color color1, Color color2, Color color3, Color color4, double lat, double lon, string label)
		{
			this.color1 = color1;
			this.color2 = color2;
			this.color3 = color3;
			this.color4 = color4;
			this.x = lon;
			this.y = lat;
			this.size = 13;
			this.featureName = label;
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

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public void SetColors(Color color1, Color color2, Color color3, Color color4)
		{
			this.color1 = color1;
			this.color2 = color2;
			this.color3 = color3;
			this.color4 = color4;
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		protected static void CreateWxAlertSymbol()
		{
			// Create the display lists
			openglDisplayLists = new int[5];
			openglDisplayLists[0] = Gl.glGenLists(5);
			openglDisplayLists[1] = openglDisplayLists[0] + 1;
			openglDisplayLists[2] = openglDisplayLists[0] + 2;
			openglDisplayLists[3] = openglDisplayLists[0] + 3;
            openglDisplayLists[4] = openglDisplayLists[0] + 4;

			// Start GL list #1 to contain the top triangle
			Gl.glNewList(openglDisplayLists[0], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices of the top triangle
			double[,] v = new double[3,3];
			v[0,0] = 0; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = -15; v[1,1] = 15; v[1,2] = 0;
			v[2,0] = 15; v[2,1] = 15; v[2,2] = 0;

			// Draw
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glColor3f(((float)Color.LightGray.R)/255f, ((float)Color.LightGray.G)/255f, ((float)Color.LightGray.B)/255f);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();

			// Start GL list #2 to contain the right-side triangle
			Gl.glNewList(openglDisplayLists[1], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices of the right-side triangle
			v[0,0] = 0; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = 15; v[1,1] = 15; v[1,2] = 0;
			v[2,0] = 15; v[2,1] = -15; v[2,2] = 0;

			// Draw
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glColor3f(((float)Color.LightGray.R)/255f, ((float)Color.LightGray.G)/255f, ((float)Color.LightGray.B)/255f);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();

			// Start GL list #3 to contain the bottom triangle
			Gl.glNewList(openglDisplayLists[2], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices of the bottom triangle
			v[0,0] = 0; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = 15; v[1,1] = -15; v[1,2] = 0;
			v[2,0] = -15; v[2,1] = -15; v[2,2] = 0;

			// Draw
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glColor3f(((float)Color.LightGray.R)/255f, ((float)Color.LightGray.G)/255f, ((float)Color.LightGray.B)/255f);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();

			// Start GL list #4 to contain the left-side triangle
			Gl.glNewList(openglDisplayLists[3], Gl.GL_COMPILE);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices of the left-side triangle
			v[0,0] = 0; v[0,1] = 0; v[0,2] = 0;
			v[1,0] = -15; v[1,1] = -15; v[1,2] = 0;
			v[2,0] = -15; v[2,1] = 15; v[2,2] = 0;

			// Draw
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();
			Gl.glColor3f(((float)Color.LightGray.R)/255f, ((float)Color.LightGray.G)/255f, ((float)Color.LightGray.B)/255f);
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i=0; i<3; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();


            // Start GL list #5 Containing the 
            Gl.glNewList(openglDisplayLists[4], Gl.GL_COMPILE);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            Gl.glColor3f(((float)Color.Silver.R) / 255f, ((float)Color.Silver.G) / 255f, ((float)Color.Silver.B) / 255f);
            Gl.glVertex2d(-15, -15);
            Gl.glVertex2d(15, -15);
            Gl.glVertex2d(15, 15);
            Gl.glVertex2d(-15, 15);
            Gl.glEnd();
            Gl.glEndList();
        }

	}
}

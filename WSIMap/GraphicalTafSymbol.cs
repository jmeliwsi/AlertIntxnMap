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
    public class GraphicalTafSymbol : Feature, IMapPoint, IProjectable
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
        protected static int nPoints = 360;
        protected static int radius = 32;
        #endregion

		static GraphicalTafSymbol()
		{
			CreateGraphicalTafSymbol();
		}

		public GraphicalTafSymbol(Color color1, Color color2, Color color3, Color color4, double lat, double lon, string label)
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
			ConfirmMainThread("GraphicalTafSymbol Draw()");
#endif

			// Preserve the projection matrix state
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();

            double _x = x;
            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                _x = parentMap.DenormalizeLongitude(x);

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Do not draw points below the equator for azimuthal projections
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude) return;

            // Calculate the rendering scale factors
            double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

            // Set the position and size of the symbol
			double px, py;
			Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
			Gl.glTranslated(px, py, 0.0);
			double symbolSize = (size + 1) * 0.03;
            Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);

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

            // Restore previous projection matrix
            Gl.glPopMatrix();
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

		protected static void CreateGraphicalTafSymbol()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("GraphicalTafSymbol CreateGraphicalTafSymbol()");
#endif
			// Create the display lists
            openglDisplayLists = new int[4];
            openglDisplayLists[0] = Gl.glGenLists(4);
            openglDisplayLists[1] = openglDisplayLists[0] + 1;
            openglDisplayLists[2] = openglDisplayLists[0] + 2;
            openglDisplayLists[3] = openglDisplayLists[0] + 3;

            // Start GL list #1 to contain the top triangle
            Gl.glNewList(openglDisplayLists[0], Gl.GL_COMPILE);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw
            Gl.glBegin(Gl.GL_POLYGON);
            Gl.glVertex2d(0, 0);
            for (double i = 45; i <= 135; i++)
            {
                double x = (Math.Cos(i * deg2rad)) * radius;
                double y = (Math.Sin(i * deg2rad)) * radius;
                Gl.glVertex2d(x, y);
            }
            Gl.glVertex2d(0, 0);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();

            // Start GL list #2 to contain the right-side triangle
            Gl.glNewList(openglDisplayLists[1], Gl.GL_COMPILE);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw

            Gl.glBegin(Gl.GL_POLYGON);
            Gl.glVertex2d(0, 0);
            for (double i = 135; i <= 225; i++)
                Gl.glVertex2d((Math.Cos(i * deg2rad)) * radius, (Math.Sin(i * deg2rad)) * radius);
            Gl.glVertex2d(0, 0);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();

            // Start GL list #3 to contain the bottom triangle
            Gl.glNewList(openglDisplayLists[2], Gl.GL_COMPILE);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw
            Gl.glBegin(Gl.GL_POLYGON);
            Gl.glVertex2d(0, 0);
            for (double i = 225; i <= 315; i++)
                Gl.glVertex2d((Math.Cos(i * deg2rad)) * radius, (Math.Sin(i * deg2rad)) * radius);
            Gl.glVertex2d(0, 0);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();

            // Start GL list #4 to contain the left-side triangle
            Gl.glNewList(openglDisplayLists[3], Gl.GL_COMPILE);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw
            Gl.glBegin(Gl.GL_POLYGON);
            Gl.glVertex2d(0, 0);
            for (double i = 315; i <= 405; i++)
                Gl.glVertex2d((Math.Cos(i * deg2rad)) * radius, (Math.Sin(i * deg2rad)) * radius);
            Gl.glVertex2d(0, 0);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();
        }
    }
}

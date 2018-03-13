using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Tao.OpenGl;

namespace WSIMap
{
    public class RunwayActivityArrows: Feature, IProjectable, IRefreshable
    {
        #region Data Memebers
        private Color color;
        private int count;
        private double x;
        private double y;
        private int width;
        private double direction;

        //Label
        private double labelLat;
        private double labelLon;
        private string labelText;
        private Color labelColor = Color.Black;

        protected MapProjections mapProjection;
        protected short centralLongitude;

        private const int size = 150;
        private const double lengthFactor = 2.5;
        private const double offset = 1;
		private const float scale = 0.0000025f;
		private const float labelRangeOffset = 195;
		private const float labelBearingOffset = -30;
		private const string TRACKING_CONTEXT = "Arrows";
		#endregion

		[DllImport("tessellate.dll", EntryPoint = "DrawString", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void DrawString(string text);
		
		public RunwayActivityArrows(double longitude, double latitude, double arrowDirection, int arrowWidth, int total, Color arrowColor)
        {
            x = longitude;
            y = latitude;
            width = arrowWidth;
            count = total;
            color = arrowColor;
            direction = arrowDirection;
			labelText = string.Empty;

            this.mapProjection = MapProjections.CylindricalEquidistant;
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public double Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public string LabelText
        {
            set { labelText = value; }
        }

        public Color LabelColor
        {
            set { labelColor = value; }
        }

        public void Dispose()
        {
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
        }

        public MapProjections MapProjection
        {
            get { return mapProjection; }
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

        protected void CreateDisplayList()
        {
            if ((openglDisplayList == -1) || Updated)
            {
                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

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
                                
                // Draw the symbol
                Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
                CreateArrow();

                // Draw the text
				Gl.glPushMatrix();

                labelColor = FUL.Utils.GetContrastingTextColor(color);
                Gl.glColor3f(glc(labelColor.R), glc(labelColor.G), glc(labelColor.B));
                Gl.glTranslated(labelLon, labelLat, 0.0);
				Gl.glRotated(-direction, 0.0, 0.0, 1.0);
				Gl.glScalef(scale, scale, scale);
				Gl.glLineWidth(2.5f);		// Thicken the text to mimic a bold font.
				DrawString(labelText);

				Gl.glPopMatrix();

				// Just for reference because this code works fine on most computers with video cards, using a 16pt "Microsoft Sans Serif" bold RotatableFont:
				//if (font.Initialized)
				//{
				//	labelColor = FUL.Utils.GetContrastingTextColor(color);
				//	Gl.glColor3f(glc(labelColor.R), glc(labelColor.G), glc(labelColor.B));
				//	Gl.glTranslated(labelLon, labelLat, 0.0);
				//	Gl.glRotated(-direction, 0.0, 0.0, 1.0);
				//	Gl.glScalef(0.00045, 0.00045, 0.00045);
				//	Gl.glPushAttrib(Gl.GL_LIST_BIT);
				//	Gl.glListBase(font.OpenGLDisplayListBase);
				//	Gl.glCallLists(labelText.Length, Gl.GL_UNSIGNED_BYTE, labelText);
				//	Gl.glPopAttrib();
				//}

                // Turn off anti-aliasing
                Gl.glDisable(Gl.GL_POINT_SMOOTH);
                Gl.glDisable(Gl.GL_LINE_SMOOTH);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

                // Restore previous projection matrix
                Gl.glPopMatrix();

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
				ConfirmMainThread("Arrow Draw()");
#endif

            if (openglDisplayList == -1) return;

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
            else
                Gl.glCallList(openglDisplayList);
        }

        private void CreateArrow()
        {
            for (int i = count; i >= 0; i--)
            {
                double lat = y;
                double lon = x;

                if (i > 0)
                    FUL.Utils.RangeBearingToLatLon(lat, lon, size * offset * i * FUL.Utils.Feet2NM, direction, ref lat, ref lon);

                CreateArrow(lat, lon);
            }
        }

        private void CreateArrow(double latitude, double longitude)
        {
            List<PointD> arrowPolygonPoints = new List<PointD>();
            List<PointD> arrowTrianglePoints = new List<PointD>();
            List<PointD> arrowOutlinePoints = new List<PointD>();

            double lat = 0;
            double lon = 0;
            double widthNew = width * FUL.Utils.Feet2NM/2;
            double bearing = direction;

            //Point 1
			FUL.Utils.RangeBearingToLatLon(latitude, longitude, widthNew, bearing + 270, ref lat, ref lon);
            double lat1 = lat; double lon1 = lon;
            arrowPolygonPoints.Add(new PointD(lon, lat));
            arrowOutlinePoints.Add(new PointD(lon, lat));
            //Point 2
            arrowPolygonPoints.Add(new PointD(longitude, latitude));
			arrowOutlinePoints.Add(new PointD(longitude, latitude));
            //Point 3
            FUL.Utils.RangeBearingToLatLon(latitude, longitude, widthNew, bearing + 90, ref lat, ref lon);
            arrowPolygonPoints.Add(new PointD(lon, lat));
			arrowOutlinePoints.Add(new PointD(lon, lat));

            double latRunway = 0, lonRunway = 0;
            //Point 4
            FUL.Utils.RangeBearingToLatLon(lat, lon, size * FUL.Utils.Feet2NM, bearing, ref latRunway, ref lonRunway);
            arrowPolygonPoints.Add(new PointD(lonRunway, latRunway));
            arrowTrianglePoints.Add(new PointD(lonRunway, latRunway));
			arrowOutlinePoints.Add(new PointD(lonRunway, latRunway));
            //Point 5
            FUL.Utils.RangeBearingToLatLon(latRunway, lonRunway, widthNew * 1.5, bearing + 90, ref latRunway, ref lonRunway);
            arrowTrianglePoints.Add(new PointD(lonRunway, latRunway));
            arrowOutlinePoints.Add(new PointD(lonRunway, latRunway));
            //Point 6
            FUL.Utils.RangeBearingToLatLon(latitude, longitude, size * lengthFactor  * FUL.Utils.Feet2NM, bearing, ref latRunway, ref lonRunway);
            arrowTrianglePoints.Add(new PointD(lonRunway, latRunway));
            arrowOutlinePoints.Add(new PointD(lonRunway, latRunway));

            double lat2 = 0, lon2 = 0;
            //Point 8
			FUL.Utils.RangeBearingToLatLon(lat1, lon1, size * FUL.Utils.Feet2NM, bearing, ref lat2, ref lon2);
            //Point 7
            FUL.Utils.RangeBearingToLatLon(lat2, lon2, widthNew * 1.5, bearing + 270, ref lat, ref lon);
            arrowTrianglePoints.Add(new PointD(lon, lat));
            arrowOutlinePoints.Add(new PointD(lon, lat));

            arrowPolygonPoints.Add(new PointD(lon2, lat2));
            arrowTrianglePoints.Add(new PointD(lon2, lat2));
            arrowOutlinePoints.Add(new PointD(lon2, lat2));

            //Calculate label location
			FUL.Utils.RangeBearingToLatLon(latitude, longitude, labelRangeOffset * FUL.Utils.Feet2NM, bearing + labelBearingOffset, ref labelLat, ref labelLon);

            // Draw Arrow
            Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));

            Gl.glBegin(Gl.GL_POLYGON);
            foreach (PointD p in arrowPolygonPoints)
            {
                double px, py;
                Projection.ProjectPoint(mapProjection, p.Longitude, p.Latitude, centralLongitude, out px, out py);
                Gl.glVertex2d(px, py);
            }
            Gl.glEnd();
           
            Gl.glBegin(Gl.GL_POLYGON);
            foreach (PointD p in arrowTrianglePoints)
            {
                double px, py;
                Projection.ProjectPoint(mapProjection, p.Longitude, p.Latitude, centralLongitude, out px, out py);
                Gl.glVertex2d(px, py);
            }
            Gl.glEnd();

            //Draw Outline
            Gl.glColor3f(Color.Black.R, Color.Black.G, Color.Black.B);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            foreach (PointD p in arrowOutlinePoints)
            {
                double px, py;
                Projection.ProjectPoint(mapProjection, p.Longitude, p.Latitude, centralLongitude, out px, out py);
                Gl.glVertex2d(px, py);
            }
            Gl.glEnd();
        }

        public static double GetArrowSize(int count)
        {
            return (count * offset + lengthFactor) * size;
        }
    }
}

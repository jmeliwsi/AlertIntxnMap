using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
    /**
     * \class Circle
     * \brief Represents a cone on the map
     */
    public class Wedge : Feature, IRefreshable
    {
        #region Data Members
        protected PointD vertex;
        protected double length;
        protected double direction;
        protected Color borderColor;
        protected Color fillColor;
        protected uint opacity;
        protected uint borderWidth;
        protected uint ticks;           // number of arcs including outer edge
        private const int angle = 30;   // central angle in degrees
		private const string TRACKING_CONTEXT = "Wedge";
        #endregion

        public Wedge(PointD vertex, double lengthInMiles, double direction, Color borderColor, uint borderWidth, Color fillColor, uint opacity, uint ticks)
        {
            this.vertex = vertex;
            this.Length = lengthInMiles;
            this.direction = direction;
            this.borderColor = borderColor;
            this.fillColor = fillColor;
            this.Opacity = opacity;
            this.borderWidth = borderWidth;
            this.ticks = ticks;
        }

        public Wedge(double lat, double lon, double lengthInMiles, double direction, Color borderColor, uint borderWidth, Color fillColor, uint opacity, uint ticks) : this(new PointD(lon,lat), lengthInMiles, direction, borderColor, borderWidth, fillColor, opacity, ticks)
        {
        }

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public PointD Vertex
        {
            get { return vertex; }
            set { vertex = value; Updated = true; }
        }

        public double Length
        {
            get { return length; }
            set
            {
                if (value < 0) value = 0;
                length = value;
                Updated = true;
            }
        }

        public double Direction
        {
            get { return direction; }
            set { direction = value; Updated = true; }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Updated = true; }
        }

        public uint BorderWidth
        {
            get { return borderWidth; }
            set { borderWidth = value; Updated = true; }
        }

        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; Updated = true; }
        }

        public uint Opacity
        {
            get { return opacity; }
            set
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;
                opacity = value;
                Updated = true;
            }
        }

        public void Refresh(MapProjections mapProjection, short centralLongitude)
        {
            if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
				CreateDisplayList();
        }

        private void CreateDisplayList()
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

                // Render the cone
                if (opacity == 0 && borderWidth == 0)
                {
                    Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;	// nothing to draw
                }
                if (opacity > 0 && fillColor != Color.Transparent)
                {
                    Gl.glColor4f(glc(fillColor.R), glc(fillColor.G), glc(fillColor.B), (float)opacity / 100);
                    Gl.glBegin(Gl.GL_POLYGON);
                    Gl.glVertex2d(vertex.X, vertex.Y);
                    int dir = Convert.ToInt32(90 - direction - (angle / 2));
                    if (dir < 0) dir += 360;
                    for (int i = dir; i < dir + angle; i++)
                    {
                        double xLengthInDeg = length / (Math.Cos(vertex.Y * deg2rad) * deg2mi);
                        double yLengthInDeg = length / deg2mi;
                        double x = (Math.Cos(i * deg2rad) * xLengthInDeg) + vertex.X;
                        double y = (Math.Sin(i * deg2rad) * yLengthInDeg) + vertex.Y;
                        Gl.glVertex2d(x, y);
                    }
                    Gl.glEnd();
                }
                if (borderWidth > 0)
                {
                    Gl.glEnable(Gl.GL_LINE_SMOOTH);
                    Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
                    Gl.glLineWidth(borderWidth);
                    Gl.glBegin(Gl.GL_LINE_LOOP);
                    Gl.glVertex2d(vertex.X, vertex.Y);
                    int dir = Convert.ToInt32(90 - direction - (angle / 2));
                    if (dir < 0) dir += 360;
                    for (int i = dir; i < dir + angle; i++)
                    {
                        double xLengthInDeg = length / (Math.Cos(vertex.Y * deg2rad) * deg2mi);
                        double yLengthInDeg = length / deg2mi;
                        double x = (Math.Cos(i * deg2rad) * xLengthInDeg) + vertex.X;
                        double y = (Math.Sin(i * deg2rad) * yLengthInDeg) + vertex.Y;
                        Gl.glVertex2d(x, y);
                    }
                    Gl.glEnd();
                    if (ticks > 0)
                    {
                        for (int j = 1; j < ticks; j++)
                        {
                            Gl.glBegin(Gl.GL_LINE_STRIP);
                            for (int i = dir; i < dir + angle; i++)
                            {
                                double xLengthInDeg = (j * length / ticks) / (Math.Cos(vertex.Y * deg2rad) * deg2mi);
                                double yLengthInDeg = (j * length / ticks) / deg2mi;
                                double x = (Math.Cos(i * deg2rad) * xLengthInDeg) + vertex.X;
                                double y = (Math.Sin(i * deg2rad) * yLengthInDeg) + vertex.Y;
                                Gl.glVertex2d(x, y);
                                numVertices++;
                            }
                            Gl.glEnd();
                        }
                    }
                    Gl.glDisable(Gl.GL_LINE_SMOOTH);
                }
                numVertices += angle;

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Wedge Draw()");
#endif

			if (openglDisplayList == -1) return;

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);            
            else
                Gl.glCallList(openglDisplayList);
        }
    }
}

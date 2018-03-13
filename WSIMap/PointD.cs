using System;
using System.Drawing;
using FUL;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class PointD
	 * \brief Represents a double precision point on the map
	 */
	[Serializable] public class PointD : Feature, IMapPoint, IProjectable, IRefreshable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected Color color;
		protected uint size;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		public static readonly PointD Empty = new PointD(double.NaN, double.NaN);
		private const string TRACKING_CONTEXT = "PointD";
		#endregion

		public PointD() : this(0.0, 0.0, Color.White, 1)
		{
		}

		public PointD(double x, double y) : this(x, y, Color.White, 1)
		{
		}

		public PointD(double x, double y, Color color) : this(x, y, color, 1)
		{
		}

		public PointD(double x, double y, Color color, uint size)
		{
			this.x = x;
			this.y = y;
			this.color = color;
			this.size = size;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.numVertices = 1;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public double X
		{
			get	{ return x; }
            set { x = value; Updated = true; }
		}

		public double Y
		{
			get	{ return y; }
            set { y = value; Updated = true; }
		}

		public double Latitude
		{
			get	{ return y;	}
            set { y = value; Updated = true; }
		}

		public double Longitude
		{
			get	{ return x;	}
            set { x = value; Updated = true; }
		}

		public virtual Color Color
		{
			get { return color; }
            set { color = value; Updated = true; }
		}

		public uint Size
		{
			get { return size; }
            set { size = value; Updated = true; }
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		public static bool IsNullOrEmpty(PointD p)
		{
			if (p == null)
				return true;
			if (double.IsNaN(p.X) && double.IsNaN(p.Y))
				return true;
			return false;
		}

		public override string ToString()
		{
			return string.Format("{0,7:###.00}, {1,6:##.00}", x, y);
		}

		public override bool Equals(object obj)
		{
			PointD p = obj as PointD;
			if (p != null)
				return (this.x == p.x && this.y == p.y);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Check duplicaple points
		/// </summary>
		/// <param name="p">The other point</param>
		/// <returns>Return true if two points have same latitude and longitude</returns>
		public bool IsSamePoint(PointD p)
		{
			// handle precision in comparisons
			return ((Convert.ToInt32(this.x * 1000) == Convert.ToInt32(p.x * 1000)) &&
					(Convert.ToInt32(this.y * 1000) == Convert.ToInt32(p.y * 1000)));
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

		private void CreateDisplayList()
        {
			if ((openglDisplayList == -1) || Updated)
            {
                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

				// Do not draw points below the equator for azimuthal projections
				if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude)
				{
					Gl.glEndList();
					return;
				}

				// Some OpenGL initialization
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_FLAT);

                // Render the PointD
                Gl.glEnable(Gl.GL_POINT_SMOOTH);
                Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
                Gl.glPointSize(size);
                Gl.glBegin(Gl.GL_POINTS);
				double px, py;
				Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out px, out py);
				Gl.glVertex2d(px, py);
                Gl.glEnd();
                Gl.glDisable(Gl.GL_POINT_SMOOTH);

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("PointD Draw()");
#endif

			if (openglDisplayList == -1) return;

			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
				MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
			else
				Gl.glCallList(openglDisplayList);
		}
	}
}

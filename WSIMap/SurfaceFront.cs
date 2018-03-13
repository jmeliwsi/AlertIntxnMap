using System;
using System.Collections.Generic;
using System.Drawing;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public class SurfaceFront : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		protected List<PointD> pointList;
		protected List<double> cusumDist;
		protected Color color;
		protected uint width;
		protected FrontType type;
		private bool crossesIDL = false;
		private bool inSouthernHemisphere;
		private static int openglDisplayListColdPips;
		private static int openglDisplayListWarmPips;
		private static int openglDisplayListDryLinePips;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		private const string TRACKING_CONTEXT = "SurfaceFront";
		#endregion

		public enum FrontType { Cold, Warm, Stationary, Occluded, Trough, DryLine, SquallLine };

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

		static SurfaceFront()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("SurfaceFront SurfaceFront()");
#endif

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Create the pip display lists once for all fronts
			DrawColdPips();
			DrawWarmPips();
			DrawDryLinePips();
		}

		public SurfaceFront(FrontType type)
		{
			pointList = new List<PointD>();
			cusumDist = new List<double>();

			width = 2;
			this.type = type;

			// Colors are from http://www.hpc.ncep.noaa.gov/html/fntcodes2.shtml
			switch (type)
			{
				case FrontType.Cold:
					color = Color.Blue;
					break;
				case FrontType.Warm:
					color = Color.Red;
					break;
				case FrontType.Occluded:
					color = Color.FromArgb(91, 42, 198);
					break;
				case FrontType.Trough:
					color = Color.FromArgb(206, 110, 25);
					break;
				case FrontType.DryLine:
					color = Color.FromArgb(206, 110, 25);
					break;
				case FrontType.SquallLine:
					color = Color.Red;
					break;
				default:
					color = Color.Blue;
					break;
			}
		}

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public int Count
		{
			get { return pointList.Count; }
		}

		public void AddRange(IEnumerable<PointD> collection)
		{
			// Add the points to the point list
			pointList.AddRange(collection);

			// Calculate cumulative summation of the distance along the front
			double totalDist = 0;
			cusumDist.Add(0);
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				PointD p1 = pointList[i];
				PointD p2 = pointList[i + 1];
				double dist = p1.DistanceTo(p2, true);
				totalDist += dist;
				cusumDist.Add(totalDist);
			}

			// Is the front entirely in the southern hemisphere?
			inSouthernHemisphere = true;
			for (int i = 0; i < pointList.Count; i++)
				if (pointList[i].Y > 0)
					inSouthernHemisphere = false;

			Updated = true;
		}

		public void Clear()
		{
			pointList.Clear();
			cusumDist.Clear();
			Updated = true;
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
			// Nothing to draw
			if (pointList == null || pointList.Count == 0)
				return;

			// Don't draw the front for azimuthal projections if it's entirely in the southern hemisphere
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && inSouthernHemisphere)
				return;

			if ((openglDisplayList == -1) || Updated)
			{
				// Some OpenGL initialization
				Gl.glEnable(Gl.GL_BLEND);
				Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
				Gl.glShadeModel(Gl.GL_FLAT);

				// Do the actual OpenGL rendering (pips were rendered in static constructor)
				DrawCurve();

				if (Updated)
					Updated = false;
			}
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("SurfaceFront Draw()");
#endif

			if (openglDisplayList == -1)
				return;

			// What type of map projection is in use?
			bool azimuthalProj = (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal);

			// Don't draw the front for azimuthal projections if it's entirely in the southern hemisphere
			if (azimuthalProj && inSouthernHemisphere)
				return;

			// Turn on anti-aliasing
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

			// Set the color
			Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), 1);

			// Draw the line portion of the front
			if (type != FrontType.Stationary)
			{
				if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180 || crossesIDL)
					MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right, crossesIDL);
				else
					Gl.glCallList(openglDisplayList);
			}

			// If this is a trough, we are done drawing
			if (type == FrontType.Trough || type == FrontType.SquallLine)
			{
				Gl.glDisable(Gl.GL_LINE_SMOOTH);
				Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
				return;
			}

			// Set the matrix mode to use the projection matrix
			Gl.glMatrixMode(Gl.GL_PROJECTION);

			// Calculate the rendering scale factors (pixels/degree)
			double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
			double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Misc. drawing values
			int direction = 0;
			double symbolSize = 10 * 0.05;
			double pipSpacing = 2000 / xFactor;
			if (azimuthalProj)
				pipSpacing = 3000 / xFactor;
			if (type == FrontType.DryLine)
				pipSpacing = 1000 / xFactor;
			int nPips = (int)(cusumDist[cusumDist.Count - 1] / pipSpacing);
			double x = 0, y = 0, px = 0, py = 0, xm = 0, ym = 0;

			// Iterate and draw the pips
			int index = 0, prevIndex = 0, midptIndex = 0;
			for (int n = 1; n <= nPips; n++)
			{
				// Determine which two points the pip falls in between
				prevIndex = index;
				index = cusumDist.FindIndex(index, (d => d > (n * pipSpacing))); // 2nd arg is a lambda expression
				if (index == 0) continue;

				// Calculate position of pip in between pointList[index-1] and pointList[index]
				px = x; py = y;
				double p1x, p1y, p2x, p2y;
				Projection.ProjectPoint(mapProjection, pointList[index].X, pointList[index].Y, centralLongitude, out p1x, out p1y);
				Projection.ProjectPoint(mapProjection, pointList[index - 1].X, pointList[index - 1].Y, centralLongitude, out p2x, out p2y);
				double dx = p1x - p2x;
				double dy = p1y - p2y;
				if (Math.Abs(dx) > 300) // front crossed date line
					dx = (360 - Math.Abs(dx)) * -Math.Sign(dx);
				double fraction = ((n * pipSpacing) - cusumDist[index - 1]) / (cusumDist[index] - cusumDist[index - 1]);
				x = p2x + (fraction * dx);
				y = p2y + (fraction * dy);
				if (!azimuthalProj)
					if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
						x = parentMap.DenormalizeLongitude(x);

				// Don't draw this pip if it's outside the map area
				if (!azimuthalProj)
					if (x < parentMap.BoundingBox.Map.left || x > parentMap.BoundingBox.Map.right || y > parentMap.BoundingBox.Map.top || y < parentMap.BoundingBox.Map.bottom)
						continue;

				// Calculate position halfway between this pip and previous pip
				if (type == FrontType.Stationary)
				{
					midptIndex = cusumDist.FindIndex(midptIndex, (d => d > ((n * pipSpacing) - (pipSpacing / 2))));
					Projection.ProjectPoint(mapProjection, pointList[midptIndex].X, pointList[midptIndex].Y, centralLongitude, out p1x, out p1y);
					Projection.ProjectPoint(mapProjection, pointList[midptIndex - 1].X, pointList[midptIndex - 1].Y, centralLongitude, out p2x, out p2y);
					double dxm = p1x - p2x;
					double dym = p1y - p2y;
					if (Math.Abs(dxm) > 300) // front crossed date line
						dxm = (360 - Math.Abs(dxm)) * -Math.Sign(dxm);
					double fractionm = (((n * pipSpacing) - (pipSpacing / 2)) - cusumDist[midptIndex - 1]) / (cusumDist[midptIndex] - cusumDist[midptIndex - 1]);
					xm = p2x + (fractionm * dxm);
					ym = p2y + (fractionm * dym);
					if (!azimuthalProj)
						if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
							xm = parentMap.DenormalizeLongitude(xm);
				}

				// Calculate orientation of pip
				direction = (int)(Math.Atan2(dy, dx) * FUL.Utils.rad2deg) - 180;
				if (type == FrontType.Stationary && (n % 2 != 0))
					direction -= 180;

				// Draw the pip
				Gl.glPushMatrix();
				Gl.glTranslated(x, y, 0.0);
				Gl.glRotated(direction, 0.0, 0.0, 1.0);	// OpenGL rotation is counterclockwise
				Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);
				if (type == FrontType.Cold)
					Gl.glCallList(openglDisplayListColdPips);
				else if (type == FrontType.Warm)
					Gl.glCallList(openglDisplayListWarmPips);
				else if (type == FrontType.DryLine)
					Gl.glCallList(openglDisplayListDryLinePips);
				else if (type == FrontType.Stationary)
				{
					if (n % 2 == 0)
					{
						Gl.glColor3f(Color.Blue.R, Color.Blue.G, Color.Blue.B);
						Gl.glCallList(openglDisplayListColdPips);
					}
					else
					{
						Gl.glColor3f(Color.Red.R, Color.Red.G, Color.Red.B);
						Gl.glCallList(openglDisplayListWarmPips);
					}
				}
				else if (type == FrontType.Occluded)
				{
					Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), 1);
					if (n % 2 == 0)
						Gl.glCallList(openglDisplayListColdPips);
					else
						Gl.glCallList(openglDisplayListWarmPips);
				}
				Gl.glPopMatrix();

				// For stationary fronts, draw alternating red/blue front line
				if (type == FrontType.Stationary && n != 1)
				{
					// Set line width
					Gl.glLineWidth(width);

					// Draw line from previous pip location to half way point
					if (n % 2 == 0)
						Gl.glColor3f(Color.Red.R, Color.Red.G, Color.Red.B);
					else
						Gl.glColor3f(Color.Blue.R, Color.Blue.G, Color.Blue.B);
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Gl.glVertex2d(px, py);
					if (Math.Abs(px - xm) < 180)
						Gl.glVertex2d(xm, ym); // exclude this point if previous point is on the other side of the map
					Gl.glEnd();

					// Draw line from half way point to current pip location
					if (n % 2 == 0)
						Gl.glColor3f(Color.Blue.R, Color.Blue.G, Color.Blue.B);
					else
						Gl.glColor3f(Color.Red.R, Color.Red.G, Color.Red.B);
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Gl.glVertex2d(xm, ym);
					if (Math.Abs(xm - x) < 180)
						Gl.glVertex2d(x, y); // exclude this point if previous point is on the other side of the map
					Gl.glEnd();
				}
			}

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
		}

		private void DrawCurve()
		{
			double px, py;

			// Create an OpenGL display list for the curve part of the front
			CreateOpenGLDisplayList(TRACKING_CONTEXT);
			Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

			// For troughs, enable stippling to draw a dashed line
			if (type == FrontType.Trough)
			{
				Gl.glLineStipple(3, 0x0FFF);
				Gl.glEnable(Gl.GL_LINE_STIPPLE);
			}
			else if (type == FrontType.SquallLine)
			{
				Gl.glLineStipple(3, 0xF24F);
				Gl.glEnable(Gl.GL_LINE_STIPPLE);
			}

			// Render the curve
			PointD newPt1, newPt2;
			numVertices += Count;
			Gl.glLineWidth(width);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Projection.ProjectPoint(mapProjection, pointList[0].X, pointList[0].Y, centralLongitude, out px, out py);
			Gl.glVertex2d(px, py);
			for (int i = 1; i < pointList.Count; i++)
			{
				crossesIDL = Curve.CrossesIDL(pointList[i - 1], pointList[i], out newPt1, out newPt2);
				if (crossesIDL)
				{
					// The segment crosses the dateline, so split segment
					// and add points at +180 & -180 longitude
					Projection.ProjectPoint(mapProjection, newPt1.X, newPt1.Y, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
					Gl.glEnd();
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Projection.ProjectPoint(mapProjection, newPt2.X, newPt2.Y, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
				}
				Projection.ProjectPoint(mapProjection, pointList[i].X, pointList[i].Y, centralLongitude, out px, out py);
				Gl.glVertex2d(px, py);
			}
			Gl.glEnd();

			// Disable stippling
			if (type == FrontType.Trough || type == FrontType.SquallLine)
				Gl.glDisable(Gl.GL_LINE_STIPPLE);

			// End the OpenGL display list
			Gl.glEndList();
		}

		private static void DrawColdPips()
		{
			// Create an OpenGL display list for the cold front pips
			openglDisplayListColdPips = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListColdPips, Gl.GL_COMPILE);

			// Create the cold front pip coordinates
			double[,] v = new double[3, 2];
			v[0, 0] = -12; v[0, 1] = 0;
			v[1, 0] = 12;  v[1, 1] = 0;
			v[2, 0] = 0;   v[2, 1] = -20;

			// Render the cold front pip into the display list
			Gl.glBegin(Gl.GL_TRIANGLES);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the OpenGL display list
			Gl.glEndList();
		}

		private static void DrawWarmPips()
		{
			// Create an OpenGL display list for the warm front pips
			openglDisplayListWarmPips = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListWarmPips, Gl.GL_COMPILE);

			// Create the warm front pip coordinates
			double[,] v = new double[7, 2];
			v[0, 0] = -12;   v[0, 1] = 0;
			v[1, 0] = 12;    v[1, 1] = 0;
			v[2, 0] = 10.4;  v[2, 1] = -6;
			v[3, 0] = 6;     v[3, 1] = -10.4;
			v[4, 0] = 0;     v[4, 1] = -12;
			v[5, 0] = -6;    v[5, 1] = -10.4;
			v[6, 0] = -10.4; v[6, 1] = -6;

			// Render the warm front pip into the display list
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i = 0; i < 7; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the OpenGL display list
			Gl.glEndList();
		}

		private static void DrawDryLinePips()
		{
			// Create an OpenGL display list for the dry line pips
			openglDisplayListDryLinePips = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListDryLinePips, Gl.GL_COMPILE);

			// Create the dry line pip coordinates
			double[,] v = new double[7, 2];
			v[0, 0] = -12;   v[0, 1] = 0;
			v[1, 0] = 12;    v[1, 1] = 0;
			v[2, 0] = 10.4;  v[2, 1] = -6;
			v[3, 0] = 6;     v[3, 1] = -10.4;
			v[4, 0] = 0;     v[4, 1] = -12;
			v[5, 0] = -6;    v[5, 1] = -10.4;
			v[6, 0] = -10.4; v[6, 1] = -6;

			// Render the dry line pip into the display list
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 7; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the OpenGL display list
			Gl.glEndList();
		}
	}
}

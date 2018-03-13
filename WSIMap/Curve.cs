using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Tao.OpenGl;
using FUL;
using System.Linq;

namespace WSIMap
{
	/**
	 * \class Curve
	 * \brief Represents a curve (set of points) on the map
	 */
	[Serializable]
	public class Curve : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		private const int MAX_INTERPOLATED_POINTS = 500;
		protected List<PointD> pointList;       // list of points set by the user
		protected List<PointD> pointListToDraw; // user point list plus points added for great circles
		protected Color color;
		protected Color color2;
		protected Color color3;
		protected int color2PVertexIndex;
		protected int color2PVertexIndexToDraw;
		protected int color3PVertexIndex;
		protected int color3PVertexIndexToDraw;
		protected bool outlined;
		protected Color outlineColor;
		protected uint width;
		protected CurveType type;
		protected InterpolationMethodType interpolationMethod;
		protected int stippleFactor;
		protected ushort stipplePattern;
		private bool isCrossIDL = false;
		private bool immediateMode;
		private bool glLineSmoothing;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		private const string TRACKING_CONTEXT = "Curve";
		#endregion

		public enum CurveType { Solid, Points, SolidWithPoints, Dashed, Dotted, DashDot, LongDash, Custom };

		public enum InterpolationMethodType { GreatCircle, SplineFit, Linear };

		public Curve() : this(new ArrayList(), Color.White, 1, CurveType.Solid)
		{
		}

		public Curve(Color color, uint width, CurveType type) : this(new ArrayList(), color, width, type)
		{
		}

		public Curve(ArrayList pointList, Color color, uint width, CurveType type)
			: this(new List<PointD>(pointList.Cast<PointD>()), color, width, type)
		{
		}

		public Curve(List<PointD> pointList, Color color, uint width, CurveType type)
		{
			this.pointListToDraw = new List<PointD>();
			this.pointList = new List<PointD>(pointList);
			this.color = color;
			this.color2 = Color.Empty;
			this.color3 = Color.Empty;
			this.color2PVertexIndex = 0;
			this.color3PVertexIndex = 0;
			this.outlined = false;
			this.outlineColor = Color.White;
			this.width = width;
			this.type = type;
			this.stippleFactor = 1;
			this.stipplePattern = 0x00FF;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.immediateMode = false;
			this.glLineSmoothing = true;
			this.interpolationMethod = InterpolationMethodType.GreatCircle;
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

		public bool ImmediateMode
		{
			get { return immediateMode; }
			set { immediateMode = value; }
		}

		public bool LineSmoothing
		{
			get { return glLineSmoothing; }
			set { glLineSmoothing = value; }
		}

		public int Count
		{
			get { return pointList.Count; }
		}

		public Color Color
		{
			get { return color; }
			set { color = value; Updated = true; }
		}

		public Color Color2
		{
			get { return color2; }
			set { color2 = value; Updated = true; }
		}

		public Color Color3
		{
			get { return color3; }
			set { color3 = value; Updated = true; }
		}

		public int Color2VertexIndex
		{
			get { return color2PVertexIndex; }
			set { color2PVertexIndex = value; Updated = true; }
		}

		public int Color3VertexIndex
		{
			get { return color3PVertexIndex; }
			set { color3PVertexIndex = value; Updated = true; }
		}

		public bool Outlined
		{
			get { return outlined; }
			set { outlined = value; Updated = true; }
		}

		public Color OutlineColor
		{
			get { return outlineColor; }
			set { outlineColor = value; Updated = true; }
		}

		public uint Width
		{
			get { return width; }
			set { width = value; Updated = true; }
		}

		public int StippleFactor
		{
			get { return stippleFactor; }
			set { stippleFactor = value; Updated = true; }
		}

		public ushort StipplePattern
		{
			get { return stipplePattern; }
			set { stipplePattern = value; Updated = true; }
		}

		public CurveType Type
		{
			get { return type; }
			set { type = value; Updated = true; }
		}

		public InterpolationMethodType InterpolationMethod
		{
			get { return interpolationMethod; }
			set { interpolationMethod = value; }
		}

		public List<PointD> PointList
		{
			get { return pointList; }
			set
			{
				pointList = new List<PointD>(value);
				Updated = true;
			}
		}

		public PointD this[int index]
		{
			get
			{
				if (index < 0 || index >= pointList.Count)
					return null;
				else
					return (PointD)pointList[index];
			}
		}

		public int Add(PointD pt)
		{
			if (double.IsNaN(pt.Latitude))
				throw new ArgumentException("Curve::Add() Invalid Latitiude: NaN");
			if (double.IsNaN(pt.Longitude))
				throw new ArgumentException("Curve::Add() Invalid Longitiude: NaN");
			Updated = true;
			pointList.Add(pt);
			return pointList.Count - 1; // return index of added point for backward compatibility
		}

		public void Insert(PointD pt, int vertexIndex)
		{
			if (vertexIndex >= 0 && vertexIndex < pointList.Count)
			{
				Updated = true;
				pointList.Insert(vertexIndex + 1, pt);
			}
		}

		public void Remove(PointD pt)
		{
			Updated = true;
			pointList.Remove(pt);
		}

		public void Remove(int vertexIndex)
		{
			if (vertexIndex >= 0 && vertexIndex < pointList.Count)
			{
				Updated = true;
				pointList.RemoveAt(vertexIndex);
			}
		}

		public void RemoveRange(int index, int count)
		{
			Updated = true;
			pointList.RemoveRange(index, count);
		}

		public void MoveVertexTo(int vertexIndex, PointD pt)
		{
			if (vertexIndex >= 0 && vertexIndex < pointList.Count)
			{
				Updated = true;
				pointList[vertexIndex].Latitude = pt.Latitude;
				pointList[vertexIndex].Longitude = pt.Longitude;
				pointList[vertexIndex].FeatureInfo = pt.FeatureInfo;
			}
		}

		public bool IsPointOnVertex(PointD pt, double distance, out int vertexIndex)
		{
			vertexIndex = -1;

			if (pointList == null || pointList.Count == 0)
				return false;

			for (int i = 0; i < pointList.Count; i++)
			{
				if (Utils.Distance(pointList[i].Latitude, pointList[i].Longitude, pt.Latitude, pt.Longitude, Utils.DistanceUnits.km) < distance)
				{
					vertexIndex = i;
					return true;
				}
			}

			return false;
		}

		public bool IsPointOn(PointD pt)
		{
			double distance;
			return IsPointOn(pt, out distance);
		}

		public bool IsPointOn(PointD pt, out double distance)
		{
			// Note: PerfTimer showed that, for input points near the end of
			// the Curve (requiring all segments to be checked) that return true,
			// this routine takes about 2.5 ms for a Curve containing 10,000 points.

			distance = double.MaxValue;
			bool isOnLine = false;

			// Need at least two points
			if (pointListToDraw.Count < 2)
				return isOnLine;

			PointD mouse = new PointD();
			mouse.Latitude = pt.Latitude;
			mouse.Longitude = ConvertLongitude(pt.Longitude);

			for (int i = 0; i < pointListToDraw.Count - 1; i++)
			{
				PointD newPt1 = new PointD();
				PointD newPt2 = new PointD();

				newPt1.Latitude = pointListToDraw[i].Latitude;
				newPt1.Longitude = ConvertLongitude(pointListToDraw[i].Longitude);
				newPt2.Latitude = pointListToDraw[i + 1].Latitude;
				newPt2.Longitude = ConvertLongitude(pointListToDraw[i + 1].Longitude);

				double d;
				if (StrictPointOnLine(newPt1.X, newPt1.Y, newPt2.X, newPt2.Y, mouse.X, mouse.Y, out d) == 2)
				{
					if (d < distance)
						distance = d;

					isOnLine = true;
				}
			}

			return isOnLine;
		}

		public bool IsPointOn(PointD pt, double distance)
		{
			int vertexIndex;
			return IsPointOn(pt, distance, out vertexIndex);
		}

		public bool IsPointOn(PointD pt, double distance, out int vertexIndex)
		{
			// Note: PerfTimer showed that, for input points near the end of
			// the Curve (requiring all segments to be checked) that return true,
			// this routine takes about 2.5 ms for a Curve containing 10,000 points.

			vertexIndex = int.MinValue;
			bool isOnLine = false;

			// Need at least two points
			if (pointListToDraw.Count < 2)
				return isOnLine;

			PointD mouse = new PointD();
			mouse.Latitude = pt.Latitude;
			mouse.Longitude = ConvertLongitude(pt.Longitude);

			try
			{
				for (int i = 0; i < pointListToDraw.Count - 1; i++)
				{
					PointD newPt1 = new PointD();
					PointD newPt2 = new PointD();

					newPt1.Latitude = pointListToDraw[i].Latitude;
					newPt1.Longitude = ConvertLongitude(pointListToDraw[i].Longitude);
					newPt2.Latitude = pointListToDraw[i + 1].Latitude;
					newPt2.Longitude = ConvertLongitude(pointListToDraw[i + 1].Longitude);

					// Find the actual curve point closest to but before the mouse position
					for (int j = 0; j < pointList.Count; j++)
					{
						if ((Math.Abs(newPt1.X - pointList[j].X) < double.Epsilon) && (Math.Abs(newPt1.Y - pointList[j].Y) < double.Epsilon))
						{
							vertexIndex = j;
							break;
						}
					}

					if (StrictPointOnLine(newPt1.X, newPt1.Y, newPt2.X, newPt2.Y, mouse.X, mouse.Y, distance) == 2)
						return true;
				}
			}
			catch { }

			return isOnLine;
		}

		public void ReducePoints(double tolerance)
		{
			List<PointD> reducedPointList = DouglasPeucker.DouglasPeuckerReduction(this.PointList, tolerance);
			this.PointList = reducedPointList;
		}

		protected int StrictPointOnLine(double px, double py, double qx, double qy, double tx, double ty, double distanceRange)
		{
			// From http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html

			if ((px == qx) && (py == qy))
			{
				if ((tx == px) && (ty == py))
					return 2;
				else
					return 0;
			}

			double range = 0.15;
			if (((qx < px) && ((px + range) < tx)) || ((qy < py) && ((py + range) < ty)))
				return (1);
			if (((tx < (px - range)) && (px < qx)) || ((ty < (py - range)) && (py < qy)))
				return (1);
			if (((px < qx) && ((qx + range) < tx)) || ((py < qy) && ((qy + range) < ty)))
				return (3);
			if (((tx < (qx - range)) && (qx < px)) || ((ty < (qy - range)) && (qy < py)))
				return (3);

			px *= deg2rad;
			py *= deg2rad;
			qx *= deg2rad;
			qy *= deg2rad;
			tx *= deg2rad;
			ty *= deg2rad;
			double distance = Math.Abs((py - ty) * (qx - px) - (qy - py) * (px - tx)) / Math.Sqrt((Math.Pow(px - qx, 2) + Math.Pow(py - qy, 2)));

			if (distance * 6378.137 <= distanceRange)
				return 2;

			return 0;
		}

		protected int StrictPointOnLine(double px, double py, double qx, double qy, double tx, double ty, out double distance)
		{
			// From http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html

			distance = double.MaxValue;

			if ((px == qx) && (py == qy))
			{
				if ((tx == px) && (ty == py))
				{
					distance = 0;
					return 2;
				}
				else
					return 0;
			}

			double range = 0.15;

			if (((qx < px) && ((px + range) < tx)) || ((qy < py) && ((py + range) < ty)))
				return (1);
			if (((tx < (px - range)) && (px < qx)) || ((ty < (py - range)) && (py < qy)))
				return (1);
			if (((px < qx) && ((qx + range) < tx)) || ((py < qy) && ((qy + range) < ty)))
				return (3);
			if (((tx < (qx - range)) && (qx < px)) || ((ty < (qy - range)) && (qy < py)))
				return (3);

			distance = Math.Abs((py - ty) * (qx - px) - (qy - py) * (px - tx)) / Math.Sqrt((Math.Pow(px - qx, 2) + Math.Pow(py - qy, 2)));

			if (distance <= range)
				return 2;

			return 0;
		}

		/// <summary>
		/// Given a start lat/lon, an end lat/lon, and a distance between, calculate all the points along a great circle route between the points.
		/// From http://williams.best.vwh.net/avform.htm.
		/// </summary>
		/// <param name="pt1">starting point</param>
		/// <param name="pt2">ending point</param>
		/// <param name="distbtwn">distance between points in degrees</param>
		/// <param name="points">return the list of points in degrees</param>
		public static void GetPointsAlongGC(PointD pt1, PointD pt2, double distbtwn, ref List<PointD> points)
		{
			if (points == null)
				points = new List<PointD>();
			else if (points.Count > 0)
				points.Clear();

			// Assign lat/lon values
			double dlat1 = pt1.Latitude;
			double dlon1 = pt1.Longitude;
			double dlat2 = pt2.Latitude;
			double dlon2 = pt2.Longitude;

			// Check if both points are the same, otherwise cause NaN error
			if (dlat1 == dlat2 && dlon1 == dlon2)
			{
				return;
			}

			// Convert lat/lon to radians
			double rlat1 = dlat1 * deg2rad;
			double rlon1 = -1 * dlon1 * deg2rad;
			double rlat2 = dlat2 * deg2rad;
			double rlon2 = -1 * dlon2 * deg2rad;

			// Make sure the points are not antipodal
			if (dlat1 + dlat2 == 0 && Math.Abs(rlon1 - rlon2) == Math.PI)
				return;

			// Calculate the step size
			double rdistbtwn = distbtwn * deg2rad;
			double increm = (100.0 / (GetDistance(dlat1, dlon1, dlat2, dlon2) / rdistbtwn)) / 100.0;
			double A = 0.0;
			double B = 0.0;
			double x = 0.0;
			double y = 0.0;
			double z = 0.0;
			double f_increm = 0.0;

			// Calculate the intermediate points
			while (true)
			{
				A = Math.Sin((1 - f_increm) * rdistbtwn) / Math.Sin(rdistbtwn);
				B = Math.Sin(f_increm * rdistbtwn) / Math.Sin(rdistbtwn);
				x = A * Math.Cos(rlat1) * Math.Cos(rlon1) + B * Math.Cos(rlat2) * Math.Cos(rlon2);
				y = A * Math.Cos(rlat1) * Math.Sin(rlon1) + B * Math.Cos(rlat2) * Math.Sin(rlon2);
				z = A * Math.Sin(rlat1) + B * Math.Sin(rlat2);
				double rlat = Math.Atan2(z, Math.Sqrt((x * x) + (y * y)));
				double rlon = Math.Atan2(y, x);
				double dlat = rlat / deg2rad;
				double dlon = -1 * rlon / deg2rad;
				points.Add(new PointD(dlon, dlat));
				f_increm += increm;
				double dist2end = GetDistance(dlat, dlon, dlat2, dlon2);
				if (dist2end <= rdistbtwn)
					break;
				else if (f_increm > 1000)   // in case something goes wrong, get out
					break;
			}
		}

		/// <summary>
		/// Get the straight line distance between point 1 and point 2.
		/// </summary>
		/// <param name="dlat1">Latitude of point 1 in degrees</param>
		/// <param name="dlon1">Longitude of point 1 in degrees</param>
		/// <param name="dlat2">Latitude of point 2 in degrees</param>
		/// <param name="dlon2">Longitude of point 2 in degrees</param>
		/// <returns>The distance in radians.</returns>
		internal static double GetDistance2(double dlat1, double dlon1, double dlat2, double dlon2)
		{
			// Convert lat/lon to radians.
			double RadLat1 = dlat1 * deg2rad;
			double RadLon1 = dlon1 * deg2rad;
			double RadLat2 = dlat2 * deg2rad;
			double RadLon2 = dlon2 * deg2rad;

			// Initialize lat/lon deltas.
			double DiffLat = 0.0;
			double DiffLon = 0.0;

			// Calculate Latitude Delta in radians.  Deal with different hemispheres.  Don't wrap around poles.  
			// Always go north/south.

			// Both points in Northern Hemi
			if (RadLat1 >= 0.0 && RadLat2 >= 0.0)
			{
				if (RadLat2 > RadLat1)
					DiffLat = RadLat2 - RadLat1;
				else
					DiffLat = RadLat1 - RadLat2;
			}
			// Both points in Southern Hemi
			else if (RadLat1 < 0.0 && RadLat2 < 0.0)
			{
				if (RadLat2 < RadLat1)
					DiffLat = RadLat2 - RadLat1;
				else
					DiffLat = RadLat1 - RadLat2;
			}
			// One point in Northern Hemi, one point in the Soutern Hemi
			else
			{
				DiffLat = Math.Abs(RadLat1) + Math.Abs(RadLat2);
			}

			// Calculate longitude difference in radians.  Deal with different hemispheres.
			// Both Points in Eastern Hemi
			if (RadLon1 >= 0.0 && RadLon2 >= 0.0)
			{
				if (RadLon2 > RadLon1)
					DiffLon = RadLon2 - RadLon1;
				else
					DiffLon = RadLon1 - RadLon2;
			}
			// Both Points in Western Hemi
			else if (RadLon1 < 0.0 && RadLon2 < 0.0)
			{
				if (RadLon2 < RadLon1)
					DiffLon = RadLon2 - RadLon1;
				else
					DiffLon = RadLon1 - RadLon2;
			}
			// One point in Western Hemi, the other in the Eastern Hemi.   Deal with 0 and 180.  Select shortest
			// distance to wrap around the earth.
			else
			{
				double difflon1 = Math.Abs(RadLon1) + Math.Abs(RadLon2);
				double difflon2 = ((180.0 * deg2rad) - Math.Abs(RadLon1)) + ((180.0 * deg2rad) - Math.Abs(RadLon1));
				if (difflon1 < difflon2)
					DiffLon = difflon1;
				else
					DiffLon = difflon2;
			}

			// Convert deltas to absolute values
			DiffLat = Math.Abs(DiffLat);
			DiffLon = Math.Abs(DiffLon);

			double dist = Math.Sqrt((DiffLat * DiffLat) + (DiffLon * DiffLon));
			return (dist);
		}

		/// <summary>
		/// Get the great circle distance between point 1 and point 2.
		/// </summary>
		/// <param name="dlat1">Latitude of point 1 in degrees</param>
		/// <param name="dlon1">Longitude of point 1 in degrees</param>
		/// <param name="dlat2">Latitude of point 2 in degrees</param>
		/// <param name="dlon2">Longitude of point 2 in degrees</param>
		/// <returns>The distance in radians.</returns>
		internal static double GetDistance(double dlat1, double dlon1, double dlat2, double dlon2)
		{
			double dist;
			double rlat1 = dlat1 * deg2rad;
			double rlon1 = dlon1 * deg2rad;
			double rlat2 = dlat2 * deg2rad;
			double rlon2 = dlon2 * deg2rad;
			dist = 2 * Math.Asin(Math.Sqrt((Math.Pow(Math.Sin((rlat1 - rlat2) / 2), 2)) + Math.Cos(rlat1) * Math.Cos(rlat2) * (Math.Pow(Math.Sin((rlon1 - rlon2) / 2), 2))));
			return dist;
		}

		public void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			if (immediateMode)
				return;

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

				// Do the actual OpenGL rendering
				Draw();

				// End the OpenGL display list
				Gl.glEndList();

				if (Updated)
					Updated = false;
			}
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Curve Draw()");
#endif

			if (glLineSmoothing)
				Gl.glEnable(Gl.GL_LINE_SMOOTH);
			else
				Gl.glDisable(Gl.GL_LINE_SMOOTH);

			if (immediateMode)
				ImmediateModeDraw(parentMap, parentLayer);
			else
				RetainedModeDraw(parentMap, parentLayer);

			Gl.glDisable(Gl.GL_LINE_SMOOTH);
		}

		private void RetainedModeDraw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Curve RetainedModeDraw()");
#endif
			if (openglDisplayList == -1)
				return;

			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180 || isCrossIDL)
				MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right, isCrossIDL);
			else
				Gl.glCallList(openglDisplayList);
		}

		private void ImmediateModeDraw(MapGL parentMap, Layer parentLayer)
		{
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;
			bool crossIDL = false;
			foreach (PointD p in pointListToDraw)
			{
				if ((p.Longitude > 180) || (p.Longitude < -180))
				{
					crossIDL = true;
					break;
				}
			}

			if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180 || crossIDL)
				DrawWithShift(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right, crossIDL);
			else
				Draw();
		}

		private void Draw()
		{
			double px, py;

#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Curve Draw()");
#endif

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Generate a point list that will be used for the actual drawing
			GeneratePointList();

			// Render the curve
			MapProjectionTypes mpType = Projection.GetProjectionType(mapProjection);
			if (Type == CurveType.Points)
			{
				// since we aren't connecting the points with a line, use pointList instead of pointListToDraw
				numVertices += Count;
				Gl.glPointSize(width * 2);
				Gl.glEnable(Gl.GL_POINT_SMOOTH);
				Gl.glBegin(Gl.GL_POINTS);
				Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));

				for (int i = 0; i < Count; i++)
				{
					if (color2 != Color.Empty && i == color2PVertexIndex + 1)
						Gl.glColor3f(glc(color2.R), glc(color2.G), glc(color2.B));
					if (color3 != Color.Empty && i == color3PVertexIndex + 1)
						Gl.glColor3f(glc(color3.R), glc(color3.G), glc(color3.B));
					if (mpType == MapProjectionTypes.Azimuthal && pointList[i].Y < Projection.MinAzimuthalLatitude)
						continue;
					Projection.ProjectPoint(mapProjection, pointList[i].X, pointList[i].Y, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
				}

				Gl.glEnd();
				Gl.glDisable(Gl.GL_POINT_SMOOTH);
			}
			else	// Solid, Dashed, Dotted, DashDot, Custom
			{
				PointD newPt1, newPt2;
				numVertices += Count;

				#region Set Stipple Pattern
				// Set stipple pattern
				if (Type == CurveType.Dashed || Type == CurveType.Dotted || Type == CurveType.LongDash ||
					Type == CurveType.DashDot || Type == CurveType.Custom)
				{
					switch (Type)
					{
						case CurveType.LongDash:
							stipplePattern = 0x3FFC;
							break;
						case CurveType.Dashed:
							stipplePattern = 0x00FF;
							break;
						case CurveType.Dotted:
							stipplePattern = 0x0C0C;
							break;
						case CurveType.DashDot:
							stipplePattern = 0x18FF;
							break;
						case CurveType.Custom:
							// stipple pattern set by user
							break;
						default:
							break;
					}
					Gl.glLineStipple(stippleFactor, stipplePattern);
					Gl.glEnable(Gl.GL_LINE_STIPPLE);
				}
				#endregion

				#region Fill Gaps in Outline
				// For wide curves, gaps can appear at the vertices when the curve bends sharply.  Draw
				// points at the vertices to fill those gaps.
				if (outlined && width >= 5)
				{
					Gl.glPointSize(width + 2);
					Gl.glEnable(Gl.GL_POINT_SMOOTH);
					Gl.glBegin(Gl.GL_POINTS);
					Gl.glColor3f(glc(outlineColor.R), glc(outlineColor.G), glc(outlineColor.B));
					for (int i = 0; i < Count; i++)
					{
						if (mpType == MapProjectionTypes.Azimuthal && pointList[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						Projection.ProjectPoint(mapProjection, pointList[i].X, pointList[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
					Gl.glDisable(Gl.GL_POINT_SMOOTH);
				}
				#endregion

				#region Draw Outline
				// Draw a wider version of the curve in the outline color; the normal curve will draw on top of this
				if (outlined)
				{
					Gl.glColor3f(glc(outlineColor.R), glc(outlineColor.G), glc(outlineColor.B));
					Gl.glLineWidth(width + 2);
					Gl.glBegin(Gl.GL_LINE_STRIP);
					for (int i = 0; i < pointListToDraw.Count; i++)
					{
						if (i != 0)
						{
							bool crossIDL = CrossesIDL(pointListToDraw[i - 1], pointListToDraw[i], out newPt1, out newPt2);
							if (crossIDL)
							{
								// The segment crosses the dateline, so split segment
								// and add points at +180 & -180 longitude
								if (!(mpType == MapProjectionTypes.Azimuthal && newPt1.Y < Projection.MinAzimuthalLatitude))
								{
									Projection.ProjectPoint(mapProjection, newPt1.X, newPt1.Y, centralLongitude, out px, out py);
									Gl.glVertex2d(px, py);
									Gl.glEnd();
									Gl.glBegin(Gl.GL_LINE_STRIP);
									Projection.ProjectPoint(mapProjection, newPt2.X, newPt2.Y, centralLongitude, out px, out py);
									Gl.glVertex2d(px, py);
								}

								isCrossIDL = crossIDL;
							}
						}
						if (mpType == MapProjectionTypes.Azimuthal && pointListToDraw[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						Projection.ProjectPoint(mapProjection, pointListToDraw[i].X, pointListToDraw[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
				}
				#endregion

				#region Fill Gaps in Curve
				// For wide curves, gaps can appear at the vertices when the curve bends sharply.  Draw
				// points at the vertices to fill those gaps.
				if (width >= 5)
				{
					Gl.glPointSize(width);
					Gl.glEnable(Gl.GL_POINT_SMOOTH);
					Gl.glBegin(Gl.GL_POINTS);
					Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
					for (int i = 0; i < Count; i++)
					{
						if (mpType == MapProjectionTypes.Azimuthal && pointList[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						Projection.ProjectPoint(mapProjection, pointList[i].X, pointList[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
					Gl.glDisable(Gl.GL_POINT_SMOOTH);
				}
				#endregion

				#region Draw Curve
				// Draw the curve
				Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
				Gl.glLineWidth(width);
				Gl.glBegin(Gl.GL_LINE_STRIP);
				for (int i = 0; i < pointListToDraw.Count; i++)
				{
					if (color2 != Color.Empty && i == color2PVertexIndexToDraw + 1)
						Gl.glColor3f(glc(color2.R), glc(color2.G), glc(color2.B));
					if (color3 != Color.Empty && i == color3PVertexIndexToDraw + 1)
						Gl.glColor3f(glc(color3.R), glc(color3.G), glc(color3.B));

					if (i != 0)
					{
						bool crossIDL = CrossesIDL(pointListToDraw[i - 1], pointListToDraw[i], out newPt1, out newPt2);
						if (crossIDL)
						{
							// The segment crosses the dateline, so split segment
							// and add points at +180 & -180 longitude
							if (!(mpType == MapProjectionTypes.Azimuthal && newPt1.Y < Projection.MinAzimuthalLatitude))
							{
								Projection.ProjectPoint(mapProjection, newPt1.X, newPt1.Y, centralLongitude, out px, out py);
								Gl.glVertex2d(px, py);
								Gl.glEnd();
								Gl.glBegin(Gl.GL_LINE_STRIP);
								Projection.ProjectPoint(mapProjection, newPt2.X, newPt2.Y, centralLongitude, out px, out py);
								Gl.glVertex2d(px, py);
							}

							isCrossIDL = crossIDL;
						}
					}
					if (mpType == MapProjectionTypes.Azimuthal && pointListToDraw[i].Y < Projection.MinAzimuthalLatitude)
						continue;
					Projection.ProjectPoint(mapProjection, pointListToDraw[i].X, pointListToDraw[i].Y, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
				}
				Gl.glEnd();
				if (Type == CurveType.Dashed || Type == CurveType.Dotted || Type == CurveType.LongDash ||
					Type == CurveType.DashDot || Type == CurveType.Custom)
					Gl.glDisable(Gl.GL_LINE_STIPPLE);
				#endregion

				#region Draw Points
				// Draw points on top of the curve
				if (Type == CurveType.SolidWithPoints)
				{
					Gl.glPointSize(width * 3);
					Gl.glEnable(Gl.GL_POINT_SMOOTH);
					Gl.glBegin(Gl.GL_POINTS);
					Gl.glColor3f(glc(color.R), glc(color.G), glc(color.B));
					for (int i = 0; i < Count; i++)
					{
						if (color2 != Color.Empty && i == color2PVertexIndex + 1)
							Gl.glColor3f(glc(color2.R), glc(color2.G), glc(color2.B));
						if (color3 != Color.Empty && i == color3PVertexIndex + 1)
							Gl.glColor3f(glc(color3.R), glc(color3.G), glc(color3.B));
						if (mpType == MapProjectionTypes.Azimuthal && pointList[i].Y < Projection.MinAzimuthalLatitude)
							continue;
						Projection.ProjectPoint(mapProjection, pointList[i].X, pointList[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
					Gl.glDisable(Gl.GL_POINT_SMOOTH);
				}
				#endregion
			}
		}

		private void GeneratePointList()
		{
			pointListToDraw.Clear();

			// Generate a point list that will be used for the actual drawing based on the interpolation method
			switch (interpolationMethod)
			{
				case InterpolationMethodType.GreatCircle:
					GenerateGreatCirclePoints();
					break;
				case InterpolationMethodType.SplineFit:
					GenerateCubicSplinePoints();
					break;
				case InterpolationMethodType.Linear:
					GenerateLinearPoints();
					break;
				default:
					pointListToDraw.AddRange(pointList);
					break;
			}
		}

		private void GenerateGreatCirclePoints()
		{
			color2PVertexIndexToDraw = 0;
			color3PVertexIndexToDraw = 0;

			// Generate extra points for great circle
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				pointListToDraw.Add(pointList[i]);

				if (color2PVertexIndex == i)
					color2PVertexIndexToDraw = pointListToDraw.Count - 1;
				if (color3PVertexIndex == i)
					color3PVertexIndexToDraw = pointListToDraw.Count - 1;

				List<PointD> points = new List<PointD>();
				GetPointsAlongGC(pointList[i], pointList[i + 1], 2.0, ref points);
				pointListToDraw.AddRange(points);
			}

			// Add the last point to the point list
			if (pointList.Count > 0)
			{
				pointListToDraw.Add(pointList[pointList.Count - 1]);
				if (color2PVertexIndex == (pointList.Count - 1))
					color2PVertexIndexToDraw = pointListToDraw.Count - 1;
				if (color3PVertexIndex == (pointList.Count - 1))
					color3PVertexIndexToDraw = pointListToDraw.Count - 1;
			}
		}

		private void GenerateCubicSplinePoints()
		{
			List<List<PointD>> listOfLists = new List<List<PointD>>();
			List<PointD> list = new List<PointD>();

			// If the curve crosses the IDL one or more times, split the list of points into segments
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				list.Add(pointList[i]);
				if (Curve.CrossesIDL(pointList[i].Longitude, pointList[i + 1].Longitude))
				{
					listOfLists.Add(list);
					list = new List<PointD>();
				}
			}
			list.Add(pointList[pointList.Count - 1]); // add the last point
			listOfLists.Add(list);

			// Do a spline fit for each segment and add it to the overall point list to draw
			foreach (List<PointD> l in listOfLists)
				pointListToDraw.AddRange(Spline.CubicSplineFit(l));
		}

		private void GenerateLinearPoints()
		{
			color2PVertexIndexToDraw = 0;
			color3PVertexIndexToDraw = 0;

			// Generate interpolated intermediate points.  This results in
			// segments that are properly curved in azimuthal projections.
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				pointListToDraw.Add(pointList[i]);

				if (color2PVertexIndex == i)
					color2PVertexIndexToDraw = pointListToDraw.Count - 1;
				if (color3PVertexIndex == i)
					color3PVertexIndexToDraw = pointListToDraw.Count - 1;

				double lon1 = pointList[i].Longitude;
				double lon2 = pointList[i + 1].Longitude;
				if (Curve.CrossesIDL(lon1, lon2))
				{
					if (lon1 > 0)
						lon1 -= 360;
					else
						lon2 -= 360;
				}

				int nPointsToAdd = (int)Math.Abs((lon2 - lon1) / 2);
				if (nPointsToAdd > 0 && nPointsToAdd < MAX_INTERPOLATED_POINTS)
				{
					double lonStep = (lon2 - lon1) / nPointsToAdd;
					double latStep = (pointList[i + 1].Latitude - pointList[i].Latitude) / nPointsToAdd;
					for (int j = 1; j < nPointsToAdd; j++)
					{
						PointD p = new PointD(MapGL.NormalizeLongitude(lon1 + (j * lonStep)), pointList[i].Y + (j * latStep));
						pointListToDraw.Add(p);
					}
				}
			}

			// Add the last point to the point list
			if (pointList.Count > 0)
			{
				pointListToDraw.Add(pointList[pointList.Count - 1]);
				if (color2PVertexIndex == (pointList.Count - 1))
					color2PVertexIndexToDraw = pointListToDraw.Count - 1;
				if (color3PVertexIndex == (pointList.Count - 1))
					color3PVertexIndexToDraw = pointListToDraw.Count - 1;
			}
		}

		internal void DrawWithShift(double left, double right, bool isCrossIDL)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Curve DrawWithShift()");
#endif

			int crossings = MapGL.GetNumberOfCrossingIDL(left, right);

			if (isCrossIDL)
			{
				// Draw left and right
				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				Draw();
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
				Draw();
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 2), 0.0f, 0.0f);
				Draw();
				Gl.glPopMatrix();

			}
			else
			{
				DrawWithShift(left, right);
			}
		}

		internal void DrawWithShift(double left, double right)
		{
			int crossings = MapGL.GetNumberOfCrossingIDL(left, right);

			double l = MapGL.NormalizeLongitude(left);
			double r = MapGL.NormalizeLongitude(right);

			// Crossing dateline
			if ((l - r) > -0.001)   // ignore small differences between l & r
			{
				// Draw left and right
				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				Draw();
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
				Draw();
				Gl.glPopMatrix();
			}
			else
			{
				if (crossings > 0)
				{
					if (l > (360 * (crossings - 1) + 180) && r < (360 * crossings + 180))
					{
						// draw right
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
						Draw();
						Gl.glPopMatrix();
					}
					else
					{
						// draw left
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
						Draw();
						Gl.glPopMatrix();
					}
				}
				else
				{
					if (l > (360 * crossings - 180) && r > (360 * (crossings + 1) - 180))
					{
						// draw left
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
						Draw();
						Gl.glPopMatrix();
					}
					else
					{
						// draw right
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
						Draw();
						Gl.glPopMatrix();
					}
				}
			}
		}

		public static bool CrossesIDL(PointD pt1, PointD pt2, out PointD newPt1, out PointD newPt2)
		{
			// What we want to do here is see if the longitudes of the 
			// points fall into the range 90 < lon1 <= 180 and
			// -180 <= lon2 < -90.  If the longitudes fall into this range
			// then we can assume the curve crosses the dateline discontinuity.
			// We then need to calculate the intersection of the curve with the
			// dateline so we can add two points to the curve - one at (-180,lat)
			// and the other at (180,lat).

			// Initialize the return points to null
			newPt1 = null;
			newPt2 = null;

			// Check for invalid longitudes
			if (Double.IsNaN(pt1.Longitude) || Double.IsNaN(pt2.Longitude))
				return false;

            var pt1Long = pt1.Longitude;
            var pt2Long = pt2.Longitude;
            if(pt1.Longitude > 180)
            {
                pt1Long = pt1.Longitude - 360;
            }
            if(pt2.Longitude > 180)
            {
                pt2Long = pt2.Longitude - 360;
            }

			// Check for a sign change between longitudes
			if (Math.Sign(pt1Long) == Math.Sign(pt2Long))
				return false;
                        

			// There is a sign change, so test for dateline crossing
			if (pt1Long < 0)
			{
				if (pt1Long >= -180 && pt1Long < -90 &&
					pt2Long > 90 && pt2Long <= 180)
				{
					double slope = 0;
					double deltax = ((180 - Math.Abs(pt1Long)) + (180 - Math.Abs(pt2Long)));
					if (deltax != 0)
						slope = (pt2.Latitude - pt1.Latitude) / deltax;
					double dy = (180 + pt1Long) * slope;
					newPt1 = new PointD(-180, pt1.Latitude + dy);
					newPt2 = new PointD(180, pt1.Latitude + dy);
					return true;
				}
			}
			else
			{
				if (pt2Long >= -180 && pt2Long < -90 &&
					pt1Long > 90 && pt1Long <= 180)
				{
					double slope = 0;
					double deltax = ((180 - Math.Abs(pt1Long)) + (180 - Math.Abs(pt2Long)));
					if (deltax != 0)
						slope = (pt2.Latitude - pt1.Latitude) / deltax;
					double dy = (180 - pt1Long) * slope;
					newPt1 = new PointD(180, pt1.Latitude + dy);
					newPt2 = new PointD(-180, pt1.Latitude + dy);
					return true;
				}
			}

			return false;
		}

		internal static bool CrossesIDL(double lon1, double lon2)
		{
			// What we want to do here is see if the longitudes of the 
			// points fall into the range 90 < lon1 <= 180 and
			// -180 <= lon2 < -90.  If the longitudes fall into this range
			// then we can assume the curve crosses the dateline discontinuity.

			// Check for invalid longitudes
			if (Double.IsNaN(lon1) || Double.IsNaN(lon2))
				return false;

			// Check for a sign change between longitudes
			if (lon1 * lon2 >= 0)
				return false;

			// There is a sign change, so test for dateline crossing
			if ((lon1 < 0 && lon1 >= -180 && lon1 < -90 && lon2 > 90 && lon2 <= 180) ||
				(lon1 >= 0 && lon2 >= -180 && lon2 < -90 && lon1 > 90 && lon1 <= 180))
				return true;

			return false;
		}

		public static List<PointD> AddGreatCirclePoints(List<PointD> pointList)
		{
			try
			{
				List<PointD> newPointList = new List<PointD>();
				for (int i = 0; i < pointList.Count - 1; i++)
				{
					List<PointD> points = new List<PointD>();
					newPointList.Add(pointList[i]);
					GetPointsAlongGC(pointList[i], pointList[i + 1], 2.0, ref points);
					newPointList.AddRange(points);
				}
				if (pointList.Count > 0)	// add the last point
				{
					newPointList.Add(pointList[pointList.Count - 1]);
				}
				return newPointList;
			}
			catch
			{
				return null;
			}
		}

		private double ConvertLongitude(double x)
		{
			if (isCrossIDL && (x > 0))
			{
				x -= 360;
			}

			return x;
		}

		public RectangleD GetBoundingRect(bool partial, MapGL parentMap, int margin, bool hasMarginLeft, bool hasMarginRight)
		{
			List<PointD> tempPointList;

			if (pointListToDraw.Count > 0)
				tempPointList = pointListToDraw;
			else
				tempPointList = pointList;

			PointD point = tempPointList[0];

			// Latitude -- Y; Longitude -- X
			double minX = ConvertLongitude(point.Longitude);
			double maxX = minX;
			double minY = point.Latitude;
			double maxY = point.Latitude;

			for (int i = 1; i < tempPointList.Count; i++)
			{
				point = tempPointList[i];

				double longitude = ConvertLongitude(point.Longitude);

				if (longitude <= minX)
					minX = longitude;
				else if (longitude > maxX)
					maxX = longitude;

				if (point.Latitude <= minY)
					minY = point.Latitude;
				else if (point.Latitude > maxY)
					maxY = point.Latitude;
			}

			// Keep ratio
			double rectWidth = maxX - minX;
			double rectHeight = maxY - minY;

			double ratio = (double)(parentMap.Width - (hasMarginLeft ? margin : 0) - (hasMarginRight ? margin : 0)) / (double)parentMap.Height;

			if (rectHeight >= rectWidth)
			{
				double width = rectHeight * ratio;

				if (rectWidth < width)
				{
					double gap = (width - rectWidth) / 2;
                    minX -= gap;
					maxX += gap;
				}
				else if (rectWidth > width)
				{
					double gap = (rectWidth / ratio - rectHeight) / 2;
					minY -= gap;
					maxY += gap;
				}
			}
			else
			{
				double height = rectWidth / ratio;

				if (rectHeight < height)
				{
					double gap = (height - rectHeight) / 2;
                    minY -= gap;
					maxY += gap;
				}
				else if (rectHeight > height)
				{
					double gap = (rectHeight * ratio - rectWidth) / 2;
					minX -= gap;
					maxX += gap;
				}
			}

            rectWidth = maxX - minX;

            var wMarginAdjust = (2 * margin * rectWidth) / parentMap.Width;

            if (hasMarginRight)
            {
                minX -= wMarginAdjust * 1 / 4;
                maxX += wMarginAdjust * 3 / 4;

                rectWidth = maxX - minX;
            }

            wMarginAdjust = (2 * margin * rectWidth) / parentMap.Width;

            if (hasMarginLeft)
            {
                minX -= wMarginAdjust * 3 / 4;
                maxX += wMarginAdjust * 1 / 4;

                rectWidth = maxX - minX;
            }

            rectHeight = maxY - minY;

            if (partial)
            {
				if ((rectHeight > 2) || (rectWidth > 2))
				{
					double hAdjust = 0.2;
					double wAdjust = ratio * hAdjust;

					minY -= hAdjust;
					maxY += hAdjust;
					minX -= wAdjust;
					maxX += wAdjust;
				}
			}

			return new RectangleD(minY, maxY, minX, maxX);
		}
	}
}

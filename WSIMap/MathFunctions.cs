using System;
using System.Collections.Generic;
using System.Linq;
using FUL;

namespace WSIMap
{
	internal enum DistanceUnits { km, mi, nm };

    public struct LatLongStruct
	{
		public double Latitude;
		public double Longitude;
		public string Id;
		public double DistanceFromMapCenter;
        public RectangleD Rect;
	}

	public struct RectangleStruct
	{
		public LatLongStruct PointA;
		public LatLongStruct PointB;
		public LatLongStruct PointC;
		public LatLongStruct PointD;
	}

	public class DouglasPeucker
	{
		/// <summary>
		/// Uses the Douglas Peucker algorithim to reduce the number of points.
		/// </summary>
		/// <param name="Points">The points.</param>
		/// <param name="Tolerance">The tolerance.</param>
		/// <returns></returns>
		public static List<PointD> DouglasPeuckerReduction(List<PointD> Points, Double Tolerance)
		{

			if (Points == null || Points.Count < 3)
				return Points;

			Int32 firstPoint = 0;
			Int32 lastPoint = Points.Count - 1;
			List<Int32> pointIndexsToKeep = new List<Int32>();

			//Add the first and last index to the keepers
			pointIndexsToKeep.Add(firstPoint);
			pointIndexsToKeep.Add(lastPoint);


			//The first and the last point can not be the same
			while (Points[firstPoint].Equals(Points[lastPoint]))
			{
				lastPoint--;
			}

			DouglasPeuckerReduction(Points, firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);

			List<PointD> returnPoints = new List<PointD>();
			pointIndexsToKeep.Sort();
			foreach (Int32 index in pointIndexsToKeep)
			{
				returnPoints.Add(Points[index]);
			}

			return returnPoints;
		}

		/// <summary>
		/// Douglases the peucker reduction.
		/// </summary>
		/// <param name="points">The points.</param>
		/// <param name="firstPoint">The first point.</param>
		/// <param name="lastPoint">The last point.</param>
		/// <param name="tolerance">The tolerance.</param>
		/// <param name="pointIndexsToKeep">The point indexs to keep.</param>
		private static void DouglasPeuckerReduction(List<PointD> points, Int32 firstPoint, Int32 lastPoint, Double tolerance, ref List<Int32> pointIndexsToKeep)
		{
			Double maxDistance = 0;
			Int32 indexFarthest = 0;

			for (Int32 index = firstPoint; index < lastPoint; index++)
			{
				Double distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
				if (distance > maxDistance)
				{
					maxDistance = distance;
					indexFarthest = index;
				}
			}

			if (maxDistance > tolerance && indexFarthest != 0)
			{
				//Add the largest point that exceeds the tolerance
				pointIndexsToKeep.Add(indexFarthest);

				DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
				DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
			}
		}

		/// <summary>
		/// The distance of a point from a line made from point1 and point2.
		/// </summary>
		/// <param name="pt1">The PT1.</param>
		/// <param name="pt2">The PT2.</param>
		/// <param name="p">The p.</param>
		/// <returns></returns>
		private static Double PerpendicularDistance(PointD Point1, PointD Point2, PointD Point)
		{
			//Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
			//Base = sqrt((x1-x2)^2+(x1-x2)^2)                          *Base of Triangle
			//Area = .5*Base*H                                          *Solve for height
			//Height = Area/.5/Base

			Double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X * Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X * Point2.Y - Point1.X * Point.Y));
			Double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) + Math.Pow(Point1.Y - Point2.Y, 2));
			Double height = area / bottom * 2;

			return height;
		}
	}

	public class Spline
	{
		public static List<PointD> CubicSplineFit(List<PointD> points)
		{
			// Spline cannot be calculated with less than two points
			if (points.Count < 2)
				return points;

			List<PointD> spline = new List<PointD>();
			double[] x = new double[points.Count];
			double[] y = new double[points.Count];
			double[] xs, ys;
			int index = 0;

			// Create the input arrays
			foreach (PointD p in points)
			{
				x[index] = p.Longitude;
				y[index] = p.Latitude;
				index++;
			}

			// Do the cubic spline fit. Use 40x points - this looks reasonably smooth without going overboard on the number of points.
			CubicSpline.FitGeometric(x, y, points.Count * 40, out xs, out ys);

			// Fill the output point list
			for (int i = 0; i < xs.Length; i++)
				spline.Add(new PointD(xs[i], ys[i]));

			return spline;
		}
	}

	public class MathFunctions
    {
		/// <summary>
		/// This method returns a polygon that allows night areas to be shaded on a MapGL control. The resulting polygon is approximate.
		/// This method should be called periodically (up to once per minute) to update the shaded area of the map.  Note that the resulting
		/// polygon only renders properly for the Cylindrical Equidistant map projection.
		/// 
		/// From http://aa.quae.nl/en/antwoorden/zonpositie.html#23 and http://pveducation.org/pvcdrom/properties-of-sunlight/declination-angle
		/// </summary>
		/// <param name="time">The date and time used to generate the polygon.</param>
		/// <returns>A Polygon that can be used to show night areas on a MapGL control or an empty Polygon if an exception occurs.</returns>
		public static Polygon GenerateNightPolygon(DateTime utcTime)
		{
			try
			{
				// Calculate the declination of the sun (the sign is opposite from normal convention to in order to properly calulate the sun's terminator)
				double b = -Math.Asin(Math.Sin(23.45 * FUL.Utils.deg2rad) * Math.Sin((360.0 / 365.0) * (utcTime.DayOfYear - 81) * FUL.Utils.deg2rad));
				if (b == 0.0) b = 0.001; // avoid degenerate polygon at equinox

				// Calculate the longitude offset based on the time of day
				double l = -15.0 * (utcTime.Hour + (utcTime.Minute / 60.0)) * FUL.Utils.deg2rad;

				// Iterate a circle, calculate the lat/lon values of the sun's terminator and add the points to the polygon
				Polygon night = new Polygon();
				for (int i = 0; i < 360; i += 2)
				{
					double psi = i * FUL.Utils.deg2rad;
					double B = Math.Asin(Math.Cos(b) * Math.Sin(psi));
					double x = (-Math.Cos(l) * Math.Sin(b) * Math.Sin(psi)) - (Math.Sin(l) * Math.Cos(psi));
					double y = (-Math.Sin(l) * Math.Sin(b) * Math.Sin(psi)) + (Math.Cos(l) * Math.Cos(psi));
					double L = Math.Atan2(y, x);

					night.Add(new PointD(L * FUL.Utils.rad2deg, B * FUL.Utils.rad2deg));
				}

				// Sort the polygon points and insert new endpoints so the polygon fills properly. (Note: The interpolated latitude for the inserted point
				// should be based on the intersection of line segments rather than a linear interpolation.  Linear is simpler and good enough.)
				night.PointList = night.PointList.OrderBy(o => o.Longitude).ToList();
				double interpolatedLat = (night.PointList[0].Latitude + night.PointList[night.PointList.Count - 1].Latitude) / 2.0;
				night.PointList.Insert(0, new PointD(-180, interpolatedLat));
				night.PointList.Insert(0, new PointD(-180, Math.Sign(b) * 90));
				night.PointList.Add(new PointD(180, interpolatedLat));
				night.PointList.Add(new PointD(180, Math.Sign(b) * 90));

				// Return the polygon
				return night;
			}
			catch
			{
				return new Polygon();
			}
		}

		internal static double DegreesToRadians(double degrees)  // degrees to radians
        {
            return degrees * Math.PI / 180.0;
        }

		internal static double RadiansToDegrees(double radians) // radians to degrees
        {
            return radians * 180.0 / Math.PI;
        }

        private static double Sin(double angleInDergees)
        {
            return Math.Sin(DegreesToRadians(angleInDergees));
        }

        private static double Pow2(double val)
        {
            return Math.Pow(val, 2);
        }

        private static void UpdateRectangleFromCornerA(ref RectangleStruct rectangle, double width, double height)
        {
            rectangle.PointB.Latitude = rectangle.PointA.Latitude;
            rectangle.PointB.Longitude = rectangle.PointA.Longitude + width;

            rectangle.PointC.Latitude = rectangle.PointA.Latitude + height;
            rectangle.PointC.Longitude = rectangle.PointB.Longitude;

            rectangle.PointD.Latitude = rectangle.PointC.Latitude;
            rectangle.PointD.Longitude = rectangle.PointA.Longitude;
        }

        private static void UpdateRectangleFromCornerB(ref RectangleStruct rectangle, double width, double height)
        {
            rectangle.PointA.Longitude = rectangle.PointB.Longitude - width;
            rectangle.PointA.Latitude = rectangle.PointB.Latitude;

            rectangle.PointC.Latitude = rectangle.PointA.Latitude + height;
            rectangle.PointC.Longitude = rectangle.PointB.Longitude;

            rectangle.PointD.Latitude = rectangle.PointC.Latitude;
            rectangle.PointD.Longitude = rectangle.PointA.Longitude;
        }

        private static void UpdateRectangleFromCornerC(ref RectangleStruct rectangle, double width, double height)
        {
            rectangle.PointA.Longitude = rectangle.PointC.Longitude - width;
            rectangle.PointA.Latitude = rectangle.PointC.Latitude - height;

            rectangle.PointB.Latitude = rectangle.PointA.Latitude;
            rectangle.PointB.Longitude = rectangle.PointC.Longitude;

            rectangle.PointD.Latitude = rectangle.PointC.Latitude;
            rectangle.PointD.Longitude = rectangle.PointA.Longitude;
        }

        private static void UpdateRectangleFromCornerD(ref RectangleStruct rectangle, double width, double height)
        {
            rectangle.PointA.Latitude = rectangle.PointD.Latitude - height;
            rectangle.PointA.Longitude = rectangle.PointD.Longitude;

            rectangle.PointB.Latitude = rectangle.PointA.Latitude;
            rectangle.PointB.Longitude = rectangle.PointA.Longitude + width;

            rectangle.PointC.Latitude = rectangle.PointD.Latitude;
            rectangle.PointC.Longitude = rectangle.PointB.Longitude;
        }

        #region quadrant 1

        private static RectangleStruct Bearings_1_1(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            var bearingSlope1 = Math.Tan(DegreesToRadians(BearingLine1));
            var bearingSlope2 = Math.Tan(DegreesToRadians(BearingLine2));

            // Offsets to top left corner - PointA
            var xOffset = (LabelBoxHeight + bearingSlope1 * LabelBoxWidth) / (bearingSlope2 - bearingSlope1);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointA.Longitude = AircraftPosition.Longitude + xOffset;
            rectangle.PointA.Latitude = AircraftPosition.Latitude - yOffset;

            UpdateRectangleFromCornerA(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_1_2(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            var bearingSlope1 = Math.Tan(DegreesToRadians(BearingLine1));
            var bearingSlope2 = Math.Tan(DegreesToRadians(BearingLine2));

            // Offsets to bottom left corner - PointD
            var xOffset = bearingSlope1 * LabelBoxWidth / (-bearingSlope1 + bearingSlope2);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointD.Longitude = AircraftPosition.Longitude + xOffset;
            rectangle.PointD.Latitude = AircraftPosition.Latitude - yOffset;

            UpdateRectangleFromCornerD(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_1_3(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            rectangle.PointC.Latitude = AircraftPosition.Latitude;
            rectangle.PointC.Longitude = AircraftPosition.Longitude;

            UpdateRectangleFromCornerC(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        #endregion

        #region quadrant 2

        private static RectangleStruct Bearings_2_2(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            // Translate angles to 1st quadrant and do similar math. Y will be the same, X should be mirrored later.
            var bearingSlope1 = Math.Tan(DegreesToRadians(180 - BearingLine2));
            var bearingSlope2 = Math.Tan(DegreesToRadians(180 - BearingLine1));

            // Offsets to top left corner - PointB
            var xOffset = (LabelBoxHeight + bearingSlope1 * LabelBoxWidth) / (bearingSlope2 - bearingSlope1);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointB.Longitude = AircraftPosition.Longitude - xOffset;
            rectangle.PointB.Latitude = AircraftPosition.Latitude - yOffset;

            UpdateRectangleFromCornerB(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_2_3(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            // Pretend that we're in quadrants 1 and 2. 
            var bearingSlope1 = Math.Tan(DegreesToRadians(BearingLine1 - 90));
            var bearingSlope2 = Math.Tan(DegreesToRadians(BearingLine2 - 90));

            // Use math similar to Bearings_1_2, but height instead of width, and x instead of y
            var yOffset = bearingSlope1 * LabelBoxHeight / (-bearingSlope1 + bearingSlope2);
            var xOffset = bearingSlope2 * yOffset;

            rectangle.PointC.Latitude = AircraftPosition.Latitude - yOffset; // yOffset is xOffset from 1_2, i.e. it is negative, but should be positive, so use '-'
            rectangle.PointC.Longitude = AircraftPosition.Longitude - xOffset; // similar approash for xOffset

            UpdateRectangleFromCornerC(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_2_4(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            rectangle.PointB.Latitude = AircraftPosition.Latitude;
            rectangle.PointB.Longitude = AircraftPosition.Longitude;

            UpdateRectangleFromCornerB(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        #endregion

        #region quadrant 3

        private static RectangleStruct Bearings_3_3(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            // Rotate bearing lines, use 1_1 math, revert X and Y later
            var bearingSlope1 = Math.Tan(DegreesToRadians(BearingLine1 - 180));
            var bearingSlope2 = Math.Tan(DegreesToRadians(BearingLine2 - 180));

            // Offsets to bottom left corner - PointC
            var xOffset = (LabelBoxHeight + bearingSlope1 * LabelBoxWidth) / (bearingSlope2 - bearingSlope1);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointC.Longitude = AircraftPosition.Longitude - xOffset;
            rectangle.PointC.Latitude = AircraftPosition.Latitude + yOffset;

            UpdateRectangleFromCornerC(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_3_4(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            var bearingSlope1 = Math.Tan(DegreesToRadians(360 - BearingLine2));
            var bearingSlope2 = Math.Tan(DegreesToRadians(360 - BearingLine1));

            // Offsets to PointA
            var xOffset = bearingSlope1 * LabelBoxWidth / (-bearingSlope1 + bearingSlope2);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointA.Longitude = AircraftPosition.Longitude + xOffset;
            rectangle.PointA.Latitude = AircraftPosition.Latitude + yOffset;

            UpdateRectangleFromCornerA(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_3_1(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            rectangle.PointA.Latitude = AircraftPosition.Latitude;
            rectangle.PointA.Longitude = AircraftPosition.Longitude;

            UpdateRectangleFromCornerA(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        #endregion

        #region quadrant 4

        private static RectangleStruct Bearings_4_4(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            var bearingSlope1 = Math.Tan(DegreesToRadians(360 - BearingLine2));
            var bearingSlope2 = Math.Tan(DegreesToRadians(360 - BearingLine1));

            var xOffset = (LabelBoxHeight + bearingSlope1 * LabelBoxWidth) / (bearingSlope2 - bearingSlope1);
            var yOffset = bearingSlope2 * xOffset;

            rectangle.PointD.Longitude = AircraftPosition.Longitude + xOffset;
            rectangle.PointD.Latitude = AircraftPosition.Latitude + yOffset;

            UpdateRectangleFromCornerD(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_4_1(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            var bearingSlope1 = Math.Tan(DegreesToRadians(BearingLine1 - 270));
            var bearingSlope2 = Math.Tan(DegreesToRadians(BearingLine2 + 360 - 270));

            var yOffset = bearingSlope1 * LabelBoxHeight / (-bearingSlope1 + bearingSlope2);
            var xOffset = bearingSlope2 * yOffset;

            rectangle.PointA.Longitude = AircraftPosition.Longitude + xOffset;
            rectangle.PointA.Latitude = AircraftPosition.Latitude + yOffset;

            UpdateRectangleFromCornerA(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        private static RectangleStruct Bearings_4_2(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight)
        {
            var rectangle = new RectangleStruct();

            rectangle.PointD.Latitude = AircraftPosition.Latitude;
            rectangle.PointD.Longitude = AircraftPosition.Longitude;

            UpdateRectangleFromCornerD(ref rectangle, LabelBoxWidth, LabelBoxHeight);

            return rectangle;
        }

        #endregion

        public static RectangleStruct FindNewRectanglePosition(LatLongStruct AircraftPosition, int BearingLine1, int BearingLine2, double LabelBoxWidth, double LabelBoxHeight, double MaxLookRange, float MapScaleFactor, out double LongestDistance)
        {
            RectangleStruct Rectangle = new RectangleStruct();
            double slope1 = 0, slope2 = 0, sloped = 0, x = 0, y = 0, Distance;
            LatLongStruct ClosestPoint = new LatLongStruct();
            LatLongStruct RectangleCenter;
            LongestDistance = 10000;

            if ((BearingLine1 >= 0) && (BearingLine1 <= 90))
            {
                if ((BearingLine2 >= 0) && (BearingLine2 <= 90))
                {
                    Rectangle = Bearings_1_1(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);
                    
                    LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                    if (LongestDistance <= MaxLookRange)
                    {
                        // If Label box is too close to aircraft symbol, push it out.
                        try
                        {
                            RectangleCenter = GetRectangleCenter(Rectangle);
                            ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointD, Rectangle.PointC, RectangleCenter, out Distance);
                            Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                        }
                        catch { }
                    }
                }
                else
                {
                    if ((BearingLine2 > 90) && (BearingLine2 < 180))
                    {
                        Rectangle = Bearings_1_2(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                        LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                        if (LongestDistance <= MaxLookRange)
                        {
                            // If Label box is too close to aircraft symbol, push it out.
                            try
                            {
                                RectangleCenter = GetRectangleCenter(Rectangle);
                                ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointD, Rectangle.PointC, RectangleCenter, out Distance);
                                Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        if ((BearingLine2 > 180) && (BearingLine2 < 270))
                        {
                            Rectangle = Bearings_1_3(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                            LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                            if (LongestDistance <= MaxLookRange)
                            {
                                // If Label box is too close to aircraft symbol, push it out.
                                try
                                {
                                    RectangleCenter = GetRectangleCenter(Rectangle);
                                    ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointB, Rectangle.PointA, Rectangle.PointD, RectangleCenter, out Distance);
                                    Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                                }
                                catch { }
                            }

                        }
                    }
                }
            } // end-if  0 <= BearingLine1 <= 90

            if ((BearingLine1 > 90) && (BearingLine1 <= 180))
            {
                if ((BearingLine2 > 90) && (BearingLine2 <= 180))
                {
                    Rectangle = Bearings_2_2(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                    LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                    if (LongestDistance <= MaxLookRange)
                    {
                        // If Label box is too close to aircraft symbol, push it out.
                        try
                        {
                            RectangleCenter = GetRectangleCenter(Rectangle);
                            ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointD, Rectangle.PointC, RectangleCenter, out Distance);
                            Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                        }
                        catch { }
                    }

                }
                else
                {
                    if ((BearingLine2 > 180) && (BearingLine2 < 270))
                    {
                        Rectangle = Bearings_2_3(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                        LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                        if (LongestDistance <= MaxLookRange)
                        {
                            // If Label box is too close to aircraft symbol, push it out.
                            try
                            {
                                RectangleCenter = GetRectangleCenter(Rectangle);
                                ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointD, Rectangle.PointC, RectangleCenter, out Distance);
                                Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        if ((BearingLine2 > 270) && (BearingLine2 < 360))
                        {
                            Rectangle = Bearings_2_4(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                            LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                            if (LongestDistance <= MaxLookRange)
                            {
                                // If Label box is too close to aircraft symbol, push it out.
                                try
                                {
                                    RectangleCenter = GetRectangleCenter(Rectangle);
                                    ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointB, Rectangle.PointA, Rectangle.PointD, RectangleCenter, out Distance);
                                    Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                                }
                                catch { }
                            }

                        }
                    }
                }
            } // end-if  90 < BearingLine1 <= 180

            if ((BearingLine1 > 180) && (BearingLine1 <= 270))
            {
                if ((BearingLine2 > 180) && (BearingLine2 <= 270))
                {
                    Rectangle = Bearings_3_3(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                    LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                    if (LongestDistance <= MaxLookRange)
                    {
                        // If Label box is too close to aircraft symbol, push it out.
                        try
                        {
                            RectangleCenter = GetRectangleCenter(Rectangle);
                            ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointB, Rectangle.PointC, RectangleCenter, out Distance);
                            Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                        }
                        catch { }
                    }

                }
                else
                {
                    if ((BearingLine2 > 270) && (BearingLine2 < 360))
                    {
                        Rectangle = Bearings_3_4(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                        LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                        if (LongestDistance <= MaxLookRange)
                        {
                            // If Label box is too close to aircraft symbol, push it out.
                            try
                            {
                                RectangleCenter = GetRectangleCenter(Rectangle);
                                ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointB, Rectangle.PointC, RectangleCenter, out Distance);
                                Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        if ((BearingLine2 > 0) && (BearingLine2 < 90))
                        {
                            Rectangle = Bearings_3_1(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                            LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                            if (LongestDistance <= MaxLookRange)
                            {
                                // If Label box is too close to aircraft symbol, push it out.
                                try
                                {
                                    RectangleCenter = GetRectangleCenter(Rectangle);
                                    ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointB, Rectangle.PointC, Rectangle.PointD, RectangleCenter, out Distance);
                                    Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                                }
                                catch { }
                            }

                        }
                    }
                }
            } // end-if  180 < BearingLine1 <= 270

            if ((BearingLine1 > 270) && (BearingLine1 < 360))
            {
                if ((BearingLine2 > 270) && (BearingLine2 < 360))
                {
                    Rectangle = Bearings_4_4(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                    LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                    if (LongestDistance <= MaxLookRange)
                    {
                        // If Label box is too close to aircraft symbol, push it out.
                        try
                        {
                            RectangleCenter = GetRectangleCenter(Rectangle);
                            ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointB, Rectangle.PointC, Rectangle.PointD, RectangleCenter, out Distance);
                            Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                        }
                        catch { }
                    }

                }
                else
                {
                    if ((BearingLine2 > 0) && (BearingLine2 < 90))
                    {
                        Rectangle = Bearings_4_1(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                        LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                        if (LongestDistance <= MaxLookRange)
                        {
                            // If Label box is too close to aircraft symbol, push it out.
                            try
                            {
                                RectangleCenter = GetRectangleCenter(Rectangle);
                                ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointB, Rectangle.PointC, Rectangle.PointD, RectangleCenter, out Distance);
                                Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        if ((BearingLine2 > 90) && (BearingLine2 < 180))
                        {
                            Rectangle = Bearings_4_2(AircraftPosition, BearingLine1, BearingLine2, LabelBoxWidth, LabelBoxHeight);

                            LongestDistance = MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rectangle, true);
                            if (LongestDistance <= MaxLookRange)
                            {
                                // If Label box is too close to aircraft symbol, push it out.
                                try
                                {
                                    RectangleCenter = GetRectangleCenter(Rectangle);
                                    ClosestPoint = FindClosestPointOnLabelBox(AircraftPosition, Rectangle.PointA, Rectangle.PointD, Rectangle.PointC, RectangleCenter, out Distance);
                                    Rectangle = AdjustLabelBoxPosition(AircraftPosition, Rectangle, RectangleCenter, Distance, MapScaleFactor);
                                }
                                catch { }
                            }

                        }
                    }
                }
            } // end-if  270 < BearingLine1 <= 360

            return Rectangle;
        }

		internal static double FindDistanceToLabelBox(LatLongStruct BasePoint, RectangleStruct Rectangle, bool Longest)
        {
			// Algorithm Solution: Find the one solution where the Label Box (rectangle)
			// intersects both bearing lines such that two rectangle points fall on Bearing Line1 and Bearing Line2.
			// Bearing Line 1:		y1=m1x1 + b1 (referenced at the origin, b1=0)
			// Bearing Line 2:		y2=m2x2 + b2 (referenced at the origin, b2=0)
			// Label box diagnol:	yd=mdxd + bd 
			
			 double LongestDistance = 0, ShortestDistance = 100000, Distance = 0;

            Distance = Math.Sqrt(Math.Pow(BasePoint.Latitude - Rectangle.PointA.Latitude, 2) + Math.Pow(BasePoint.Longitude - Rectangle.PointA.Longitude, 2));
            if (Distance > LongestDistance)
                LongestDistance = Distance;
            if (Distance < ShortestDistance)
                ShortestDistance = Distance;

            Distance = Math.Sqrt(Math.Pow(BasePoint.Latitude - Rectangle.PointB.Latitude, 2) + Math.Pow(BasePoint.Longitude - Rectangle.PointB.Longitude, 2));
            if (Distance > LongestDistance)
                LongestDistance = Distance;
            if (Distance < ShortestDistance)
                ShortestDistance = Distance;

            Distance = Math.Sqrt(Math.Pow(BasePoint.Latitude - Rectangle.PointC.Latitude, 2) + Math.Pow(BasePoint.Longitude - Rectangle.PointC.Longitude, 2));
            if (Distance > LongestDistance)
                LongestDistance = Distance;
            if (Distance < ShortestDistance)
                ShortestDistance = Distance;

            Distance = Math.Sqrt(Math.Pow(BasePoint.Latitude - Rectangle.PointD.Latitude, 2) + Math.Pow(BasePoint.Longitude - Rectangle.PointD.Longitude, 2));
            if (Distance > LongestDistance)
                LongestDistance = Distance;
            if (Distance < ShortestDistance)
                ShortestDistance = Distance;

            if (Longest == true)
                return LongestDistance;
            else
                return ShortestDistance;

        } // End FindDistanceToLabelBox

		internal static LatLongStruct GetRectangleCenter(RectangleStruct Rectangle)
        {
            LatLongStruct LabelBoxDrawPoint = new LatLongStruct();

            LabelBoxDrawPoint.Latitude = ((Rectangle.PointA.Latitude - Rectangle.PointD.Latitude) / 2) + Rectangle.PointD.Latitude;
            LabelBoxDrawPoint.Longitude = ((Rectangle.PointB.Longitude - Rectangle.PointA.Longitude) / 2) + Rectangle.PointA.Longitude;
            return LabelBoxDrawPoint;
        }

		internal static LatLongStruct FindClosestPointOnLabelBox(LatLongStruct AircraftPosition, LatLongStruct BoxPoint1, LatLongStruct BoxPoint2, LatLongStruct BoxPoint3, LatLongStruct RectangleCenterPoint, out Double Distance)
        {
            // Find the point on the label box where the leader line is to be drawn from the plane.

            FUL.Coordinate PointA1, PointA2, PointB1, PointB2, IntersectPt1, IntersectPt2;
            Double DistanceToIntersect1, DistanceToIntersect2;
            LatLongStruct ClosestPoint = RectangleCenterPoint; // Default Center of Rectangle if no intersections with sides.
            Distance = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, RectangleCenterPoint.Latitude, RectangleCenterPoint.Longitude, FUL.Utils.DistanceUnits.nm);

            PointA1.Lat = AircraftPosition.Latitude;
            PointA1.Lon = AircraftPosition.Longitude;
            PointA2.Lat = RectangleCenterPoint.Latitude;
            PointA2.Lon = RectangleCenterPoint.Longitude;
            PointB1.Lat = BoxPoint2.Latitude;
            PointB1.Lon = BoxPoint2.Longitude;
            PointB2.Lat = BoxPoint1.Latitude;
            PointB2.Lon = BoxPoint1.Longitude;
            if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt1) == true)
            {
                ClosestPoint.Latitude = IntersectPt1.Lat;
                ClosestPoint.Longitude = IntersectPt1.Lon;
                DistanceToIntersect1 = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt1.Lat, IntersectPt1.Lon, FUL.Utils.DistanceUnits.nm);
                Distance = DistanceToIntersect1;
                PointB2.Lat = BoxPoint3.Latitude;
                PointB2.Lon = BoxPoint3.Longitude;
                if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt2) == true)
                {
                    DistanceToIntersect2 = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt2.Lat, IntersectPt2.Lon, FUL.Utils.DistanceUnits.nm);
                    if (DistanceToIntersect2 < DistanceToIntersect1)
                    {
                        ClosestPoint.Latitude = IntersectPt2.Lat;
                        ClosestPoint.Longitude = IntersectPt2.Lon;
                        Distance = DistanceToIntersect2;
                    }
                }
            }
            else
            {
                PointB2.Lat = BoxPoint3.Latitude;
                PointB2.Lon = BoxPoint3.Longitude;
                if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt2) == true)
                {
                    ClosestPoint.Latitude = IntersectPt2.Lat;
                    ClosestPoint.Longitude = IntersectPt2.Lon;
                    Distance = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt2.Lat, IntersectPt2.Lon, FUL.Utils.DistanceUnits.nm);
                }
            }

            return ClosestPoint;

        } // End FindClosestPointOnLabelBox

        private static double SimpleDistance(LatLongStruct a, LatLongStruct b)
        {
            return Math.Sqrt(Math.Pow(a.Latitude - b.Latitude, 2) + Math.Pow(a.Longitude - b.Longitude, 2));
        }

        private static double SimpleDistance(LatLongStruct a, Coordinate b)
        {
            return Math.Sqrt(Math.Pow(a.Latitude - b.Lat, 2) + Math.Pow(a.Longitude - b.Lon, 2));
        }

        internal static LatLongStruct FindClosestPointOnLabelBox2(LatLongStruct AircraftPosition, LatLongStruct BoxPoint1, LatLongStruct BoxPoint2, LatLongStruct BoxPoint3, LatLongStruct RectangleCenterPoint, out Double Distance)
        {
            // Find the point on the label box where the leader line is to be drawn from the plane.

            FUL.Coordinate PointA1, PointA2, PointB1, PointB2, IntersectPt1, IntersectPt2;
            Double DistanceToIntersect1, DistanceToIntersect2;
            LatLongStruct ClosestPoint = RectangleCenterPoint; // Default Center of Rectangle if no intersections with sides.
            Distance = SimpleDistance(AircraftPosition, RectangleCenterPoint);
            //Distance = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, RectangleCenterPoint.Latitude, RectangleCenterPoint.Longitude, FUL.Utils.DistanceUnits.nm);

            PointA1.Lat = AircraftPosition.Latitude;
            PointA1.Lon = AircraftPosition.Longitude;
            PointA2.Lat = RectangleCenterPoint.Latitude;
            PointA2.Lon = RectangleCenterPoint.Longitude;
            PointB1.Lat = BoxPoint2.Latitude;
            PointB1.Lon = BoxPoint2.Longitude;
            PointB2.Lat = BoxPoint1.Latitude;
            PointB2.Lon = BoxPoint1.Longitude;
            if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt1) == true)
            {
                ClosestPoint.Latitude = IntersectPt1.Lat;
                ClosestPoint.Longitude = IntersectPt1.Lon;
                //DistanceToIntersect1 = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt1.Lat, IntersectPt1.Lon, FUL.Utils.DistanceUnits.nm);
                DistanceToIntersect1 = SimpleDistance(AircraftPosition, IntersectPt1);

                Distance = DistanceToIntersect1;
                PointB2.Lat = BoxPoint3.Latitude;
                PointB2.Lon = BoxPoint3.Longitude;
                if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt2) == true)
                {
                    //DistanceToIntersect2 = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt2.Lat, IntersectPt2.Lon, FUL.Utils.DistanceUnits.nm);
                    DistanceToIntersect2 = SimpleDistance(AircraftPosition, IntersectPt2);
                    if (DistanceToIntersect2 < DistanceToIntersect1)
                    {
                        ClosestPoint.Latitude = IntersectPt2.Lat;
                        ClosestPoint.Longitude = IntersectPt2.Lon;
                        Distance = DistanceToIntersect2;
                    }
                }
            }
            else
            {
                PointB2.Lat = BoxPoint3.Latitude;
                PointB2.Lon = BoxPoint3.Longitude;
                if (FUL.Utils.LineSegmentsIntersect(PointA1, PointA2, PointB1, PointB2, true, out IntersectPt2) == true)
                {
                    ClosestPoint.Latitude = IntersectPt2.Lat;
                    ClosestPoint.Longitude = IntersectPt2.Lon;
                    //Distance = FUL.Utils.Distance(AircraftPosition.Latitude, AircraftPosition.Longitude, IntersectPt2.Lat, IntersectPt2.Lon, FUL.Utils.DistanceUnits.nm);
                    Distance = SimpleDistance(AircraftPosition, IntersectPt2);
                }
            }

            return ClosestPoint;

        } // End FindClosestPointOnLabelBox


        internal static RectangleStruct AdjustLabelBoxPosition(LatLongStruct AircraftPosition, RectangleStruct Rectangle, LatLongStruct RectangleCenter, Double Distance, float MapScaleFactor)
        {
            Double MinDistance = 0.25;
            RectangleStruct NewRectangle = Rectangle;

            if ((Distance / 60.0) < MinDistance)
            {
                Double AddLength = (MinDistance - (Distance/60));
                if (MapScaleFactor > 50)
                    AddLength = AddLength * 50 / MapScaleFactor;
                Double Delta_Y = RectangleCenter.Latitude - AircraftPosition.Latitude;
                Double Delta_X = RectangleCenter.Longitude - AircraftPosition.Longitude;
                Double Angle = Math.Atan2(Delta_Y, Delta_X);
                Double Y_Component = AddLength * Math.Sin(Angle);
                Double X_Component = AddLength * Math.Cos(Angle);

                NewRectangle.PointA.Latitude = Rectangle.PointA.Latitude + Y_Component;
                NewRectangle.PointA.Longitude = Rectangle.PointA.Longitude + X_Component;
                NewRectangle.PointB.Latitude = Rectangle.PointB.Latitude + Y_Component;
                NewRectangle.PointB.Longitude = Rectangle.PointB.Longitude + X_Component;
                NewRectangle.PointC.Latitude = Rectangle.PointC.Latitude + Y_Component;
                NewRectangle.PointC.Longitude = Rectangle.PointC.Longitude + X_Component;
                NewRectangle.PointD.Latitude = Rectangle.PointD.Latitude + Y_Component;
                NewRectangle.PointD.Longitude = Rectangle.PointD.Longitude + X_Component;
            }
            return NewRectangle;

        } // End AdjustLabelBoxPosition
    } // End class
} // End namespace

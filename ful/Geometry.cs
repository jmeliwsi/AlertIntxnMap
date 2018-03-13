using System;
using System.Collections.Generic;
using System.Text;

namespace FUL
{
    public class FPoint
    {
        private int x;  // microdegrees
        private int y;  // microdegrees
        private const double toDeg = 1000000.0;

        public FPoint() : this(0.0, 0.0)
        {
        }

        public FPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public double X
        {
            get { return (x / toDeg); }
            set { x = (int)(value * toDeg); }
        }

        public double Y
        {
            get { return (y / toDeg); }
            set { y = (int)(value * toDeg); }
        }

        public double Lat
        {
            get { return this.Y; }
            set { this.Y = value; }
        }

        public double Lon
        {
            get { return this.X; }
            set { this.X = value; }
        }

        public double DistanceTo(FPoint p, Utils.DistanceUnits units)
        {
            if (units == Utils.DistanceUnits.km)
                return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
            else if (units == Utils.DistanceUnits.nm)
                return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.nm);
            else
                return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
        }

        public override string ToString()
        {
            return string.Format("{0,7:###.00}, {1,6:##.00}", this.X, this.Y);
        }
    } // End FPoint class




    public static class Geometry
    {
        /// <summary>
        /// For a 2 point line, return 4 points at least OffsetDistance from the line (corridor)
        /// </summary>
        /// <param name="PointList"></param>
        /// <param name="OffsetDistance"></param>
        /// <returns></returns>
        public static List<FUL.Coordinate> GetLineCorridorPoints(List<FUL.Coordinate> PointList, double OffsetDistance)
        {
            List<FUL.Coordinate> ResultPoints = new List<FUL.Coordinate>();

            if (PointList.Count == 2)
            {
                double Range = OffsetDistance / 0.7071;
                FUL.Coordinate NewPoint;
                double NewLat = 0;
                double NewLon = 0;
                double NotUsed = 0;
                double BearingDegrees = 0;

                try
                {
                    if (FUL.Utils.RangeBearing(PointList[0].Lat, PointList[0].Lon, PointList[1].Lat, PointList[1].Lon, FUL.Utils.DistanceUnits.nm, ref NotUsed, ref BearingDegrees) == true)
                    {
                        BearingDegrees = BearingDegrees + 135;
                        if (BearingDegrees > 360)
                            BearingDegrees = BearingDegrees - 360;
                        FUL.Utils.RangeBearingToLatLon(PointList[0].Lat, PointList[0].Lon, Range, BearingDegrees, ref NewLat, ref NewLon);
                        NewPoint = new Coordinate(NewLon, NewLat);
                        ResultPoints.Add(NewPoint);

                        BearingDegrees = BearingDegrees - 270;
                        if (BearingDegrees < 0)
                            BearingDegrees = BearingDegrees + 360;
                        FUL.Utils.RangeBearingToLatLon(PointList[0].Lat, PointList[0].Lon, Range, BearingDegrees, ref NewLat, ref NewLon);
                        NewPoint = new Coordinate(NewLon, NewLat);
                        ResultPoints.Add(NewPoint);

                        if (FUL.Utils.RangeBearing(PointList[1].Lat, PointList[1].Lon, PointList[0].Lat, PointList[0].Lon, FUL.Utils.DistanceUnits.nm, ref NotUsed, ref BearingDegrees) == true)
                        {
                            BearingDegrees = BearingDegrees + 135;
                            if (BearingDegrees > 360)
                                BearingDegrees = BearingDegrees - 360;
                            FUL.Utils.RangeBearingToLatLon(PointList[1].Lat, PointList[1].Lon, Range, BearingDegrees, ref NewLat, ref NewLon);
                            NewPoint = new Coordinate(NewLon, NewLat);
                            ResultPoints.Add(NewPoint);

                            BearingDegrees = BearingDegrees - 270;
                            if (BearingDegrees < 0)
                                BearingDegrees = BearingDegrees + 360;
                            FUL.Utils.RangeBearingToLatLon(PointList[1].Lat, PointList[1].Lon, Range, BearingDegrees, ref NewLat, ref NewLon);
                            NewPoint = new Coordinate(NewLon, NewLat);
                            ResultPoints.Add(NewPoint);
                        }
                        else
                            ResultPoints.Clear();
                    }
                }
                catch
                {
                    ResultPoints.Clear();
                }
            }

            return ResultPoints;
        }


        public static void FindMinMaxBoundingCircle(FUL.Coordinate CenterPoint, double radius, ref double MinLat, ref double MaxLat, ref double MinLon, ref double MaxLon)
        {
            double NotUsed = 0;
            try
            {
                FUL.Utils.RangeBearingToLatLon(CenterPoint.Lat, CenterPoint.Lon, radius, 0, ref MaxLat, ref NotUsed);
                FUL.Utils.RangeBearingToLatLon(CenterPoint.Lat, CenterPoint.Lon, radius, 90, ref NotUsed, ref MaxLon);
                FUL.Utils.RangeBearingToLatLon(CenterPoint.Lat, CenterPoint.Lon, radius, 180, ref MinLat, ref NotUsed);
                FUL.Utils.RangeBearingToLatLon(CenterPoint.Lat, CenterPoint.Lon, radius, 270, ref NotUsed, ref MinLon);
            }
            catch { }
        }
    } // End Geometry class
}

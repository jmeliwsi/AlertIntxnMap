using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace FUL
{
	public class RunwayItem
	{
		public string AirportID;
		public string RunwayID;
		public int Length;
		public DateTime ValidTime;
		public int Width;
        public string Bearing;
		public double Lat;
		public double Lon;
		public int Elevation;
        public string MagneticVariation;
        public double TrueBearing;

		public RunwayItem()
		{
		}

		public static List<RunwayItem> CreateList(DataSet ds)
		{
			List<RunwayItem> collection = new List<RunwayItem>();
			//Fill list
			using (DataTableReader myReader = ds.CreateDataReader())
			{
				while (myReader.Read())
				{
					RunwayItem runway = new RunwayItem();
					runway.AirportID = myReader.GetString(0);
					runway.RunwayID = myReader.GetString(1);
					runway.Length = myReader.GetInt32(2);
					runway.ValidTime = myReader.GetDateTime(3);
					runway.Width = Convert.ToInt32(myReader["RunwayWidth"]);
					runway.Bearing = myReader["RunwayBearing"].ToString();
					runway.Lat = Convert.ToDouble(myReader["Lat"]);
					runway.Lon = Convert.ToDouble(myReader["Lon"]);
					runway.Elevation = Convert.ToInt32(myReader["LandingThresholdElev"]);
                    runway.MagneticVariation = myReader["magnetic_variation"].ToString();
                    runway.TrueBearing = runway.GetRunwayTrueBearing();

                    int displacedThresholdDist = Convert.ToInt32(myReader["DisplacedThresholdDist"]);
                    if (displacedThresholdDist > 0)
                    {
                        double lat = 0;
                        double lon = 0;

                        FUL.Utils.RangeBearingToLatLon(runway.Lat, runway.Lon, displacedThresholdDist * FUL.Utils.Feet2NM, runway.TrueBearing + 180, ref lat, ref lon);
                        runway.Lat = lat;
                        runway.Lon = lon;
                    }	

					collection.Add(runway);
				}
			}
			return collection;
		}

		public double GetRunwayTrueBearing()
		{
            string bearingStr = Bearing;
            string variationStr = string.Empty;
            if (bearingStr.EndsWith("T"))
            {
                bearingStr = bearingStr.Replace("T", "");
            }
            else
                variationStr = MagneticVariation;

            double bearing = 0;
            double.TryParse(bearingStr, out bearing);
                        
            if (!string.IsNullOrEmpty(variationStr))
            {
                int i = 1;
                if (variationStr.StartsWith("W"))
                    i = -1;

                double magnetic = 0;
                double.TryParse(variationStr.Substring(1), out magnetic);

                bearing += i * magnetic/10;
            }

			return bearing;
		}

        public double GetRunwayBearing()
        {
            string bearingStr = Bearing;
            if (bearingStr.EndsWith("T"))
                bearingStr = bearingStr.Replace("T", "");

            double bearing = 0;
            double.TryParse(bearingStr, out bearing);

            return bearing;
        }

		public bool MatchRunway(RunwayItem anotherRunway)
		{
            if (Math.Abs((int)this.GetRunwayBearing() - (int)anotherRunway.GetRunwayBearing()) != 180)
                return false;

            double range = 0;
            double bearing = 0;

            FUL.Utils.RangeBearing(Lat, Lon, anotherRunway.Lat, anotherRunway.Lon, FUL.Utils.DistanceUnits.nm, ref range, ref bearing);

			double a = Math.Abs(bearing - this.TrueBearing);

			if (Math.Abs(bearing - this.TrueBearing) < 2.5 || (Math.Abs(bearing - this.TrueBearing) > 357.5 ))
            {
                if (this.RunwayID.EndsWith("L"))
                {
                    if (anotherRunway.RunwayID.EndsWith("R"))
                        return true;

                    return false;
                }

                if (this.RunwayID.EndsWith("R"))
                {
                    if (anotherRunway.RunwayID.EndsWith("L"))
                        return true;

                    return false;
                }

                return true;
            }

            return false;
		}
	}
}

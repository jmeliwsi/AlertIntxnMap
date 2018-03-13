using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	public class TurbPlotShape
	{
		public List<Coordinate> Points;
		public string RawText;
		public DateTime ValidTime;
		public DateTime ExpireTime;

		public TurbPlotShape(List<Coordinate> points, string rawText, DateTime validTime, DateTime expireTime)
		{
			this.Points = points;
			this.RawText = rawText;
			this.ValidTime = validTime;
			this.ExpireTime = expireTime;
		}
	}

	public class BasicFlightKey
	{
		public DateTime SchedDepDateTime;
		public string SchedDepICAO;
		public string FlightNumber;
		public int FlightLegInstanceNumber;
		public int FlightPlanID;

		public BasicFlightKey()
		{
			this.SchedDepDateTime = DateTime.MinValue;
			this.SchedDepICAO = string.Empty;
			this.FlightNumber = string.Empty;
			this.FlightLegInstanceNumber = 0;
			this.FlightPlanID = 0;
		}

		public BasicFlightKey(DateTime schedDepDateTime, string schedDepICAO, string flightNumber, int flightLegInstanceNumber, int flightPlanID)
		{
			this.SchedDepDateTime = schedDepDateTime;
			this.SchedDepICAO = schedDepICAO;
			this.FlightNumber = flightNumber;
			this.FlightLegInstanceNumber = flightLegInstanceNumber;
			this.FlightPlanID = flightPlanID;
		}
	}
}

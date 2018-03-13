using System;
using System.Collections.Generic;

namespace FUL
{
	public enum FlightRules { None, VFR, MVFR, IFR, LIFR, BCAT1 };
	public enum AirportCodeType { ICAO, IATA, FAA };

	[Serializable]
	public class AirportThresholdSet : IComparable<AirportThresholdSet>
	{
		public string ICAO = "";
		public string Name = "";
		public double VisCritical = 0.5;
		public double VisWarning = 1.0;
		public double CeilingCritical = 200;
		public double CeilingWarning = 500;
        public double WindCritical = 40;
		public double WindWarning = 30;
		public double RunwayVisCritical = 3000;
		public double RunwayVisWarning = 5000;
        public int DiversionWarning = 5;
        public int DiversionCritical = 10;
        public int NarrowCount = -1;
        public int WideCount = -1;
		public int DiversionCapacityTotal = 0;

		public AirportThresholdSet()
		{
			ICAO = "none";
		}
		public AirportThresholdSet(string icao)
		{
			ICAO = icao;
		}
		public override string ToString()
		{
			return ICAO + " : " + Name;
		}
		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(AirportThresholdSet)) return false;
			AirportThresholdSet temp = (AirportThresholdSet)obj;
			if (temp.ICAO == ICAO &&
				temp.Name == Name &&
				temp.VisWarning == VisWarning &&
				temp.VisCritical == VisCritical &&
				temp.CeilingWarning == CeilingWarning &&
				temp.CeilingCritical == CeilingCritical &&
				temp.WindWarning == WindWarning &&
                temp.WindCritical == WindCritical &&
				temp.RunwayVisWarning == RunwayVisWarning &&
				temp.RunwayVisCritical == RunwayVisCritical &&
                temp.DiversionWarning == DiversionWarning &&
                temp.DiversionCritical == DiversionCritical &&
                temp.NarrowCount == NarrowCount &&
                temp.WideCount == WideCount)
				return true;
			return false;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		public int CompareTo(AirportThresholdSet obj)
		{
			return this.ICAO.CompareTo(obj.ICAO);
		}
	}

	[Serializable]
	public class AirportThresholdSetCollection : System.Collections.Generic.List<AirportThresholdSet>
	{
		public AirportThresholdSetCollection() : base() { }
		public AirportThresholdSetCollection(int capacity) : base(capacity) { }
		public AirportThresholdSetCollection(IEnumerable<AirportThresholdSet> col) : base(col) { }
		public AirportThresholdSet GetAirport(string ICAO)
		{
			int index = this.BinarySearch(new AirportThresholdSet(ICAO));
			if (index >= 0)
				return this[index];

			return null;
		}
		public AirportThresholdSetCollection GetAirports(List<string> ICAO)
		{
			AirportThresholdSetCollection atsc = new AirportThresholdSetCollection();
			for (int x = 0; x < Count; x++)
				if (ICAO.Contains(this[x].ICAO))
					atsc.Add(this[x]);
			return atsc;
		}
		public AirportThresholdSetCollection Merge(AirportThresholdSetCollection add)
		{
			if (this == add) return this;
			if (add == null) return this;
			if (this.Count == 0)
			{
				this.AddRange(add);
				return this;
			}

			AirportThresholdSetCollection newSet = new AirportThresholdSetCollection();

			for (int x = 0; x < add.Count; x++)
			{
				AirportThresholdSet atset = GetAirport(add[x].ICAO);
				if (atset != null)
					this.Remove(atset);
				newSet.Add(add[x]);
			}

			this.AddRange(newSet);

			return this;
		}
	}

	/**
	 * \class Airport
	 * \brief Represents an airport object
	 */
	[Serializable]
	public class Airport
	{
		#region Data Members
		protected string icao;	// four letter code
		protected string iata;	// three letter code
		protected uint declutterLevel;
		protected double latitude;
		protected double longitude;
		protected string fullName;
		protected string state;
		protected DateTime validTime;
        protected int elevation;
		#endregion

		public string ICAO
		{
			get { return icao; }
		}

		public string IATA
		{
			get { return iata; }
		}

		public uint DeclutterLevel
		{
			get { return declutterLevel; }
		}

		public double Latitude
		{
			get { return latitude; }
		}

		public double Longitude
		{
			get { return longitude; }
		}

		public string Name
		{
			get { return fullName; }
		}

		public string State
		{
			get { return state; }
		}

		public DateTime ValidTime
		{
			get { return validTime; }
		}

        public int Elevation
        {
            get { return elevation; }
        }

		public Airport(string icao, string iata, uint declutterLevel, double latitude, double longitude, string fullName, string state, DateTime validTime, int elevation)
		{
			this.icao = icao;
			this.iata = iata;
			this.declutterLevel = declutterLevel;
			this.latitude = latitude;
			this.longitude = longitude;
			this.fullName = fullName;
			this.state = state;
			this.validTime = validTime;
            this.elevation = elevation;
		}

		public Airport(string icao, string iata)
		{
			this.icao = icao;
			this.iata = iata;
			this.declutterLevel = 0;
			this.latitude = 0;
			this.longitude = 0;
			this.fullName = string.Empty;
			this.state = string.Empty;
			this.validTime = DateTime.MinValue;
			this.elevation = int.MinValue;
		}

		public Airport()
		{
			this.icao = string.Empty;
			this.iata = string.Empty;
			this.declutterLevel = 0;
			this.latitude = 0;
			this.longitude = 0;
			this.fullName = string.Empty;
			this.state = string.Empty;
			this.validTime = DateTime.MinValue;
            this.elevation = int.MinValue;
		}

		/**
		 * \fn public double DistanceTo(double lat, double lon, DistanceUnits units)
		 * \brief Returns distance between this airport and the given lat/lon
		 * \param lat Latitude of the point in degrees
		 * \param lon Longitude of the point in degrees
		 * \return Distance between this airport and the point
		 */
		public double DistanceTo(double lat, double lon, Utils.DistanceUnits units)
		{
			return Utils.Distance(this.latitude, this.longitude, lat, lon, units);
		}

		public static bool operator ==(Airport airport1, Airport airport2)
		{
			bool airport1Null = Airport.IsNull(airport1);
			bool airport2Null = Airport.IsNull(airport2);

			if (airport1Null && airport2Null)
				return true;
			else if (airport1Null || airport2Null)
				return false;
			else
				return airport1.Equals(airport2);
		}

		public static bool operator !=(Airport airport1, Airport airport2)
		{
			return !(airport1 == airport2);
		}

		public override bool Equals(object obj)
		{
			Airport airport = obj as Airport;

			if (Airport.IsNull(airport))
				return false;

			try
			{
				return (string.Compare(this.ICAO, airport.ICAO, true) == 0 ||
					(string.Compare(this.IATA, string.Empty) != 0 && string.Compare(this.IATA, airport.IATA, true) == 0));
			}
			catch
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static bool IsNull(Airport airport)
		{
			return Object.Equals(airport, null);
		}
	}

	public class AirportCurfew
	{
		private DateTime startTime;
		public DateTime StartTime
		{
			get { return startTime; }
			set { startTime = value; }
		}

		private DateTime endTime;
		public DateTime EndTime
		{
			get { return endTime; }
			set { endTime = value; }
		}

		private DayOfWeek intervalStartDay;
		public DayOfWeek IntervalStartDay
		{
			get { return intervalStartDay; }
			set { intervalStartDay = value; }
		}

		private DayOfWeek intervalEndDay;
		public DayOfWeek IntervalEndDay
		{
			get { return intervalEndDay; }
			set { intervalEndDay = value; }
		}

		private int intervalStartHour;
		public int IntervalStartHour
		{
			get { return intervalStartHour; }
			set { intervalStartHour = value; }
		}

		private int intervalEndHour;
		public int IntervalEndHour
		{
			get { return intervalEndHour; }
			set { intervalEndHour = value; }
		}

		private int intervalStartMinute;
		public int IntervalStartMinute
		{
			get { return intervalStartMinute; }
			set { intervalStartMinute = value; }
		}

		private int intervalEndMinute;
		public int IntervalEndMinute
		{
			get { return intervalEndMinute; }
			set { intervalEndMinute = value; }
		}
	}

	public class AirportCurfewList
	{
		private string airportICAO;
		private List<AirportCurfew> curfewList;

		public AirportCurfewList()
		{
			airportICAO = string.Empty;
			curfewList = new List<AirportCurfew>();
		}

		public AirportCurfewList(string ICAO)
		{
			airportICAO = ICAO;
			curfewList = new List<AirportCurfew>();
		}

		public void AddCurfew(AirportCurfew curfew)
		{
			curfewList.Add(curfew);
		}

		public bool VioLateCurfew(int minutes, DateTime ETA, DateTime currentTime)
		{
			int currentDay = (int)ETA.DayOfWeek;
			int adjustCurrentDay = currentDay;

			foreach (AirportCurfew curfew in curfewList)
			{
				if (ETA > curfew.EndTime || ETA < curfew.StartTime)
					continue;

				int startDay = (int)curfew.IntervalStartDay;
				int endDay = (int)curfew.IntervalEndDay;
				if (curfew.IntervalStartDay > curfew.IntervalEndDay)
				{
					endDay += 7;
					adjustCurrentDay += 7;
				}

				if ((currentDay >= startDay && currentDay <= endDay)
				  || (adjustCurrentDay >= startDay && adjustCurrentDay <= endDay))
				{
					DateTime startTime = ETA;
					DateTime endTime = ETA;

					if (currentDay == adjustCurrentDay)
					{
						startTime = startTime.AddDays(startDay - currentDay);
						endTime = endTime.AddDays(endDay - currentDay);
					}
					else
					{
						startTime = startTime.AddDays(startDay - adjustCurrentDay);
						endTime = endTime.AddDays(endDay - adjustCurrentDay);
					}

					startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, curfew.IntervalStartHour, curfew.IntervalStartMinute, 0).AddMinutes(-minutes);
					endTime = new DateTime(endTime.Year, endTime.Month, endTime.Day, curfew.IntervalEndHour, curfew.IntervalEndMinute, 0).AddMinutes(minutes);

					if (ETA >= startTime && ETA <= endTime)
						return true;
				}
			}

			return false;
		}
	}

	public class AirportRunwayVisibilty
	{
		public string ICAO;
		public string Runway;
		public string MinRollOut;
		public string MinMidPoint;
		public string MinTouchDown;
		public int MinVisibility;
	}
}

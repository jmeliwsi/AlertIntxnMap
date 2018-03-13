using System;
using System.IO;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FusionSettings;
using System.Configuration;
using System.Xml;
//using System.Security.Cryptography.X509Certificates;
//using System.Security.Cryptography.Xml;
//using System.Security.Cryptography;


/**
 * \namespace FUL
 * \brief Fusion Utility Library - FUL (pronounced "fool")
 */
namespace FUL
{
	[Serializable]
	public struct Coordinate : IEquatable<Coordinate>
	{
		public double Lat;
		public double Lon;
		public Coordinate(double lon, double lat)
		{
			this.Lat = lat;
			this.Lon = lon;
		}
		public Coordinate(bool initialize)
		{
			this.Lat = -91.0;
			this.Lon = -181.0;
		}

		//Data sources are different, so we cannot do exact equal 
		public bool IsSameAirport(Coordinate p)
		{
			if ((Math.Abs(this.Lat - p.Lat) > 0.05) ||
				(Math.Abs(this.Lon - p.Lon) > 0.05))
				return false;
			return true;
		}

		public bool IsSamePoint(Coordinate p)
		{
			if ((Math.Abs(this.Lat - p.Lat) > 0.005) ||
				(Math.Abs(this.Lon - p.Lon) > 0.005))
				return false;
			return true;
		}

        public bool Equals(Coordinate other)
        {
            // Make certain the value type used for type T implements the IEquatable generic interface. 
            // If not, methods such as Contains must call the Object.Equals(Object) method, which boxes the affected list element.
            return Lat == other.Lat && Lon == other.Lon;
        }
    }

	[Serializable]
	public class Coordinate3D
	{
		private double lat;
		private double lon;
		private double elev;

		public Coordinate3D()
		{
			this.lat = 0;
			this.lon = 0;
			this.elev = 0;
		}

		public Coordinate3D(double lat, double lon, double elev)
		{
			this.lat = lat;
			this.lon = lon;
			this.elev = elev;
		}

		public double Lat
		{
			get { return lat; }
			set { lat = value; }
		}

		public double Lon
		{
			get { return lon; }
			set { lon = value; }
		}

		public double Elev
		{
			get { return elev; }
			set { elev = value; }
		}
	}

	[Serializable]
	public struct RoutePoints
	{
		public Coordinate location;
		public DateTime TimeOverPoint;
		public int Speed;

		public RoutePoints(bool initialize)
		{
			this.location.Lat = -91.0;
			this.location.Lon = -181.0;
			this.TimeOverPoint = DateTime.MinValue;
			this.Speed = int.MinValue;
		}

	}

	[Serializable]
	public class GribDataAvailableInfo
	{
		public bool HasNewData;
		public DateTime LastRequestTime;

		public GribDataAvailableInfo()
		{
			HasNewData = false;
			LastRequestTime = DateTime.MinValue;
		}
	}

	// Enumeration Types for American Airline integration.
	public enum RerouteRequest { None = 0, Merge, Create, Release, Update };
	public enum RerouteRequestStatus { None = 0, R, C, F };
	public enum ACARSMessageType { Unknown = 0, FTM, FPR };
	public enum ACARSMessageState { Unknown = 0, PreRequest, RequestNeeded, Requested, Response, Acknowledge };
	public enum ACARSMessageStatus { Unknown = 0, SENT, ERR, Pending, Fail, Acked };
	public enum ACARSMessagePriority { None = 0, Unknown, Mundane, Low, High, Normal, Urgent};

	/**
	 * \class Utils
	 * \brief A container class for generally useful static methods
	 */
	public class Utils
	{
		public enum DistanceUnits { km, mi, nm };

		public enum StormCellClassificationType { Unknown, Strong, Hail, Rotating, Tornadic };

		// The order of the Zoom Levels (0 - 5) is important. See array reference in GetStormList(ZoomLevelType ZoomLevel).
		public enum ZoomLevelType { Global, Continental, State, County, Terminal, Airport, None };

		public enum TFRClassificationType { Unknown, Nuclear, Stadium, Presidential, Fire, Rescue_or_Spill_or_Volcano, Fire_or_Spraying_or_Rescue, Special_Security, Sports_Event_or_Air_Show, Special_Event_of_Public_Interest, Emergency_Air_Traffic_Rules, Rocket_Launch_or_Flight, Disaster_in_Hawaii, High_Barometric_Pressure };

		public static System.Globalization.GregorianCalendar myCalendar = new System.Globalization.GregorianCalendar();

		public const double EarthRadius_km = 6378.137; // equatorial radius in kilometers
		public const double EarthRadius_sm = 3963.19; // equatorial radius in statute miles
		public const double MilesPerDegLat = 69.1707; // statute miles per degree of latitude (spherical earth assumed)
		public const double MilesPerDegLon = 69.1707; // statute miles per degree of longitude (spherical earth assumed)
		public const double deg2rad = Math.PI / 180;
		public const double rad2deg = 180 / Math.PI;

		public const double Feet2NM = 0.000164578834;

		public static int ASDI_Class;

		private static string TheFlights_DB_Name = "FusionFlights"; // Default;
		public static string Flights_DB_Name
		{
			get { return TheFlights_DB_Name; }
			set { TheFlights_DB_Name = value; }
		}

		private static string TheWeather_DB_Name = "Fusion_Datastore";	// Default.
		public static string Weather_DB_Name
		{
			get { return TheWeather_DB_Name; }
			set { TheWeather_DB_Name = value; }
		}

		private static string TheAnalytics_DB_Name = "Analytics";	// Default.
		public static string Analytics_DB_Name
		{
			get { return TheAnalytics_DB_Name; }
			set { TheAnalytics_DB_Name = value; }
		}

		private static string TheIngestHeartBeat_DB_Name = "Fusion_Admin"; // Default;
		public static string IngestHeartBeat_DB_Name
		{
			get { return TheIngestHeartBeat_DB_Name; }
			set { TheIngestHeartBeat_DB_Name = value; }
		}

		private static bool RecordFlightDataValue = false;
		public static bool RecordFlightData
		{
			get { return RecordFlightDataValue; }
			set { RecordFlightDataValue = value; }
		}

		private static bool RecordWeatherDataValue = false;
		public static bool RecordWeatherData
		{
			get { return RecordWeatherDataValue; }
			set { RecordWeatherDataValue = value; }
		}

		private static string RecordFlightDayValue;
		public static string RecordFlightDay
		{
			get { return RecordFlightDayValue; }
			set { RecordFlightDayValue = value; }
		}

		private static DateTime theRecordFlightDataInsertTime;
		public static DateTime RecordFlightDataInsertTime
		{
			get { return theRecordFlightDataInsertTime; }
			set { theRecordFlightDataInsertTime = value; }
		}

		private static int theLastRouteID = 0;
		public static int LastRouteID
		{
			get { return theLastRouteID; }
			set { theLastRouteID = value; }
		}

		// This tables maintains the success/failed correlation between the EAG.Airspace restricted areas and the dynamic SUAs.
		public static Hashtable AirspaceIndex_Table = new Hashtable();

		public static FUL.AirportCollection theAirportCollection = new FUL.AirportCollection();
		public static DateTime AirportDataValidTime = new DateTime(1800, 1, 1);

		public static string IngestorServiceName = string.Empty;
		/// <summary>
		/// If it exists, return the Ingestor Service Executable path.
		/// </summary>
		/// <returns></returns>
		public static string Get_IngestorServerExecutablePath()
		{
			string ExecutablePath = string.Empty;
            bool SuppliedDirectory = false;

			//IngestorServiceName = "FusionIngestorServiceASD-X";
			switch (IngestorServiceName)
			{
				case "FusionIngestorService": ExecutablePath = Server.FusionIngestorServiceExecutablePath; break;
				case "FusionIngestorServiceASDE-X": ExecutablePath = Server.FusionIngestorServiceASDE_XExecutablePath; break;
				case "FusionIngestorServiceClass2": ExecutablePath = Server.FusionIngestorServiceClass2ExecutablePath; break;
				case "FusionIngestorServiceADS-B": ExecutablePath = Server.FusionIngestorServiceADS_BExecutablePath; break;
				case "FusionAALDataIngestorService": ExecutablePath = Server.FusionIngestorServiceAAL_BExecutablePath; break;
				case "LatitudeFlightsService": ExecutablePath = Server.LatitudeFlightsServiceExecutablePath; break;
				case "FusionIngestorFlightDataVendor": ExecutablePath = Server.FusionIngestorFlightDataVendorPath; break;
				case "FusionFlightsIngestorService": ExecutablePath = Server.FusionIngestorServiceFlightsExecutablePath; break;
				case "FusionIngestorServiceMisc": ExecutablePath = Server.FusionIngestorServiceBrakeAction_BExecutablePath; break;
				case "FusionIngestorServiceFlights": ExecutablePath = Server.FusionIngestorServiceFlightsExecutablePath; break;
                case "FusionDataService": ExecutablePath = Server.FusionDataServiceExecutablePath; break;
                default: // Allow for supplied directory.
                    ExecutablePath = IngestorServiceName;
                    SuppliedDirectory = true;
                    break; 
            }

			if ((!SuppliedDirectory) && (!string.IsNullOrEmpty(ExecutablePath)))
			{
				int Index = ExecutablePath.LastIndexOf('\\');
				ExecutablePath = ExecutablePath.Substring(1, Index);
			}
			return ExecutablePath;
		}


		// ------------------------------------------------------------------------
		/// <summary>
		/// This method returns the current Flights DB Connection string.
		/// </summary>
		/// <returns></returns>
		public static string Get_FlightsDB_ConnectionString()
		{
			return Get_DB_ConnectionString(TheFlights_DB_Name);
		}

		// ------------------------------------------------------------------------
		/// <summary>
        /// This method returns the current Flights DB Connection string.
        /// </summary>
        /// <returns></returns>
        public static string Get_WeatherDB_ConnectionString()
        {
            return Get_DB_ConnectionString(TheWeather_DB_Name);
        }

        // ------------------------------------------------------------------------
        /// <summary>
		/// Access Database connection strings.  Checks for values in .config file first.  If not there checks the registry.
		/// This purpose of checking .config is to start transition away from registry.
		/// </summary>
		/// <param name="DB_Name">Name of database or database code</param>
		/// <returns>database connection string</returns>
		public static string Get_DB_ConnectionString(string DB_Name)
		{
			string ConnectionString = string.Empty;
			string ServerName = string.Empty;
			string Password = string.Empty;
			string UserID = string.Empty;
			string RegCon = string.Empty;	// Connection String in Registry.

			try
			{
				if( DB_Name == "Fusion_DataServices")
				{
					// check for "sv" in .config file.  This is the database code for Fusion_DataServices
					try
					{
						// check if we got a string from .config file to prevent extra exception calls.
						string dbcode = ConfigurationManager.ConnectionStrings["sv"].ConnectionString;
						if (!String.IsNullOrEmpty(dbcode))
						{
							ConnectionString = FUL.Encryption.Decrypt(dbcode);
							if (!String.IsNullOrEmpty(ConnectionString))
								return ConnectionString;
						}
					}
					catch
					{
						// any exceptions, continue on to use other behavior to get the string.
					}
				}
				// Fusion Database code current practice is to make code 4 characters or less.  So do a quick check here to verify if
				// this string could be a database code.  If the data base name is 4 characters or less it will also fall into this check.
				if( DB_Name.Length < 5)
				{
					// check for other database codes in the configuraton file.
					try
					{
						// check if we got a string from .config file to prevent extra exception calls.
						string dbcode = ConfigurationManager.ConnectionStrings[DB_Name].ConnectionString;
						if (!String.IsNullOrEmpty(dbcode))
						{
							ConnectionString = FUL.Encryption.Decrypt(dbcode);
							if (!String.IsNullOrEmpty(ConnectionString))
								return ConnectionString;
						}
					}
					catch
					{
						// any exceptions, continue on to use other behavior to get the string.
					}
				}
				// get database connection string from registry.
				if ((ASDI_Class == 2) && (string.Equals(DB_Name, "FusionFlights")))
				{
					DB_Name = "FusionFlightsClass2";
					RegCon = Server.DatabaseFlightsClass2Connection;	// If not present in Registry, returns empty string.
				}

				if (string.Equals(DB_Name, "EAG"))
					RegCon = Server.EAG_DatabaseConnection;	// If not present in Registry, returns empty string.

				if (string.IsNullOrEmpty(RegCon))
					RegCon = Server.DatabaseConnection;

				ConnectionString = RegCon.Replace(";DATABASE=;", ";DATABASE=" + DB_Name + ";");

				// This is only required for AA_DataControl when its running on Dev but writing data to LoadTest DB.
				string AALcon = Server.DatabaseAALConnection;
				if ((AALcon != null) && (!string.IsNullOrEmpty(AALcon)))
					ConnectionString = AALcon.Replace(";DATABASE=;", ";DATABASE=" + DB_Name + ";");
			}
			catch
			{
				FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, " *** ERROR *** reading connection string from Registry.");
				ConnectionString = string.Empty;
			}

			return ConnectionString;

		} // End Get_DB_ConnectionString

		public static string FormatRunTime(TimeSpan RunTime)
		{
			string RunTimeString = string.Empty;

			if (RunTime.TotalSeconds >= 60)
			{
				if (RunTime.TotalMinutes >= 60)
				{
					if (RunTime.TotalHours >= 24)
					{
						RunTimeString = RunTime.TotalDays.ToString("####.#") + " days";
					}
					else
						RunTimeString = RunTime.TotalHours.ToString("##.#") + " hours";
				}
				else
					RunTimeString = RunTime.TotalMinutes.ToString("##.#") + " mins";
			}
			else
			{
				if (RunTime.TotalSeconds < 0.1)
					RunTimeString = "0 secs";
				else
					RunTimeString = RunTime.TotalSeconds.ToString("##.#") + " secs";
			}

			return RunTimeString;

		} 

		// ------------------------------------------------------------------------
		/**
		 * \fn public static bool RangeBearing(double lat1, double lon1, double lat2, double lon2, DistanceUnits units, ref double range, ref double bearing)
		 * \brief Given two points, returns the range and bearing between the points
		 * \note Formulae from http://williams.best.vwh.net/avform.htm
		 * \param lat1 Latitude of the first point in degrees
		 * \param lon1 Longitude of the first point in degrees
		 * \param lat2 Latitude of the second point in degrees
		 * \param lon2 Longitude of the second point in degrees
		 * \return A boolean value indicating status
		 */
		public static bool RangeBearing(double lat1, double lon1, double lat2, double lon2, DistanceUnits units, ref double range, ref double bearing)
		{
			bool status = true;

			try
			{
				double d = 0, b = 0;

				// Convert lat/lon from degrees to radians
				lat1 *= deg2rad;
				lon1 *= deg2rad;
				lat2 *= deg2rad;
				lon2 *= deg2rad;

				// Calculate the great circle distance - use a mathematically equivalent formula,
				// which is less subject to rounding error for short distances
				//d = Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1-lon2)));
				d = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat1 - lat2) / 2.0), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2.0), 2)));
				switch (units)
				{
					case DistanceUnits.km:
						range = EarthRadius_km * d;	// equatorial radius of 6378.137 kilometers
						break;
					case DistanceUnits.mi:
						range = EarthRadius_sm * d;	// equatorial radius of 3963.19 statute miles
						break;
					case DistanceUnits.nm:
						range = rad2deg * 60 * d;
						break;
					default:
						throw (new ArgumentException());
				}

				// Calculate the bearing
				if ((lon1 == lon2) || (Math.Abs(lon1 - lon2) < 0.000001))
				{
					if (lat1 > lat2)
						bearing = 180;
					else
						bearing = 0;
				}
				else
				{
					if (Math.Sin(lon2 - lon1) < 0)
						b = Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(d)) / (Math.Sin(d) * Math.Cos(lat1)));
					else
						b = 2.0 * Math.PI - Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(d)) / (Math.Sin(d) * Math.Cos(lat1)));
					bearing = 360 - (rad2deg * b);
				}
			}
			catch
			{
				status = false;
			}

			return status;
		}

		public static bool RangeBearingInRadians(double lat1, double lon1, double lat2, double lon2, ref double range, ref double bearing)
		{
			bool status = true;

			try
			{
				// Convert lat/lon from degrees to radians
				lat1 *= deg2rad;
				lon1 *= deg2rad;
				lat2 *= deg2rad;
				lon2 *= deg2rad;

				// Calculate the great circle distance - use a mathematically equivalent formula,
				// which is less subject to rounding error for short distances
				//range = Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2)));
				range = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat1 - lat2) / 2.0), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2.0), 2)));

				// Calculate the bearing
				if ((lon1 == lon2) || (Math.Abs(lon1 - lon2) < 0.000001))
				{
					if (lat1 > lat2)
						bearing = 2 * Math.PI;
					else
						bearing = 0;
				}
				else
				{
					if (Math.Sin(lon1 - lon2) < 0)
						bearing = Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(range)) / (Math.Sin(range) * Math.Cos(lat1)));
					else
						bearing = 2.0 * Math.PI - Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(range)) / (Math.Sin(range) * Math.Cos(lat1)));
				}
			}
			catch
			{
				status = false;
			}

			return status;
		}

		// ------------------------------------------------------------------------
		/**
		 * \fn public static double Distance(double lat1, double lon1, double lat2, double lon2, DistanceUnits units)
		 * \brief Given two points, returns distance between the points
		 * \note Formulae from http://williams.best.vwh.net/avform.htm
		 * \param lat1 Latitude of the first point in degrees
		 * \param lon1 Longitude of the first point in degrees
		 * \param lat2 Latitude of the second point in degrees
		 * \param lon2 Longitude of the second point in degrees
		 * \return Distance between the points
		 */
		public static double Distance(double lat1, double lon1, double lat2, double lon2, DistanceUnits units)
		{
			double d = 0;

			try
			{
				// Convert lat/lon from degrees to radians
				lat1 *= deg2rad;
				lon1 *= deg2rad;
				lat2 *= deg2rad;
				lon2 *= deg2rad;

				// Calculate the great circle distance - use a mathematically equivalent formula,
				// which is less subject to rounding error for short distances
				//d = Math.Acos((Math.Sin(lat1) * Math.Sin(lat2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1-lon2)));
				d = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat1 - lat2) / 2.0), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2.0), 2)));

				switch (units)
				{
					case DistanceUnits.km:
						d = EarthRadius_km * d;	// equatorial radius of 6378.137 kilometers
						break;
					case DistanceUnits.mi:
						d = EarthRadius_sm * d;	// equatorial radius of 3963.19 statute miles
						break;
					case DistanceUnits.nm:
						d = rad2deg * 60 * d;
						break;
					default:
						throw (new ArgumentException());
				}
			}
			catch
			{
				return double.NaN;
			}

			return d;
		}

		// ------------------------------------------------------------------------
		/// <summary>
		/// Determine if the two given rectangles intersect.
		/// If neither rectangle contains a point inside the other, then the Rectangles do not intersect.
		/// </summary>
		/// <param name="Rectangle_A"></param>
		/// <param name="Rectangle_B"></param>
		/// <returns></returns>
		public static bool RectanglesIntersect(Coordinate[] Rectangle_A, Coordinate[] Rectangle_B)
		{
			bool result = false;
			double Slope_A = 0;
			double Y_Intercept_A = 0;
			double Slope_B = 0;
			double Y_Intercept_B = 0;
			Coordinate Intersect;

			// Check for Rectangle_A's points in Rectangle_B
			for (int i = 0; i <= 3; i++)
			{
				result = PointInsideRectangle(Rectangle_A[i], Rectangle_B);
				if (result == true)
					break;
			}

			if (result == false)
			{ // Check for Rectangle_B's points in Rectangle_A
				for (int i = 0; i <= 3; i++)
				{
					result = PointInsideRectangle(Rectangle_B[i], Rectangle_A);
					if (result == true)
						break;
				}
			}

			if (result == false)
			{ // Check (up to two) adjacent Rectangle_A lines intersecting any of Rectangle_Bs four lines.
				for (int j = 0; j < 2; j++)
				{
					if (Rectangle_A[j].Lon == Rectangle_A[j + 1].Lon)
						Slope_A = 10000.0;
					else
						Slope_A = (Rectangle_A[j].Lat - Rectangle_A[j + 1].Lat) / (Rectangle_A[j].Lon - Rectangle_A[j + 1].Lon);

					Y_Intercept_A = Rectangle_A[j].Lat - (Slope_A * Rectangle_A[j].Lon);


					for (int i = 0; i < 3; i++)
					{
						if (Rectangle_B[i].Lon == Rectangle_B[(i + 1) % 4].Lon)
							Slope_B = 10000.0;
						else
							Slope_B = (Rectangle_B[i].Lat - Rectangle_B[(i + 1) % 4].Lat) / (Rectangle_B[i].Lon - Rectangle_B[(i + 1) % 4].Lon);

						Y_Intercept_B = Rectangle_B[i].Lat - (Slope_B * Rectangle_B[i].Lon);


						if (Slope_A != Slope_B)  // parallel lines will not intersect.
						{
							Intersect.Lon = (Y_Intercept_A - Y_Intercept_B) / (Slope_B - Slope_A);
							Intersect.Lat = (Slope_A * Intersect.Lon) + Y_Intercept_A;

							// Check intersection point on line segment (not infinite line)
							if (
								((Intersect.Lon > Rectangle_A[j].Lon) && (Intersect.Lon < Rectangle_A[j + 1].Lon) || (Intersect.Lon < Rectangle_A[j].Lon) && (Intersect.Lon > Rectangle_A[j + 1].Lon))
								&&
								((Intersect.Lat > Rectangle_A[j].Lat) && (Intersect.Lat < Rectangle_A[j + 1].Lat) || (Intersect.Lat < Rectangle_A[j].Lat) && (Intersect.Lat > Rectangle_A[j + 1].Lat))
								&&
								((Intersect.Lon > Rectangle_B[j].Lon) && (Intersect.Lon < Rectangle_B[j + 1].Lon) || (Intersect.Lon < Rectangle_B[j].Lon) && (Intersect.Lon > Rectangle_B[j + 1].Lon))
								&&
								((Intersect.Lat > Rectangle_B[j].Lat) && (Intersect.Lat < Rectangle_B[j + 1].Lat) || (Intersect.Lat < Rectangle_B[j].Lat) && (Intersect.Lat > Rectangle_B[j + 1].Lat))
								)
							{
								double y_diff = (Slope_A * Intersect.Lon + Y_Intercept_A) - Intersect.Lat;
								//								Console.WriteLine("Y diff is " + y_diff);
								y_diff = (Slope_B * Intersect.Lon + Y_Intercept_B) - Intersect.Lat;
								//								Console.WriteLine("Y diff is " + y_diff);
								result = true;
								break;
							}
						} // end-if parallel lines
					} // end-for all 4 lines in Rectangle B
					if (result == true)
						break;
				} // end-for two lines on Rectangle_A
			} // end-if result is false

			return result;

		} // End- RectanglesIntersect

		// ------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given Point is inside the given Rectangle.
		/// </summary>
		/// <param name="Point"></param>
		/// <param name="Rectangle"></param>
		/// <returns></returns>
		public static bool PointInsideRectangle(Coordinate Point, Coordinate[] Rectangle)
		/// determine if the Point is inside the Rectangle.
		{
			bool result = false;
			Coordinate OutsidePoint;
			Coordinate Intersection_Point;
			int IntersectCount = 0;

			// Find a point known to be outside the rectangle.
			double Smallest_Lon = Rectangle[0].Lon;
			for (int i = 1; i <= 3; i++)
			{
				if (Rectangle[i].Lon < Smallest_Lon)
					Smallest_Lon = Rectangle[i].Lon;
			}
			OutsidePoint.Lat = Point.Lat;
			OutsidePoint.Lon = Smallest_Lon - 2.0;

			// Calculate line equation containing the "known" Outside point and the given point.
			double Slope1 = 0;
			double Y_intercept1 = Point.Lat;


			double Slope2 = 0;
			double Y_intercept2 = 0;
			// Check for intersection of the new line with the 4 lines of the rectangle.
			for (int i = 0; i <= 3; i++)
			{
				if (Rectangle[i].Lon == Rectangle[(i + 1) % 4].Lon)
					Slope2 = 10000.0;
				else
					Slope2 = (Rectangle[i].Lat - Rectangle[(i + 1) % 4].Lat) / (Rectangle[i].Lon - Rectangle[(i + 1) % 4].Lon);

				Y_intercept2 = Rectangle[i].Lat - (Slope2 * Rectangle[i].Lon);

				if (Slope1 != Slope2)  // parallel lines will not intersect.
				{
					Intersection_Point.Lon = (Y_intercept1 - Y_intercept2) / (Slope2);		// Solve intercept point by simultaneous equations.
					Intersection_Point.Lat = Y_intercept1;

					// Check if intersection point is on segment (rather than infinite line)
					if ((Intersection_Point.Lon < Point.Lon)
						&&
						((Intersection_Point.Lat > Rectangle[i].Lat) && (Intersection_Point.Lat < Rectangle[(i + 1) % 4].Lat)
						|| (Intersection_Point.Lat < Rectangle[i].Lat) && (Intersection_Point.Lat > Rectangle[(i + 1) % 4].Lat)
						)
						)
						IntersectCount++;


				} // end-if not parallel lines

			}  // end-for all four rectangle line segments


			// Count the number of intersections. 
			// 1 intersection then point is inside rectangle.
			// 0 or 2 intersections then point is outside rectangle.
			if (IntersectCount % 2 == 1) // odd number of intersections (should only be 1), Point is inside rectangle.
				result = true;

			return result;

		} // End- PointInsideRectangle

		// RadialDME pattern
		public static bool IsRadialDME(string name)
		{
			// Format: aa(a)(a)(a)dddddd
			return RadialDMEPattern.IsMatch(name);
		}

		public static bool IsSpeedFlightLevel(string name)
		{
			return SpeedFlightLevelPattern.IsMatch(name);
		}

		public static bool IsSpeedFlightLevel(ref string name)
		{
			Match m = Regex.Match(name, SpeedFlightLevelPattern.ToString());
			if (m.Success)
			{
				name = name.Substring(0, m.Index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Does this string match a simple NAV (Navaid) Radial (Bearing) pattern of 3 letters followed by 3 numbers.
		/// Example:  BAL170 ... BAL is the NAV designator.  170 is a Radial or bearing in degrees.
		/// This is a simple search.  It does not verify that the 3 letters are a NAV and it does not work for the case of FIX<radial>
		/// There appears to be no other tokens in flight routes which will match this.  If one does cause issues, need to add
		/// check that 3 letters is a valid NAV desginator.
		/// </summary>
		/// <param name="name">name or token from route string to compare to see if it matches the pattern</param>
		/// <returns>true if it matches, false if it does not.</returns>
		public static bool IsNAVRadial(string name)
		{
			return (NAVRadialPattern.IsMatch(name));
		}

		// ------------------------------------------------------------------------
		/// <summary>
		/// A point {Lat2,Lon2} is a distance "Range" out on the "Bearing" radial from point (Lat1, Lon1)
		/// Bearing is input in degrees.
		/// Range is input in nautical miles.
		/// </summary>
		/// <param name="Lat1"></param>
		/// <param name="Lon1"></param>
		/// <param name="Range"></param>
		/// <param name="Bearing"></param>
		/// <param name="Lat2"></param>
		/// <param name="Lon2"></param>
		public static void RangeBearingToLatLon(double Lat1, double Lon1, double Range, double Bearing, ref double Lat2, ref double Lon2)
		{
			Bearing = 0.0 - Bearing;

			double pi = 3.1415926;
			double angle_radians = (pi / 180.0) * Bearing;
			double distance_radians = (pi / (180.0 * 60.0)) * Range;
			double y = 0;

			Lat1 = (pi / 180.0) * Lat1;
			Lon1 = (pi / 180.0) * Lon1;

			Lat2 = Math.Asin(Math.Sin(Lat1) * Math.Cos(distance_radians) + Math.Cos(Lat1) * Math.Sin(distance_radians) * Math.Cos(angle_radians));
			if (Math.Cos(Lat2) == 0)
				Lon2 = Lon1;      // endpoint a pole
			else
			{
				y = Lon1 - Math.Asin(Math.Sin(angle_radians) * Math.Sin(distance_radians) / Math.Cos(Lat2)) + pi;
				Lon2 = (y - 2 * pi * Math.Floor(y / (2 * pi))) - pi;
			}

			Lat2 = Lat2 / (pi / 180.0);
			Lon2 = Lon2 / (pi / 180.0);

		} // End - RangeBearingToLatLon

		// ------------------------------------------------------------------------
		public static double PolygonArea(List<Coordinate> Polygon)
		{
			double Area = 0;

			Polygon.Add(Polygon[0]);    // duplicate first point to "close" polygon.

			for (int i = 0; i < Polygon.Count - 1; i++)
				Area += Polygon[i].Lon * Polygon[i + 1].Lat - Polygon[i + 1].Lon * Polygon[i].Lat;

			Polygon.RemoveAt(Polygon.Count - 1);

			return Area / 2;

		} // End PolygonArea
		// ------------------------------------------------------------------------

		public static Coordinate PolygonCentroid(List<Coordinate> Polygon, double Area)
		{
			Coordinate Centroid = new Coordinate();
			double PartB = 0;

			Polygon.Add(Polygon[0]);    // duplicate first point to "close" polygon.

			for (int i = 0; i < Polygon.Count - 1; i++)
			{
				PartB = Polygon[i].Lon * Polygon[i + 1].Lat - Polygon[i + 1].Lon * Polygon[i].Lat;

				Centroid.Lon += (Polygon[i].Lon + Polygon[i + 1].Lon) * PartB;

				Centroid.Lat += (Polygon[i].Lat + Polygon[i + 1].Lat) * PartB;
			}
			Polygon.RemoveAt(Polygon.Count - 1);

			Centroid.Lon = Centroid.Lon / (6 * Area);
			Centroid.Lat = Centroid.Lat / (6 * Area);

			return Centroid;

		} // End PolygonCentroid

		//////////////////////////////////////////////////////////////////////////////
		// Copied from WSIMap
		//////////////////////////////////////////////////////////////////////////////
		public static bool PointOnLine(Coordinate pt, List<Coordinate> pointList)
		{
			// Note: PerfTimer showed that, for input points near the end of
			// the Curve (requiring all segments to be checked) that return true,
			// this routine takes about 2.5 ms for a Curve containing 10,000 points.

			// Need at least two points
			if (pointList.Count < 2)
				return false;

			// Iterate the points to see if the input point lies on
			// any of the curve's segments
			Coordinate newPt1, newPt2;
			for (int i = 0; i < pointList.Count - 1; i++)
			{

				if (CrossesIDL(pointList[i], pointList[i + 1], out newPt1, out newPt2))
				{
					if (PointOnLine(pointList[i].Lon, pointList[i].Lat, newPt1.Lon, newPt1.Lat, pt.Lon, pt.Lat) == 2)
						return true;
					if (PointOnLine(newPt2.Lon, newPt2.Lat, pointList[i + 1].Lon, pointList[i + 1].Lat, pt.Lon, pt.Lat) == 2)
						return true;
				}
				else
				{
					if (StrictPointOnLine(pointList[i].Lon, pointList[i].Lat, pointList[i + 1].Lon, pointList[i + 1].Lat, pt.Lon, pt.Lat) == 2)
						return true;
				}
			}

			return false;
		}

		private static int StrictPointOnLine(double px, double py, double qx, double qy, double tx, double ty)
		{
			if ((px == qx) && (py == qy))
			{
				if ((tx == px) && (ty == py))
				{
					return 2;
				}
				else
				{
					return 0;
				}
			}

			if ((tx < px) && (tx < qx))
				return 0;
			if ((tx > px) && (tx > qx))
				return 0;
			if ((ty < py) && (ty < qy))
				return 0;
			if ((ty > py) && (ty > qy))
				return 0;

			double distance = Math.Abs((py - ty) * (qx - px) - (qy - py) * (px - tx)) / Math.Sqrt((Math.Pow(px - qx, 2) + Math.Pow(py - qy, 2)));

			if (distance <= 0.1)
				return 2;
			return 0;
		}
		private static int PointOnLine(double px, double py, double qx, double qy, double tx, double ty)
		{
			// From: http://www.acm.org/pubs/tog/GraphicsGems/gems/PntOnLine.c
			// A Fast 2D Point-On-Line Test
			// by Alan Paeth
			// from "Graphics Gems", Academic Press, 1990

			if ((px == qx) && (py == qy))
				if ((tx == px) && (ty == py)) return 2;
				else return 0;

			if (Math.Abs((qy - py) * (tx - px) - (ty - py) * (qx - px)) >=
				(Math.Max(Math.Abs(qx - px), Math.Abs(qy - py)))) return (0);
			if (((qx < px) && (px < tx)) || ((qy < py) && (py < ty))) return (1);
			if (((tx < px) && (px < qx)) || ((ty < py) && (py < qy))) return (1);
			if (((px < qx) && (qx < tx)) || ((py < qy) && (qy < ty))) return (3);
			if (((tx < qx) && (qx < px)) || ((ty < qy) && (qy < py))) return (3);

			return (2);
		}
		public static bool PointInPolygon(Coordinate pt, List<Coordinate> pointList)
		{
			// From: http://www.alienryderflex.com/polygon/
			// The function will return true if the point is inside the
			// polygon or false if it is not. If the point is exactly on
			// the edge of the polygon, then the function may return true
			// or false.
			int i, j = 0;
			bool oddNodes = false;

			// Check for dateline crossings
			int nCrossings = NumDatelineCrossings(pointList);

			// If there are more than two crossings, just return false
			// because we do not draw these polygons
			if (nCrossings > 2) return false;

			// If there are dateline crossings, calculate new point lists
			List<Coordinate> pl1 = null, pl2 = null;

			// If the polygon crosses the dateline, form new point lists
			if (nCrossings > 0)
				CreateNewPointLists(out pl1, out pl2, pointList);
			else
				pl1 = pointList;

			// Point in polygon test
			for (i = 0; i < pl1.Count; i++)
			{
				j++;
				if (j == pl1.Count) j = 0;
				double polyYi = ((Coordinate)pl1[i]).Lat;
				double polyYj = ((Coordinate)pl1[j]).Lat;
				double polyXi = ((Coordinate)pl1[i]).Lon;
				double polyXj = ((Coordinate)pl1[j]).Lon;
				if (polyYi < pt.Lat && polyYj >= pt.Lat || polyYj < pt.Lat && polyYi >= pt.Lat)
				{
					if (polyXi + (pt.Lat - polyYi) / (polyYj - polyYi) * (polyXj - polyXi) < pt.Lon)
						oddNodes = !oddNodes;
				}
			}

			// If the polygon crossed the dateline, we might have a 2nd point list
			if (pl2 != null && !oddNodes)
			{
				for (i = 0; i < pl2.Count; i++)
				{
					j++;
					if (j == pl2.Count) j = 0;
					double polyYi = ((Coordinate)pl2[i]).Lat;
					double polyYj = ((Coordinate)pl2[j]).Lat;
					double polyXi = ((Coordinate)pl2[i]).Lon;
					double polyXj = ((Coordinate)pl2[j]).Lon;
					if (polyYi < pt.Lat && polyYj >= pt.Lat || polyYj < pt.Lat && polyYi >= pt.Lat)
					{
						if (polyXi + (pt.Lat - polyYi) / (polyYj - polyYi) * (polyXj - polyXi) < pt.Lon)
							oddNodes = !oddNodes;
					}
				}
			}

			return oddNodes;
		}
		private static int NumDatelineCrossings(List<Coordinate> pointList)
		{
			// Check for segments that cross the international dateline
			int nCrossings = 0;
			Coordinate temp1, temp2;
			for (int i = 1; i < pointList.Count; i++)
			{
				if (CrossesIDL(pointList[i - 1], pointList[i], out temp1, out temp2))
					nCrossings++;
			}
			return nCrossings;
		}
		private static void CreateNewPointLists(out List<Coordinate> pl1, out List<Coordinate> pl2, List<Coordinate> pointList)
		{
			// Check for empty point list
			if (pointList.Count == 0)
			{
				pl1 = null;
				pl2 = null;
				return;
			}

			// The polygon crosses the dateline, so form new point lists
			int crossings = 0;
			pl1 = new List<Coordinate>();
			pl2 = new List<Coordinate>();
			Coordinate newPt1, newPt2;
			pl1.Add(pointList[0]);
			for (int i = 1; i < pointList.Count; i++)
			{
				if (!CrossesIDL(pointList[i - 1], pointList[i], out newPt1, out newPt2))
				{
					if (crossings == 1)
						pl2.Add(pointList[i]);
					else
						pl1.Add(pointList[i]);

				}
				else
				{
					crossings++;
					if (crossings == 1)
					{
						pl1.Add(newPt1);
						pl2.Add(newPt2);
						pl2.Add(pointList[i]);
					}
					else
					{
						pl2.Add(newPt1);
						pl1.Add(newPt2);
						pl1.Add(pointList[i]);
					}
				}
			}
		}

		private static bool CrossesIDL(double lon1, double lon2)
		{
			// What we want to do here is see if the longitudes of the 
			// points fall into the range 90 < lon1 <= 180 and
			// -180 <= lon2 < -90.  If the longitudes fall into this range
			// then we can assume the curve crosses the dateline discontinuity.

			// Check for a sign change between longitudes
			if (lon1 * lon2 >= 0)
				return false;

			// There is a sign change, so test for dateline crossing
			if ((lon1 < 0 && lon1 >= -180 && lon1 < -90 && lon2 > 90 && lon2 <= 180) ||
				(lon1 >= 0 && lon2 >= -180 && lon2 < -90 && lon1 > 90 && lon1 <= 180))
				return true;

			return false;
		}

		private static bool CrossesIDL(Coordinate pt1, Coordinate pt2, out Coordinate newPt1, out Coordinate newPt2)
		{
			// What we want to do here is see if the longitudes of the 
			// points fall into the range 90 < lon1 <= 180 and
			// -180 <= lon2 < -90.  If the longitudes fall into this range
			// then we can assume the curve crosses the dateline discontinuity.
			// We then need to calculate the intersection of the curve with the
			// dateline so we can add two points to the curve - one at (-180,lat)
			// and the other at (180,lat).

			//// Initialize the return points to null
			//newPt1 = null;
			//newPt2 = null;
			newPt1 = new Coordinate(181, 181);
			newPt2 = new Coordinate(181, 181);

			// Check for a sign change between longitudes
			if (pt1.Lat * pt2.Lon >= 0)
				return false;

			// There is a sign change, so test for dateline crossing
			if (pt1.Lon < 0)
			{
				if (pt1.Lon >= -180 && pt1.Lon < -90 &&
					pt2.Lon > 90 && pt2.Lon <= 180)
				{
					double slope = 0;
					double deltax = ((180 - Math.Abs(pt1.Lon)) + (180 - Math.Abs(pt2.Lon)));
					if (deltax != 0)
						slope = (pt2.Lat - pt1.Lat) / deltax;
					double dy = (180 + pt1.Lon) * slope;
					//newPt1 = new Coordinate(-180, pt1.Lat + dy);
					//newPt2 = new Coordinate(180, pt1.Lat + dy);
					newPt1.Lat = pt1.Lat + dy;
					newPt1.Lon = -180;
					newPt2.Lat = pt1.Lat + dy;
					newPt2.Lon = 180;
					return true;
				}
			}
			else
			{
				if (pt2.Lon >= -180 && pt2.Lon < -90 &&
					pt1.Lon > 90 && pt1.Lon <= 180)
				{
					double slope = 0;
					double deltax = ((180 - Math.Abs(pt1.Lon)) + (180 - Math.Abs(pt2.Lon)));
					if (deltax != 0)
						slope = (pt2.Lat - pt1.Lat) / deltax;
					double dy = (180 - pt1.Lon) * slope;
					//newPt1 = new Coordinate(180, pt1.Lat + dy);
					//newPt2 = new Coordinate(-180, pt1.Lat + dy);
					newPt1.Lat = pt1.Lat + dy;
					newPt1.Lon = 180;
					newPt2.Lat = pt1.Lat + dy;
					newPt2.Lon = -180;
					return true;
				}
			}

			return false;
		}

		public static void GetBoundingBox(List<Coordinate> points, ref double minLat, ref double maxLat, ref double minLon, ref double maxLon)
		{
			minLat = 90;
			maxLat = -90;
			minLon = 180;
			maxLon = -180;
			bool crossesIDL = CrossesDateline(points);

			foreach (Coordinate p in points)
			{
				if (p.Lat < minLat)
					minLat = p.Lat;
				if (p.Lat > maxLat)
					maxLat = p.Lat;

				if (crossesIDL)
				{
					if (p.Lon >= 0 && p.Lon < minLon)
						minLon = p.Lon;
					if (p.Lon < 0 && p.Lon > maxLon)
						maxLon = p.Lon;
				}
				else
				{
					if (p.Lon < minLon)
						minLon = p.Lon;
					if (p.Lon > maxLon)
						maxLon = p.Lon;
				}
			}
		}

		private static bool CrossesDateline(List<Coordinate> points)
		{
			// Check for segments that cross the international dateline.
			for (int i = 1; i < points.Count; i++)
				if (CrossesIDL(points[i - 1].Lon, points[i].Lon))
					return true;

			return false;
		}
		//////////////////////////////////////////////////////////////////////////////
		// END: Copied from WSIMap
		//////////////////////////////////////////////////////////////////////////////

		// --------------------------------------------------------------------------
		/// <summary>
		/// This method determines the intersection of 2 line segments.
		/// if they intersect (return TRUE), the the intersection point is also returned.
		/// The algorithm is based on the 2d line intersection method from "comp.graphics.algorithms
		/// </summary>
		/// <param name="LineSegmentA_Point1">Line Segment 1 (1st point)</param>
		/// <param name="LineSegmentA_Point2">Line Segment 1 (2nd point)</param>
		/// <param name="LineSegmentB_Point1">Line Segment 2 (1st point)</param>
		/// <param name="LineSegmentB_Point2">Line Segment 2 (2nd point)</param>
		/// <returns>Result of intersection</returns>
		public static bool LineSegmentsIntersect(Coordinate LineSegmentA_Point1, Coordinate LineSegmentA_Point2, Coordinate LineSegmentB_Point1, Coordinate LineSegmentB_Point2, bool PointDesired, out Coordinate IntersectionPoint)
		{
			bool SegmentsIntersect = false;
			IntersectionPoint.Lon = 0;
			IntersectionPoint.Lat = 0;

			double dx = LineSegmentA_Point2.Lon - LineSegmentA_Point1.Lon;
			double dy = LineSegmentA_Point2.Lat - LineSegmentA_Point1.Lat;
			double da = LineSegmentB_Point2.Lon - LineSegmentB_Point1.Lon;
			double db = LineSegmentB_Point2.Lat - LineSegmentB_Point1.Lat;
			if ((da * dy - db * dx) == 0) //The segments are parallel, thus will never intersect.
				return false;

			double s = (dx * (LineSegmentB_Point1.Lat - LineSegmentA_Point1.Lat) + dy * (LineSegmentA_Point1.Lon - LineSegmentB_Point1.Lon)) / (da * dy - db * dx);
			double t = (da * (LineSegmentA_Point1.Lat - LineSegmentB_Point1.Lat) + db * (LineSegmentB_Point1.Lon - LineSegmentA_Point1.Lon)) / (db * dx - da * dy);

			if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
			{
				SegmentsIntersect = true;
				if (PointDesired == true)
				{
					IntersectionPoint.Lon = LineSegmentA_Point1.Lon + t * dx;
					IntersectionPoint.Lat = LineSegmentA_Point1.Lat + t * dy;
				}
			}

			return SegmentsIntersect;

		} // End LineSegmentsIntersect

		// --------------------------------------------------------------------------
		/// <summary>
		/// This method determines the intersection of a circle and a line segment.
		/// if they intersect (return TRUE), the the intersection points are also returned.
		/// The algorithm source is from "http://local.wasp.uwa.edu.au/~pbourke/geometry/sphereline/
		/// </summary>
		/// <param name="CircleCenterPoint"></param>
		/// <param name="CircleRadius">Radius of Circle in NM</param>
		/// <param name="LineSegment_Point1"></param>
		/// <param name="LineSegment_Point2"></param>
		/// <param name="PointsDesired"></param>
		/// <param name="IntersectionPoint_1"></param>
		/// <param name="IntersectionPoint_2"></param>
		/// <returns>Intersection Result</returns>
		public static bool CircleLineSegmentIntersect(Coordinate CircleCenterPoint, Double CircleRadius, Coordinate LineSegment_Point1, Coordinate LineSegment_Point2, bool PointDesired, out Coordinate IntersectionPoint)
		{
			bool CircleSegmentIntersect = false;
			Coordinate IntersectionPoint_1 = new Coordinate();
			Coordinate IntersectionPoint_2 = new Coordinate();
			IntersectionPoint.Lat = 0;
			IntersectionPoint.Lon = 0;

			double x1 = LineSegment_Point1.Lon;
			double y1 = LineSegment_Point1.Lat;
			double x2 = LineSegment_Point2.Lon;
			double y2 = LineSegment_Point2.Lat;
			double x3 = CircleCenterPoint.Lon;
			double y3 = CircleCenterPoint.Lat;

			// Substituting the equation of the line into the circle gives a quadratic equation
			double a = ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
			double b = 2 * ((x2 - x1) * (x1 - x3) + (y2 - y1) * (y1 - y3));
			double r = CircleRadius / Conversions.MiletoNM(Math.Cos(CircleCenterPoint.Lat * deg2rad) * 69.17);
			double c = x3 * x3 + y3 * y3 + x1 * x1 + y1 * y1 - 2 * (x3 * x1 + y3 * y1) - (r * r);
			double Q = Math.Sqrt(b * b - 4 * a * c);

			if (Q > 0)
			{
				// Infinite Line intersects circle but now check if the Line Segment intersects.
				// If u is between 0 and 1 then the closest Line Segment point to the Circle Center (perpendicular) is between the Line Segment End Points.
				double u = ((x3 - x1) * (x2 - x1) + (y3 - y1) * (y2 - y1)) / ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
				if (((u > 0) && (u < 1))
					|| (FUL.Utils.Distance(CircleCenterPoint.Lat, CircleCenterPoint.Lon, LineSegment_Point1.Lat, LineSegment_Point1.Lon, FUL.Utils.DistanceUnits.nm) < CircleRadius)
					|| (FUL.Utils.Distance(CircleCenterPoint.Lat, CircleCenterPoint.Lon, LineSegment_Point2.Lat, LineSegment_Point2.Lon, FUL.Utils.DistanceUnits.nm) < CircleRadius)
				   )
				{
					CircleSegmentIntersect = true;

					if (PointDesired == true)
					{
						double u1 = (-b - Q) / (2 * a);
						IntersectionPoint_1.Lon = x1 + u1 * (x2 - x1);
						IntersectionPoint_1.Lat = y1 + u1 * (y2 - y1);
						x3 = IntersectionPoint_1.Lon;
						y3 = IntersectionPoint_1.Lat;
						u = ((x3 - x1) * (x2 - x1) + (y3 - y1) * (y2 - y1)) / ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
						if ((u > 0) && (u < 1))
							IntersectionPoint = IntersectionPoint_1;
						else
						{
							double u2 = (-b + Q) / (2 * a);
							IntersectionPoint_2.Lon = x1 + u2 * (x2 - x1);
							IntersectionPoint_2.Lat = y1 + u2 * (y2 - y1);
							IntersectionPoint = IntersectionPoint_2;
						}

					}
				}
			} // end-if line intersects circle in two places (Q > 0)

			return CircleSegmentIntersect;

		} // End CircleLineSegmentIntersect

		// --------------------------------------------------------------------------
		/// <summary>
		/// Determine and calculate the intersection of a line segment and an ellipse.
		/// </summary>
		/// <param name="EllipseCenterPoint"></param>
		/// <param name="SemiMajor"></param>
		/// <param name="SemiMinor"></param>
		/// <param name="LineSegment_Point1"></param>
		/// <param name="LineSegment_Point2"></param>
		/// <param name="IntersectionPoint"></param>
		/// <returns></returns>
		public static bool EllipseLineSegmentIntersect(Coordinate EllipseCenterPoint, double SemiMajor, double SemiMinor, Coordinate LineSegment_Point1, Coordinate LineSegment_Point2, out Coordinate IntersectionPoint)
		{
			bool EllipseSegmentIntersect = false;
			double Slope = 9999; // infinity slope
			IntersectionPoint.Lat = 0;
			IntersectionPoint.Lon = 0;

			double x1 = LineSegment_Point1.Lon - EllipseCenterPoint.Lon;
			double y1 = LineSegment_Point1.Lat - EllipseCenterPoint.Lat;
			double x2 = LineSegment_Point2.Lon - EllipseCenterPoint.Lon;
			double y2 = LineSegment_Point2.Lat - EllipseCenterPoint.Lat;
			if (x1 != x2)
				Slope = (y1 - y2) / (x1 - x2);
			double Y_Intercept = y1 - (Slope * x1);

			// Substituting the equation of the line into the Ellipse gives a quadratic equation
			double a = (SemiMinor * SemiMinor) + (SemiMajor * SemiMajor) * (Slope * Slope);
			double b = 2 * (SemiMajor * SemiMajor) * Slope * Y_Intercept;
			double c = (SemiMajor * SemiMajor) * ((Y_Intercept * Y_Intercept) - (SemiMinor * SemiMinor));
			double Q = Math.Sqrt(b * b - 4 * a * c);

			if (Q > 0)
			{
				// Infinite Line intersects Ellipse but now check if the Line Segment intersects.

				double x3 = 0;
				if ((x1 < x2) || ((x1 == x2) && (y1 < y2)))
					x3 = (-b - Q) / (2 * a);
				else
					x3 = (-b + Q) / (2 * a);

				double y3 = (Slope * x3 + Y_Intercept);
				double u = ((x3 - x1) * (x2 - x1) + (y3 - y1) * (y2 - y1)) / ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
				if ((u > 0) && (u < 1))
				{
					IntersectionPoint.Lon = x3 + EllipseCenterPoint.Lon;
					IntersectionPoint.Lat = y3 + EllipseCenterPoint.Lat;
					EllipseSegmentIntersect = true;
				}
			} // end-if line intersects Ellipse in two places (Q > 0)

			return EllipseSegmentIntersect;

		} // End EllipseLineSegmentIntersect

		// --------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given point is inside the given ellipse
		/// </summary>
		/// <param name="EllipseCenterPoint"></param>
		/// <param name="SemiMajor"></param>
		/// <param name="SemiMinor"></param>
		/// <param name="Point"></param>
		/// <returns></returns>
		public static bool PointInsideEllipse(double radius, Coordinate centerPoint, Coordinate point)
		{
			double semiMajor = GetEllipseSemiMajor(radius, centerPoint);
			double semiMinor = GetEllipseSemiMinor(semiMajor, centerPoint);

			return PointInsideEllipse(centerPoint, semiMajor, semiMinor, point);
		}

		public static bool PointInsideEllipse(Coordinate EllipseCenterPoint, double SemiMajor, double SemiMinor, Coordinate Point)
		{
			bool Inside = false;

			double boundary_x = Math.Sqrt(SemiMajor * SemiMajor * (1 - ((Point.Lat - EllipseCenterPoint.Lat) * (Point.Lat - EllipseCenterPoint.Lat) / (SemiMinor * SemiMinor))));
			if (boundary_x >= 0)
			{
				if ((Point.Lon >= EllipseCenterPoint.Lon) && (Point.Lon <= (boundary_x + EllipseCenterPoint.Lon)))
					Inside = true;
				else
				{
					if ((Point.Lon <= EllipseCenterPoint.Lon) && (Point.Lon >= (EllipseCenterPoint.Lon - boundary_x)))
						Inside = true;
				}
			}

			return Inside;
		} // End PointInsideEllipse

		public static double GetEllipseSemiMajor(double radius, Coordinate center)
		{
			return radius / Conversions.MiletoNM(Math.Cos(center.Lat * FUL.Utils.deg2rad) * 69.17);
		}

		public static double GetEllipseSemiMinor(double semiMajor, Coordinate center)
		{
			return semiMajor * Math.Cos(center.Lat * FUL.Utils.deg2rad);
		}

		// ------------------------------------------------------------------------
		/// <summary>
		/// Finds difference between two bearings (0 - 360)
		/// </summary>
		/// <param name="Heading1"></param>
		/// <param name="Heading2"></param>
		/// <returns></returns>
		public static int BearingDifference(int Heading1, int Heading2)
		{
			int HeadingDifference1 = 0;
			int HeadingDifference2 = 0;

			HeadingDifference1 = Heading1 - Heading2;
			HeadingDifference2 = Heading2 - Heading1;
			if (HeadingDifference1 < 0)
				HeadingDifference1 += 360;
			if (HeadingDifference2 < 0)
				HeadingDifference2 += 360;

			if (HeadingDifference1 < HeadingDifference2)
				return HeadingDifference1;
			else
				return HeadingDifference2;
		} // End BearingDifference

		public static List<FUL.Coordinate> GetRemainingFlightRoute(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints)
		{
			int index = -1;
			return GetRemainingFlightRoute(AircraftPosition, FlightRoutePoints, ref index);
		}

		// --------------------------------------------------------------------------
		/// <summary>
		/// Use the aircraft's current position as the first point in the remaining flight route.
		/// </summary>
		/// <param name="AircraftPosition">Aircraft lat/long Position</param>
		/// <param name="FlightRoutePoints">Flight Route Points (lat/long)</param>
		/// <returns>Remaining Flight Route</returns>
		public static List<FUL.Coordinate> GetRemainingFlightRoute(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints, ref int index)
		{
			return GetRemainingFlightRoute(AircraftPosition, FlightRoutePoints, false, ref index);
		}

		public static List<FUL.Coordinate> GetRemainingFlightRoute(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints, bool partialRoute, ref int index)
		{
			return GetRemainingFlightRoute(AircraftPosition, FlightRoutePoints, partialRoute, -1, ref index);
		}

		public static List<FUL.Coordinate> GetRemainingFlightRoute(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints, bool partialRoute, int currentHeading, ref int index)
		{
			List<FUL.Coordinate> RoutePoints = new List<FUL.Coordinate>();
			bool UseEntireRoute = true;
			int AircraftIndex = -1;
			index = -1;

			double delta = 1.0;
			double distance = FUL.Utils.Distance(AircraftPosition.Lat, AircraftPosition.Lon, FlightRoutePoints[FlightRoutePoints.Count - 1].Lat, FlightRoutePoints[FlightRoutePoints.Count - 1].Lon, DistanceUnits.nm);
			if (distance < 50)
				delta = 0.5;
			try
			{
				// Find which flight route leg the aircraft is currently on.
				for (int i = 0; i < FlightRoutePoints.Count - 1; i++)
				{
					double MinDeltaLat = delta;	// 1 degree. ~30 miles on either side of the flight route.
					double MinDeltaLon = delta;

					double RoutePointA_Lat = FlightRoutePoints[i].Lat;
					double RoutePointB_Lat = FlightRoutePoints[i + 1].Lat;
					// if the values are too close together, increase the distance.
					if (Math.Abs(RoutePointA_Lat - RoutePointB_Lat) < MinDeltaLat)
					{
						double offset = (MinDeltaLat - (Math.Abs(RoutePointA_Lat - RoutePointB_Lat))) / 2;
						if (RoutePointA_Lat > RoutePointB_Lat)
						{
							RoutePointA_Lat = RoutePointA_Lat + offset;
							RoutePointB_Lat = RoutePointB_Lat - offset;
						}
						else
						{
							RoutePointA_Lat = RoutePointA_Lat - offset;
							RoutePointB_Lat = RoutePointB_Lat + offset;
						}
					} // end-if points are too close

					double RoutePointA_Lon = FlightRoutePoints[i].Lon;
					double RoutePointB_Lon = FlightRoutePoints[i + 1].Lon;
					if (Math.Abs(LonDiff(RoutePointA_Lon, RoutePointB_Lon)) < MinDeltaLon)
					{
						double offset = (MinDeltaLon - (Math.Abs(LonDiff(RoutePointA_Lon, RoutePointB_Lon)))) / 2;
						if (LonDiff(RoutePointA_Lon, RoutePointB_Lon) > 0.0)
						{
							RoutePointA_Lon = RoutePointA_Lon + offset;
							RoutePointB_Lon = RoutePointB_Lon - offset;
						}
						else
						{
							RoutePointA_Lon = RoutePointA_Lon - offset;
							RoutePointB_Lon = RoutePointB_Lon + offset;
						}
					} // end-if points are too close

					double DeltaLatA = AircraftPosition.Lat - RoutePointA_Lat;
					double DeltaLatB = AircraftPosition.Lat - RoutePointB_Lat;
					double DeltaLonA = LonDiff(AircraftPosition.Lon, RoutePointA_Lon);
					double DeltaLonB = LonDiff(AircraftPosition.Lon, RoutePointB_Lon);

					if (((DeltaLatA >= 0 && DeltaLatB <= 0) || (DeltaLatA <= 0 && DeltaLatB >= 0))
						&& ((DeltaLonA >= 0 && DeltaLonB <= 0) || (DeltaLonA <= 0 && DeltaLonB >= 0)))
					{
						AircraftIndex = i;
						break;
					}
				} // end-for all flight route points

				if (AircraftIndex > -1)
				{
					RoutePoints.Add(AircraftPosition);	// Aircraft position will be the first point on the first leg.
					int startIndex = AircraftIndex + 1; //partialRoute ? AircraftIndex : AircraftIndex + 1;
					if (currentHeading > 0)
					{
						Coordinate p = FlightRoutePoints[startIndex];
						double range = 0, bearing = 0;

						if (FUL.Utils.RangeBearing(AircraftPosition.Lat, AircraftPosition.Lon, p.Lat, p.Lon, DistanceUnits.nm, ref range, ref bearing) && FUL.Utils.BearingDifference((int)bearing, currentHeading) > 90)
							startIndex++;
					}

					for (int j = startIndex; j < FlightRoutePoints.Count; j++)	// Add the remaining flight legs to the destination.
						RoutePoints.Add(FlightRoutePoints[j]);
					index = startIndex;
				}
				else
				{
					UseEntireRoute = true;
					// check for aircraft in close proximity to its destination airport.
					int NearDestinationAirportDistanceThreshold = 50; //nm
					if (FUL.Utils.Distance(AircraftPosition.Lat, AircraftPosition.Lon, FlightRoutePoints[FlightRoutePoints.Count - 1].Lat, FlightRoutePoints[FlightRoutePoints.Count - 1].Lon, Utils.DistanceUnits.nm)
						<= NearDestinationAirportDistanceThreshold)
					{
						// Use Aircraft position to Destination as route.
						RoutePoints.Add(AircraftPosition);
						RoutePoints.Add(FlightRoutePoints[FlightRoutePoints.Count - 1]);
						index = FlightRoutePoints.Count - 1;
						UseEntireRoute = false;
					}

					if (UseEntireRoute == true)
					{
						// check for aircraft in close proximity to its departure airport.
						int NearDepartureAirportDistanceThreshold = 30; //nm
						if (FUL.Utils.Distance(AircraftPosition.Lat, AircraftPosition.Lon, FlightRoutePoints[0].Lat, FlightRoutePoints[0].Lon, Utils.DistanceUnits.nm)
							<= NearDepartureAirportDistanceThreshold)
						{
							// Use Aircraft position to first route point as route.
							RoutePoints.Add(AircraftPosition);
							int startIndex = partialRoute ? 0 : 1;
							for (int j = startIndex; j < FlightRoutePoints.Count; j++)	// Add the remaining flight leg to the destination.
								RoutePoints.Add(FlightRoutePoints[j]);
							UseEntireRoute = false;
							index = startIndex;
						}
					}

					if (UseEntireRoute == true)
					{
						// Aircraft Position not found along route. Use entire route.
						RoutePoints.Add(AircraftPosition);
						double ShortestDistanceToWaypoint;
						AircraftIndex = FindClosestRoutePointIndex(AircraftPosition, FlightRoutePoints, partialRoute, out ShortestDistanceToWaypoint);
						if (AircraftIndex < 0)
							AircraftIndex = 0; // use all the points
						else if (currentHeading > 0)
						{
							Coordinate p = FlightRoutePoints[AircraftIndex];
							double range = 0, bearing = 0;

							if (FUL.Utils.RangeBearing(AircraftPosition.Lat, AircraftPosition.Lon, p.Lat, p.Lon, DistanceUnits.nm, ref range, ref bearing) && FUL.Utils.BearingDifference((int)bearing, currentHeading) > 90)
								AircraftIndex++;
						}
						
						for (int j = AircraftIndex; j < FlightRoutePoints.Count; j++)	// Add the remaining flight legs to the destination.
							RoutePoints.Add(FlightRoutePoints[j]);
						index = AircraftIndex;
					}
				} // end-else aircraft not found along route.

			}
			catch (Exception ex)
			{
				FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "Exception caught in FUL.Utils.GetRemainingFlightRoute \r\n" + ex);
				RoutePoints.Clear();
			}

			return RoutePoints;

		} // End GetRemainingFlightRoute

		// --------------------------------------------------------
		private static double LonDiff(double lon1, double lon2)
		{
			double diff = lon1 - lon2;
			if (diff < -180.0)
				diff += 360.0;
			if (diff > 180.0)
				diff -= 360;
			return diff;
		}

		// --------------------------------------------------------------------------
		/// <summary>
		/// This method finds the closest waypoint along the route from the given point.
		/// The index (into the route array) is returned as well as the distance itself.
		/// </summary>
		/// <param name="AircraftPosition"></param>
		/// <param name="Route"></param>
		/// <param name="ShortestToWaypoint"></param>
		/// <returns></returns>
		public static int FindClosestRoutePointIndex(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> Route, bool partialRoute, out double ShortestToWaypoint)
		{
			int RouteIndex = -1;
			double Distance = 0;
			ShortestToWaypoint = 1000000;

			int startIndex = partialRoute ? 0 : 1;
			for (int i = startIndex; i < Route.Count; i++) // Find closest point along route which is not the first point (departure airport)
			{
				Distance = FUL.Utils.Distance(AircraftPosition.Lat, AircraftPosition.Lon, Route[i].Lat, Route[i].Lon, FUL.Utils.DistanceUnits.nm);
				if (Distance < ShortestToWaypoint)
				{
					ShortestToWaypoint = Distance;
					RouteIndex = i;
				}
			}

			return RouteIndex;

		} // End FindClosestRoutePointIndex

		// --------------------------------------------------------------------------
		/// <summary>
		/// Find the distance from the aircraft's current position to the destination along
		/// the remaining flight route.
		/// If the route is unknown but the destination is known, the destination lat/lon will
		/// be the only coordinate in the FlightRoutePoints and the distance will be from
		/// the current aircraft position to the destination.
		/// </summary>
		/// <param name="AircraftPosition">Aircraft lat/long Position</param>
		/// <param name="FlightRoutePoints">Flight Route Points (lat/long)</param>
		/// <returns>Distance of Remaining Flight Route</returns>
		public static double GetDistanceOfRemainingFlightRoute(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints)
		{
			double Distance = 0;
			List<double> LegDistanceList = new List<double>(12);
			try
			{
				if (FlightRoutePoints.Count > 0)
				{
					if (FlightRoutePoints.Count == 1) // Flight Route is unknown. Find distance from Aircraft to Destination airport.
						Distance = FUL.Utils.Distance(AircraftPosition.Lat, AircraftPosition.Lon, FlightRoutePoints[0].Lat, FlightRoutePoints[0].Lon, Utils.DistanceUnits.nm);
					else
					{
						List<FUL.Coordinate> RemainingFlightRoutePoints = FUL.Utils.GetRemainingFlightRoute(AircraftPosition, FlightRoutePoints);
						Distance = GetFlightRouteDistance(RemainingFlightRoutePoints, DistanceUnits.nm, out LegDistanceList);
					}
				}
			}
			catch (Exception ex)
			{
				FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "Exception caught in FUL.Utils.GetDistanceOfRemainingFlightRoute \r\n" + ex);
				Distance = 0;
			}

			return Distance;

		} // End GetDistanceOfRemainingFlightRoute

		// --------------------------------------------------------------------------
		/// <summary>
		/// Calculate the distance along a flight route (planned route, flown route, remaining route, etc.)
		/// </summary>
		/// <param name="Route"></param>
		/// <param name="Units">specify nm, km, mi</param>
		/// <returns>Calulated Distance of Route</returns>
		public static double GetFlightRouteDistance(List<FUL.Coordinate> Route, Utils.DistanceUnits Units, out List<double>LegDistanceList)
		{
			double TotalRouteDistance = 0;
			LegDistanceList = new List<double>(Route.Count - 1);

			try
			{
				for (int i = 0; i < Route.Count - 1; i++)
				{
					LegDistanceList.Add(FUL.Utils.Distance(Route[i].Lat, Route[i].Lon, Route[i + 1].Lat, Route[i + 1].Lon, Units));
					TotalRouteDistance = TotalRouteDistance + LegDistanceList[i];
				}

			}
			catch (ArgumentException)
			{
				//FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "GetFlightRouteDistance: Unsupported Units " + Units.ToString() + "\r\n" + ae);
				TotalRouteDistance = 0;
			}
			catch (Exception)
			{
				//FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "Exception caught in FUL.Utils.GetFlightRouteDistance \r\n" + ex);
				TotalRouteDistance = 0;
			}

			return TotalRouteDistance;
		}
		// --------------------------------------------------------------------------
		/// <summary>
		/// If ETA is not available, call this method to get an Estimate of ETA.
		/// </summary>
		/// <param name="AircraftPosition"></param>
		/// <param name="FlightRoutePoints"></param>
		/// <param name="TimeStamp"></param>
		/// <param name="Speed"></param>
		/// <returns></returns>
		public static DateTime CalculateETA(FUL.Coordinate AircraftPosition, List<FUL.Coordinate> FlightRoutePoints, DateTime TimeStamp, int Speed)
		{
			DateTime CalculatedETA = new DateTime(1800, 1, 1);
			double theSpeed = 415;  // Default if not supplied.
			if (Speed > 0)
				theSpeed = Speed;

			try
			{
				double DistanceToDestinationAlongRoute = FUL.Utils.GetDistanceOfRemainingFlightRoute(AircraftPosition, FlightRoutePoints);
				double Seconds = (DistanceToDestinationAlongRoute / theSpeed) * 3600;
				int IntSeconds = Convert.ToInt32(Math.Round(Seconds, MidpointRounding.AwayFromZero));
				TimeSpan RemainingFlyTime = new TimeSpan(0, 0, 0, IntSeconds);
				CalculatedETA = TimeStamp.Add(RemainingFlyTime);
			}
			catch (Exception ex)
			{
				FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.ASD_Parse_Error, "CalculateETA", "Exception caught while calculating ETA." + "\r\n" + ex);
			}

			return CalculatedETA;

		} // End CalculateETA

		// --------------------------------------------------------------------------
		/// <summary>
		/// Populate TimeOverPoint for the List of Route points.
		/// Departure Time needs to be stored in the TimeOverPoint parmeter of the first Route Point.
		/// Either ETA needs to be stored in the TimeOverPoint parmeter of the last Route Point or Speed over point needs to be 
		/// populated in at least the first Route Point.
		/// </summary>
		/// <param name="Route"></param>
		public static double CalculateRouteTimeOverPoint(ref List<FUL.RoutePoints> Route)
		{
			double TotalRouteDistance = 0;

			try
			{
				DateTime DepartureTime = Route[0].TimeOverPoint;
				if (DepartureTime > DateTime.MinValue)
				{
					int InitialSpeed = Route[0].Speed;
					DateTime ETA = Route[Route.Count - 1].TimeOverPoint;
					if (ETA > DateTime.MinValue)
					{
						// Get total route distance and each Leg distance
						List<FUL.Coordinate> LocationList = new List<Coordinate>(Route.Count);
						List<double> LegDistanceList = new List<double>(Route.Count - 1);
						foreach (FUL.RoutePoints Point in Route)
							LocationList.Add(Point.location);
						TotalRouteDistance = GetFlightRouteDistance(LocationList, DistanceUnits.nm, out LegDistanceList);

						TimeSpan FlightTime = ETA.Subtract(DepartureTime);
						double PreviousLegsDistance = 0;
						double DistancePercentage;
						for (int i = 1; i < Route.Count - 1; i++)
						{
							FUL.RoutePoints Point = Route[i];
							DistancePercentage = (PreviousLegsDistance + LegDistanceList[i - 1]) / TotalRouteDistance;
							Point.TimeOverPoint = DepartureTime.Add(TimeSpan.FromMinutes(DistancePercentage * FlightTime.TotalMinutes));
							Route[i] = Point;
							PreviousLegsDistance = PreviousLegsDistance + LegDistanceList[i - 1];
						}
					}
					else if (InitialSpeed > int.MinValue)
					{
						// TODO

						// From FlightRouteIntesect. Needs modifying.
						//for (int i = intersectionEndIndex; i > 0; i--)
						//{
						//    Coordinate point1 = wbFlight.Flight.RoutePoints[i];
						//    Coordinate point2 = wbFlight.Flight.RoutePoints[i - 1];
						//    FlightLegDistance = FUL.Utils.Distance(point1.Lat, point1.Lon, point2.Lat, point2.Lon, FUL.Utils.DistanceUnits.mi);
						//    distanceToExit = distanceToExit + FlightLegDistance;

						//    if ((wbFlight.Flight.SpeedOverPoint != null) && (wbFlight.Flight.SpeedOverPoint.Count > 0) && (wbFlight.Flight.SpeedOverPoint[i - 1] > 0) && (MissingExitSpeedOverPointData == false))
						//        ExitIntersectionTimeBasedOnSpeedOverPoint = ExitIntersectionTimeBasedOnSpeedOverPoint.Add(new TimeSpan(0, 0, Convert.ToInt32((FlightLegDistance / wbFlight.Flight.SpeedOverPoint[i - 1]) * 3600)));
						//    else
						//        MissingExitSpeedOverPointData = true;
						//}
					}
				}
			}
			catch (Exception)
			{
				TotalRouteDistance = 0;
			}

			return TotalRouteDistance;
		} // end CalculateRouteTimeOverPoint

		// --------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputString"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		public static string WrapString(string inputString, int maxLength)
		{
			System.Text.StringBuilder newString = new System.Text.StringBuilder();
			try
			{
				if (!string.IsNullOrEmpty(inputString) && inputString.Length > maxLength)
				{
					string[] lines = inputString.Split('\n');
					foreach (string line in lines)
					{
						if (line.Length > maxLength)
						{
							char[] cutPossible = new char[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' };
							int startIndex = 0;
							while ((startIndex + maxLength) < line.Length)
							{
								string cutString = line.Substring(startIndex, maxLength);
								int cutIndex = cutString.LastIndexOfAny(cutPossible);
								if (cutIndex != -1)
								{
									newString.Append(cutString.Substring(0, cutIndex + 1)).Append("\n");
									startIndex += cutIndex + 1;
								}
								else
								{
									newString.Append(cutString).Append("\n");
									startIndex += cutString.Length;
								}
							}

							if (startIndex < line.Length)
								newString.Append(line.Substring(startIndex)).Append('\n');
						}
						else
							newString.Append(line).Append('\n');
					}
				}
				else
					return inputString;
			}
			catch { }

			return newString.ToString().TrimEnd('\n');
		}

		// http://williams.best.vwh.net/avform.htm#Intro
		// Cross track error: computer XTD (Distance off route)
		public static double DistanceOffCourse(double latA, double lonA, double latB, double lonB, double latC, double lonC, FUL.Utils.DistanceUnits units)
		{
			double distanceAC = 0;
			double courseAC = 0;
			if (!RangeBearingInRadians(latA, lonA, latC, lonC, ref distanceAC, ref courseAC))
				return double.MaxValue;

			double distanceAB = 0;
			double courseAB = 0;
			if (!RangeBearingInRadians(latA, lonA, latB, lonB, ref distanceAB, ref courseAB))
				return double.MaxValue;

			double XDT = Math.Asin(Math.Sin(distanceAC) * Math.Sin(courseAC - courseAB));
			switch (units)
			{
				case DistanceUnits.km:
					XDT *= EarthRadius_km;	// equatorial radius of 6378.137 kilometers
					break;
				case DistanceUnits.mi:
					XDT *= EarthRadius_sm;	// equatorial radius of 3963.19 statute miles
					break;
				case DistanceUnits.nm:
					XDT *= rad2deg * 60;
					break;
				default:
					throw (new ArgumentException());
			}

			return Math.Abs(XDT);
		}

		public static double DotProduct(FUL.Coordinate PointA, FUL.Coordinate PointB, FUL.Coordinate PointC)
		{
			Coordinate vectorAB = new Coordinate();
			Coordinate vectorBC = new Coordinate();
			vectorAB.Lat = PointB.Lat - PointA.Lat;
			vectorAB.Lon = PointB.Lon - PointA.Lon;
			vectorBC.Lat = PointC.Lat - PointB.Lat;
			vectorBC.Lon = PointC.Lon - PointB.Lon;
			return vectorAB.Lat * vectorBC.Lat + vectorAB.Lon * vectorBC.Lon;
		}

		// http://williams.best.vwh.net/avform.htm#Intersection
		// Intersecting radials 
		// Compute the latitude(lat3) and longitude(lon3) of an intersection formed by the true bearing(crs13) from point1 and the true bearing(crs23) from point2
		public static bool GetIntersectingRadialsPoint(double lat1, double lon1, double lat2, double lon2, int bearing13, int bearing23, out double lat3, out double lon3)
		{
			lat3 = 999;
			lon3 = 999;

			lat1 *= deg2rad;
			lon1 *= deg2rad;
			lat2 *= deg2rad;
			lon2 *= deg2rad;

			double dst12 = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat1 - lat2) / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2), 2)));

			double crs12 = 0;
			double crs21 = 0;
			if (Math.Sin(lon1 - lon2) < 0)
			{
				crs12 = Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(dst12)) / (Math.Sin(dst12) * Math.Cos(lat1)));
				crs21 = 2 * Math.PI - Math.Acos((Math.Sin(lat1) - Math.Sin(lat2) * Math.Cos(dst12)) / (Math.Sin(dst12) * Math.Cos(lat2)));
			}
			else
			{
				crs12 = 2 * Math.PI - Math.Acos((Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(dst12)) / (Math.Sin(dst12) * Math.Cos(lat1)));
				crs21 = Math.Acos((Math.Sin(lat1) - Math.Sin(lat2) * Math.Cos(dst12)) / (Math.Sin(dst12) * Math.Cos(lat2)));
			}

			double crs13 = bearing13 * deg2rad;
			double crs23 = bearing23 * deg2rad;
			double ang1 = (crs13 - crs12 + Math.PI) % (2 * Math.PI) - Math.PI;
			double ang2 = (crs21 - crs23 + Math.PI) % (2 * Math.PI) - Math.PI;

			if (Math.Sin(ang1) == 0 && Math.Sin(ang2) == 0)
				//infinity of intersections
				return false;
			else if (Math.Sin(ang1) * Math.Sin(ang2) < 0)
				//intersection ambiguous
				//TODO: need to check
				return false;
			else
			{
				ang1 = Math.Abs(ang1);
				ang2 = Math.Abs(ang2);
				double ang3 = Math.Acos(-Math.Cos(ang1) * Math.Cos(ang2) + Math.Sin(ang1) * Math.Sin(ang2) * Math.Cos(dst12));
				double dst13 = Math.Atan2(Math.Sin(dst12) * Math.Sin(ang1) * Math.Sin(ang2), Math.Cos(ang2) + Math.Cos(ang1) * Math.Cos(ang3));
				lat3 = Math.Asin(Math.Sin(lat1) * Math.Cos(dst13) + Math.Cos(lat1) * Math.Sin(dst13) * Math.Cos(crs13));
				double dlon = Math.Atan2(Math.Sin(crs13) * Math.Sin(dst13) * Math.Cos(lat1), Math.Cos(dst13) - Math.Sin(lat1) * Math.Sin(lat3));
				lon3 = (lon1 - dlon + Math.PI) % (2 * Math.PI) - Math.PI;
				lat3 *= rad2deg;
				lon3 *= rad2deg;
			}

			return true;
		}

		// ---------------------------------------------------------------------
		/// <summary>
		/// This is the midpoint along a great circle path between the two points.
		/// Just as the initial bearing may vary from the final bearing, the midpoint may not be located half-way between latitudes/longitudes; 
		/// for example, the midpoint between 35°N,45°E and 35°N,135°E is around 45°N,90°E.
		/// Reference: http://www.movable-type.co.uk/scripts/latlong.html
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <returns></returns>
		public static FUL.Coordinate LatLonMidpoint(FUL.Coordinate A, FUL.Coordinate B)
		{
			FUL.Coordinate MidPoint = new Coordinate(true);

			try
			{
				double lat1 = A.Lat * deg2rad;
				double lon1 = A.Lon * deg2rad;
				double lat2 = B.Lat * deg2rad;
				double lon2 = B.Lon * deg2rad;
				double dLon = Math.Abs(lon1 - lon2);
				double Bx = Math.Cos(lat2) * Math.Cos(dLon);
				double By = Math.Cos(lat2) * Math.Sin(dLon);
				MidPoint.Lat = rad2deg * (Math.Atan2(Math.Sin(lat1) + Math.Sin(lat2), Math.Sqrt((Math.Cos(lat1) + Bx) * (Math.Cos(lat1) + Bx) + By * By)));
				MidPoint.Lon = rad2deg * (lon1 + Math.Atan2(By, Math.Cos(lat1) + Bx));
			}
			catch (Exception ex)
			{
				FUL.FileWriter.WriteLine(true, FileWriter.EventType.Error, "LatLonMidpoint calculation Exception \r\n" + ex);
			}

			return MidPoint;
		} // End LatLonMidpoint

		//Copy from AA_RouteParser 
		public static Regex OceanicPointPattern = new Regex("^[0-9]{2}00(00)?[NS]/[0-9]{3}00(00)?[EW]$");
		public static Regex LatitudeLongitudePattern = new Regex("^[0-9]{2}(\\d\\d)?(\\d\\d)?[NS](/)?[0-9]{2,3}(\\d\\d)?(\\d\\d)?[EW]$");
		public static Regex IntlLatitudeLongitudePattern = new Regex("^[NS][0-9]{2}(\\d\\d)?(\\d\\d)?(/)?[EW][0-9]{2,3}(\\d\\d)?(\\d\\d)?$");
		public static Regex RadialDMEPattern = new Regex("^.*[a-zA-Z]+.*[0-9]{6}$");
		public static Regex SpeedFlightLevelPattern = new Regex("([KkNn]\\d{4}|[Mm]\\d{3})([FfAa]\\d{3}|[SsMn]\\d{4})$");
		/// <summary>
		/// Does this string match a simple NAV (Navaid) Radial (Bearing) pattern of 3 letters followed by 3 numbers.
		/// Example:  BAL170 ... BAL is the NAV designator.  170 is the Radial or bearing in degrees.
		/// </summary>
		public static Regex NAVRadialPattern = new Regex("^[a-zA-Z]{3}[0-9]{3}$");

		// Minutes & seconds are 0s
		public static bool IsOceanicPoint(string name)
		{
			if (!OceanicPointPattern.IsMatch(name))
				return false;

			return true;
		}

		// Include oceanic points & non-oceation points
		// Pending zero if longitude degree is only 2-digit
		public static bool IsLatitudeLongitudePoint(ref string name)
		{
			if (IntlLatitudeLongitudePattern.IsMatch(name))
			{
				int index = name.IndexOfAny(new char[] { 'E', 'W' });
				string temp = name.Substring(index - 1, 1);
				if (temp.Equals("/"))
				{
					string lat = name.Substring(1, index - 2);
					string latDirection = name.Substring(0, 1);

					string lon = name.Substring(index + 1);
					string lonDirection = name.Substring(index, 1);

					name = string.Concat(lat, latDirection, "/", lon, lonDirection);
				}
				else
				{
					string lat = name.Substring(1, index - 1);
					string latDirection = name.Substring(0, 1);

					string lon = name.Substring(index + 1);
					string lonDirection = name.Substring(index, 1);

					name = string.Concat(lat, latDirection, lon, lonDirection);
				}
			}

			if (LatitudeLongitudePattern.IsMatch(name))
			{
				int index = name.IndexOfAny(new char[] { 'N', 'S' });
				string temp = name.Substring(index + 1, 1);
				if (temp.Equals("/"))
				{
					temp = name.Substring(index + 2);
				}
				else
					temp = name.Substring(index + 1);

				if (temp.Length % 2 != 0)
					name = name.Replace(temp, temp.Insert(0, "0"));

				return true;
			}

			return false;
		}

		// Convert string Sexagesimal to decimal degrees.
		// ABC.DEFGHI     degress=ABC minutes=DE seconds=FG fractional seconds = HI
		public static double ConvertSexagesimalToDecimal(string value)
		{
			double DecimalDegrees = 0;
			bool negative = false;

			try
			{
				value = value.Trim();
				string[] parts = value.Split('.');
				if (parts[0].Length > 0)
					DecimalDegrees = Convert.ToDouble(parts[0]);
				if ((DecimalDegrees < 0) || (value.StartsWith("-")))
				{
					negative = true;
					DecimalDegrees = Math.Abs(DecimalDegrees);
				}

				parts[1] = parts[1].PadRight(6, '0');
				double minutes = Convert.ToDouble(parts[1].Substring(0, 2));
				double seconds = Convert.ToDouble(parts[1].Substring(2, 2));
				seconds = seconds + ((Convert.ToDouble(parts[1].Substring(4, 2))) / 100);

				DecimalDegrees = DecimalDegrees + (minutes / 60) + (seconds / 3600);
				if (negative == true)
					DecimalDegrees = DecimalDegrees * -1;
			}
			catch
			{
				DecimalDegrees = -181;
			}

			return DecimalDegrees;
	  }


		// Convert double precision degrees to a Sexagesimal string.
		// Sexagesima: ABC.DEFGHI     degress=ABC minutes=DE seconds=FG fractional seconds = HI
		public static string ConvertDoubleDegreesToSexagesimal(double dblDegrees)
		{
			bool isNegative = dblDegrees < 0.0;
			double absDegrees = isNegative ? -dblDegrees : dblDegrees;
			int nDegrees = (int)absDegrees;
			double dblMinutes = (absDegrees - nDegrees) * 60.0;
			int nMinutes = (int)dblMinutes;
			if (nMinutes > 59) nMinutes = 59;
			double dblSeconds = (dblMinutes - nMinutes) * 60.0;
			int nSeconds = (int)dblSeconds;
			if (nSeconds > 59) nSeconds = 59;
			int nFracSeconds = (int)((dblSeconds - nSeconds) * 100);
			if (nFracSeconds > 99) nFracSeconds = 99;

			return (isNegative ? "-" : "") +
				   nDegrees.ToString() + "." + 
				   nMinutes.ToString("D2") + 
				   nSeconds.ToString("D2") + 
				   nFracSeconds.ToString("D2");
		}

		// Such as 4012N/05023E
		// These are temporary points
		public static bool ConvertLatLonToDecimal(string latLon, ref double latitude, ref double longitude)
		{
			string latitudeString = string.Empty;
			string longitudeString = string.Empty;

			int index = latLon.IndexOf('/');
			if (index < 0)
			{
				index = latLon.IndexOfAny(new char[] { 'N', 'S' });
				latitudeString = latLon.Substring(0, index + 1);
				longitudeString = latLon.Substring(index + 1);
			}
			else
			{
				latitudeString = latLon.Substring(0, index);
				longitudeString = latLon.Substring(index + 1);
			}

			int latitudeDirection = 1;
			if (latitudeString.EndsWith("S"))
				latitudeDirection = -1;
			string latValue = latitudeString.Substring(0, latitudeString.Length - 1);

			double tempLatitude = CalculateDegrees(true, latValue);
			if (tempLatitude > 90)
				return false;

			int longitudeDirection = 1;
			if (longitudeString.EndsWith("W"))
				longitudeDirection = -1;
			string lonValue = longitudeString.Substring(0, longitudeString.Length - 1);

			double tempLongitude = CalculateDegrees(false, lonValue);
			if (tempLongitude > 180)
				return false;

			latitude = Math.Round(latitudeDirection * tempLatitude, 2);
			longitude = Math.Round(longitudeDirection * tempLongitude, 2);

			return true;
		}

		private static double CalculateDegrees(bool latitude, string coordinate)
		{
			double decimalValue = 999.0;
			int degree = 0;
			int index = 0;
			if (latitude)
			{
				degree = Convert.ToInt32(coordinate.Substring(0, 2));
				if ((degree < 0) || (degree > 90))
					return decimalValue;
				index += 2;
			}
			else
			{
				degree = Convert.ToInt32(coordinate.Substring(0, 3));
				if ((degree < 0) || (degree > 180))
					return decimalValue;
				index += 3;
			}

			int minutes = 0;
			if (index < coordinate.Length)
			{
				minutes = Convert.ToInt32(coordinate.Substring(index, 2));
				if ((minutes < 0) || (minutes > 59))
					return decimalValue;
				index += 2;
			}

			int seconds = 0;
			if (index < coordinate.Length)
			{
				seconds = Convert.ToInt32(coordinate.Substring(index, 2));
				if ((seconds < 0) || (seconds > 59))
					return decimalValue;
			}

			//Decimal Degrees = Degrees + (Minutes/60) + (Seconds/3600) 
			decimalValue = degree + ((double)minutes / 60.0) + ((double)seconds / 3600.0);
			return decimalValue;
		}

		public static string ConvertDecimalToLatLon(double x, double y)
		{
			if (double.IsNaN(x) || double.IsNaN(y))
				return string.Empty;

			int xdeg = (int)x;
			double xddeg = Math.Abs(x - xdeg);
			int xmin = (int)Math.Round(xddeg * 60);
			if (xmin == 60)
			{
				xdeg += Math.Sign(x); // add or subtract 1 deg
				xmin = 0;
			}

			int ydeg = (int)y;
			double yddeg = Math.Abs(y - ydeg);
			int ymin = (int)Math.Round(yddeg * 60);
			if (ymin == 60)
			{
				ydeg += Math.Sign(y); // add or subtract 1 deg
				ymin = 0;
			}

			string strX, strY;

			if (Math.Sign(x) >= 0)
				strX = xdeg.ToString("000") + xmin.ToString("00E");
			else
			{
				xdeg *= -1;
				strX = xdeg.ToString("000") + xmin.ToString("00W");
			}

			if (Math.Sign(y) >= 0)
				strY = ydeg.ToString("00") + ymin.ToString("00N");
			else
			{
				ydeg *= -1;
				strY = ydeg.ToString("00") + ymin.ToString("00S");
			}

			return strY + "/" + strX;
		}

		public static bool IsStringNumeric(string strText, bool integer, int min, int max)
		{
			bool retVal = true;
			float f;
			try
			{
				if (!float.TryParse(strText, out f))
				{
					retVal = false;
					return retVal;
				}

				if (f < min || f > max)
				{
					retVal = false;
					return retVal;
				}

				if (integer && (f % 1 > 0))
				{
					retVal = false;
					return retVal;
				}

			}
			catch
			{
				retVal = false;
			}
			return retVal;
		}

		public static FlightRules GetAirportFlightRules(int conditionCeiling, float conditionVisiblity)
		{
			int ceiling = 0, visibility = 0, checkvalue = 0;
			int debugval = conditionCeiling; // Hundreds of ft
			if (conditionCeiling == 999 || conditionCeiling == 99900 || conditionCeiling < 0)
				ceiling = 0; // no data
			else if (conditionCeiling >= 30)
				ceiling = 1; //VFR
			else if (conditionCeiling >= 10)
				ceiling = 2; //MVFR
			else if (conditionCeiling >= 5)
				ceiling = 3; //IFR
			else if (conditionCeiling >= 2)
				ceiling = 4; //LIFR
			else
				ceiling = 5; //BCAT1

			float debugval2 = conditionVisiblity; // In statute miles
			if (conditionVisiblity == (float)-99.9 || conditionVisiblity < 0)
				visibility = 0; //no data
			else if (conditionVisiblity >= 5)
				visibility = 1; // VFR
			else if (conditionVisiblity >= 3)
				visibility = 2; //MVFR
			else if (conditionVisiblity >= 1)
				visibility = 3; //IFR
			else if (conditionVisiblity >= .5)
				visibility = 4; //LIFR
			else
				visibility = 5; //BCAT1

			if (ceiling == 0 || visibility == 0)
				checkvalue = 0;
			else if (ceiling > visibility)
				checkvalue = ceiling;
			else
				checkvalue = visibility;

			return (FUL.FlightRules)Enum.ToObject(typeof(FUL.FlightRules), checkvalue);
		}

		public static System.Drawing.Color GetContrastingTextColor(System.Drawing.Color backgroundColor)
		{
			int bkgColorBrightness = ((backgroundColor.R * 299) + (backgroundColor.G * 587) + (backgroundColor.B * 114)) / 1000;
			int whiteBrightness = ((System.Drawing.Color.White.R * 299) + (System.Drawing.Color.White.G * 587) + (System.Drawing.Color.White.B * 114)) / 1000;

			return (Math.Abs(bkgColorBrightness - whiteBrightness) > 125) ? System.Drawing.Color.White : System.Drawing.Color.Black;
		}

		public static System.Drawing.Color GetContrastingLinkColor(System.Drawing.Color backgroundColor)
		{
			int bkgColorBrightness = ((backgroundColor.R * 299) + (backgroundColor.G * 587) + (backgroundColor.B * 114)) / 1000;
			int aquaBrightness = ((System.Drawing.Color.Aqua.R * 299) + (System.Drawing.Color.Aqua.G * 587) + (System.Drawing.Color.Aqua.B * 114)) / 1000;

			return (Math.Abs(bkgColorBrightness - aquaBrightness) > 100) ? System.Drawing.Color.Aqua : System.Drawing.Color.Blue;
		}

		public static string[] SplitMessage(string message, int lineSize, int messageSize)
		{			
			string[] lines = message.Split('\n');

			List<string> messageList = new List<string>();
			System.Text.StringBuilder currentMessage = new System.Text.StringBuilder();
					
			foreach (string currentLine in lines)
			{
				List<string> wrappedLines = WrapLine(currentLine, lineSize);

				foreach (string line in wrappedLines)
				{
					string currWrappedLine = line;

					if (currentMessage.Length > messageSize) 
					{
						messageList.Add(currentMessage.ToString());
						currentMessage.Clear();
					}
					else if (currentMessage.Length + currWrappedLine.Length > messageSize)
					{
						string line1 = string.Empty;
						string line2 = string.Empty;
						bool splitted = SplitLine(currWrappedLine, messageSize - currentMessage.Length, out line1, out line2);
						if (splitted)
						{
							if (currentMessage.Length > 0)
								currentMessage.Append("\n");
							currentMessage.Append(line1);
							currWrappedLine = line2;
						}
						messageList.Add(currentMessage.ToString());
						currentMessage.Clear();
					}
					
					if (currentMessage.Length > 0)
						currentMessage.Append("\n");
					currentMessage.Append(currWrappedLine);
				}
			}

			messageList.Add(currentMessage.ToString());

			if (messageList.Count > 1)
			{
				for (int i = 0; i < messageList.Count; i++)
				{
					messageList[i] = string.Format("{0}\nPart {1} of {2}", messageList[i], i + 1, messageList.Count);
				}
			}
			
			return messageList.ToArray();
		}

		// Split line by size and avoid splitting words 
		public static bool SplitLine(string text, int size, out string firstLine, out string secondLine)
		{
			string line1 = string.Empty;
			string line2 = text;
			
			firstLine = string.Empty;
			secondLine = text;

			System.Text.StringBuilder newLine = new System.Text.StringBuilder();
		
			while (newLine.Length + line1.Length <= size)
			{
				newLine.Append(line1);
				secondLine = line2;

				int index = line2.IndexOf(' ');
				if (index == -1)
					break;

				if (index == 0)
				{
					line1 = " ";
					line2 = line2.Substring(1);
				}
				else
				{
					line1 = line2.Substring(0, index);
					line2 = line2.Substring(index);
				}
			}
					
			if (newLine.Length == 0)
				return false;

			firstLine = newLine.ToString();
			return true;
		}
		
		private static List<string> WrapLine(string lineText, int maxSize)
		{
			List<string> lines = new List<string>();

			if (maxSize <= 0 || lineText.Length < maxSize)
				lines.Add(lineText);
			else
			{
				string[] words = lineText.Split(' ');
				System.Text.StringBuilder currentLine = new System.Text.StringBuilder();

				foreach (string word in words)
				{
					string currentWord = word;
					if (string.IsNullOrEmpty(currentWord))
						currentWord = " ";

					if ((currentLine.Length > maxSize) || (currentLine.Length + currentWord.Length) > maxSize)
					{
						lines.Add(currentLine.ToString());
						currentLine.Clear();
					}

					if (currentLine.Length > 0)
						currentLine.Append(" ");
					currentLine.Append(currentWord);
				}

				lines.Add(currentLine.ToString());
			}

			return lines;
		}

		//public static string[] WrapMessage(string message, int lineSize, int messageSize)
		//{
		//    List<string> messageList = new List<string>();
		//    System.Text.StringBuilder currentMessage = new System.Text.StringBuilder();

		//    //Get lines
		//    List<string> lines = new List<string>(message.Split('\n'));

		//    //Iterate line by line
		//    for (int i = 0; i < lines.Count; i++)
		//    {
		//        string currentLine = lines[i];
		//        currentMessage.Append("\n");
		//        int remainingSize = messageSize - currentMessage.Length;

		//        if (lineSize <= 0 || currentLine.Length <= lineSize)
		//        {
		//            if (currentLine.Length <= remainingSize)
		//            {
		//                if (currentMessage.Length > 0)
		//                    currentMessage.Append("\n");
		//                currentMessage.Append(currentLine);
		//                continue;
		//            }
		//            else
		//            {
		//                currentMessage.AppendLine(currentLine.Substring(0, remainingSize));
		//                messageList.Add(currentMessage.ToString());
		//                //Start a new message
		//                currentMessage = new System.Text.StringBuilder(currentLine.Substring(remainingSize));
		//            }
		//        }
		//        else
		//        {
		//            //Split first
		//            if (remainingSize > 0 && currentLine.Length > remainingSize)
		//            {
		//                string line1 = currentLine.Substring(0, remainingSize);
		//                lines.RemoveAt(i);
		//                lines.Insert(i, line1);
		//                string line2 = currentLine.Substring(remainingSize);
		//                lines.Insert(i+1, line2);
		//                i--;
		//            }
		//            else
		//            {

		//                List<string> wrappedLines = WrapLine(currentLine, lineSize);

		//                foreach (string currWrappedLine in wrappedLines)
		//                {
		//                    if (currentMessage.Length >= messageSize)
		//                    {
		//                        messageList.Add(currentMessage.ToString());
		//                        currentMessage.Clear();
		//                    }
		//                    //else if (currentMessage.Length + currWrappedLine.Length > messageSize)
		//                    //{
		//                    //    //TODO: move part line
		//                    //}

		//                    if (currentMessage.Length > 0)
		//                        currentMessage.Append("\n");
		//                    currentMessage.AppendLine(currWrappedLine);
		//                }
		//            }
		//        }
		//    }

		//    messageList.Add(currentMessage.ToString());

		//    return messageList.ToArray();
		//}

		/// <summary>
		/// This method takes a space separated string of lat,lon or lon,lat points and converts them
		/// to a List of FUL.Coordinates.
		/// </summary>
		/// <param name="PointsString"></param>
		/// <param name="LatFirst"></param>
		/// <returns></returns>
		public static List<FUL.Coordinate> ConvertStringPointsToCoordinateList(string PointsString, bool LatFirst)
		{
			List<FUL.Coordinate> PointList = new List<FUL.Coordinate>();
			double lat;
			double lon;
			try
			{
				string[] StringPointsArray = PointsString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string location in StringPointsArray)
				{
					int commaIndex = location.IndexOf(',');
					if (LatFirst == true)
					{
						lat = double.Parse(location.Substring(0, commaIndex));
						lon = double.Parse(location.Substring(commaIndex + 1, location.Length - commaIndex - 1));
					}
					else
					{
						lon = double.Parse(location.Substring(0, commaIndex));
						lat = double.Parse(location.Substring(commaIndex + 1, location.Length - commaIndex - 1));
					}
					FUL.Coordinate pt = new FUL.Coordinate(lon, lat);
					PointList.Add(pt);
				}
			}
			catch
			{
				PointList.Clear();
			}
			return PointList;
		}

		/// <summary>
		/// Get the location of the specified airport.
		/// If Airport data is too old, get latest set.
		/// </summary>
		/// <param name="AirportICAO"></param>
		/// <returns></returns>
		public static FUL.Coordinate GetAirportLocation(string AirportICAO, out DateTime ValidTime)
		{
			FUL.Coordinate location = new FUL.Coordinate(true);
			ValidTime = DateTime.MinValue;

			FUL.Airport theAirport = theAirportCollection[AirportICAO];
			if (theAirport != null)
			{
				location = new FUL.Coordinate(theAirport.Longitude, theAirport.Latitude);
				ValidTime = theAirport.ValidTime;
			}

			return location;
		}

		// ------------------------------------------------------------------------------
		public static String PrettyPrint(String XML)
		{
			String Result = string.Empty;

			MemoryStream MS = new MemoryStream();
			System.Xml.XmlTextWriter W = new System.Xml.XmlTextWriter(MS, System.Text.Encoding.Unicode);
			System.Xml.XmlDocument D = new System.Xml.XmlDocument();

			try
			{
				// Load the XmlDocument with the XML. 
				D.LoadXml(XML);

				W.Formatting = System.Xml.Formatting.Indented;

				// Write the XML into a formatting XmlTextWriter 
				D.WriteContentTo(W);
				W.Flush();
				MS.Flush();

				// Have to rewind the MemoryStream in order to read 
				// its contents. 
				MS.Position = 0;

				// Read MemoryStream contents into a StreamReader. 
				StreamReader SR = new StreamReader(MS);

				// Extract the text from the StreamReader. 
				String FormattedXML = SR.ReadToEnd();

				Result = FormattedXML;
			}
			catch (System.Xml.XmlException)
			{
			}

			MS.Close();
			W.Close();

			return Result;
		}

		/// <summary>
		/// Remove consecutive duplicates in a string
		/// </summary>
		/// <param name="inputString">a string that may have duplicates</param>
		/// <param name="seperator">a special character that seperates tokens in inputString </param>
		/// <returns>A similar string without consecutive duplicates </returns>
		public static string RemoveDuplicates(string inputString, char seperator)
		{
			string[] points = inputString.Split(seperator);

			List<string> pointList = new List<string>();
			string prevPoint = points[0].Trim();
			pointList.Add(prevPoint);

			for (int j = 1; j < points.Length; j++)
			{
				string currentPoint = points[j].Trim();
				if (currentPoint.Equals(prevPoint))
					continue;
				pointList.Add(currentPoint);
				prevPoint = currentPoint;
			}

			return string.Join(seperator.ToString(), pointList);
		}

		public static double NormalizeLongitude(double lon)
		{
			if (lon > 180)
				lon -= 360;
			else if (lon < -180)
				lon += 360;

			return lon;
		}

		public static bool IsDoubleEqual(double d1, double d2)
		{
			return Math.Abs(d1 - d2) <= 0.0001;
		}

        public static double DepthOfContaminationStringToDouble(string text)
        {
            double Result = double.NaN;

            switch (text.Trim().ToUpper())
            {
                case "THIN": Result = 0.125; break;
                case "1/4 IN": Result = 0.25; break;
                case "1/2 IN": Result = 0.5; break;
                case "3/4 IN": Result = 0.75; break;
                case "1 IN": Result = 1.0; break;
                case "2 IN": Result = 2.0; break;
                case "3 IN": Result = 3.0; break;
                case "4 IN": Result = 4.0; break;
                case "5 IN": Result = 5.0; break;
                case "6 IN": Result = 6.0; break;
                case "7 IN": Result = 7.0; break;
                case "8 IN": Result = 8.0; break;
                case "9 IN": Result = 9.0; break;
                case "10 IN": Result = 1.0; break;
                case "11 IN": Result = 1.0; break;
                case "12 IN": Result = 1.0; break;
                case "> 12 IN": Result = 12.1; break;
                default: Result = double.NaN; break; // "UNKNOWN"
            }
            return Result;
        }

        //public static void SignXML(ref XmlDocument XML, XmlNamespaceManager nsmgr)
        //{
        //    try
        //    {
        //        XMLUtils.CustomIdSignedXml signedDoc = new XMLUtils.CustomIdSignedXml(XML);
        //        string[] nodes = new string[1];

        //        XmlNode UniqueIDNode = XML.DocumentElement.GetElementsByTagName("ACARSMessageUniqueID")[0];
        //        string UniqueIDStr = "#" + UniqueIDNode.Attributes["Id"].Value;


        //        // enveloped Signature transform with exclusive canonicalization
        //        XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
        //        Reference reference = new Reference(UniqueIDStr);
        //        reference.AddTransform(env);
        //        reference.AddTransform(new XmlDsigExcC14NTransform(false));

        //        // signed info canonicalization
        //        signedDoc.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

        //        signedDoc.AddReference(reference);

        //        // get the key out of the local machine store
        //        // (local machine certs are in the personal (aka "My") store of the local machine store)
        //        X509Certificate2 signingCert = GetCertificateByFriendlyName("WSISAMLSigningKey", StoreName.My, StoreLocation.LocalMachine);

        //        // verify private key is not null
        //        if (signingCert.PrivateKey != null)
        //        {

        //            // sign away!
        //            signedDoc.SigningKey = signingCert.PrivateKey;
        //            signedDoc.ComputeSignature();

        //            // append signature to root element
        //            XML.DocumentElement.AppendChild(XML.ImportNode(signedDoc.GetXml(), true));
        //        }
        //        //else
        //        // Unable to read certificate our of machine store

        //    }
        //    catch (Exception ex)
        //    {
        //        //WriteServiceErrors.WriteServiceError((new StackFrame()).GetMethod().Name, ex.Message, ex.StackTrace, string.Empty, ErrorSource.WebService);
        //    }

        //}

        //public static string SignXML2(XmlDocument XML, XmlNamespaceManager nsmgr)
        //{
        //    string Signature = string.Empty;
        //    try
        //    {
        //        XMLUtils.CustomIdSignedXml signedDoc = new XMLUtils.CustomIdSignedXml(XML);
        //        string[] nodes = new string[1];

        //        XmlNode UniqueIDNode = XML.DocumentElement.GetElementsByTagName("ACARSMessageUniqueID")[0];
        //        string UniqueIDStr = "#" + UniqueIDNode.Attributes["Id"].Value;

        //        // enveloped Signature transform with exclusive canonicalization
        //        XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
        //        Reference reference = new Reference(UniqueIDStr);
        //        reference.AddTransform(env);
        //        reference.AddTransform(new XmlDsigExcC14NTransform(false));

        //        // signed info canonicalization
        //        signedDoc.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        //        // Set the InclusiveNamespacesPrefixList property.        
        //        XmlDsigExcC14NTransform canMethod = (XmlDsigExcC14NTransform)signedDoc.SignedInfo.CanonicalizationMethodObject;
        //        canMethod.InclusiveNamespacesPrefixList = "ds";


        //        signedDoc.AddReference(reference);

        //        // get the key out of the local machine store
        //        // (local machine certs are in the personal (aka "My") store of the local machine store)
        //        //X509Certificate2 signingCert = GetCertificateByFriendlyName("WSISAMLSigningKey", StoreName.My, StoreLocation.LocalMachine); // Jim Flynn private key
        //        X509Certificate2 signingCert = GetCertificateByFriendlyName("WSI's Symantec Corporation ID", StoreName.My, StoreLocation.LocalMachine); // Jim Flynn private key

        //        // verify private key is not null
        //        if (signingCert.PrivateKey != null)
        //        {

        //            // sign away!
        //            signedDoc.SigningKey = signingCert.PrivateKey;
        //            signedDoc.ComputeSignature();

        //            XmlElement Ele =  signedDoc.GetXml();
        //            System.IO.StringWriter strWriter = new System.IO.StringWriter();
        //            strWriter.Write(Ele.OuterXml);
        //            Signature = strWriter.ToString();
        //            // add namespace ds
        //            Signature = Signature.Replace("<Signature xmlns=", "<ds:Signature xmlns:ds=");
        //            Signature = Signature.Replace("</Signature>", "</ds:Signature>");
        //            Signature = Signature.Replace("<SignedInfo>", "<ds:SignedInfo>");
        //            Signature = Signature.Replace("</SignedInfo>", "</ds:SignedInfo>");
        //            Signature = Signature.Replace("<CanonicalizationMethod", "<ds:CanonicalizationMethod");
        //            Signature = Signature.Replace("</CanonicalizationMethod", "</ds:CanonicalizationMethod");
        //            Signature = Signature.Replace("<SignatureMethod", "<ds:SignatureMethod");
        //            Signature = Signature.Replace("<Reference", "<ds:Reference");
        //            Signature = Signature.Replace("</Reference", "</ds:Reference");
        //            Signature = Signature.Replace("<Transform", "<ds:Transform");
        //            Signature = Signature.Replace("</Transform", "</ds:Transform");
        //            Signature = Signature.Replace("<DigestMethod", "<ds:DigestMethod");
        //            Signature = Signature.Replace("<DigestValue", "<ds:DigestValue");
        //            Signature = Signature.Replace("</DigestValue", "</ds:DigestValue");
        //            Signature = Signature.Replace("<SignedInfo>", "<ds:SignedInfo>");
        //            Signature = Signature.Replace("</SignedInfo>", "</ds:SignedInfo>");
        //            Signature = Signature.Replace("<SignatureValue>" , "<ds:SignatureValue>");
        //            Signature = Signature.Replace("</SignatureValue>", "</ds:SignatureValue>");
        //        }
        //        //else
        //        // Unable to read certificate our of machine store

        //    }
        //    catch (Exception ex)
        //    {
        //        //WriteServiceErrors.WriteServiceError((new StackFrame()).GetMethod().Name, ex.Message, ex.StackTrace, string.Empty, ErrorSource.WebService);
        //    }

        //    return Signature;

        //}

        //public static string SignEnveloped(XmlDocument xmlDoc)
        //{
        //    SignedXml signedXml = new SignedXml(xmlDoc);
            
        //    X509Certificate2 signingCert = GetCertificateByFriendlyName("WSI's Symantec Corporation ID", StoreName.My, StoreLocation.LocalMachine); // Jim Flynn private key
        //        // verify private key is not null
        //    if (signingCert.PrivateKey != null)
        //    {

        //        // sign away!
        //        signedXml.SigningKey = signingCert.PrivateKey;
        //    }

        //    // Create a new KeyInfo object.
        //    KeyInfo keyInfo = new KeyInfo();
        //    //keyInfo.Id = keyInfoRefId;

        //    // Load the certificate into a KeyInfoX509Data object
        //    // and add it to the KeyInfo object.
        //    KeyInfoX509Data keyInfoData = new KeyInfoX509Data();
        //    keyInfoData.AddCertificate(signingCert);
        //    keyInfo.AddClause(keyInfoData);
        //    // Add the KeyInfo object to the SignedXml object.
        //    signedXml.KeyInfo = keyInfo;

        //    Reference reference = new Reference(""); // ("#id-7620D52D24B25440F9146662373023413");
        //    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());

        //    signedXml.AddReference(reference);

        //    // Add an RSAKeyValue KeyInfo (optional; helps recipient find key to validate).
        //    //KeyInfo keyInfo = new KeyInfo();
        //    //keyInfo.AddClause(new RSAKeyValue((RSA)signingCert));
        //    //signedXml.KeyInfo = keyInfo;

        //    signedXml.ComputeSignature();

        //    XmlElement xmlSignature = signedXml.GetXml();

        //        //Here we set the namespace prefix on the signature element and all child elements to "ds", invalidating the signature.
        //        AssignNameSpacePrefixToElementTree(xmlSignature, "ds");

        //        //So let's recompute the SignatureValue based on our new SignatureInfo...

        //        //For XPath
        //        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        //        namespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#"); //this prefix is arbitrary and used only for XPath

        //        XmlElement xmlSignedInfo = xmlSignature.SelectSingleNode("ds:SignedInfo", namespaceManager) as XmlElement;

        //        //Canonicalize the SignedInfo element
        //        XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
        //        XmlDocument signedInfoDoc = new XmlDocument();
        //        signedInfoDoc.LoadXml(xmlSignedInfo.OuterXml);
        //        transform.LoadInput(signedInfoDoc);

        //        //Compute the new SignatureValue//Create a RSA Provider, using the private key
        //        RSACryptoServiceProvider rsaCryptoServiceProvider = (RSACryptoServiceProvider)signingCert.PrivateKey;

        //        string signatureValue = Convert.ToBase64String(rsaCryptoServiceProvider.SignData(transform.GetOutput() as MemoryStream, new SHA1CryptoServiceProvider()));
        //        //Set it in the xml
        //        XmlElement xmlSignatureValue = xmlSignature.SelectSingleNode("ds:SignatureValue", namespaceManager) as XmlElement;
        //        xmlSignatureValue.InnerText = signatureValue;

        //        XmlDocument SignatureDoc = new XmlDocument();
        //        SignatureDoc.LoadXml(xmlSignature.OuterXml);

        //        return SignatureDoc.OuterXml.ToString();

        //}

        //private static byte[] GetC14NDigest(SignedXml signedXml, HashAlgorithm hashAlgorithm)
        //{
        //    Transform canonicalizeTransform = signedXml.SignedInfo.CanonicalizationMethodObject;
        //    XmlDocument xmlDoc = new XmlDocument();
        //    xmlDoc.LoadXml(signedXml.SignedInfo.GetXml().OuterXml);
        //    canonicalizeTransform.LoadInput(xmlDoc);
        //    return canonicalizeTransform.GetDigestedOutput(hashAlgorithm);
        //}

        private static void AssignNameSpacePrefixToElementTree(XmlElement element, string prefix)
        {
            element.Prefix = prefix;

            foreach (var child in element.ChildNodes)
            {
                if (child is XmlElement)
                    AssignNameSpacePrefixToElementTree(child as XmlElement, prefix);
            }
        }

        //public static X509Certificate2 GetCertificateByFriendlyName(string friendlyName, StoreName storeName, StoreLocation storeLocation)
        //{
        //    X509Store xstore = new X509Store(storeName, storeLocation);
        //    xstore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        //    X509Certificate2 cert = null;

        //    foreach (X509Certificate2 cert2 in xstore.Certificates)
        //    {

        //        string friendlyNamestr = cert2.FriendlyName;
        //        if (friendlyName == friendlyNamestr)
        //        {
        //            cert = cert2;
        //            break;
        //        }
        //    }

        //    return cert;
        //}

        //public static bool VerifySignature(string SignedInfo)
        //{
        //    bool Result = false;
        //    XmlDocument SignedXML_PrivateDoc;

        //    try
        //    {
        //        // convert from Base64 to string.
        //        //byte[] encodedDataAsBytes = System.Convert.FromBase64String(SignedInfo);
        //        //SignedInfo = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

        //        // Deserialize
        //        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(XmlDocument));
        //        XmlReaderSettings settings = new XmlReaderSettings(); // No settings need modifying here
        //        using (StringReader textReader = new StringReader(SignedInfo))
        //        {
        //            using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
        //            {
        //                SignedXML_PrivateDoc = (XmlDocument)serializer.Deserialize(xmlReader);
        //            }
        //        }
        //        SignedXML_PrivateDoc.PreserveWhitespace = true;


        //        XMLUtils.CustomIdSignedXml PrivateXmlElement = new XMLUtils.CustomIdSignedXml(SignedXML_PrivateDoc);
        //        string[] nodes = new string[1];
        //        nodes[0] = "Id";
        //        PrivateXmlElement.SetIDAttributeValues(nodes);

        //        XmlNode PrivateDigitalSignature = SignedXML_PrivateDoc.GetElementsByTagName("Signature")[0];
        //        PrivateXmlElement.LoadXml((XmlElement)PrivateDigitalSignature);

        //        X509Certificate2 KeyPublic = new X509Certificate2("C:\\TEMP\\WSI2.cer");

        //        Result = PrivateXmlElement.CheckSignature(KeyPublic, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        //WriteServiceErrors.WriteServiceError((new StackFrame()).GetMethod().Name, ex.Message, ex.StackTrace, string.Empty, ErrorSource.WebService);
        //    }

        //    return Result;
        //}

	} // End class

	public class LimitedQueue<T> : Queue<T>
	{
		private int limit = -1;

		public int Limit
		{
			get { return limit; }
			set { limit = value; }
		}

		public LimitedQueue(int limit)
			: base(limit)
		{
			this.Limit = limit;
		}

		public new void Enqueue(T item)
		{
			while (this.Count >= this.Limit)
			{
				this.Dequeue();
			}
			base.Enqueue(item);
		}
	}

	public class LimitedConcurrentQueue<T> : System.Collections.Concurrent.ConcurrentQueue<T>
	{
		private int limit = -1;

		public int Limit
		{
			get { return limit; }
			set { limit = value; }
		}

		public LimitedConcurrentQueue(int limit)
			: base()
		{
			this.Limit = limit;
		}

		public new void Enqueue(T item)
		{
			while (this.Count >= this.Limit)
			{
				T result;
				TryDequeue(out result);
			}
			base.Enqueue(item);
		}
	}


} // End namespace

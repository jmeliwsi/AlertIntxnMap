using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace FUL
{
	public class AirportItem
	{
		public string ICAO;
		public string IATA;
		public string AreaCode;
		public uint DeclutterLevel;
		public double Latitude;
		public double Longitude;
		public string Name;
		public DateTime ValidTime;
		public string CountryCode;
		public int LongestRunway;
		public string LongestRunwaySurface;
		public string AirportAccessLevel;
		public string FIRAirControlID;
		public string State;
		public string UIRAirControlID;
		public string CenterAirControlID;
		public string TimeZoneId;
		public string ObservesDaylightSavings;

		public AirportItem()
		{ }

		public static List<AirportItem> CreateList(DataSet ds)
		{
			List<AirportItem> collection = new List<AirportItem>();

			if (ds == null || ds.Tables.Count == 0)
				return collection;

			//Fill list
			using (DataTableReader myReader = ds.CreateDataReader())
			{
				while (myReader.Read())
				{
					AirportItem arpt = new AirportItem();

					arpt.ICAO = myReader.GetString(0);
					if (myReader.IsDBNull(1))
						arpt.IATA = String.Empty;
					else
						arpt.IATA = myReader.GetString(1);
					arpt.AreaCode = myReader.GetString(2);
					arpt.DeclutterLevel = (uint)myReader.GetInt32(3);
					arpt.Latitude = myReader.GetDouble(4);
					arpt.Longitude = myReader.GetDouble(5);
					if (myReader.IsDBNull(6))
						arpt.Name = string.Empty;
					else
						arpt.Name = myReader.GetString(6).TrimEnd(' ');
					arpt.ValidTime = myReader[8] != System.DBNull.Value ? myReader.GetDateTime(8) : new DateTime(1800, 1, 1);
					arpt.CountryCode = myReader.GetString(9);
					arpt.LongestRunway = myReader.GetInt32(10);

					//Longest Runway Surface
					string longestRunwaySurface = myReader.GetString(11);
					switch (longestRunwaySurface.ToUpper())
					{
						case "H":
							longestRunwaySurface = "Hard";
							break;
						case "S":
							longestRunwaySurface = "Soft";
							break;
						case "W":
							longestRunwaySurface = "Water";
							break;
						default:
							longestRunwaySurface = "Undefined";
							break;
					}
					arpt.LongestRunwaySurface = longestRunwaySurface;

					if (myReader.IsDBNull(12))
						arpt.AirportAccessLevel = string.Empty;
					else
					{
						string arptAccess = myReader.GetString(12);
						switch (arptAccess)
						{
							case "C":
								arptAccess = "Civil";
								break;
							case "M":
								arptAccess = "Military";
								break;
							case "P":
								arptAccess = "Private";
								break;
							default:
								arptAccess = string.Empty;
								break;
						}

						arpt.AirportAccessLevel = arptAccess;
					}

					if (myReader.IsDBNull(13))
						arpt.FIRAirControlID = string.Empty;
					else
						arpt.FIRAirControlID = myReader.GetString(13);

					if (myReader.IsDBNull(14))
						arpt.State = string.Empty;
					else
						arpt.State = myReader.GetString(14);

					if (myReader.IsDBNull(15))
						arpt.UIRAirControlID = string.Empty;
					else
						arpt.UIRAirControlID = myReader.GetString(15);

					if (myReader.IsDBNull(16))
						arpt.CenterAirControlID = string.Empty;
					else
						arpt.CenterAirControlID = myReader.GetString(16);
					
					if (myReader.IsDBNull(17))
						arpt.TimeZoneId = string.Empty;
					else
						arpt.TimeZoneId = myReader.GetString(17);

					if (myReader.IsDBNull(18))
						arpt.ObservesDaylightSavings = string.Empty;
					else
						arpt.ObservesDaylightSavings = myReader.GetString(18);
					
					collection.Add(arpt);

				}// end-while reading from DB
			} // end using DataTableReader

			return collection;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	public class AirportPriorityListHelpers
	{
		public static string BuildAirportPriorityListQuery(string where, AirportCodeType? forceToType)
		{
			bool addTimeStamp = false;
			return BuildAirportPriorityListQuery(where, DateTime.MinValue, ref addTimeStamp, true, forceToType);
		}

		public static string BuildAirportPriorityListQuery(string where, DateTime timeStamp, ref bool addTimeStamp)
		{
			return BuildAirportPriorityListQuery(where, timeStamp, ref addTimeStamp, false, null);
		}

		public static string BuildAirportPriorityListQuery(string where, DateTime timeStamp, ref bool addTimeStamp, bool addQualifier)
		{
			return BuildAirportPriorityListQuery(where, timeStamp, ref addTimeStamp, addQualifier, null);
		}

		public static string BuildAirportPriorityListQuery(string where, DateTime timeStamp, ref bool addTimeStamp, bool addQualifier, AirportCodeType? forceToType)
		{
			addTimeStamp = false;
			StringBuilder sql = new StringBuilder();
			string identifierColumn = forceToType == null || forceToType == AirportCodeType.ICAO ? "ICAO" : "ICAO, IATA";

			if (where.StartsWith("(ICAO"))
			{
				sql.Append("SELECT DISTINCT ").Append(identifierColumn).Append(" FROM Airports WHERE ValidTime ");
				if (timeStamp.Year > 2000)
				{
					sql.Append("= @timestamp");
					addTimeStamp = true;
				}
				else
					sql.Append("IN (SELECT MIN(ValidTime) FROM Airports)");
				sql.Append(" AND ").Append(where);
			}
			else
			{
				//Decode string first
				List<Dictionary<string, List<string>>> selectedFiltersDictList = new List<Dictionary<string, List<string>>>();
				List<string> PriorityIncludeItems = new List<string>(), PriorityExcludeItems = new List<string>();
				DecodeDictionaryList(where, ref selectedFiltersDictList, ref PriorityIncludeItems, ref PriorityExcludeItems);

				//change generic lists to strings
				string include = (PriorityIncludeItems.Count > 0) ? "(ICAO IN ('" + string.Join("', '", PriorityIncludeItems.ToArray()) + "'))" : string.Empty;
				string exclude = (PriorityExcludeItems.Count > 0) ? "(ICAO NOT IN ('" + string.Join("', '", PriorityExcludeItems.ToArray()) + "'))" : string.Empty;

				Dictionary<string, List<string>> selectedFiltersDict = null;

				for (int ix = 0; ix < selectedFiltersDictList.Count; ix++)
				{
					selectedFiltersDict = selectedFiltersDictList[ix];
					bool hasAircraftTypes = selectedFiltersDict.ContainsKey("Aircraft Type") && selectedFiltersDict["Aircraft Type"].Count > 0;
					bool hasC70Categories = selectedFiltersDict.ContainsKey("C70 Category") && selectedFiltersDict["C70 Category"].Count > 0;
					bool hasAreaCodes = selectedFiltersDict.ContainsKey("Area Code") && selectedFiltersDict["Area Code"].Count > 0;
					bool hasCountryCodes = selectedFiltersDict.ContainsKey("Country Code") && selectedFiltersDict["Country Code"].Count > 0;
					bool hasLongestRunway = selectedFiltersDict.ContainsKey("Runway Length") && selectedFiltersDict["Runway Length"].Count > 0;
					bool hasLongestRunwaySurface = selectedFiltersDict.ContainsKey("Longest Runway Surface") && selectedFiltersDict["Longest Runway Surface"].Count > 0;
					bool hasAirportAccessLevels = selectedFiltersDict.ContainsKey("Access Level") && selectedFiltersDict["Access Level"].Count > 0;
					bool hasFIRAirControlIDs = selectedFiltersDict.ContainsKey("FIR ID") && selectedFiltersDict["FIR ID"].Count > 0;
					bool hasStates = selectedFiltersDict.ContainsKey("State") && selectedFiltersDict["State"].Count > 0;
					bool hasNumberofRunways = selectedFiltersDict.ContainsKey("Number of Runways") && selectedFiltersDict["Number of Runways"].Count > 0;

					if (ix > 0)
						sql.Append(" UNION ");

					sql.Append("SELECT DISTINCT Airports.").Append(identifierColumn).Append(" FROM Airports ");

					string RunwayLength = "0";
					if (hasLongestRunway)
						RunwayLength = selectedFiltersDict["Runway Length"][0];

					if (hasAircraftTypes || hasC70Categories)
					{
						sql.Append("INNER JOIN ");
						if (addQualifier)
							sql.Append("Fusion_Admin.dbo.");
						sql.Append("AirportData ON Airports.ICAO = AirportData.ICAO ");
					}

					if (hasNumberofRunways)
					{
						sql.Append("INNER JOIN (Select AirportID, count(*) as RunwayCount from Runways WHERE ValidTime ");

						if (timeStamp.Year > 2000)
						{
							sql.Append("= @timestamp");
							addTimeStamp = true;
						}
						else
							sql.Append("IN (SELECT MIN(ValidTime) FROM Airports)");

						sql.Append(" AND runwayLength >= ").Append(RunwayLength).Append(" GROUP BY AirportID) TEMP ON TEMP.AirportID = Airports.ICAO ");
					}

					sql.Append("WHERE Airports.ValidTime ");

					if (timeStamp.Year > 2000)
						sql.Append("= '").Append(timeStamp.ToString()).Append("'");
					else
						sql.Append("IN (SELECT MIN(ValidTime) FROM Airports)");

					if (hasAircraftTypes)
						sql.Append(" AND AirportData.AircraftType IN ('").Append(string.Join("', '", selectedFiltersDict["Aircraft Type"].ToArray())).Append("')");

					if (hasC70Categories)
						sql.Append(" AND AirportData.C70Category IN ('").Append(string.Join("', '", selectedFiltersDict["C70 Category"].ToArray())).Append("')");

					if (hasAreaCodes)
						sql.Append(" AND Airports.AreaCode IN ('").Append(string.Join("', '", selectedFiltersDict["Area Code"].ToArray())).Append("')");

					if (hasCountryCodes)
						sql.Append(" AND Airports.CountryCode IN ('").Append(string.Join("', '", selectedFiltersDict["Country Code"].ToArray())).Append("')");

					if (hasLongestRunway && !hasNumberofRunways)
						sql.Append(" AND Airports.Longest_Runway >= ").Append(selectedFiltersDict["Runway Length"][0]);

					if (hasLongestRunwaySurface)
						sql.Append(" AND Airports.Longest_Runway_surface IN ('").Append(string.Join("', '", selectedFiltersDict["Longest Runway Surface"].ToArray())).Append("')");

					if (hasAirportAccessLevels)
						sql.Append(" AND Airports.Airport_Access_Level IN ('").Append(string.Join("', '", selectedFiltersDict["Access Level"].ToArray())).Append("')");

					if (hasFIRAirControlIDs)
						sql.Append(" AND Airports.fir_air_control_id IN ('").Append(string.Join("', '", selectedFiltersDict["FIR ID"].ToArray())).Append("')");

					if (hasStates)
						sql.Append(" AND Airports.State IN ('").Append(string.Join("', '", selectedFiltersDict["State"].ToArray())).Append("')");

					if (hasNumberofRunways)
						sql.Append(" AND TEMP.RunwayCount >= ").Append(selectedFiltersDict["Number of Runways"][0]);

					if (exclude.Length > 0)
						sql.Append(" AND ").Append(exclude);
				}

				//Include Query
				if (include.Length > 0)
				{
					if (sql.Length > 0)
						sql.Append(" UNION ");
					sql.Append("SELECT DISTINCT Airports.").Append(identifierColumn).Append(" FROM Airports ");
					sql.Append("WHERE ").Append(include);
				}
			}

			return sql.ToString();
		}

		public static void DecodeDictionaryList(string str, ref List<Dictionary<string, List<string>>> selectedFiltersCollection, ref List<string> PriorityIncludeItems, ref List<string> PriorityExcludeItems)
		{
			//KEY:
			//ALT-0254 þ = Dictionary Separator
			//ALT-0222 Þ
			//ALT-0187 » = Key/Value Separator
			//ALT-0188 ¼
			//ALT-0189 ½
			//ALT-0190 ¾
			//ALT-0191 ¿ = Value Separator
			//ALT-0177 ± = Key Separator

			selectedFiltersCollection.Clear();
			PriorityIncludeItems.Clear();
			PriorityExcludeItems.Clear();

			string[] dictArray = str.Split(new char[] { 'þ' });

			for (int dictCount = 0; dictCount < dictArray.Length; dictCount++)
			{
				bool add = true;
				Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
				string[] keyValueArray = dictArray[dictCount].Split(new char[] { '±' });

				for (int kvpCount = 0; kvpCount < keyValueArray.Length; kvpCount++)
				{
					string[] valueArray = keyValueArray[kvpCount].Split(new char[] { '»', '¿' });
					List<string> values = new List<string>();
					values.AddRange(valueArray);
					values.RemoveAt(0);

					if (string.Compare(valueArray[0], "PriorityIncludeItems", true) == 0)
					{
						//if (values.Count > 1 || (values.Count == 1 && values[0] != "!!!"))
						PriorityIncludeItems = values;
						add = false;
					}
					else if (string.Compare(valueArray[0], "PriorityExcludeItems", true) == 0)
					{
						//if (values.Count > 1 || (values.Count == 1 && values[0] != "!!!"))
						PriorityExcludeItems = values;
						add = false;
					}
					else
					{
						dict.Add(valueArray[0], values);
						add = true;
					}
				}

				if (add)
					selectedFiltersCollection.Add(dict);
			}
		}
	}
}

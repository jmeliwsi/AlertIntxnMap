using System;
using System.Collections.Generic;
using System.Data;

namespace FUL
{
	public class AirportDataItem
	{
		public string ICAO;
		public string AircraftType;
		public string C70Category;
		public DateTime ValidTime;

		public AirportDataItem()
		{

		}

        public AirportDataItem(string airport, string craftType, string category, DateTime validTime)
        {
            ICAO = airport;
            AircraftType = craftType;
            C70Category = category;
            ValidTime = validTime;
        }

		public static List<AirportDataItem> CreateList(DataSet ds)
		{
			List<AirportDataItem> collection = new List<AirportDataItem>();

			if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
			{
				using (DataTableReader myReader = ds.CreateDataReader())
				{
					int icaoOrd = myReader.GetOrdinal("ICAO");
					int aircraftTypeOrd = myReader.GetOrdinal("AircraftType");
					int c70CategoryOrd = myReader.GetOrdinal("C70Category");
					int validTimeOrd = myReader.GetOrdinal("ValidTime");

					while (myReader.Read())
					{
						AirportDataItem item = new AirportDataItem();
						item.ICAO = myReader.GetString(icaoOrd);
						item.AircraftType = myReader.GetString(aircraftTypeOrd);
                        if (myReader.IsDBNull(c70CategoryOrd))
                            item.C70Category = string.Empty;
                        else
                            item.C70Category = myReader.GetString(c70CategoryOrd);
						item.ValidTime = myReader.GetDateTime(validTimeOrd);

						collection.Add(item);
					}
				}
			}

			return collection;
		}
	}
}

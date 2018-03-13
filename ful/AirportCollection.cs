using System;
using System.Collections;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace FUL
{
	/**
	 * \class AirportCollection
	 * \brief Represents a list of airports
	 */
	[Serializable]
	public class AirportCollection : IEnumerable
	{
		private const char space = ' ';

		#region Data Members
		private System.Collections.Generic.Dictionary<string, Airport> icaoTable;
		private System.Collections.Generic.Dictionary<string, Airport> iataTable;
		#endregion

        public AirportCollection()
        {
			icaoTable = new System.Collections.Generic.Dictionary<string, Airport>(25000);
			iataTable = new System.Collections.Generic.Dictionary<string, Airport>(8000);
        }

        public bool Create(DataSet ds)
        {
            // Read Airports from DataBase.
            try
            {
				if (ds.IsNullOrEmpty())
					return false;

				// Clear tables first for subsequent reloads due to updated Airport source.
				this.icaoTable.Clear();
				this.iataTable.Clear();

				foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    // Create the airport object and add it to the lists
					Airport airport = new Airport(
						dr[0].ToString(),									// ICAO
						dr.IsNull(1) ? string.Empty : dr[1].ToString(),		// IATA
						(uint)Convert.ToInt32(dr[3]),						// ZoomLevelInt
						Convert.ToDouble(dr[4]),							// Lat
						Convert.ToDouble(dr[5]),							// Long
						dr[6].ToString().TrimEnd(space),					// Name
						dr[2].ToString(),									// AreaCode
						Convert.ToDateTime(dr[8]),							// ValidTime
						Convert.ToInt32(dr[9]));							// Elevation

					// ICAO is required and will always exist.
					this.icaoTable[airport.ICAO] = airport;

					// IATA is not, and may also be a duplicate.
					// If we didn't care about if we kept the first or last we could skip the ContainsKey check which would improve performance,
					// but for compatibility with how this structure has always been we will keep doing it, thus keeping the first found.
					if (!string.IsNullOrEmpty(airport.IATA) && !iataTable.ContainsKey(airport.IATA))
						this.iataTable[airport.IATA] = airport;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void Add(Airport airport)
		{
			try
			{
				if (Airport.IsNull(airport))
					return;

				if (!string.IsNullOrEmpty(airport.ICAO))
					icaoTable[airport.ICAO.ToUpper()] = airport;

				if (!string.IsNullOrEmpty(airport.IATA))
					iataTable[airport.IATA.ToUpper()] = airport;
			}
			catch {}
		}

        public void Add(System.Object airport)
        {
            try
            {
                if (airport.GetType() == typeof(Airport))
                    Add((Airport)airport);
            }
            catch { }
        }

		public void Remove(Airport airport)
		{
			try
			{
				if (Airport.IsNull(airport))
					return;

				if (!string.IsNullOrEmpty(airport.ICAO))
					icaoTable.Remove(airport.ICAO.ToUpper());

				if (!string.IsNullOrEmpty(airport.IATA))
					iataTable.Remove(airport.IATA.ToUpper());
			}
			catch {}
		}

		public int Count
		{
			get
			{
				return icaoTable.Count;
			}
		}

		/**
		 * \fn public Airport this[string code]
		 * \brief List accessor
		 * \param code Airport IATA or ICAO code (case insensitive)
		 * \return Returns the airport if found in the list, null otherwise
		 */
		public Airport this[string code]
		{
			get
			{
				try
				{
					if (string.IsNullOrEmpty(code))
						return null;

					Airport airport = null;

					if (code.Length == 3)
					{
						if (iataTable.TryGetValue(code.ToUpper(), out airport))
							return airport;
					}
					else if (code.Length == 4)
					{
						if (icaoTable.TryGetValue(code.ToUpper(), out airport))
							return airport;
					}
				}
				catch { }

				return null;
			}
		}

		/**
		 * \fn public bool Compare(string code1, string code2)
		 * \brief Compares two IATA and/or ICAO codes to see if they correspond to the same airport
		 * \param code1 Airport 1 IATA or ICAO code (case insensitive)
		 * \param code2 Airport 2 IATA or ICAO code (case insensitive)
		 * \return Return true if the airports are the same, false otherwise
		 */
		public bool Compare(string code1, string code2)
		{
			Airport airport1 = this[code1];
			Airport airport2 = this[code2];

			if (Airport.IsNull(airport1) && Airport.IsNull(airport2))
			{
				if (string.Compare(code1, code2, true) == 0)
					return true;
				else
					return false;
			}

			return (airport1 == airport2);
		}

		public string IataToIcao(string iata)
		{
			if (string.IsNullOrWhiteSpace(iata) || iata.Length != 3)	// not an IATA code
				return string.Empty;

			Airport a = this[iata];
			if (a != null)
				return a.ICAO;
			else
				return string.Empty;
		}

        /// <summary>
        /// Returns ICAO airport when input parameter length is 3, else returns input parameter
        /// </summary>
        /// <param name="airport"></param>
        /// <returns></returns>
        public string Get_ICAOairport(string airport)
        {
			if (string.IsNullOrEmpty(airport))
				return string.Empty;

            if (airport.Length == 3)
                return IataToIcao(airport);

            return airport;
        }

		public IEnumerator GetEnumerator()
		{
			return (icaoTable as IEnumerable).GetEnumerator();
		}

    } // End Class
} // End namespace

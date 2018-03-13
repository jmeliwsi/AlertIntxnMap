using System;
using System.Data;

namespace FUL
{
	[Serializable]
	public class NotamFilterSettings
	{
		private static char[] pipe = new char[] { '|' };

		public bool IsFilterApplied;

		public string HighlightText;
		public int HighlightInLastDays;
		public int ValidityPeriodStart;
		public int ValidityPeriodEnd;

		public bool IncludeIntl;
		public bool IncludeUsDomestic;
		public bool IncludeFDC;
		public bool IncludeMilitary;
		public bool IncludeCompany;
        public bool IncludeCanadian;
		public bool IncludeSnowtams;
		// JNotams are contained within IncludeCanadian.

		public bool IncludeAllUirs;
		public string[] Uirs;
		public bool IncludeAllFirs;
		public string[] Firs;
		public bool IncludeAllCenters;
		public string[] Centers;

		public bool IncludeAllIntlSubjects;
		public string[] IntlSubjects;
		public string[] IntlPurpose;
		public string[] IntlTrafficType;
		public string[] IntlScope;
		public int IntlFlightLevelMin;
		public int IntlFlightLevelMax;

		public bool IncludeAllMilitarySubjects;
		public string[] MilitarySubjects;
		public string[] MilitaryPurpose;
		public string[] MilitaryTrafficType;
		public string[] MilitaryScope;
		public int MilitaryFlightLevelMin;
		public int MilitaryFlightLevelMax;

		public static NotamFilterSettings Empty
		{
			get
			{
				NotamFilterSettings empty = new NotamFilterSettings();
				empty.IsFilterApplied = false;

				empty.HighlightText = string.Empty;
				empty.HighlightInLastDays = 0;
				empty.ValidityPeriodStart = 1;
				empty.ValidityPeriodEnd = -1;

				empty.IncludeIntl = true;
				empty.IncludeUsDomestic = true;
				empty.IncludeFDC = true;
				empty.IncludeMilitary = true;
				empty.IncludeCompany = true;
                empty.IncludeCanadian = true;
				empty.IncludeSnowtams = true;

				empty.IncludeAllUirs = true;
				empty.Uirs = new string[0];
				empty.IncludeAllFirs = true;
				empty.Firs = new string[0];
				empty.IncludeAllCenters = true;
				empty.Centers = new string[0];

				empty.IncludeAllIntlSubjects = true;
				empty.IntlSubjects = new string[0];
				empty.IntlPurpose = new string[0];
				empty.IntlTrafficType = new string[0];
				empty.IntlScope = new string[0];
				empty.IntlFlightLevelMin = 0;
				empty.IntlFlightLevelMax = 999;

				empty.IncludeAllMilitarySubjects = true;
				empty.MilitarySubjects = new string[0];
				empty.MilitaryPurpose = new string[0];
				empty.MilitaryTrafficType = new string[0];
				empty.MilitaryScope = new string[0];
				empty.MilitaryFlightLevelMin = 0;
				empty.MilitaryFlightLevelMax = 999;

				return empty;
			}
		}

		public void InitialDisplayFilter()
		{
			IsFilterApplied = true;

			HighlightText = string.Empty;
			HighlightInLastDays = 0;
			ValidityPeriodStart = 0;
			ValidityPeriodEnd = 1; 

			IncludeIntl = true;
			IncludeUsDomestic = false;
			IncludeFDC = false;
			IncludeMilitary = false;
			IncludeCompany = false;
            IncludeCanadian = false;
			IncludeSnowtams = false;

			IncludeAllUirs = true;
			Uirs = new string[0];
			IncludeAllFirs = true;
			Firs = new string[0];
			IncludeAllCenters = true;
			Centers = new string[0];

			IncludeAllIntlSubjects = true;
			IntlSubjects = new string[0];
			IntlPurpose = new string[0];
			IntlTrafficType = new string[1] {"I"};
			IntlScope = new string[2] {"E", "W"};
			IntlFlightLevelMin = 0;
			IntlFlightLevelMax = 450;

			IncludeAllMilitarySubjects = true;
			MilitarySubjects = new string[0];
			MilitaryPurpose = new string[0];
			MilitaryTrafficType = new string[0];
			MilitaryScope = new string[0];
			MilitaryFlightLevelMin = 0;
			MilitaryFlightLevelMax = 999;
		}

		public void ParseDataRow(DataRow dr)
		{
			try
			{
				if (dr.Table.Columns.Contains("IsFilterAplied"))
					IsFilterApplied = Convert.ToBoolean(dr["IsFilterAplied"]);

				HighlightText= !dr.IsNull("HighlightText") ? dr["HighlightText"].ToString() : string.Empty;
				HighlightInLastDays = Convert.ToInt32(dr["HighlightInLastDays"]);
				ValidityPeriodStart = Convert.ToInt32(dr["ValidityPeriodStart"]);
				ValidityPeriodEnd = Convert.ToInt32(dr["ValidityPeriodEnd"]);

				IncludeIntl = Convert.ToBoolean(dr["IncludeInternational"]);
				IncludeUsDomestic = Convert.ToBoolean(dr["IncludeUsDomestic"]);
				IncludeFDC = Convert.ToBoolean(dr["IncludeFDC"]);
				IncludeMilitary = Convert.ToBoolean(dr["IncludeMilitary"]);
				IncludeCompany = Convert.ToBoolean(dr["IncludeCompany"]);
				IncludeCanadian = !dr.IsNull("IncludeCanadian") ? Convert.ToBoolean(dr["IncludeCanadian"]) : false;
				IncludeSnowtams = !dr.IsNull("IncludeSnowtams") ? Convert.ToBoolean(dr["IncludeSnowtams"]) : false;

				if (!dr.IsNull("IncludeAllUirs"))
					IncludeAllUirs = Convert.ToBoolean(dr["IncludeAllUirs"]);
				if (!dr.IsNull("Uirs"))
					Uirs = dr["Uirs"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);

				if (!dr.IsNull("IncludeAllFirs"))
					IncludeAllFirs = Convert.ToBoolean(dr["IncludeAllFirs"]);
				if (!dr.IsNull("Firs"))
					Firs = dr["Firs"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);

				if (!dr.IsNull("IncludeAllCenters"))
					IncludeAllCenters = Convert.ToBoolean(dr["IncludeAllCenters"]);
				if (!dr.IsNull("Centers"))
					Centers = dr["Centers"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);

				if (!dr.IsNull("IncludeAllIntlSubjects"))
					IncludeAllIntlSubjects = Convert.ToBoolean(dr["IncludeAllIntlSubjects"]);
				if (!dr.IsNull("IntlSubjects"))
					IntlSubjects = dr["IntlSubjects"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);
				if (!dr.IsNull("IntlPurpose"))
					IntlPurpose = dr["IntlPurpose"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);
				IntlTrafficType = !dr.IsNull("IntlTrafficType") ? dr["IntlTrafficType"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries) : new string[0];
				IntlScope = !dr.IsNull("IntlScope") ? dr["IntlScope"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries) : new string[0];
				IntlFlightLevelMin = Convert.ToInt32(dr["IntlFlightLevelMin"]);
				IntlFlightLevelMax = Convert.ToInt32(dr["IntlFlightLevelMax"]);

				if (!dr.IsNull("IncludeAllMilitarySubjects"))
					IncludeAllMilitarySubjects = Convert.ToBoolean(dr["IncludeAllMilitarySubjects"]);
				if (!dr.IsNull("MilitarySubjects"))
					MilitarySubjects = dr["MilitarySubjects"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);
				if (!dr.IsNull("MilitaryPurpose"))
					MilitaryPurpose = dr["MilitaryPurpose"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries);
				MilitaryTrafficType = !dr.IsNull("MilitaryTrafficType") ? dr["MilitaryTrafficType"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries) : new string[0];
				IntlScope = !dr.IsNull("MilitaryScope") ? dr["MilitaryScope"].ToString().Split(pipe, StringSplitOptions.RemoveEmptyEntries) : new string[0];
				MilitaryFlightLevelMin = Convert.ToInt32(dr["MilitaryFlightLevelMin"]);
				MilitaryFlightLevelMax = Convert.ToInt32(dr["MilitaryFlightLevelMax"]);
			}
			catch { }
		}

		/// <summary>
		/// Check the filter to see if any types are included. If the filter isn't applied or any one type is selected then we can return true.
		/// </summary>
		/// <param name="displayFilter"></param>
		/// <returns></returns>
		public bool AnyTypesIncluded(bool displayFilter)
		{
			if (!IsFilterApplied)
				return true;

			if (displayFilter)
				return IncludeIntl || IncludeMilitary;
			else
				return IncludeIntl || IncludeUsDomestic || IncludeFDC || IncludeMilitary || IncludeCompany || IncludeCanadian || IncludeSnowtams;
		}

		/// <summary>
		/// We want to know if the filter is applied and any advanced filtering is done on Int'l or Military NOTAMs.
		/// </summary>
		/// <returns></returns>
		public bool HasAdvancedIntlOrMilitaryFiltering()
		{
			if (!IsFilterApplied)
				return false;

			if (IncludeIntl)
			{
				if (!IncludeAllIntlSubjects && IntlSubjects != null && IntlSubjects.Length > 0)
					return true;

				if (IntlPurpose != null && IntlPurpose.Length > 0)
					return true;

				if (IntlTrafficType != null && IntlTrafficType.Length > 0)
					return true;

				if (IntlFlightLevelMin >= 0)
					return true;

				if (IntlFlightLevelMax >= 0)
					return true;
			}

			if (IncludeMilitary)
			{
				if (!IncludeAllMilitarySubjects && MilitarySubjects != null && MilitarySubjects.Length > 0)
					return true;
			}

			return false;
		}
	}

	[Serializable]
	public class NotamFilterValiditySettings
	{
		public int ValidityPeriodStart;
		public int ValidityPeriodEnd;

		public NotamFilterValiditySettings()
		{
			ValidityPeriodStart = 0;
			ValidityPeriodEnd = 1; 
		}

		public NotamFilterValiditySettings(int start, int end)
		{
			ValidityPeriodStart = start;
			ValidityPeriodEnd = end;
		}
	}
}

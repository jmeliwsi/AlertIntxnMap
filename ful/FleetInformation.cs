using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
    public class FleetInformation
    {
        public string Registration { get; set; }
        public string NoseNumber { get; set; }
        public string AC_Fleet { get; set; }
        public string AC_Subfleet { get; set; }
        public string IATA { get; set; }
        public string ICAO { get; set; }
        public string ICAO_Equipment { get; set; }

    }

    // column names for data access
    public static class FLEET_INFORMATION_CELL_NAMES
    {
        public const string REGISTRATION = "Registration";
        public const string NOSE_NUMBER = "NoseNumber";
        public const string FLEET = "AC_Fleet";
        public const string SUBFLEET = "AC_Subfleet";
        public const string ICAO = "ICAO";
        public const string IATA = "IATA";
        public const string ICAO_EQUIPMENT = "ICAOEquipment";
        public const string FLEET_INFORMATION_ID = "FleetInformationID";
        public static string CUSTOMER_ID = "CustomerID";
    }

}

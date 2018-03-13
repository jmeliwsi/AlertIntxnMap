using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
    public class DiversionMonitorSettings
    {
        public bool WatchDesk;
        public bool DisplayLastLocation;
        public int TimeRange;
        public string FlightFilter;
        public bool GroupByActual;
        public bool OnlyCompanyFlights;

        public DiversionMonitorSettings()
        {
            WatchDesk = true;
            DisplayLastLocation = true;
            TimeRange = -3;
            FlightFilter = string.Empty;
            GroupByActual = true;
            OnlyCompanyFlights = false;
        }
    }
}

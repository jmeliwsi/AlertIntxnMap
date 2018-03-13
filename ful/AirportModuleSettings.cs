using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	/// <summary>
	/// Provide an event arguments class which contains an integer value.
	/// </summary>
	public class IntegerEventArgs : EventArgs
	{
		public int IntegerValue;

		public IntegerEventArgs(int i)
		{
			IntegerValue = i;
		}
	}

	/// <summary>
	/// Provide an event handler which contains an integer value.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="ie"></param>
	public delegate void IntegerHandler(object sender, IntegerEventArgs ie);

    public class AirportModule
    {
        public int AirportModulesID;
        public bool Selected;
    }

    public class AirportModuleSettings
    {
        public enum FlightSymbolSize { Small = 3, Medium = 5, Large = 7, ExtraLarge = 9, Largest = 11 }
        public bool ShowCompanyFlights = false;
        public bool ShowDeskFlights = false;
        public bool ShowAllAircraft = false;
        public bool ShowVehicles = false;
        public bool ShowArrivalAircraft = false;
        public bool ShowDepartureAircraft = false;
        public bool ShowAll = true;

        public FlightSymbolSize AircraftSize = FlightSymbolSize.Medium;
        public bool ShowLabels = false;
        public bool ShowTracks = false;

        public string Exclude = string.Empty;

        public List<AirportModule> Modules = new List<AirportModule>();

		public event EventHandler SettingsUpdated;
		public event EventHandler ModulesUpdated;
		public event IntegerHandler ModuleRemoved;

        public AirportModuleSettings()
        { }

        public int[] GetSelectedModules()
        {
            List<int> selected = new List<int>();

            foreach (AirportModule module in Modules)
                if (module.Selected)
                    selected.Add(module.AirportModulesID);

            return selected.ToArray();
        }

		public void SettingsUpdateComplete()
		{
			if (SettingsUpdated != null)
				SettingsUpdated(this, EventArgs.Empty);
		}

		public void ModulesUpdateComplete()
		{
			if (ModulesUpdated != null)
				ModulesUpdated(this, EventArgs.Empty);
		}

		public void RemoveModule(int id)
		{
			if (ModuleRemoved != null)
				ModuleRemoved(this, new IntegerEventArgs(id));
		}
    }
}

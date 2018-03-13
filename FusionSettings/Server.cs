using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace FusionSettings
{
	public class Server
	{
		private static string InstallDirectoryStr = "InstallDir";
		private static string VersionStr = "Version";
		private static string ServerStr = "FusionServer";
		private static string DatabaseStr = "DBConnection";
        private static string DatabaseFlightsClass2Str = "DBConnectionFlightsClass2";
        private static string DatabaseAALStr = "DBaal";
		private static string MasterServerStr = "MasterFusionServer";
		private static string EAGDataBaseStr = "EAG_DBConnection";

		private static string ServerRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\Server";

        private static string FusionIngestorServiceStr = "ImagePath";

        private static string GetRegValue(string key, string RegistryKey)
		{
			object regvalue = null;

			try
			{
				RegistryKey regkey = Registry.LocalMachine.OpenSubKey(RegistryKey);

				if (regkey != null)
				{
					regvalue = regkey.GetValue(key);
					regkey.Close();
				}
			}
			catch
			{
				regvalue = String.Empty;
			}

			if (regvalue == null)
				return string.Empty;

			return regvalue.ToString();
		}

		public static string Version
		{
            get { return GetRegValue(VersionStr, ServerRegistryKey); }
		}

		public static string Directory
		{
            get { return GetRegValue(InstallDirectoryStr, ServerRegistryKey); }
		}

		public static string FusionServer
		{
            get { return GetRegValue(ServerStr, ServerRegistryKey); }
		}

		public static string DatabaseConnection
		{
            get { return GetRegValue(DatabaseStr, ServerRegistryKey); }
		}

		public static string DatabaseFlightsClass2Connection
		{
            get { return GetRegValue(DatabaseFlightsClass2Str, ServerRegistryKey); }
		}

        public static string DatabaseAALConnection
        {
            get { return GetRegValue(DatabaseAALStr, ServerRegistryKey); }
        }

        public static string MasterFusionServer
		{
            get { return GetRegValue(MasterServerStr, ServerRegistryKey); }
		}

		public static string EAG_DatabaseConnection
		{
            get { return GetRegValue(EAGDataBaseStr, ServerRegistryKey); }
		}

        public static string FusionIngestorFlightDataVendorPath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Flight Data Vendor"); }
        }
        
        public static string LatitudeFlightsServiceExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\LatitudeFlightsService"); }
        }

        public static string FusionIngestorServiceExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service"); }
        }

        public static string FusionIngestorServiceASDE_XExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service ASDE-X"); }
        }

        public static string FusionIngestorServiceClass2ExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service Class2"); }
        }

        public static string FusionIngestorServiceADS_BExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service ADS-B"); }
        }

        public static string FusionIngestorServiceAAL_BExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service AAL"); }
        }

        public static string FusionFlightsIngestorServiceExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\FusionFlightsIngestorService"); }
        }

        public static string FusionIngestorServiceFlightsExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\FusionIngestorServiceFlights"); }
        }

		public static string FusionIngestorServiceBrakeAction_BExecutablePath
		{
			get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\Fusion Ingestor Service Misc"); }
		}

        public static string FusionDataServiceExecutablePath
        {
            get { return GetRegValue(FusionIngestorServiceStr, @"SYSTEM\CurrentControlSet\services\WSI Fusion Data Service"); }
        }
        
        public static void ResetKeyLocations(bool beta, string name)
		{
			if (beta)
				ServerRegistryKey = @"SOFTWARE\WSI\Fusion_" + name + @"\1.0\Server";
			else
				ServerRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\Server";
		}
	}
}




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace FusionSettings
{
	public class NavData
	{
		private static string InstallDirectoryStr = "InstallDir";
		private static string VersionStr = "Version";

		private static string NavDataRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\NavData";

		private static object GetRegValue(string key)
		{
			object regvalue = null;

			try
			{
				RegistryKey regkey = Registry.LocalMachine.OpenSubKey(NavDataRegistryKey);

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

			return regvalue;
		}

		public static string Version
		{
			get { object regValue = GetRegValue(VersionStr); return regValue == null ? string.Empty : regValue.ToString(); }
		}

		public static string Directory
		{
			get { object regValue = GetRegValue(InstallDirectoryStr); return regValue == null ? string.Empty : regValue.ToString(); }
		}

		public static void ResetKeyLocations(bool beta, string name)
		{
			if (beta)
				NavDataRegistryKey = @"SOFTWARE\WSI\Fusion_" + name + @"\1.0\NavData";
			else
				NavDataRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\NavData";
		}
	}
}

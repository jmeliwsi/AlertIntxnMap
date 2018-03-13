using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace FusionSettings
{
	public class Map
	{
		private static string InstallDirectoryStr = "InstallDir";
		private static string VersionStr = "Version";
		private static string RasterFixStr = "RasterFix";
		private static string SimplePolygonsFixStr = "SimplePolygons";
		private static string SimplePolygonsMaxPointsStr = "SimplePolygonsMaxPoints";
		private static string SimplePolygonsToleranceStr = "SimplePolygonsTolerance";

		// Default values for the video card fix; these are optimized for the
		// Intel HD 4000 card.
		private static readonly bool SIMPLEPOLYGONS_DEFAULT_VALUE = false;
		private static readonly int SIMPLEPOLYGONS_DEFAULT_MAX_POINTS = 600;
		private static readonly double SIMPLEPOLYGONS_DEFAULT_TOLERANCE = 0.01;

		private static string MapRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\Maps";

		private static object GetRegValue(string key)
		{
			object regvalue = null;

			try
			{
				RegistryKey regkey = Registry.LocalMachine.OpenSubKey(MapRegistryKey);

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

		public static bool RasterFix
		{
			get { object regValue = GetRegValue(RasterFixStr); return regValue == null ? false : Convert.ToBoolean(regValue); }
		}

		public static bool SimplePolygonsFix
		{
			get { object regValue = GetRegValue(SimplePolygonsFixStr); return regValue == null ? SIMPLEPOLYGONS_DEFAULT_VALUE : Convert.ToBoolean(regValue); }
		}

		public static int SimplePolygonsMaxPoints
		{
			get { object regValue = GetRegValue(SimplePolygonsMaxPointsStr); return regValue == null ? SIMPLEPOLYGONS_DEFAULT_MAX_POINTS : Convert.ToInt32(regValue); }
		}

		public static double SimplePolygonsTolerance
		{
			get { object regValue = GetRegValue(SimplePolygonsToleranceStr); return regValue == null ? SIMPLEPOLYGONS_DEFAULT_TOLERANCE : Convert.ToDouble(regValue); }
		}

		public static void ResetKeyLocations(bool beta, string name)
		{
			if(beta)
				MapRegistryKey = @"SOFTWARE\WSI\Fusion_" + name + @"\1.0\Maps";
			else
				MapRegistryKey = @"SOFTWARE\WSI\Fusion\1.0\Maps";
		}

		public static uint ToIconSize(int size)
		{
			return (uint)(size * 2 + 3);
		}
	}
}

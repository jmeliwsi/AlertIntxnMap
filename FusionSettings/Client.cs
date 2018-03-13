using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using Microsoft.Win32;

namespace FusionSettings
{
	public enum ClientType { Fusion, AdminTool, Replay, WebFusion, WebArchive, Optima, FieldAndFacilitiesPortal };
	public enum RegistryType { Client, User, AdminMnager, Base };

	public class Client
	{
		//Client & AdminManager
		private static string UserNameStr = "Username";
		private static string PasswordStr = "Password";

		private static string CustomerIDStr = "CustomerID";
		private static string ServerOneStr = "FusionServer";
		private static string ServerTwoStr = "FusionServer2";
		private static string ReplayServerStr = "ReplayServer";
		private static string InstallDirectoryStr = "InstallDir";
		private static string LoadBalancedStr = "LoadBalanced";
        private static string LoadTestModeStr = "LoadTestMode";
		private static string AlertLoggingStr = "AlertLogging";
		private static string RAIMStr = "RAIM";
		private static string ScrollOptimizationStr = "ScrollOptimization";
		private static string CacheRadarStr = "CacheRadar";
		private static string DeveloperStr = "Developer";

        private static string SoftwareKey = String.Empty;
        private static string UserRegistryKey = String.Empty;
        private static string AdminManagerRegistryKey = String.Empty;
        private static string ClientRegistryKey = String.Empty;
        private static string BaseRegistryKey = String.Empty;

        static Client()
        {
            SoftwareKey = "Software";
            if (Environment.Is64BitOperatingSystem)
                SoftwareKey = String.Format("{0}\\Wow6432Node", SoftwareKey);
            UserRegistryKey = String.Format("{0}\\WSI\\Fusion", SoftwareKey);
            AdminManagerRegistryKey = String.Format("{0}\\WSI\\AdminManager", SoftwareKey);
            ClientRegistryKey = String.Format("{0}\\WSI\\Fusion\\1.0\\Client", SoftwareKey);
            BaseRegistryKey = String.Format("{0}\\WSI\\Fusion\\1.0", SoftwareKey);
        }
		public static object GetRegValue(string key)
		{
			return GetRegValue(key, RegistryType.Client);
		}

		private static object GetRegValue(string key, RegistryType type)
		{
			RegistryKey regkey = null;
			object regvalue = null;
			try
			{
                
				switch (type)
				{
					case RegistryType.User:
						regkey = Registry.CurrentUser.OpenSubKey(UserRegistryKey);
						break;
					case RegistryType.Client:
						regkey = Registry.LocalMachine.OpenSubKey(ClientRegistryKey);
						break;
					case RegistryType.AdminMnager:
						regkey = Registry.CurrentUser.OpenSubKey(AdminManagerRegistryKey);
						break;
					case RegistryType.Base:
						regkey = Registry.LocalMachine.OpenSubKey(BaseRegistryKey);
						break;
					default:
						break;
				}

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

		public static object Username
		{
			get { return GetRegValue(UserNameStr, RegistryType.User); }
		}

		public static void SaveUsername(string newUsername)
		{
			RegistryKey rk = Registry.CurrentUser.CreateSubKey(UserRegistryKey);
			rk.SetValue(UserNameStr, newUsername, RegistryValueKind.String);
		}

		public static object Password
		{
			get { return GetRegValue(PasswordStr, RegistryType.User); }
		}

		public static void SavePassword(string newPassword)
		{
			RegistryKey rk = Registry.CurrentUser.CreateSubKey(UserRegistryKey);
			rk.SetValue(PasswordStr, newPassword, RegistryValueKind.String);
		}

		public static void DeleteUserRegistry()
		{
			Registry.CurrentUser.DeleteSubKey(UserRegistryKey, false);
		}

		public static object AdminManagerUsername
		{
			get { return GetRegValue(UserNameStr, RegistryType.AdminMnager); }
		}

		public static void SaveAdminManagerUsername(string newUsername)
		{
			RegistryKey rk = Registry.CurrentUser.CreateSubKey(AdminManagerRegistryKey);
			rk.SetValue(UserNameStr, newUsername, RegistryValueKind.String);
		}

		public static object AdminManagerPassword
		{
			get { return GetRegValue(PasswordStr, RegistryType.AdminMnager); }
		}

		public static void SaveAdminManagerPassword(string newPassword)
		{
			RegistryKey rk = Registry.CurrentUser.CreateSubKey(AdminManagerRegistryKey);
			rk.SetValue(PasswordStr, newPassword, RegistryValueKind.String);
		}

		public static object CustomerID
		{
			get { return GetRegValue(CustomerIDStr, RegistryType.Base); }
		}

		public static object ServerOne
		{
			get { return GetRegValue(ServerOneStr); }
		}

		public static object ServerTwo
		{
			get { return GetRegValue(ServerTwoStr); }
		}

		public static object ReplayServer
		{
			get { return GetRegValue(ReplayServerStr); }
		}

		public static object Directory
		{
			get { return GetRegValue(InstallDirectoryStr); }
		}

		public static object LoadBalanced
		{
			get { return GetRegValue(LoadBalancedStr); }
		}

        public static object LoadTestMode
        {
            get { return GetRegValue(LoadTestModeStr); }
        }

        //public static object Version
        //{
        //    get { return GetRegValue(VersionStr); }
        //}

		public static object AlertLogging
		{
			get { return GetRegValue(AlertLoggingStr); }
		}

		public static void SaveAlertLogging(bool loggingEnabled)
		{
			RegistryKey rk = Registry.LocalMachine.CreateSubKey(ClientRegistryKey);
			rk.SetValue(AlertLoggingStr, Convert.ToInt32(loggingEnabled), RegistryValueKind.String);
		}

		public static object TextualRaim
		{
			get { return GetRegValue(RAIMStr); }
		}

		public static bool ScrollOptimization
		{
			get
			{
				try
				{
					object regValue = GetRegValue(ScrollOptimizationStr);
					return regValue == null ? false : Convert.ToBoolean(regValue); // default to false
				}
				catch
				{
					return false; // default to false
				}
			}
		}

		public static bool CacheRadar
		{
			get
			{
				try
				{
					object regValue = GetRegValue(CacheRadarStr);
					return regValue == null ? true : Convert.ToBoolean(regValue); // default to true
				}
				catch
				{
					return true; // default to true
				}
			}
		}

		public static bool Developer
		{
			get
			{
				try
				{
					object regValue = GetRegValue(DeveloperStr,RegistryType.Base);
					return regValue == null ? false : Convert.ToBoolean(regValue); // default to false
				}
				catch
				{
					return false; // default to false
				}
			}
		}

		public static void ResetKeyLocations(bool beta, string name)
		{
        	if (beta)
			{
                ClientRegistryKey = String.Format("{0}\\WSI\\Fusion_{1}\\1.0\\Client", SoftwareKey, name);
                BaseRegistryKey = String.Format("{0}\\WSI\\Fusion_{1}\\1.0", SoftwareKey, name);
			}
			else
			{
                ClientRegistryKey = String.Format("{0}\\WSI\\Fusion\\1.0\\Client", SoftwareKey);
                BaseRegistryKey = String.Format("{0}\\WSI\\Fusion\\1.0", SoftwareKey);
        	}
		}
	}
}

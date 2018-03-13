using System;
using System.Runtime.InteropServices;

namespace FUL
{
	public class Win32API
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct FLASHWINFO
		{
			public UInt32 cbSize;
			public IntPtr hwnd;
			public UInt32 dwFlags;
			public UInt32 uCount;
			public UInt32 dwTimeout;
		}

		//Stop flashing. The system restores the window to its original state. 
		public const UInt32 FLASHW_STOP = 0;
		//Flash the window caption. 
		public const UInt32 FLASHW_CAPTION = 1;
		//Flash the taskbar button. 
		public const UInt32 FLASHW_TRAY = 2;
		//Flash both the window caption and taskbar button.
		//This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
		public const UInt32 FLASHW_ALL = 3;
		//Flash continuously, until the FLASHW_STOP flag is set. 
		public const UInt32 FLASHW_TIMER = 4;
		//Flash continuously until the window comes to the foreground. 
		public const UInt32 FLASHW_TIMERNOFG = 12;
		
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		// Flash the taskbar button forever for the provided window handle
		public static bool FlashWindow(IntPtr windowHandle)
		{
			FUL.Win32API.FLASHWINFO fInfo = new FUL.Win32API.FLASHWINFO();

			fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
			fInfo.hwnd = windowHandle;
			fInfo.dwFlags = FUL.Win32API.FLASHW_TRAY | FUL.Win32API.FLASHW_TIMER;
			fInfo.uCount = UInt32.MaxValue;
			fInfo.dwTimeout = 0;

			return FUL.Win32API.FlashWindowEx(ref fInfo);
		}

		[DllImport("user32.dll")]
		public static extern IntPtr LoadCursorFromFile(string lpFileName);

		public const int SW_SHOWNOACTIVATE = 4;
		public const int HWND_TOPMOST = -1;
		public const int HWND_TOP = 0;
		public const int HWND_NOTOPMOST = -2;
		public const int HWND_BOTTOM = 1;
		public const uint SWP_NOACTIVATE = 0x0010;
		public const int SW_RESTORE = 9;

		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		public static extern bool SetWindowPos(int hWnd, // window handle
										int hWndInsertAfter, // placement-order handle
										int X, // horizontal position
										int Y, // vertical position
										int cx, // width
										int cy, // height
										uint uFlags); // window positioning flags

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern int SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int IsIconic(IntPtr hWnd);

		public const int WM_SETREDRAW = 11;

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
	}
}

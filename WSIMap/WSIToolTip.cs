using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace WSIMap
{
    // Based on http://www.codeproject.com/cs/miscctrl/ballontooltip.asp
    [ProvideProperty("ToolTip", typeof(Control))]
    public class WSIToolTip : System.ComponentModel.Component, IExtenderProvider
    {
        #region Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(int exstyle, string classname, string windowtitle, int style, int x, int y, int width, int height, IntPtr parent, int menu, int nullvalue, int nullptr);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int DestroyWindow(IntPtr hwnd);
        [DllImport("User32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);
        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        private struct toolinfo
        {
            public int size;
            public int flag;
            public IntPtr parent;
            public int id;
            public Rectangle rect;
            public int nullvalue;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string text;
            public int param;
        }

        private struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
        }

        private const int WM_USER = 0x0400;
        private const int WM_STYLECHANGED = 0x7D;
        private const int WS_BORDER = 0x800000;

        private const int TTM_ADDTOOL = WM_USER + 50;
        private const int TTM_DELTOOL = WM_USER + 51;
        private const int TTM_ACTIVATE = WM_USER + 1;
        private const int TTM_SETMAXTIPWIDTH = WM_USER + 24;
        private const int TTM_SETTITLE = WM_USER + 33;
        private const int TTM_SETDELAYTIME = WM_USER + 3;
        private const int TTM_UPDATETIPTEXT = WM_USER + 57;
        private const int TTM_SETTIPBKCOLOR = WM_USER + 19;
        private const int TTM_SETTIPTEXTCOLOR = WM_USER + 20;
        private const int TTM_GETTOOLINFO = WM_USER + 53;
        private const int TTM_SETTOOLINFO = WM_USER + 54;

        private const int TTS_ALWAYSTIP = 0x01;
        private const int TTS_NOPREFIX = 0x02;
        private const int TTS_BALLOON = 0x40;

        private const int TTF_SUBCLASS = 0x0010;
        private const int TTF_TRANSPARENT = 0x0100;

        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;

        private const int TTDT_RESHOW = 1;
        private const int TTDT_AUTOPOP = 2;
        private const int TTDT_INITIAL = 3;

        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int GWL_STYLE = -16;

        private IntPtr TOPMOST = new IntPtr(-1);
        #endregion

        #region Data Members
        public enum Icons : int { None = 0, Info = 1, Warning = 2, Error = 3 }
        private int max;
        private int autopop;
        private int initial;
        private int reshow;
        private bool enabled;
        private bool isBalloon;
        private string title;
        private toolinfo tf;
        private System.Collections.Hashtable tooltexts;
        private Icons icon;
        private IntPtr toolwindow;
        private IntPtr tempptr;
        private Color bgcolor;
        private Color fgcolor;
        #endregion

        public WSIToolTip()
        {
            // Private members initial values.
            max = 200;
            autopop = 5000;
            initial = 500;
            reshow = 100;
            title = string.Empty;
            bgcolor = Color.FromKnownColor(KnownColor.Info);
            fgcolor = Color.FromKnownColor(KnownColor.InfoText);
            tooltexts = new System.Collections.Hashtable();
            enabled = true;
            isBalloon = false;
            icon = Icons.None;

            // Creating the tooltip control.
            toolwindow = CreateWindowEx(0, "tooltips_class32", string.Empty, WS_POPUP | WS_BORDER | TTS_NOPREFIX | TTS_ALWAYSTIP, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, 0, 0, 0);
            SetWindowPos(toolwindow, TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            SendMessage(toolwindow, TTM_SETMAXTIPWIDTH, 0, new IntPtr(max));

            // Creating the toolinfo structure to be used later.
            tf = new toolinfo();
            tf.flag = TTF_SUBCLASS | TTF_TRANSPARENT;
            tf.size = Marshal.SizeOf(typeof(toolinfo));
			tf.id = 0;
			tf.nullvalue = 0;
			tf.param = 0;
        }

        ~WSIToolTip()
        {
            Dispose(false);
        }

        // Extend any control except itself and the form, this function gets called for use automatically by the designer.
        public bool CanExtend(object extendee)
        {
            if (extendee is Control && !(extendee is WSIToolTip) && !(extendee is Form))
            {
                return true;
            }

            return false;
        }

        // This is not a regular function, its our extender property seprated as two functions for get and set.
        public string GetToolTip(Control parent)
        {
            if (tooltexts.Contains(parent))
            {
                return tooltexts[parent].ToString();
            }
            else
            {
                return null;
            }
        }

        // This is where the tool text is validated and updated for the controls.
        public void SetToolTip(Control parent, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            // If the tool text has been cleared, remove the control from our service list.
            if (value == string.Empty)
            {
                tooltexts.Remove(parent);

                tf.parent = parent.Handle;

                tempptr = Marshal.AllocHGlobal(tf.size);
                Marshal.StructureToPtr(tf, tempptr, false);
                SendMessage(toolwindow, TTM_DELTOOL, 0, tempptr);
                Marshal.FreeHGlobal(tempptr);
                parent.Resize -= new EventHandler(Control_Resize);

            }
            else
            {
                tf.parent = parent.Handle;
                tf.rect = parent.ClientRectangle;
                tf.text = value;
                tempptr = Marshal.AllocHGlobal(tf.size);
                Marshal.StructureToPtr(tf, tempptr, false);

                if (tooltexts.Contains(parent))
                {
                    tooltexts[parent] = value;
                    SendMessage(toolwindow, TTM_UPDATETIPTEXT, 0, tempptr);
                }
                else
                {
                    tooltexts.Add(parent, value);
                    SendMessage(toolwindow, TTM_ADDTOOL, 0, tempptr);
                    parent.Resize += new EventHandler(Control_Resize);
                }

                Marshal.FreeHGlobal(tempptr);
            }
        }

        [DefaultValue(Icons.None)]
        public Icons Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = value;
                Title = title;
            }
        }

        [DefaultValue(200)]
        public int MaximumWidth
        {
            get
            {
                return max;
            }
            set
            {
                // Refuse any strange values, (feel free to modify).
                if (max >= 100 && max <= 2000)
                {
                    max = value;
                    SendMessage(toolwindow, TTM_SETMAXTIPWIDTH, 0, new IntPtr(max));
                }
            }
        }

        [DefaultValue(true)]
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                SendMessage(toolwindow, TTM_ACTIVATE, Convert.ToInt32(enabled), new IntPtr(0));
            }
        }

        [DefaultValue(false)]
        public bool IsBalloon
        {
            get
            {
                return isBalloon;
            }
            set
            {
                isBalloon = value;
                STYLESTRUCT ss = new STYLESTRUCT();
                if (isBalloon)
                {
                    ss.styleOld = WS_POPUP | WS_BORDER | TTS_NOPREFIX | TTS_ALWAYSTIP;
                    ss.styleNew = WS_POPUP | TTS_BALLOON | TTS_NOPREFIX | TTS_ALWAYSTIP;
                }
                else
                {
                    ss.styleOld = WS_POPUP | TTS_BALLOON | TTS_NOPREFIX | TTS_ALWAYSTIP;
                    ss.styleNew = WS_POPUP | WS_BORDER | TTS_NOPREFIX | TTS_ALWAYSTIP;
                }
                tempptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(STYLESTRUCT)));
                Marshal.StructureToPtr(ss, tempptr, false);
                SetWindowLong(toolwindow, GWL_STYLE, ss.styleNew);
                SendMessage(toolwindow, WM_STYLECHANGED, GWL_STYLE, tempptr);
                Marshal.FreeHGlobal(tempptr);
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                tempptr = Marshal.StringToHGlobalUni(title);
                SendMessage(toolwindow, TTM_SETTITLE, (int)icon, tempptr);
                Marshal.FreeHGlobal(tempptr);
            }
        }

        [DefaultValue(5000)]
        public int AutoPopDelay
        {
            get
            {
                return autopop;
            }
            set
            {
                // Refuse any strange values, (feel free to modify).
                if (value >= 100 && value < 10000)
                {
                    autopop = value;
                    SendMessage(toolwindow, TTM_SETDELAYTIME, TTDT_AUTOPOP, new IntPtr(autopop));
                }
            }
        }

        [DefaultValue(500)]
        public int InitialDelay
        {
            get
            {
                return initial;
            }
            set
            {
                // Refuse any strange values, (feel free to modify).
                if (value >= 100 && value <= 2000)
                {
                    initial = value;
                    SendMessage(toolwindow, TTM_SETDELAYTIME, TTDT_INITIAL, new IntPtr(initial));
                }
            }
        }

        [DefaultValue(100)]
        public int ReshowDelay
        {
            get
            {
                return reshow;
            }
            set
            {
                // Refuse any strange values, (feel free to modify).
                if (value >= 100 && value <= 2000)
                {
                    reshow = value;
                    SendMessage(toolwindow, TTM_SETDELAYTIME, TTDT_RESHOW, new IntPtr(reshow));
                }
            }
        }

        public Color BackColor
        {
            get
            {
                return bgcolor;
            }
            set
            {
                bgcolor = value;
                SendMessage(toolwindow, TTM_SETTIPBKCOLOR, System.Drawing.ColorTranslator.ToWin32(value), new IntPtr(0));
            }
        }

        public Color ForeColor
        {
            get
            {
                return fgcolor;
            }
            set
            {
                fgcolor = value;
                SendMessage(toolwindow, TTM_SETTIPTEXTCOLOR, System.Drawing.ColorTranslator.ToWin32(value), new IntPtr(0));
            }
        }

        // Overriding Dispose is a must to free our window handle we created at the constructor.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tooltexts.Clear();
                tooltexts = null;
            }
            DestroyWindow(toolwindow); // Free the window handle obtained by CreateWindowEx.
            base.Dispose(disposing);

        }

        private void Control_Resize(object sender, EventArgs e)
        {
            tf.parent = ((Control)sender).Handle;

            tempptr = Marshal.AllocHGlobal(tf.size);
            Marshal.StructureToPtr(tf, tempptr, false);

            SendMessage(toolwindow, TTM_GETTOOLINFO, 0, tempptr);

            tf = (toolinfo)Marshal.PtrToStructure(tempptr, typeof(toolinfo));
            tf.rect = ((Control)sender).ClientRectangle;

            Marshal.StructureToPtr(tf, tempptr, false);

            SendMessage(toolwindow, TTM_SETTOOLINFO, 0, tempptr);

            Marshal.FreeHGlobal(tempptr);
        }
    }

}

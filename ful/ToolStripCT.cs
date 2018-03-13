using System;
using System.Windows.Forms;

namespace FUL
{
	/// <summary>
	/// Built with help from: http://blogs.msdn.com/b/rickbrew/archive/2006/01/09/511003.aspx
	/// </summary>
	public class ToolStripCT : ToolStrip
	{
		internal const uint WM_MOUSEACTIVATE = 0x21;
		internal const uint MA_ACTIVATE = 1;
		internal const uint MA_ACTIVATEANDEAT = 2;
		internal const uint MA_NOACTIVATE = 3;
		internal const uint MA_NOACTIVATEANDEAT = 4;

		private bool clickThrough = true;
		/// <summary>
		/// Gets or sets whether the ToolStripCT honors item clicks when its containing form does not have input focus.
		/// </summary>
		/// <remarks>
		/// Default value is true, which differs from the default ToolStrip behavior.
		/// </remarks>
		public bool ClickThrough
		{
			get { return this.clickThrough; }
			set { this.clickThrough = value; }
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (this.clickThrough && m.Msg == WM_MOUSEACTIVATE && m.Result == (IntPtr)MA_ACTIVATEANDEAT)
				m.Result = (IntPtr)MA_ACTIVATE;
		}
	}
}

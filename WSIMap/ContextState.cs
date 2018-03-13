using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace WSIMap
{
	/**
	 * \class ContextState
	 * \brief A control used for creating a base rendering context, useful
	 * for applications with multiple map control windows
	 */
	public class ContextState : System.Windows.Forms.UserControl
	{
		#region Data Members
		private byte accumBits = 0;                                         // Accumulation buffer bits
		private byte colorBits = 32;                                        // Color buffer bits
		private byte depthBits = 16;                                        // Depth buffer bits
		private byte stencilBits = 0;                                       // Stencil buffer bits
		private IntPtr windowHandle = IntPtr.Zero;
		private IntPtr deviceContext = IntPtr.Zero;
		private static IntPtr renderingContext = IntPtr.Zero;
		private System.ComponentModel.Container components = null;
		#endregion

		internal static IntPtr RenderingContext
		{
			get { return renderingContext; }
		}

		public ContextState()
		{
			InitializeComponent();
			this.Visible = false;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            InitializeContexts();

			// Initialize the symbols
			Symbol.Initialize();
			WxAlertSymbol.Initialize();
			RadarSummarySymbol.Initialize();
			NavaidSymbol.Initialize();
			PIREPSymbol.Initialize();
            GraphicalTafSymbol.Initialize();
			SurfaceFront.Initialize();
			TropopauseSymbol.Initialize();
			TurbulenceSymbol.Initialize();
			EWSDSymbol.Initialize();
			VolcanoSymbol.Initialize();
			DemandCapacitySymbol.Initialize();
		}

		protected void InitializeContexts()
		{
			int pixelFormat;                                                // Holds the selected pixel format

			windowHandle = this.Handle;                                     // Get window handle

			if(windowHandle == IntPtr.Zero) 
			{                               // No window handle means something is wrong
				MessageBox.Show("Window creation error.  No window handle.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();// The pixel format descriptor
			pfd.nSize = (short) Marshal.SizeOf(pfd);                        // Size of the pixel format descriptor
			pfd.nVersion = 1;                                               // Version number (always 1)
			pfd.dwFlags = Gdi.PFD_DRAW_TO_WINDOW |                          // Format must support windowed mode
				Gdi.PFD_SUPPORT_OPENGL |                            // Format must support OpenGL
				Gdi.PFD_DOUBLEBUFFER;                               // Must support double buffering
			pfd.iPixelType = (byte) Gdi.PFD_TYPE_RGBA;                      // Request an RGBA format
			pfd.cColorBits = (byte) colorBits;                              // Select our color depth
			pfd.cRedBits = 0;                                               // Individual color bits ignored
			pfd.cRedShift = 0;
			pfd.cGreenBits = 0;
			pfd.cGreenShift = 0;
			pfd.cBlueBits = 0;
			pfd.cBlueShift = 0;
			pfd.cAlphaBits = 0;                                             // No alpha buffer
			pfd.cAlphaShift = 0;                                            // Alpha shift bit ignored
			pfd.cAccumBits = accumBits;                                     // Accumulation buffer
			pfd.cAccumRedBits = 0;                                          // Individual accumulation bits ignored
			pfd.cAccumGreenBits = 0;
			pfd.cAccumBlueBits = 0;
			pfd.cAccumAlphaBits = 0;
			pfd.cDepthBits = depthBits;                                     // Z-buffer (depth buffer)
			pfd.cStencilBits = stencilBits;                                 // No stencil buffer
			pfd.cAuxBuffers = 0;                                            // No auxiliary buffer
			pfd.iLayerType = (byte) Gdi.PFD_MAIN_PLANE;                     // Main drawing layer
			pfd.bReserved = 0;                                              // Reserved
			pfd.dwLayerMask = 0;                                            // Layer masks ignored
			pfd.dwVisibleMask = 0;
			pfd.dwDamageMask = 0;

			deviceContext = User.GetDC(windowHandle);                       // Attempt to get the device context
			if(deviceContext == IntPtr.Zero) 
			{                              // Did we not get a device context?
				MessageBox.Show("Can not create a GL device context.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			pixelFormat = Gdi.ChoosePixelFormat(deviceContext, ref pfd);    // Attempt to find an appropriate pixel format
			if(pixelFormat == 0) 
			{                                          // Did windows not find a matching pixel format?
				MessageBox.Show("Can not find a suitable PixelFormat.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			if(!Gdi.SetPixelFormat(deviceContext, pixelFormat, ref pfd)) 
			{  // Are we not able to set the pixel format?
				MessageBox.Show("Can not set the chosen PixelFormat.  Chosen PixelFormat was " + pixelFormat + ".", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			renderingContext = Wgl.wglCreateContext(deviceContext);         // Attempt to get the rendering context
			if(renderingContext == IntPtr.Zero) 
			{                           // Are we not able to get a rendering context?
				MessageBox.Show("Can not create a GL rendering context.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			MakeCurrent();                                                  // Attempt to activate the rendering context

			// Force A Reset On The Working Set Size
			Kernel.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
		}

		public void DestroyContexts() 
		{
			if(renderingContext != IntPtr.Zero) 
			{
				Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
				Wgl.wglDeleteContext(renderingContext);
				renderingContext = IntPtr.Zero;
			}

			if(deviceContext != IntPtr.Zero) 
			{
				if(windowHandle != IntPtr.Zero) 
				{
					User.ReleaseDC(windowHandle, deviceContext);
				}
				deviceContext = IntPtr.Zero;
			}
		}

		public void MakeCurrent() 
		{
			// Are we not able to activate the rending context?
			if(!Wgl.wglMakeCurrent(deviceContext, renderingContext)) 
			{
				MessageBox.Show("Can not activate the GL rendering context.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			DestroyContexts();
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// ContextState
			// 
			this.Name = "ContextState";
			this.Size = new System.Drawing.Size(24, 24);

		}
		#endregion
	}
}

using System;
using System.Runtime.InteropServices;
using Tao.OpenGl;
using Tao.Platform.Windows;
using System.Drawing;

namespace WSIMap
{
	/**
	 * \class Font
	 * \brief Represents a font that can be attached to a Label
	 */
	public class Font : IDisposable
	{
		#region Data Members
		protected int openglDisplayListBase;
        internal ABCFloat[] abcf;
		protected string typeFace;
		protected int weight;
        protected System.UInt16 pointSize;
        protected Color color;
		protected IntPtr hDC;
		protected IntPtr font;
		protected const int nChars = 256;
		protected bool initialized;
		protected int lastWin32Error = 0;
		protected bool italic = false;
		protected bool underline = false;
		private bool bold = false;
		#endregion

		public Font() : this("Arial", 9, false, false, false)
		{
		}

		public Font(string typeFace, bool bold) : this(typeFace, 9, bold, false, false)
		{
		}

		public Font(string typeFace, System.UInt16 pointSize, bool bold) : this(typeFace, pointSize, bold, false, false)
		{

		}

        public Font(string typeFace, System.UInt16 pointSize, bool bold, bool italic, bool underline)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("Font Font()");
#endif

			openglDisplayListBase = -1;
			hDC = IntPtr.Zero;
			font = IntPtr.Zero;

			this.typeFace = typeFace;
            this.pointSize = pointSize;
			if (this.pointSize == 0) this.pointSize = 9;
			this.bold = bold;
			if (bold)
				weight = Gdi.FW_BOLD;
			else
				weight = 0;
            color = Color.Black;
			this.underline = underline;
			this.italic = italic;
			
			// Initialize the font; set last Win32 error if it fails
			initialized = InitFont();
			if (!initialized)
			{
                lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
				if (lastWin32Error == 0)
					initialized = true;
				else
					Gl.glDeleteLists(openglDisplayListBase, nChars);
			}
        }

        public System.UInt16 PointSize
        {
            get { return pointSize; }
            set
            {
#if TRACK_OPENGL_DISPLAY_LISTS
				Feature.ConfirmMainThread("Font setPointSize()");
#endif

				Gdi.DeleteObject(font);
                font = IntPtr.Zero;
                pointSize = value;
				if (pointSize == 0)	pointSize = 9;
                if (!InitFont())
                {
                    lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
					if (lastWin32Error != 0)
						Gl.glDeleteLists(openglDisplayListBase, nChars);
                }
            }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

		public bool Italic
		{
			get { return italic; }
			set { italic = true; }
		}

		public bool Underline
		{
			get { return underline; }
			set { underline = value; }
		}

		public bool Bold
		{ 
			get { return bold; } 
		}

		public bool Initialized
		{
			get { return initialized; }
		}

		public string FontName
		{
			get { return typeFace; }
		}

		public int LastWin32Error
		{
			get { return lastWin32Error; }
		}

		public void Dispose()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("Font Dispose()");
#endif

			Gl.glDeleteLists(openglDisplayListBase, nChars);
            openglDisplayListBase = -1;
            Gdi.DeleteObject(font);
            font = IntPtr.Zero;
        }

		internal int OpenGLDisplayListBase
		{
			get { return openglDisplayListBase; }
		}

		protected bool InitFont()
		{
			try
			{
				// Get the current device context
				hDC = Wgl.wglGetCurrentDC();
				if (hDC == IntPtr.Zero)	return false;

				// Create the display lists to hold the characters
				if (openglDisplayListBase == -1)
					openglDisplayListBase = Gl.glGenLists(nChars);
				if (openglDisplayListBase == 0)	return false;

				// Create the font
				font = Gdi.CreateFont(-pointSize, 0, 0, 0, weight, italic, underline, false,
					Gdi.ANSI_CHARSET, Gdi.OUT_TT_PRECIS, Gdi.CLIP_DEFAULT_PRECIS,
					Gdi.NONANTIALIASED_QUALITY, Gdi.FF_DONTCARE | Gdi.DEFAULT_PITCH, typeFace);
				if (font == IntPtr.Zero) return false;

				// Select the font into the device context
				if (Gdi.SelectObject(hDC, font) == IntPtr.Zero) return false;

				// Get the font glyph metrics
				abcf = new ABCFloat[nChars];
				if (!GetCharABCWidthsFloat(hDC, 0, (uint)(nChars - 1), abcf)) return false;

				// Create the OpenGL font bitmaps
                return Wgl.wglUseFontBitmaps(hDC, 0, nChars - 1, openglDisplayListBase);
                //xyz = new Gdi.GLYPHMETRICSFLOAT[nChars];
                //return Wgl.wglUseFontOutlines(hDC, 0, nChars - 1, openglDisplayListBase, 0, 0.0f, Wgl.WGL_FONT_POLYGONS, xyz);
			}
			catch
			{
				return false;
			}
		}

		[DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool GetCharABCWidthsFloat(IntPtr hdc, uint iFirstChar, uint iLastChar, [Out] ABCFloat[] lpABCF);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ABCFloat
        {
            public float abcfA;
            public float abcfB;
            public float abcfC;
        }
    }

    public class RotatableFont : IDisposable
    {
        #region Data Members
        protected int openglDisplayListBase;
        internal Gdi.GLYPHMETRICSFLOAT[] xyz;
        protected string typeFace;
        protected int weight;
        protected System.UInt16 pointSize;
        protected Color color;
        protected IntPtr hDC;
        protected IntPtr font;
        protected const int nChars = 256;
        protected bool initialized;
        protected int lastWin32Error = 0;
        #endregion

        public RotatableFont()
            : this("Arial", 9, false)
        {
        }

        public RotatableFont(string typeFace, bool bold)
            : this(typeFace, 9, bold)
        {
        }

        public RotatableFont(string typeFace, System.UInt16 pointSize, bool bold)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("RotatableFont RotatableFont()");
#endif

			openglDisplayListBase = -1;
            hDC = IntPtr.Zero;
            font = IntPtr.Zero;

            this.typeFace = typeFace;
            this.pointSize = pointSize;
            if (this.pointSize == 0) this.pointSize = 9;
            if (bold)
                weight = Gdi.FW_BOLD;
            else
                weight = 0;
            color = Color.Black;

            // Initialize the font; set last Win32 error if it fails
            initialized = InitFont();
            if (!initialized)
            {
                lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (lastWin32Error == 0)
                    initialized = true;
                else
                    Gl.glDeleteLists(openglDisplayListBase, nChars);
            }
        }

        public System.UInt16 PointSize
        {
            get { return pointSize; }
            set
            {
#if TRACK_OPENGL_DISPLAY_LISTS
				Feature.ConfirmMainThread("RotatableFont setPointSize()");
#endif

				Gdi.DeleteObject(font);
                font = IntPtr.Zero;
                pointSize = value;
                if (pointSize == 0) pointSize = 9;
                if (!InitFont())
                {
                    lastWin32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    if (lastWin32Error != 0)
                        Gl.glDeleteLists(openglDisplayListBase, nChars);
                }
            }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public bool Initialized
        {
            get { return initialized; }
        }

        public int LastWin32Error
        {
            get { return lastWin32Error; }
        }

        public void Dispose()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("RotatableFont Dispose()");
#endif

			Gl.glDeleteLists(openglDisplayListBase, nChars);
            openglDisplayListBase = -1;
            Gdi.DeleteObject(font);
            font = IntPtr.Zero;
        }

        internal int OpenGLDisplayListBase
        {
            get { return openglDisplayListBase; }
        }

        protected bool InitFont()
        {
            try
            {
                // Get the current device context
                hDC = Wgl.wglGetCurrentDC();
                if (hDC == IntPtr.Zero) return false;

                // Create the display lists to hold the characters
                if (openglDisplayListBase == -1)
                    openglDisplayListBase = Gl.glGenLists(nChars);
                if (openglDisplayListBase == 0) return false;

                // Create the font
                font = Gdi.CreateFont(-pointSize, 0, 0, 0, weight, false, false, false,
                    Gdi.ANSI_CHARSET, Gdi.OUT_TT_PRECIS, Gdi.CLIP_DEFAULT_PRECIS,
                    Gdi.NONANTIALIASED_QUALITY, Gdi.FF_DONTCARE | Gdi.DEFAULT_PITCH, typeFace);
                if (font == IntPtr.Zero) return false;

                // Select the font into the device context
                if (Gdi.SelectObject(hDC, font) == IntPtr.Zero) return false;

                // Create the OpenGL font bitmaps
                xyz = new Gdi.GLYPHMETRICSFLOAT[nChars];
                return Wgl.wglUseFontOutlines(hDC, 0, nChars, openglDisplayListBase, 0, 0.0f, Wgl.WGL_FONT_POLYGONS, xyz);
            }
            catch
            {
                return false;
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool GetCharABCWidthsFloat(IntPtr hdc, uint iFirstChar, uint iLastChar, [Out] ABCFloat[] lpABCF);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ABCFloat
        {
            public float abcfA;
            public float abcfB;
            public float abcfC;
        }
    }

}

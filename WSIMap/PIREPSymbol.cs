using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
    public enum PIREPSymbolType
    {
        Icing_None = 0,
        Icing_Light,
        Icing_Moderate,
        Icing_Severe,
        Turbulence_Light,
        Turbulence_Moderate,
        Turbulence_Severe,
        Turbulence_Extreme,
        Other,
        LowLevelWindShear,
        Turbulence_None,
		TAP_None,
		TAP_Light,
		TAP_Moderate,
		TAP_Severe
    };
		
	/**
	 * \class Symbol
	 * \brief Handles rendering of plane and other map symbols
	 */
	public class PIREPSymbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected static int openglDisplayListNone;
        protected static int openglDisplayListIcing_Light;
		protected static int openglDisplayListIcing_Moderate;
		protected static int openglDisplayListIcing_Severe;
		//protected static int openglDisplayListTurbulence_None;
		protected static int openglDisplayListTurbulence_Light;
		protected static int openglDisplayListTurbulence_Moderate;
		protected static int openglDisplayListTurbulence_Severe;
		protected static int openglDisplayListTurbulence_Extreme;
		protected static int openglDisplayListLLWS;
		protected static int openglDisplayListOther;
		protected static int openglDisplayListBoxLine;
        protected static int openglDisplayListBoxPolygon;
		protected static int openglDisplayListTap_None;
		protected static int openglDisplayListTap_Moderate;
		protected static int openglDisplayListTap_Light;
		protected static int openglDisplayListTap_Severe;
		protected double direction;
		protected bool highlight;
        protected enum BoundingBoxType { Line, Polygon };
        private static uint[] texture = new uint[15];
        private static string directory;
        private PIREPSymbolType type;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		#endregion

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("PIREPSymbol Draw()");
#endif

			if (openglDisplayList == -1) return;

            double _x = x;
            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                _x = parentMap.DenormalizeLongitude(x);

			// Set the map projection
			this.mapProjection = parentMap.MapProjection;
			this.centralLongitude = parentMap.CentralLongitude;

			// Do not draw points below the equator for azimuthal projections
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && y < Projection.MinAzimuthalLatitude) return;

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Turn on anti-aliasing
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

			// Preserve the projection matrix state
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();

			// Calculate the rendering scale factors
            double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));
               
			// Set the position and size of the symbol
			double px, py;
			Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
			Gl.glTranslated(px, py, 0.0);
            double symbolSize = size * 1.5;
            Gl.glScaled(symbolSize / xFactor, symbolSize / yFactor, 1.0);

			// Set the symbol color
			double alpha = 1;
			if (color == System.Drawing.Color.Transparent) alpha = 0;
			Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), alpha);

			// Draw the symbol
			Gl.glCallList(openglDisplayList);

			// Add highlight bounding box if requested
            if (highlight)
                Gl.glCallList(openglDisplayListBoxLine);

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}

        static PIREPSymbol()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("PIREPSymbol PIREPSymbol()");
#endif

			if (!FusionSettings.Client.Developer)
				directory = FusionSettings.Data.Directory;
			else
				directory = directory = @"..\Data\";

            if (!System.IO.Directory.Exists(directory))
	            throw new Exception("Cannot load pirep legend");

			CreateNoneSymbol();
			CreateIcing_LightSymbol();
			CreateIcing_ModerateSymbol();
			CreateIcing_SevereSymbol();
			//CreateTurbulence_NoneSymbol();
			CreateTurbulence_LightSymbol();
			CreateTurbulence_ModerateSymbol();
			CreateTurbulence_SevereSymbol();
			CreateTurbulence_ExtremeSymbol();
			CreateOtherSymbol();
			CreateLLWSSymbol();
			CreateBoundingBox(BoundingBoxType.Line);
			//CreateBoundingBox(BoundingBoxType.Polygon);
			CreateTap_NoneSymbol();
			CreateTap_LightSymbol();
			CreateTap_ModerateSymbol();
			CreateTap_SevereSymbol();
        }

		public PIREPSymbol(PIREPSymbolType type, uint size, double latitude, double longitude)
		{
			this.size = size;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
            this.type = type;
			this.mapProjection = MapProjections.CylindricalEquidistant;

			if (type == PIREPSymbolType.Icing_None || type == PIREPSymbolType.Turbulence_None)
			{
				openglDisplayList = openglDisplayListNone;
				this.color = Color.Blue;
			}
			else if (type == PIREPSymbolType.Icing_Light)
			{
				openglDisplayList = openglDisplayListIcing_Light;
				this.color = Color.Green;
			}
			else if (type == PIREPSymbolType.Icing_Moderate)
			{
				openglDisplayList = openglDisplayListIcing_Moderate;
				this.color = Color.Yellow;
			}
			else if (type == PIREPSymbolType.Icing_Severe)
			{
				openglDisplayList = openglDisplayListIcing_Severe;
				this.color = Color.Red;
			}
			//else if (type == PIREPSymbolType.None)
			//{
			//    openglDisplayList = openglDisplayListTurbulence_None;
			//    this.color = Color.Blue;
			//}
			else if (type == PIREPSymbolType.Turbulence_Light)
			{
				openglDisplayList = openglDisplayListTurbulence_Light;
				this.color = Color.Green;
			}
			else if (type == PIREPSymbolType.Turbulence_Moderate)
			{
				openglDisplayList = openglDisplayListTurbulence_Moderate;
				this.color = Color.Yellow;
			}
			else if (type == PIREPSymbolType.Turbulence_Severe)
			{
				openglDisplayList = openglDisplayListTurbulence_Severe;
				this.color = Color.Red;
			}
			else if (type == PIREPSymbolType.Turbulence_Extreme)
			{
				openglDisplayList = openglDisplayListTurbulence_Extreme;
				this.color = Color.Red;
			}
			else if (type == PIREPSymbolType.LowLevelWindShear)
			{
				openglDisplayList = openglDisplayListLLWS;
				this.color = Color.Red;
			}
			else if (type == PIREPSymbolType.Other)
			{
				openglDisplayList = openglDisplayListOther;
				this.color = Color.Blue;
			}
			else if (type == PIREPSymbolType.TAP_None)
			{
				openglDisplayList = openglDisplayListTap_None;
				this.color = Color.Blue;
			}
			else if (type == PIREPSymbolType.TAP_Light)
			{
				openglDisplayList = openglDisplayListTap_Light;
				this.color = Color.LimeGreen;
			}
			else if (type == PIREPSymbolType.TAP_Moderate)
			{
				openglDisplayList = openglDisplayListTap_Moderate;
				this.color = Color.Peru;
			}
			else if (type == PIREPSymbolType.TAP_Severe)
			{
				openglDisplayList = openglDisplayListTap_Severe;
				this.color = Color.Red;
			}

			SetPosition(latitude, longitude);

			this.highlight = false;
		}

		public PIREPSymbol(PIREPSymbolType type, uint size, double latitude, double longitude, bool highlight): this(type, size, latitude, longitude)
		{
			this.highlight = highlight;
		}

		public double X
		{
			get { return x; }
			set { x = value; }
		}

		public double Y
		{
			get { return y; }
			set { y = value; }
		}

		public double Longitude
		{
			get { return x; }
			set { x = value; }
		}

		public double Latitude
		{
			get { return y; }
			set { y = value; }
		}

		public uint Size
		{
			get { return size; }
			set { size = value; }
		}

		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public void SetPosition(double latitude, double longitude)
		{
			x = longitude;
			y = latitude;
		}

		public void SetPosition(PointD point)
		{
			x = point.X;
			y = point.Y;
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		private static void BindTexture(PIREPSymbolType pirepType)
        {
            Gl.glEnable(Gl.GL_TEXTURE_2D);									// Enable Texture Mapping
            Gl.glShadeModel(Gl.GL_SMOOTH);									// Enable Smooth Shading

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[(int)pirepType]);

            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex2f(-1.0f, -1.0f);
            Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex2f(1.0f, -1.0f);
            Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex2f(1.0f, 1.0f);
            Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex2f(-1.0f, 1.0f);
            Gl.glEnd();

            Gl.glDisable(Gl.GL_TEXTURE_2D);
        }

        protected static void CreateNoneSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Icing_None))
            {
                openglDisplayListNone = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListNone = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListNone, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Icing_None);

            Gl.glEndList();
        }

        protected static void CreateIcing_LightSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Icing_Light))
            {
                openglDisplayListIcing_Light = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListIcing_Light = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListIcing_Light, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Icing_Light);

            Gl.glEndList();
        }

        protected static void CreateIcing_ModerateSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Icing_Moderate))
            {
                openglDisplayListIcing_Moderate = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListIcing_Moderate = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListIcing_Moderate, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Icing_Moderate);

            Gl.glEndList();
        }

        protected static void CreateIcing_SevereSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Icing_Severe))
            {
                openglDisplayListIcing_Severe = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListIcing_Severe = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListIcing_Severe, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Icing_Severe);

            Gl.glEndList();
        }

        protected static void CreateTurbulence_LightSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Turbulence_Light))
            {
                openglDisplayListTurbulence_Light = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListTurbulence_Light = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTurbulence_Light, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Turbulence_Light);

            Gl.glEndList();
        }

        protected static void CreateTurbulence_ModerateSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Turbulence_Moderate))
            {
                openglDisplayListTurbulence_Moderate = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListTurbulence_Moderate = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTurbulence_Moderate, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Turbulence_Moderate);

            Gl.glEndList();
        }

        protected static void CreateTurbulence_SevereSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Turbulence_Severe))
            {
                openglDisplayListTurbulence_Severe = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListTurbulence_Severe = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTurbulence_Severe, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Turbulence_Severe);

            Gl.glEndList();
        }

        protected static void CreateTurbulence_ExtremeSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Turbulence_Extreme))
            {
                openglDisplayListTurbulence_Extreme = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListTurbulence_Extreme = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTurbulence_Extreme, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Turbulence_Extreme);

            Gl.glEndList();
        }

        protected static void CreateOtherSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.Other))
            {
                openglDisplayListOther = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListOther = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListOther, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.Other);

            Gl.glEndList();
        }

        protected static void CreateLLWSSymbol()
        {
            if (!LoadTextures(PIREPSymbolType.LowLevelWindShear))
            {
                openglDisplayListLLWS = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListLLWS = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListLLWS, Gl.GL_COMPILE);

            BindTexture(PIREPSymbolType.LowLevelWindShear);

            Gl.glEndList();
        }

		protected static void CreateTap_NoneSymbol()
		{
			if (!LoadTextures(PIREPSymbolType.TAP_None))
			{
				openglDisplayListTap_None = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListTap_None = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTap_None, Gl.GL_COMPILE);

			BindTexture(PIREPSymbolType.TAP_None);

			Gl.glEndList();
		}

		protected static void CreateTap_LightSymbol()
		{
			if (!LoadTextures(PIREPSymbolType.TAP_Light))
			{
				openglDisplayListTap_Light = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListTap_Light = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTap_Light, Gl.GL_COMPILE);

			BindTexture(PIREPSymbolType.TAP_Light);

			Gl.glEndList();
		}

		protected static void CreateTap_ModerateSymbol()
		{
			if (!LoadTextures(PIREPSymbolType.TAP_Moderate))
			{
				openglDisplayListTap_Moderate = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListTap_Moderate = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTap_Moderate, Gl.GL_COMPILE);

			BindTexture(PIREPSymbolType.TAP_Moderate);

			Gl.glEndList();
		}

		protected static void CreateTap_SevereSymbol()
		{
			if (!LoadTextures(PIREPSymbolType.TAP_Severe))
			{
				openglDisplayListTap_Severe = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListTap_Severe = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListTap_Severe, Gl.GL_COMPILE);

			BindTexture(PIREPSymbolType.TAP_Severe);

			Gl.glEndList();
		}

        protected static void CreateBoundingBox(BoundingBoxType type)
		{
			// Create an OpenGL display list
            if (type == BoundingBoxType.Line)
            {
                openglDisplayListBoxLine = Gl.glGenLists(1);
			    Gl.glNewList(openglDisplayListBoxLine, Gl.GL_COMPILE);
            }
            else
            {
                openglDisplayListBoxPolygon = Gl.glGenLists(1);
			    Gl.glNewList(openglDisplayListBoxPolygon, Gl.GL_COMPILE);
            }

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			Gl.glLineWidth(1);

			// Initialize vertices
            double size = 1.0;

			double[,] v = new double[4,3];
			v[0,0] = -size; v[0,1] = size; v[0,2] = 0;
			v[1,0] = size; v[1,1] = size; v[1,2] = 0;
			v[2,0] = size; v[2,1] = -size; v[2,2] = 0;
			v[3,0] = -size; v[3,1] = -size; v[3,2] = 0;

			if (type == BoundingBoxType.Line)
                Gl.glBegin(Gl.GL_LINE_LOOP);
            else
                Gl.glBegin(Gl.GL_POLYGON);
			for (int i=0; i<4; i++)
				Gl.glVertex2d(v[i,0], v[i,1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

        private static bool LoadTextures(PIREPSymbolType pirepType)
        {
            Bitmap image = null;
            string fileName = string.Empty;

            switch (pirepType)
            {
                case PIREPSymbolType.Icing_None:
                    fileName = "noreport.png";
                    break;
                case PIREPSymbolType.Icing_Light:
                    fileName = "icglght.png";
                    break;
                case PIREPSymbolType.Icing_Moderate:
                    fileName = "icgmod.png";
                    break;
                case PIREPSymbolType.Icing_Severe:
                    fileName = "icgsev.png";
                    break;
                case PIREPSymbolType.Turbulence_Light:
                    fileName = "turbclght.png";
                    break;
                case PIREPSymbolType.Turbulence_Moderate:
                    fileName = "turbmod.png";
                    break;
                case PIREPSymbolType.Turbulence_Severe:
                    fileName = "turbsev.png";
                    break;
                case PIREPSymbolType.Turbulence_Extreme:
                    fileName = "turbext.png";
                    break;
                case PIREPSymbolType.LowLevelWindShear:
                    fileName = "llws.png";
                    break;
                case PIREPSymbolType.Other:
					fileName = "routine.png"; //"otherurgent.png";
                    break;
				case PIREPSymbolType.TAP_None:
					fileName = "TapNone.png";
					break;
				case PIREPSymbolType.TAP_Light:
					fileName = "TapLight.png";
					break;
				case PIREPSymbolType.TAP_Moderate:
					fileName = "TapModerate.png";
					break;
				case PIREPSymbolType.TAP_Severe:
					fileName = "TapSevere.png";
					break;
                default: return false;
            }

            try
            {
                // If the file doesn't exist or can't be found, an ArgumentException is thrown instead of
                // just returning null
                image = new Bitmap(directory + fileName);
            }
            catch (System.ArgumentException)
            {
                image = null;
            }

            try
            {
                if (image != null)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    System.Drawing.Imaging.BitmapData bitmapdata;
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    System.Drawing.Imaging.PixelFormat format = image.PixelFormat;
                    if (fileName.EndsWith("bmp"))
                        format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

                    bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, format);

                    Gl.glGenTextures(1, out texture[(int)pirepType]);

                    // Create Linear Filtered Texture
                    Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[(int)pirepType]);
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);

                    int iFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_RGBA : Gl.GL_RGB;
                    int eFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_BGRA : Gl.GL_BGR;

                    Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, iFormat, image.Width, image.Height, 0, eFormat, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

                    image.UnlockBits(bitmapdata);
                    image.Dispose();
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        public PIREPSymbolType Type
        {
            get { return type; }
        }
	}
}

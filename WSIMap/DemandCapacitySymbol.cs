using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public enum DemandCapacitySymbolType
	{
		None = 0,
		Light, 
		Moderate,
		Heavy,
		Extreme
	}
	public class DemandCapacitySymbol : Feature, IMapPoint, IProjectable
	{
		#region DataMembers
		protected double x;
		protected double y;
		protected uint size;
		protected Color color;
		protected static int openglDisplayList_None;
		protected static int openglDisplayList_Light;
		protected static int openglDisplayList_Moderate;
		protected static int openglDisplayList_Heavy;
		protected static int openglDisplayList_Extreme;
		protected static int openglDisplayListBoxLine;
		protected static int openglDisplayListBoxPolygon;

		protected bool highlight;
		protected enum BoundingBoxType { Line, Polygon };
		private static uint[] texture = new uint[5];
		private static string directory;
		private DemandCapacitySymbolType type;

		protected MapProjections mapProjection;
		protected short centralLongitude;

		#endregion

		internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

		public void SetSize(uint size)
		{
			this.size = size;
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("DemandCapacitySymbol Draw()");
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

		static DemandCapacitySymbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("DemandCapacitySymbol DemandCapacitySymbol()");
#endif

			if (!FusionSettings.Client.Developer)
				directory = FusionSettings.Data.Directory;
			else
				directory = directory = @"..\Data\";

			if (!System.IO.Directory.Exists(directory))
				throw new Exception("Cannot load Demand Capacity legend");

			CreateDemandCapactiy_NoneSymbol(); // Grey #aeb3b9
			CreateDemandCapactity_LightSymbol(); //Green #009933
			CreateDemandCapactity_ModerateSymbol(); // Yellow #ffff00
			CreateDemandCapactity_HeavySymbol(); // Orange #ffa400
			CreateDemandCapactity_ExtremeSymbol(); // Red #ff0000
			CreateBoundingBox(BoundingBoxType.Line);

		}

		public DemandCapacitySymbol(DemandCapacitySymbolType type, double latitude, double longitude)
		{
			this.size = 5;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
            this.type = type;
			this.mapProjection = MapProjections.CylindricalEquidistant;

			if (type == DemandCapacitySymbolType.None)
			{
				openglDisplayList = openglDisplayList_None;
				this.color = Color.White;
				//this.color = Color.Gray;
			}
			else if (type == DemandCapacitySymbolType.Light)
			{
				openglDisplayList = openglDisplayList_Light;
				this.color = Color.White;
				//this.color = Color.Gray;
			}
			else if (type == DemandCapacitySymbolType.Moderate)
			{
				openglDisplayList = openglDisplayList_Moderate;
				this.color = Color.White;
				//this.color = Color.Green;
			}
			else if (type == DemandCapacitySymbolType.Heavy)
			{
				openglDisplayList = openglDisplayList_Heavy;
				this.color = Color.White;
				//this.color = Color.Yellow;
			}
			else if (type == DemandCapacitySymbolType.Extreme)
			{
				openglDisplayList = openglDisplayList_Extreme;
				this.color = Color.White;
				//this.color = Color.Red;
			}		

			SetPosition(latitude, longitude);

			this.highlight = false;
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

		protected static void CreateDemandCapactiy_NoneSymbol()
		{
			if (!LoadTextures(DemandCapacitySymbolType.None))
			{
				openglDisplayList_None = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayList_None = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayList_None, Gl.GL_COMPILE);

			BindTexture(DemandCapacitySymbolType.None);

			Gl.glEndList();
		}

		protected static void CreateDemandCapactity_ExtremeSymbol()
		{
			if (!LoadTextures(DemandCapacitySymbolType.Extreme))
			{
				openglDisplayList_Extreme = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayList_Extreme = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayList_Extreme, Gl.GL_COMPILE);

			BindTexture(DemandCapacitySymbolType.Extreme);

			Gl.glEndList();
		}

		protected static void CreateDemandCapactity_HeavySymbol()
		{
			if (!LoadTextures(DemandCapacitySymbolType.Heavy))
			{
				openglDisplayList_Heavy = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayList_Heavy = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayList_Heavy, Gl.GL_COMPILE);

			BindTexture(DemandCapacitySymbolType.Heavy);

			Gl.glEndList();
		}

		protected static void CreateDemandCapactity_ModerateSymbol()
		{
			if (!LoadTextures(DemandCapacitySymbolType.Moderate))
			{
				openglDisplayList_Moderate = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayList_Moderate = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayList_Moderate, Gl.GL_COMPILE);

			BindTexture(DemandCapacitySymbolType.Moderate);

			Gl.glEndList();
		}
		
		protected static void CreateDemandCapactity_LightSymbol()
		{
			if (!LoadTextures(DemandCapacitySymbolType.Light))
			{
				openglDisplayList_Light = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayList_Light = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayList_Light, Gl.GL_COMPILE);

			BindTexture(DemandCapacitySymbolType.Light);

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

			double[,] v = new double[4, 3];
			v[0, 0] = -size; v[0, 1] = size; v[0, 2] = 0;
			v[1, 0] = size; v[1, 1] = size; v[1, 2] = 0;
			v[2, 0] = size; v[2, 1] = -size; v[2, 2] = 0;
			v[3, 0] = -size; v[3, 1] = -size; v[3, 2] = 0;

			if (type == BoundingBoxType.Line)
				Gl.glBegin(Gl.GL_LINE_LOOP);
			else
				Gl.glBegin(Gl.GL_POLYGON);
			for (int i = 0; i < 4; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void BindTexture(DemandCapacitySymbolType symbolType)
		{
			Gl.glEnable(Gl.GL_TEXTURE_2D);									// Enable Texture Mapping
			Gl.glShadeModel(Gl.GL_SMOOTH);									// Enable Smooth Shading

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[(int)symbolType]);

			Gl.glBegin(Gl.GL_QUADS);
			Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex2f(-1.0f, -1.0f);
			Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex2f(1.0f, -1.0f);
			Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex2f(1.0f, 1.0f);
			Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex2f(-1.0f, 1.0f);
			Gl.glEnd();

			Gl.glDisable(Gl.GL_TEXTURE_2D);
		}

		private static bool LoadTextures(DemandCapacitySymbolType symbolType)
		{
			Bitmap image = null;
			string fileName = string.Empty;

			//switch (symbolType)
			//{
			//	case DemandCapacitySymbolType.None:
			//		fileName = "Gray_diversion_16.png";
			//		break;
			//	case DemandCapacitySymbolType.Light:
			//		fileName = "Green_diversion_16.png";
			//		break;
			//	case DemandCapacitySymbolType.Moderate:
			//		fileName = "Yellow_diversion_16.png";
			//		break;
			//	case DemandCapacitySymbolType.Heavy:
			//		fileName = "Orange_diversion_16.png";
			//		break;
			//	case DemandCapacitySymbolType.Extreme:
			//		fileName = "Red_diversion_16.png";
			//		break;
			//	default: return false;
			//}

			switch (symbolType)
			{
				case DemandCapacitySymbolType.None:
					fileName = "Gray_diversion_32.png";
					break;
				case DemandCapacitySymbolType.Light:
					fileName = "Green_diversion_32.png";
					break;
				case DemandCapacitySymbolType.Moderate:
					fileName = "Yellow_diversion_32.png";
					break;
				case DemandCapacitySymbolType.Heavy:
					fileName = "Orange_diversion_32.png";
					break;
				case DemandCapacitySymbolType.Extreme:
					fileName = "Red_diversion_32.png";
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

					Gl.glGenTextures(1, out texture[(int)symbolType]);

					// Create Linear Filtered Texture
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[(int)symbolType]);
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

		public DemandCapacitySymbolType Type
		{
			get { return type; }
		}
	}
}

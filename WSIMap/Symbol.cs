using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	public enum SymbolType { Arrow, Circle, Triangle, Square, Plane, Ring, Bolt, Plus, UnfilledPlane, OutlinedPlane,
		Volcano, Box, EquilateralTriangle, TPCircle, High, Low, HurricaneN, HurricaneS, TropicalStormN, TropicalStormS,
		WSIHurricaneN, WSIHurricaneS, WSITropicalStormN, WSITropicalStormS, Bell206, Boeing737, Boeing747, C172, LearJet,
		DoubleTriangle, SAAB, ShiftedArrow, ParkedAircraft, Pushpin, UnfilledBell206, BrakeActionFair, 
        BrakeActionGood, BrakeActionMedium, BrakeActionPoor, BrakeActionNil, BrakeActionNA };
	
	/**
	 * \class Symbol
	 * \brief Handles rendering of plane and other map symbols
	 */
    [Serializable]
	public class Symbol : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		protected double x;
		protected double y;
		protected double xOffset;
		protected double yOffset;
		protected uint size;
		protected Color color;
		protected static int openglDisplayListCircle;
		protected static int openglDisplayListTriangle;
		protected static int openglDisplayListSquare;
		protected static int openglDisplayListPlane;
		protected static int openglDisplayListArrow;
		protected static int openglDisplayListRing;
        protected static int openglDisplayListBolt;
        protected static int openglDisplayListPlus;
        protected static int openglDisplayListUnfilledPlane;
        protected static int openglDisplayListOutlinedPlane;
        protected static int openglDisplayListVolcano;
        protected static int openglOutlineDisplayListTriangle;
        protected static int openglOutlineDisplayListSquare;
        protected static int openglOutlineDisplayListArrow;
        protected static int openglDisplayListEquilateralTriangle;
		protected static int openglOutlineDisplayListEquilateralTriangle;
        protected static int openglDisplayListTPCircle;
		protected static int openglDisplayListHigh;
		protected static int openglOutlineDisplayListHigh;
		protected static int openglDisplayListLow;
		protected static int openglOutlineDisplayListLow;
		protected static int openglDisplayListNorthHurricane;
		protected static int openglDisplayListNorthTropicalStorm;
		protected static int openglDisplayListSouthHurricane;
		protected static int openglDisplayListSouthTropicalStorm;
		protected static int openglDisplayListWSINorthHurricane;
		protected static int openglDisplayListWSISouthHurricane;
		protected static int openglDisplayListWSINorthTropicalStorm;
		protected static int openglDisplayListWSISouthTropicalStorm;
		protected static int openglDisplayListParkedAircraft;
		protected static int openglDisplayListPushpin;
		protected static int openglDisplayListBell206;
		protected static int openglDisplayListOutlinedBell206;
		protected static int openglDisplayList737;
		protected static int openglDisplayListOutlined737;
		protected static int openglDisplayList747;
		protected static int openglDisplayListOutlined747;
		protected static int openglDisplayListC172;
		protected static int openglDisplayListOutlinedC172;
		protected static int openglDisplayListLearJet;
		protected static int openglDisplayListOutlinedLearJet;
		protected static int openglDisplayListSAAB;
		protected static int openglDisplayListOutlinedSAAB;
		protected static int openglDisplayListShiftedArrow;
		protected static int openglDisplayListOutlinedShiftedArrow;
		protected static int openglDisplayListDoubleTriangle;
		protected static int openglOutlineDisplayListDoubleTriangle;
        protected static int openglDisplayListBrakeActionFair;
        protected static int openglDisplayListBrakeActionGood;
        protected static int openglDisplayListBrakeActionMedium;
        protected static int openglDisplayListBrakeActionPoor;
        protected static int openglDisplayListBrakeActionNIL;
        protected static int openglDisplayListBrakeActionNA;
		protected double direction;
		protected bool rotate;
        protected SymbolType type;
        protected bool outlined;
        protected Color outlineColor;
        protected int openglOutlineDisplayList = -1;
		protected static uint[] texture = new uint[12];
		protected static string directory;
		protected MapProjections mapProjection;
		protected short centralLongitude;
        protected int opacity;

        private bool unfilled = false;
		#endregion

        #region DllImports
		[DllImport("tessellate.dll", EntryPoint = "TessellatePlaneSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellatePlaneSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateOutlinedPlaneSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateOutlinedPlaneSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateTriangleSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateTriangleSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateCircleSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateCircleSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateSquareSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateSquareSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateArrowSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateArrowSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateBell206Symbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateBell206Symbol();
		[DllImport("tessellate.dll", EntryPoint = "Tessellate737Symbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int Tessellate737Symbol();
		[DllImport("tessellate.dll", EntryPoint = "Tessellate747Symbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int Tessellate747Symbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateC172Symbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateC172Symbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateLearJetSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateLearJetSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateSAABSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateSAABSymbol();
		[DllImport("tessellate.dll", EntryPoint = "TessellateShiftedArrowSymbol", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int TessellateShiftedArrowSymbol();
        #endregion

        internal static void Initialize()
		{
			// calling this function forces the static constructor to get called
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Symbol Draw()");
#endif

			if (openglDisplayList == -1) return;

            double _x = x;
			double _y = y;
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
			if (type != SymbolType.HurricaneN && type != SymbolType.HurricaneS)
				Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

			// Set the symbol color
			double alpha = 1;
            if (color == System.Drawing.Color.Transparent)
                alpha = 0;
            else if (opacity > 0)
                alpha = (float)opacity / 100;
			Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), alpha);

			// Preserve the projection matrix state
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();

			// Calculate the rendering scale factors
            double xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            double yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Set the position and size of the symbol
			double px, py, pdir;
			Projection.ProjectPoint(mapProjection, _x, _y, centralLongitude, out px, out py);			
			px += this.xOffset / xFactor;
			py += this.yOffset / yFactor;
			
			if (type == SymbolType.EquilateralTriangle || type == SymbolType.ParkedAircraft
				|| type == SymbolType.Pushpin || type == SymbolType.DoubleTriangle) // Map symbols that need to be oriented "up" rather than "north"
				pdir = direction;
			else
				Projection.ProjectDirection(mapProjection, _x, _y, direction, centralLongitude, out pdir);
			Gl.glTranslated(px, py, 0.0);
			if (rotate)
				Gl.glRotated(360 - pdir, 0.0, 0.0, 1.0);	// OpenGL rotation is counterclockwise
			double symbolSize = (size + 1) * 0.05;
			if (type == SymbolType.WSITropicalStormN || type == SymbolType.WSITropicalStormS
				|| type == SymbolType.WSIHurricaneN || type == SymbolType.WSIHurricaneS
				|| type == SymbolType.ParkedAircraft || type == SymbolType.Pushpin
                || type == SymbolType.BrakeActionFair || type == SymbolType.BrakeActionGood
                || type == SymbolType.BrakeActionMedium || type == SymbolType.BrakeActionNil 
                || type == SymbolType.BrakeActionNA)
				symbolSize = size * 1.5;
			Gl.glScaled(symbolSize/xFactor, symbolSize/yFactor, 1.0);
 
            // Draw the symbol
            if (unfilled && (openglOutlineDisplayList != -1))
            {
                if (type != SymbolType.High && type != SymbolType.Low)
                    Gl.glScaled(1.02, 1.02, 1.0);

                Gl.glCallList(openglOutlineDisplayList);
            }
            else
            {
                Gl.glCallList(openglDisplayList);

                if (outlined && (openglOutlineDisplayList != -1))
                {
                    if (type != SymbolType.High && type != SymbolType.Low)
                        Gl.glScaled(1.02, 1.02, 1.0);
                    alpha = 1;
                    if (outlineColor == System.Drawing.Color.Transparent) alpha = 0;
                    Gl.glColor4d(glc(outlineColor.R), glc(outlineColor.G), glc(outlineColor.B), alpha);

                    Gl.glCallList(openglOutlineDisplayList);
                }
            }

			// Turn off anti-aliasing
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
			Gl.glDisable(Gl.GL_POLYGON_SMOOTH);

			// Restore previous projection matrix
			Gl.glPopMatrix();
		}
		static Symbol()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Symbol Symbol()");
#endif

			if (!FusionSettings.Client.Developer)
				directory = FusionSettings.Data.Directory;
			else
				directory = directory = @"..\Data\";

			if (!System.IO.Directory.Exists(directory))
				throw new Exception("Cannot load symbols");

			CreatePlaneSymbol();
            CreateOutlinedPlaneSymbol();
            CreateUnfilledPlaneSymbol();
			CreateCircleSymbol();
			CreateTriangleSymbol();
			CreateSquareSymbol();
			CreateArrowSymbol();
			CreateRingSymbol();
            CreateBoltSymbol();
            CreatePlusSymbol();
            CreateVolcanoSymbol();
            CreateOutlineSquareSymbol();
            CreateOutlineTriangleSymbol();
			CreateOutlineEquilateralTriangleSymbol();
            CreateOutlineArrowSymbol();
            CreateEquilateralTriangleSymbol();
            CreateTPCircleSymbol();
			CreateHighSymbol();
			CreateLowSymbol();
			CreateTropicalSymbol(SymbolType.HurricaneN);
			CreateTropicalSymbol(SymbolType.HurricaneS);
			CreateTropicalSymbol(SymbolType.TropicalStormN);
			CreateTropicalSymbol(SymbolType.TropicalStormS);
			CreateWSIHurricaneNSymbol();
			CreateWSIHurricaneSSymbol();
			CreateWSITropicalStormNSymbol();
			CreateWSITropicalStormSSymbol();
			CreateParkedAircraftSymbol();
			CreatePushpinSymbol();
			CreateBell206Symbol();
			CreateOutlinedBell206Symbol();
			Create737Symbol();
			CreateOutlined737Symbol();
			Create747Symbol();
			CreateOutlined747Symbol();
			CreateC172Symbol();
			CreateOutlinedC172Symbol();
			CreateLearJetSymbol();
			CreateOutlinedLearJetSymbol();
			CreateSAABSymbol();
			CreateOutlinedSAABSymbol();
			CreateShiftedArrowSymbol();
			CreateOutlineShiftedArrowSymbol();
			CreateDoubleTriangleSymbol();
			CreateOutlineDoubleTriangleSymbol();
            CreateBrakeActionFairSymbol();
            CreateBrakeActionGoodSymbol();
            CreateBrakeActionMediumSymbol();
            CreateBrakeActionPoorSymbol();
            CreateBrakeActionNILSymbol();
            CreateBrakeActionNASymbol();
		}

		public Symbol() 
		{
		}
		
		public Symbol(SymbolType type, Color color, uint size) : this(type,color,size,0,0,0)
		{
		}

        public Symbol(SymbolType type, Color color, uint size, bool outlined) : this(type, color, size, 0, 0, 0, outlined)
        {
        }

		public Symbol(SymbolType type, Color color, uint size, double latitude, double longitude) : this(type,color,size,latitude,longitude,0)
		{
		}

		public Symbol(SymbolType type, Color color, uint size, PointD point, double dir) : this(type,color,size,point.Latitude,point.Longitude,dir)
		{
		}

		public Symbol(SymbolType type, Color color, uint size, double latitude, double longitude, double dir)
		{
			this.color = color;
			this.size = size;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
            this.type = type;
            SetDisplayList();
			SetPosition(latitude, longitude, dir);
			this.xOffset = 0;
			this.yOffset = 0;
			this.rotate = true;
            this.outlineColor = Color.Black;
            this.outlined = false;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

        public Symbol(SymbolType type, Color color, uint size, double latitude, double longitude, double dir, bool outlined)
        {
            this.color = color;
            this.size = size;
            this.featureInfo = string.Empty;
            this.featureName = string.Empty;
            this.type = type;
            SetDisplayList();
            SetPosition(latitude, longitude, dir);
			this.xOffset = 0;
			this.yOffset = 0;
			this.rotate = true;
            this.outlineColor = Color.Black;
            this.outlined = outlined;
			this.mapProjection = MapProjections.CylindricalEquidistant;
        }

        public Symbol(SymbolType type, Color color, uint size, double latitude, double longitude, double dir, bool outlined, Color outlineColor)
        {
            this.color = color;
            this.size = size;
            this.featureInfo = string.Empty;
            this.featureName = string.Empty;
            this.type = type;
            SetDisplayList();
            SetPosition(latitude, longitude, dir);
			this.xOffset = 0;
			this.yOffset = 0;
			this.rotate = true;
            this.outlineColor = outlineColor;
            this.outlined = outlined;
			this.mapProjection = MapProjections.CylindricalEquidistant;
        }

		public Symbol(SymbolType type, Color color, uint size, double latitude, double longitude, double xOffset, double yOffset, double dir, bool outlined, Color outlineColor)
		{
			this.color = color;
			this.size = size;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.type = type;
			SetDisplayList();
			SetPosition(latitude, longitude, dir);
			this.xOffset = xOffset;
			this.yOffset = yOffset;
			this.rotate = true;
            this.outlineColor = outlineColor;
            this.outlined = outlined;
			this.mapProjection = MapProjections.CylindricalEquidistant;
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

		public double XOffset
		{
			get { return xOffset; }
			set { xOffset = value; }
		}

		public double YOffset
		{
			get { return yOffset; }
			set { yOffset = value; }
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

		public double Direction
		{
			get { return direction; }
			set { direction = value; }
		}

		public bool Rotate
		{
			get { return rotate; }
			set { rotate = value; }
		}

		public SymbolType Type
        {
            get { return type; }
            set
            {
                type = value;
                SetDisplayList();
            }
        }

        public bool Unfilled
        {
            get { return unfilled; }
            set { unfilled = value; }
        }

        public bool Outlined
        {
            get { return outlined; }
            set { outlined = value; }
        }

        public Color OutlineColor
        {
            get { return outlineColor; }
            set { outlineColor = value; }
        }

        public int Opacity
        {
            get { return opacity; }
            set { opacity = value; }
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

		public void SetPosition(double latitude, double longitude, double dir)
		{
			x = longitude;
			y = latitude;
			direction = dir;
		}

		public void SetPosition(PointD point, double dir)
		{
			x = point.X;
			y = point.Y;
			direction = dir;
		}

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

		protected void SetDisplayList()
        {
			switch (type)
			{
				case SymbolType.Circle:
					openglDisplayList = openglDisplayListCircle;
					openglOutlineDisplayList = openglDisplayListRing;
					break;
				case SymbolType.Triangle:
					openglDisplayList = openglDisplayListTriangle;
					openglOutlineDisplayList = openglOutlineDisplayListTriangle;
					break;
				case SymbolType.Square:
					openglDisplayList = openglDisplayListSquare;
					openglOutlineDisplayList = openglOutlineDisplayListSquare;
					break;
				case SymbolType.Arrow:
					openglDisplayList = openglDisplayListArrow;
					openglOutlineDisplayList = openglOutlineDisplayListArrow;
					break;
				case SymbolType.Ring:
					openglDisplayList = openglDisplayListRing;
					openglOutlineDisplayList = openglDisplayListRing;
					break;
				case SymbolType.Bolt:
					openglDisplayList = openglDisplayListBolt;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.Plus:
					openglDisplayList = openglDisplayListPlus;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.UnfilledPlane:
					openglDisplayList = openglDisplayListUnfilledPlane;
					openglOutlineDisplayList = openglDisplayListUnfilledPlane;
					break;
				case SymbolType.OutlinedPlane:
					openglDisplayList = openglDisplayListOutlinedPlane;
					openglOutlineDisplayList = openglDisplayListUnfilledPlane;
					break;
				case SymbolType.Volcano:
					openglDisplayList = openglDisplayListVolcano;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.Box:
					openglDisplayList = openglOutlineDisplayListSquare;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.EquilateralTriangle:
					openglDisplayList = openglDisplayListEquilateralTriangle;
					openglOutlineDisplayList = openglOutlineDisplayListEquilateralTriangle;
					break;
				case SymbolType.TPCircle:
					openglDisplayList = openglDisplayListTPCircle;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.High:
					openglDisplayList = openglDisplayListHigh;
					openglOutlineDisplayList = openglOutlineDisplayListHigh;
					break;
				case SymbolType.Low:
					openglDisplayList = openglDisplayListLow;
					openglOutlineDisplayList = openglOutlineDisplayListLow;
					break;
				case SymbolType.HurricaneN:
					openglDisplayList = openglDisplayListNorthHurricane;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.HurricaneS:
					openglDisplayList = openglDisplayListSouthHurricane;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.TropicalStormN:
					openglDisplayList = openglDisplayListNorthTropicalStorm;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.TropicalStormS:
					openglDisplayList = openglDisplayListSouthTropicalStorm;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.WSIHurricaneN:
					openglDisplayList = openglDisplayListWSINorthHurricane;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.WSIHurricaneS:
					openglDisplayList = openglDisplayListWSISouthHurricane;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.WSITropicalStormN:
					openglDisplayList = openglDisplayListWSINorthTropicalStorm;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.WSITropicalStormS:
					openglDisplayList = openglDisplayListWSISouthTropicalStorm;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.ParkedAircraft:
					openglDisplayList = openglDisplayListParkedAircraft;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.Pushpin:
					openglDisplayList = openglDisplayListPushpin;
					openglOutlineDisplayList = -1;
					break;
				case SymbolType.Bell206:
					openglDisplayList = openglDisplayListBell206;
					openglOutlineDisplayList = openglDisplayListOutlinedBell206;
					break;
				case SymbolType.UnfilledBell206:
					openglDisplayList = openglDisplayListOutlinedBell206;
					openglOutlineDisplayList = openglDisplayListOutlinedBell206;
					break;
				case SymbolType.Boeing737:
					openglDisplayList = openglDisplayList737;
					openglOutlineDisplayList = openglDisplayListOutlined737;
					break;
				case SymbolType.Boeing747:
					openglDisplayList = openglDisplayList747;
					openglOutlineDisplayList = openglDisplayListOutlined747;
					break;
				case SymbolType.C172:
					openglDisplayList = openglDisplayListC172;
					openglOutlineDisplayList = openglDisplayListOutlinedC172;
					break;
				case SymbolType.LearJet:
					openglDisplayList = openglDisplayListLearJet;
					openglOutlineDisplayList = openglDisplayListOutlinedLearJet;
					break;
				case SymbolType.SAAB:
					openglDisplayList = openglDisplayListSAAB;
					openglOutlineDisplayList = openglDisplayListOutlinedSAAB;
					break;
				case SymbolType.ShiftedArrow:
					openglDisplayList = openglDisplayListShiftedArrow;
					openglOutlineDisplayList = openglDisplayListOutlinedShiftedArrow;
					break;
				case SymbolType.DoubleTriangle:
					openglDisplayList = openglDisplayListDoubleTriangle;
					openglOutlineDisplayList = openglOutlineDisplayListDoubleTriangle;
					break;
                case SymbolType.BrakeActionFair:
                    openglDisplayList = openglDisplayListBrakeActionFair;
                    openglOutlineDisplayList = -1;
                    break;
                case SymbolType.BrakeActionGood:
                    openglDisplayList = openglDisplayListBrakeActionGood;
                    openglOutlineDisplayList = -1;
                    break;
                case SymbolType.BrakeActionMedium:
                    openglDisplayList = openglDisplayListBrakeActionMedium;
                    openglOutlineDisplayList = -1;
                    break;
                case SymbolType.BrakeActionPoor:
                    openglDisplayList = openglDisplayListBrakeActionPoor;
                    openglOutlineDisplayList = -1;
                    break;
                case SymbolType.BrakeActionNil:
                    openglDisplayList = openglDisplayListBrakeActionNIL;
                    openglOutlineDisplayList = -1;
                    break;
                case SymbolType.BrakeActionNA:
                    openglDisplayList = openglDisplayListBrakeActionNA;
                    openglOutlineDisplayList = -1;
                    break;
				default:
					openglDisplayList = openglDisplayListPlane;
					openglOutlineDisplayList = openglDisplayListUnfilledPlane;
					break;
			}
        }

        protected static void CreatePlaneSymbol()
		{
			openglDisplayListPlane = TessellatePlaneSymbol();
		}

		protected static void CreateOutlinedPlaneSymbol()
        {
            openglDisplayListOutlinedPlane = TessellateOutlinedPlaneSymbol();
        }

        protected static void CreateUnfilledPlaneSymbol()
        {
			// Create an OpenGL display list
			openglDisplayListUnfilledPlane = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListUnfilledPlane, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

            // Initialize the vertices
	        double[,] v = new double[20,3];
	        v[0,0] = 4; v[0,1] = 3; v[0,2] = 0;
	        v[1,0] = 28; v[1,1] = -7; v[1,2] = 0;
	        v[2,0] = 28; v[2,1] = -2; v[2,2] = 0;
	        v[3,0] = 4; v[3,1] = 14; v[3,2] = 0;
	        v[4,0] = 3; v[4,1] = 23; v[4,2] = 0;
	        v[5,0] = 2; v[5,1] = 26; v[5,2] = 0;
	        v[6,0] = 0; v[6,1] = 29; v[6,2] = 0;
	        v[7,0] = -2; v[7,1] = 26; v[7,2] = 0;
	        v[8,0] = -3; v[8,1] = 23; v[8,2] = 0;
	        v[9,0] = -4; v[9,1] = 14; v[9,2] = 0;
	        v[10,0] = -28; v[10,1] = -2; v[10,2] = 0;
	        v[11,0] = -28; v[11,1] = -7; v[11,2] = 0;
	        v[12,0] = -4; v[12,1] = 3; v[12,2] = 0;
	        v[13,0] = -2; v[13,1] = -20; v[13,2] = 0;
	        v[14,0] = -8; v[14,1] = -24; v[14,2] = 0;
	        v[15,0] = -8; v[15,1] = -27; v[15,2] = 0;
	        v[16,0] = 0; v[16,1] = -24; v[16,2] = 0;
	        v[17,0] = 8; v[17,1] = -27; v[17,2] = 0;
	        v[18,0] = 8; v[18,1] = -24; v[18,2] = 0;
	        v[19,0] = 2; v[19,1] = -20; v[19,2] = 0;

            // Draw the unfilled plane
            Gl.glLineWidth(0.5f);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 20; i++)
                Gl.glVertex2d(v[i,0], v[i,1]);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();
        }

		protected static void CreateCircleSymbol()
		{
			openglDisplayListCircle = TessellateCircleSymbol();
		}

        protected static void CreateEquilateralTriangleSymbol()
        {
            openglDisplayListEquilateralTriangle = TessellateTriangleSymbol();
        }

		protected static void CreateTriangleSymbol()
		{
            
            // Create an OpenGL display list
            openglDisplayListTriangle = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTriangle, Gl.GL_COMPILE);


            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            double[,] v = new double[3, 3];
            v[0, 0] = 0; v[0, 1] = 25; v[0, 2] = 0;
            v[1, 0] = -15; v[1, 1] = -15; v[1, 2] = 0;
            v[2, 0] = 15; v[2, 1] = -15; v[2, 2] = 0;

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_TRIANGLES);
            for (int i = 0; i < 3; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();
                      
            // End the GL list
            Gl.glEndList();
		}

		protected static void CreateSquareSymbol()
		{
			openglDisplayListSquare = TessellateSquareSymbol();
		}

		protected static void CreateArrowSymbol()
		{
			openglDisplayListArrow = TessellateArrowSymbol();
		}

		protected static void CreateShiftedArrowSymbol()
		{
			openglDisplayListShiftedArrow = TessellateShiftedArrowSymbol();
		}

        protected static void CreateRingSymbol()
        {
            // Create an OpenGL display list
            openglDisplayListRing = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListRing, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw the ring
            Gl.glLineWidth(1);

            // Initialize the vertices
            double[,] v = new double[24, 3];
            v[0, 0] = 15.000000; v[0, 1] = 0.000000; v[0, 2] = 0;
            v[1, 0] = 14.488887; v[1, 1] = 3.882286; v[1, 2] = 0;
            v[2, 0] = 12.990381; v[2, 1] = 7.500000; v[2, 2] = 0;
            v[3, 0] = 10.606602; v[3, 1] = 10.606602; v[3, 2] = 0;
            v[4, 0] = 7.500000; v[4, 1] = 12.990381; v[4, 2] = 0;
            v[5, 0] = 3.882285; v[5, 1] = 14.488887; v[5, 2] = 0;
            v[6, 0] = -0.000000; v[6, 1] = 15.000000; v[6, 2] = 0;
            v[7, 0] = -3.882286; v[7, 1] = 14.488887; v[7, 2] = 0;
            v[8, 0] = -7.500000; v[8, 1] = 12.990381; v[8, 2] = 0;
            v[9, 0] = -10.606602; v[9, 1] = 10.606601; v[9, 2] = 0;
            v[10, 0] = -12.990381; v[10, 1] = 7.499999; v[10, 2] = 0;
            v[11, 0] = -14.488888; v[11, 1] = 3.882285; v[11, 2] = 0;
            v[12, 0] = -15.000000; v[12, 1] = -0.000001; v[12, 2] = 0;
            v[13, 0] = -14.488887; v[13, 1] = -3.882286; v[13, 2] = 0;
            v[14, 0] = -12.990381; v[14, 1] = -7.500001; v[14, 2] = 0;
            v[15, 0] = -10.606601; v[15, 1] = -10.606602; v[15, 2] = 0;
            v[16, 0] = -7.499999; v[16, 1] = -12.990382; v[16, 2] = 0;
            v[17, 0] = -3.882285; v[17, 1] = -14.488888; v[17, 2] = 0;
            v[18, 0] = 0.000001; v[18, 1] = -15.000000; v[18, 2] = 0;
            v[19, 0] = 3.882287; v[19, 1] = -14.488887; v[19, 2] = 0;
            v[20, 0] = 7.500001; v[20, 1] = -12.990380; v[20, 2] = 0;
            v[21, 0] = 10.606603; v[21, 1] = -10.606601; v[21, 2] = 0;
            v[22, 0] = 12.990382; v[22, 1] = -7.499999; v[22, 2] = 0;
            v[23, 0] = 14.488888; v[23, 1] = -3.882284; v[23, 2] = 0;

            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 24; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();
        }

        protected static void CreateBoltSymbol()
        {
            // Create an OpenGL display list
            openglDisplayListBolt = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBolt, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw the bolt
            int size = 20;
            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINE_STRIP);
                Gl.glVertex2d(size, size);
                Gl.glVertex2d(0, 0);
                Gl.glVertex2d(size, 0);
                Gl.glVertex2d(0, -size);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();
        }

        protected static void CreatePlusSymbol()
        {
            // Create an OpenGL display list
            openglDisplayListPlus = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListPlus, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Draw the bolt
            int size = 20;
            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2d(-size, 0);
            Gl.glVertex2d(size, 0);
            Gl.glVertex2d(0, size);
            Gl.glVertex2d(0, -size);
            Gl.glEnd();

            // End the GL list
            Gl.glEndList();
        }

        protected static void CreateVolcanoSymbol()
        {
            // Create an OpenGL display list
            openglDisplayListVolcano = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListVolcano, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

			int size = 20;
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			Gl.glVertex2d(-10, size);
			Gl.glVertex2d(10, size);
			Gl.glVertex2d(30, -size);
			Gl.glVertex2d(-30, -size);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2d(-6, 0);
			Gl.glVertex2d(6, 0);
			Gl.glVertex2d(0, 6);
			Gl.glVertex2d(0, -6);
			Gl.glEnd();

			Gl.glColor3f(1.0f, 0.0f, 0.0f);
			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(-16, 40);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(0, 40);
			Gl.glVertex2d(0, size);
			Gl.glVertex2d(16, 40);
			Gl.glEnd();
			//End the GL list
			Gl.glEndList();

			//int size = 20;
			//Gl.glLineWidth(1);
			//Gl.glBegin(Gl.GL_POLYGON);
			//double height = 2 * size;
			//Gl.glVertex2d(-size, height);
			//Gl.glVertex2d(size, height);
			//Gl.glVertex2d(size, size);
			//double width = 2.732 * size;
			//Gl.glVertex2d(width, -height);
			//Gl.glVertex2d(-width, -height);
			//Gl.glVertex2d(-size, size);
			//Gl.glEnd();

			//Gl.glLineWidth(1);
			//Gl.glColor3f(0.0f, 0.0f, 0.0f);
			//Gl.glBegin(Gl.GL_LINE_STRIP);
			//height = 2 * size;
			//Gl.glVertex2d(-size, height);
			//Gl.glVertex2d(size, height);
			//Gl.glVertex2d(size, size);
			//width = 2.732 * size;
			//Gl.glVertex2d(width, -height);
			//Gl.glVertex2d(-width, -height);
			//Gl.glVertex2d(-size, size);
			//Gl.glVertex2d(-size, height);
			//Gl.glEnd();

			//Gl.glPointSize(2);
			//Gl.glColor3f(1.0f, 0.0f, 0.0f);
			//Gl.glBegin(Gl.GL_POINTS);
			//height = 2.5 * size;
			//Gl.glVertex2d(-size, height);
			//Gl.glVertex2d(0, height);
			//Gl.glVertex2d(size, height);

			//height = 3 * size;
			//width = 1.5 * size;
			//Gl.glVertex2d(-width, height);
			//Gl.glVertex2d(-size, height);
			//Gl.glVertex2d(0, height);
			//Gl.glVertex2d(width, height);
			//Gl.glEnd();
			////End the GL list
			//Gl.glEndList();
        }

        protected static void CreateOutlineSquareSymbol()
        {
            // Create an OpenGL display list
            openglOutlineDisplayListSquare = Gl.glGenLists(1);
            Gl.glNewList(openglOutlineDisplayListSquare, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            double[,] v = new double[4, 4];
            v[0, 0] = 15; v[0, 1] = -15; v[0, 2] = 0;
            v[1, 0] = 15; v[1, 1] = 15; v[1, 2] = 0;
            v[2, 0] = -15; v[2, 1] = 15; v[2, 2] = 0;
            v[3, 0] = -15; v[3, 1] = -15; v[3, 2] = 0;

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 4; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();
            //End the GL list
            Gl.glEndList();
        }

		protected static void CreateOutlineEquilateralTriangleSymbol()
		{
			// Create an OpenGL display list
			openglOutlineDisplayListEquilateralTriangle = Gl.glGenLists(1);
			Gl.glNewList(openglOutlineDisplayListEquilateralTriangle, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = new double[3, 3];
			v[0, 0] = 0; v[0, 1] = 15; v[0, 2] = 0;
			v[1, 0] = -15; v[1, 1] = -15; v[1, 2] = 0;
			v[2, 0] = 15; v[2, 1] = -15; v[2, 2] = 0;

			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			//End the GL list
			Gl.glEndList();
		}

        protected static void CreateOutlineTriangleSymbol()
        {
            // Create an OpenGL display list
            openglOutlineDisplayListTriangle = Gl.glGenLists(1);
            Gl.glNewList(openglOutlineDisplayListTriangle, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            double[,] v = new double[3, 3];
            v[0, 0] = 0; v[0, 1] = 25; v[0, 2] = 0;
            v[1, 0] = -15; v[1, 1] = -15; v[1, 2] = 0;
            v[2, 0] = 15; v[2, 1] = -15; v[2, 2] = 0;

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 3; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();

            //End the GL list
            Gl.glEndList();
        }

        protected static void CreateOutlineArrowSymbol()
        {
            // Create an OpenGL display list
            openglOutlineDisplayListArrow = Gl.glGenLists(1);
            Gl.glNewList(openglOutlineDisplayListArrow, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Initialize vertices
            double[,] v = new double[7, 3];
            v[0, 0] = 0; v[0, 1] = 30; v[0, 2] = 0;
            v[1, 0] = -12; v[1, 1] = 5; v[1, 2] = 0;
            v[2, 0] = -3; v[2, 1] = 5; v[2, 2] = 0;
            v[3, 0] = -3; v[3, 1] = -30; v[3, 2] = 0;
            v[4, 0] = 3; v[4, 1] = -30; v[4, 2] = 0;
            v[5, 0] = 3; v[5, 1] = 5; v[5, 2] = 0;
            v[6, 0] = 12; v[6, 1] = 5; v[6, 2] = 0;

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 7; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();

            //End the GL list
            Gl.glEndList();
        }

		protected static void CreateOutlineShiftedArrowSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlinedShiftedArrow = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlinedShiftedArrow, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize vertices
			double[,] v = new double[7, 3];
			v[0, 0] = 0; v[0, 1] = 60; v[0, 2] = 0;
			v[1, 0] = -12; v[1, 1] = 35; v[1, 2] = 0;
			v[2, 0] = -3; v[2, 1] = 35; v[2, 2] = 0;
			v[3, 0] = -3; v[3, 1] = 0; v[3, 2] = 0;
			v[4, 0] = 3; v[4, 1] = 0; v[4, 2] = 0;
			v[5, 0] = 3; v[5, 1] = 35; v[5, 2] = 0;
			v[6, 0] = 12; v[6, 1] = 35; v[6, 2] = 0;

			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 7; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			//End the GL list
			Gl.glEndList();
		}

        protected static void CreateTPCircleSymbol()
        {
            // Create an OpenGL display list
            openglDisplayListTPCircle = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListTPCircle, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            double[,] v = new double[4, 4];
            v[0, 0] = 25; v[0, 1] = -25; v[0, 2] = 0;
            v[1, 0] = 25; v[1, 1] = 25; v[1, 2] = 0;
            v[2, 0] = -25; v[2, 1] = 25; v[2, 2] = 0;
            v[3, 0] = -25; v[3, 1] = -25; v[3, 2] = 0;

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (int i = 0; i < 4; i++)
                Gl.glVertex2d(v[i, 0], v[i, 1]);
            Gl.glEnd();

            CreateCircle(15);

            //End the GL list
            Gl.glEndList();
        }

		protected static void CreateHighSymbol()
		{
            // Create an OpenGL display list
            openglDisplayListHigh = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListHigh, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Define coordinates
			double[,] v = new double[12, 3];
            v[0, 0] = -20; v[0, 1] = 30; v[0, 2] = 0;
            v[1, 0] = -10; v[1, 1] = 30; v[1, 2] = 0;
            v[2, 0] = -10; v[2, 1] = 5; v[2, 2] = 0;
            v[3, 0] = 10; v[3, 1] = 5; v[3, 2] = 0;
            v[4, 0] = 10; v[4, 1] = 30; v[4, 2] = 0;
            v[5, 0] = 20; v[5, 1] = 30; v[5, 2] = 0;
            v[6, 0] = 20; v[6, 1] = -30; v[6, 2] = 0;
            v[7, 0] = 10; v[7, 1] = -30; v[7, 2] = 0;
            v[8, 0] = 10; v[8, 1] = -5; v[8, 2] = 0;
            v[9, 0] = -10; v[9, 1] = -5; v[9, 2] = 0;
            v[10, 0] = -10; v[10, 1] = -30; v[10, 2] = 0;
            v[11, 0] = -20; v[11, 1] = -30; v[11, 2] = 0;

            // Draw into the OpenGL display list
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(v[0, 0], v[0, 1]);
			Gl.glVertex2d(v[1, 0], v[1, 1]);
			Gl.glVertex2d(v[10, 0], v[10, 1]);
			Gl.glVertex2d(v[11, 0], v[11, 1]);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(v[2, 0], v[2, 1]);
			Gl.glVertex2d(v[3, 0], v[3, 1]);
			Gl.glVertex2d(v[8, 0], v[8, 1]);
			Gl.glVertex2d(v[9, 0], v[9, 1]);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(v[4, 0], v[4, 1]);
			Gl.glVertex2d(v[5, 0], v[5, 1]);
			Gl.glVertex2d(v[6, 0], v[6, 1]);
			Gl.glVertex2d(v[7, 0], v[7, 1]);
			Gl.glEnd();

			// End the OpenGL display list
            Gl.glEndList();

			// Create an OpenGL display list for the outline
			openglOutlineDisplayListHigh = Gl.glGenLists(1);
			Gl.glNewList(openglOutlineDisplayListHigh, Gl.GL_COMPILE);

			// Draw into the OpenGL display list
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 12; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the OpenGL display list
            Gl.glEndList();
		}

		protected static void CreateLowSymbol()
		{
            // Create an OpenGL display list
            openglDisplayListLow = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListLow, Gl.GL_COMPILE);

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Define coordinates
			double[,] v = new double[7, 3];
            v[0, 0] = -20; v[0, 1] = 30; v[0, 2] = 0;
            v[1, 0] = -10; v[1, 1] = 30; v[1, 2] = 0;
            v[2, 0] = -10; v[2, 1] = -20; v[2, 2] = 0;
            v[3, 0] = 20; v[3, 1] = -20; v[3, 2] = 0;
            v[4, 0] = 20; v[4, 1] = -30; v[4, 2] = 0;
            v[5, 0] = -10; v[5, 1] = -30; v[5, 2] = 0;
            v[6, 0] = -20; v[6, 1] = -30; v[6, 2] = 0;

            // Draw into the OpenGL display list
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(v[0, 0], v[0, 1]);
			Gl.glVertex2d(v[1, 0], v[1, 1]);
			Gl.glVertex2d(v[5, 0], v[5, 1]);
			Gl.glVertex2d(v[6, 0], v[6, 1]);
			Gl.glEnd();
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(v[2, 0], v[2, 1]);
			Gl.glVertex2d(v[3, 0], v[3, 1]);
			Gl.glVertex2d(v[4, 0], v[4, 1]);
			Gl.glVertex2d(v[5, 0], v[5, 1]);
			Gl.glEnd();

            // End the OpenGL display list
            Gl.glEndList();

			// Create an OpenGL display list for the outline
			openglOutlineDisplayListLow = Gl.glGenLists(1);
			Gl.glNewList(openglOutlineDisplayListLow, Gl.GL_COMPILE);

			// Draw into the OpenGL display list
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 7; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the OpenGL display list
			Gl.glEndList();
		}

		protected static void CreateTropicalSymbol(SymbolType symbolType)
		{
			// Create an OpenGL display list based on the symbol type
			switch (symbolType)
			{
				case SymbolType.HurricaneN:
					openglDisplayListNorthHurricane = Gl.glGenLists(1);
					Gl.glNewList(openglDisplayListNorthHurricane, Gl.GL_COMPILE);
					break;
				case SymbolType.HurricaneS:
					openglDisplayListSouthHurricane = Gl.glGenLists(1);
					Gl.glNewList(openglDisplayListSouthHurricane, Gl.GL_COMPILE);
					break;
				case SymbolType.TropicalStormN:
					openglDisplayListNorthTropicalStorm = Gl.glGenLists(1);
					Gl.glNewList(openglDisplayListNorthTropicalStorm, Gl.GL_COMPILE);
					break;
				case SymbolType.TropicalStormS:
					openglDisplayListSouthTropicalStorm = Gl.glGenLists(1);
					Gl.glNewList(openglDisplayListSouthTropicalStorm, Gl.GL_COMPILE);
					break;
				default:
					break;
			}

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);
			Gl.glLineWidth(2);

            // Coordinates for the top arm
			double[,] v = new double[9, 9];
            v[0, 0] = -20; v[0, 1] = 0; v[0, 2] = 0;
            v[1, 0] = -19.6; v[1, 1] = 5; v[1, 2] = 0;
            v[2, 0] = -18.6; v[2, 1] = 10; v[2, 2] = 0;
            v[3, 0] = -17; v[3, 1] = 15; v[3, 2] = 0;
			v[4, 0] = -15; v[4, 1] = 20; v[4, 2] = 0;
            v[5, 0] = -12.4; v[5, 1] = 25; v[5, 2] = 0;
            v[6, 0] = -9; v[6, 1] = 30; v[6, 2] = 0;
			v[7, 0] = -5; v[7, 1] = 35; v[7, 2] = 0;
            v[8, 0] = 0; v[8, 1] = 40; v[8, 2] = 0;

			if (symbolType == SymbolType.TropicalStormS || symbolType == SymbolType.HurricaneS)
			{
				for (int i = 0; i < 9; i++)
					v[i, 0] *= -1;
			}

			// Draw the top arm
			Gl.glBegin(Gl.GL_LINE_STRIP);
			for (int i = 0; i < 9; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

            // Coordinates for the bottom arm
			v = new double[9, 9];
            v[0, 0] = 20; v[0, 1] = 0; v[0, 2] = 0;
            v[1, 0] = 19.6; v[1, 1] = -5; v[1, 2] = 0;
            v[2, 0] = 18.6; v[2, 1] = -10; v[2, 2] = 0;
            v[3, 0] = 17; v[3, 1] = -15; v[3, 2] = 0;
			v[4, 0] = 15; v[4, 1] = -20; v[4, 2] = 0;
            v[5, 0] = 12.4; v[5, 1] = -25; v[5, 2] = 0;
            v[6, 0] = 9; v[6, 1] = -30; v[6, 2] = 0;
			v[7, 0] = 5; v[7, 1] = -35; v[7, 2] = 0;
            v[8, 0] = 0; v[8, 1] = -40; v[8, 2] = 0;

			if (symbolType == SymbolType.TropicalStormS || symbolType == SymbolType.HurricaneS)
			{
				for (int i = 0; i < 9; i++)
					v[i, 0] *= -1;
			}

			// Draw the bottom arm
			Gl.glBegin(Gl.GL_LINE_STRIP);
			for (int i = 0; i < 9; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

            // Draw the body of the symbol
			switch (symbolType)
			{
				case SymbolType.HurricaneN:
					CreateCircle(20.1, true, true);
					break;
				case SymbolType.HurricaneS:
					CreateCircle(20.1, true, true);
					break;
				case SymbolType.TropicalStormN:
					CreateCircle(20.1, false, true);
					break;
				case SymbolType.TropicalStormS:
					CreateCircle(20.1, false, true);
					break;
				default:
					break;
			}

            //End the GL list
            Gl.glEndList();
		}

		protected static void CreateWSIHurricaneNSymbol()
		{
			if (!LoadTextures(SymbolType.WSIHurricaneN))
			{
				openglDisplayListWSINorthHurricane = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListWSINorthHurricane = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListWSINorthHurricane, Gl.GL_COMPILE);
			BindTexture(0);
			Gl.glEndList();
		}

		protected static void CreateWSIHurricaneSSymbol()
		{
			if (!LoadTextures(SymbolType.WSIHurricaneS))
			{
				openglDisplayListWSISouthHurricane = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListWSISouthHurricane = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListWSISouthHurricane, Gl.GL_COMPILE);
			BindTexture(1);
			Gl.glEndList();
		}

		protected static void CreateWSITropicalStormNSymbol()
		{
			if (!LoadTextures(SymbolType.WSITropicalStormN))
			{
				openglDisplayListWSINorthTropicalStorm = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListWSINorthTropicalStorm = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListWSINorthTropicalStorm, Gl.GL_COMPILE);
			BindTexture(2);
			Gl.glEndList();
		}

		protected static void CreateWSITropicalStormSSymbol()
		{
			if (!LoadTextures(SymbolType.WSITropicalStormS))
			{
				openglDisplayListWSISouthTropicalStorm = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListWSISouthTropicalStorm = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListWSISouthTropicalStorm, Gl.GL_COMPILE);
			BindTexture(3);
			Gl.glEndList();
		}

		protected static void CreateParkedAircraftSymbol()
		{
			if (!LoadTextures(SymbolType.ParkedAircraft))
			{
				openglDisplayListParkedAircraft = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListParkedAircraft = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListParkedAircraft, Gl.GL_COMPILE);
			BindTexture(4);
			Gl.glEndList();
		}

		protected static void CreatePushpinSymbol()
		{
			if (!LoadTextures(SymbolType.Pushpin))
			{
				openglDisplayListPushpin = -1;
				return;
			}

			// Create an OpenGL display list
			openglDisplayListPushpin = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListPushpin, Gl.GL_COMPILE);
			BindTexture(5);
			Gl.glEndList();
		}

        protected static void CreateBrakeActionFairSymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionFair))
            {
                openglDisplayListBrakeActionFair = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionFair = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionFair, Gl.GL_COMPILE);
            BindTexture(7);
            Gl.glEndList();
        }

        protected static void CreateBrakeActionGoodSymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionGood))
            {
                openglDisplayListBrakeActionGood = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionGood = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionGood, Gl.GL_COMPILE);
            BindTexture(6);
            Gl.glEndList();
        }

        protected static void CreateBrakeActionMediumSymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionMedium))
            {
                openglDisplayListBrakeActionMedium = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionMedium = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionMedium, Gl.GL_COMPILE);
            BindTexture(8);
            Gl.glEndList();
        }

        protected static void CreateBrakeActionNILSymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionNil))
            {
                openglDisplayListBrakeActionNIL = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionNIL = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionNIL, Gl.GL_COMPILE);
            BindTexture(11);
            Gl.glEndList();
        }

        protected static void CreateBrakeActionNASymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionNA))
            {
                openglDisplayListBrakeActionNA = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionNA = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionNA, Gl.GL_COMPILE);
            BindTexture(10);
            Gl.glEndList();
        }

        protected static void CreateBrakeActionPoorSymbol()
        {
            if (!LoadTextures(SymbolType.BrakeActionPoor))
            {
                openglDisplayListBrakeActionPoor = -1;
                return;
            }

            // Create an OpenGL display list
            openglDisplayListBrakeActionPoor = Gl.glGenLists(1);
            Gl.glNewList(openglDisplayListBrakeActionPoor, Gl.GL_COMPILE);
            BindTexture(9);
            Gl.glEndList();
        }

		protected static void CreateBell206Symbol()
		{
			openglDisplayListBell206 = TessellateBell206Symbol();
		}

		protected static void CreateOutlinedBell206Symbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlinedBell206 = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlinedBell206, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0,     28.5, 0},
				{1,     28,   0},
				{2,     27.5, 0},
				{3,     26.5, 0},
				{4,     25,   0},
				{4.5,   23,   0},
				{4.5,   15,   0},
				{21.5,  31,   0},
				{24,    29,   0},
				{4.5,   10,   0},
				{4.5,   6,    0},
				{24,    -12,  0},
				{21.5,  -15,  0},
				{3,     2,    0},
				{2.75,  1,    0},
				{2.5,   0,    0},
				{2.25,  -1,   0},
				{2,     -2,   0},
				{1.75,  -3,   0},
				{1.5,   -4,   0},
				{1.25,  -5,   0},
				{1,     -6,   0},
				{1,     -12,  0},
				{6,     -12,  0},
				{6,     -15,  0},
				{1,     -15,  0},
				{1,     -29,  0},
				{-1,    -29,  0},
				{-1,    -15,  0},
				{-6,    -15,  0},
				{-6,    -12,  0},
				{-1,    -12,  0},
				{-1,    -6,   0},
				{-1.25, -5,   0},
				{-1.5,  -4,   0},
				{-1.75, -3,   0},
				{-2,    -2,   0},
				{-2.25, -1,   0},
				{-2.5,  0,    0},
				{-2.75, 1,    0},
				{-21.5, -15,  0},
				{-24,   -12,  0},
				{-4.5,  6,    0},
				{-4.5,  10,   0},
				{-24,   29,   0},
				{-21.5, 31,   0},
				{-4.5,  15,   0},
				{-4.5,  23,   0},
				{-4,    25,   0},
				{-3,    26.5, 0},
				{-2,    27.5, 0},
				{-1,    28,   0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void Create737Symbol()
		{
			openglDisplayList737 = Tessellate737Symbol();
		}

		protected static void CreateOutlined737Symbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlined737 = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlined737, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0,      30.5,  0},
				{0.5,    30,    0},
				{1,      28.5,  0},
				{1.5,    27.5,  0},
				{2,      26,    0},
				{2.5,    23.5,  0},
				{3,      21,    0},
				{3.5,    18,    0},
				{4,      15,    0},
				{3.75,   9.5,   0},
				{8.75,   6,     0},
				{8.75,   8,     0},
				{11,     8,     0},
				{11.75,  4.25,  0},
				{28,     -4,    0},
				{28.75,  -7,    0},
				{28,     -7.75, 0},
				{11,     -3,    0},
				{4,      -3,    0},
				{4,      -6,    0},
				{3.75,   -9,    0},
				{3.5,    -11,   0},
				{3.25,   -13,   0},
				{3,      -15,   0},
				{2.75,   -17,   0},
				{2.5,    -18,   0},
				{2.25,   -19,   0},
				{2,      -20,   0},
				{2,      -21,   0},
				{10.75,  -27,   0},
				{11.5,   -28,   0},
				{11,     -30,   0},
				{1,      -28,   0},
				{0,      -28.5, 0},
				{-0,     -28.5, 0},
				{-1,     -28,   0},
				{-11,    -30,   0},
				{-11.5,  -28,   0},
				{-10.75, -27,   0},
				{-2,     -21,   0},
				{-2,     -20,   0},
				{-2.25,  -19,   0},
				{-2.5,   -18,   0},
				{-2.75,  -17,   0},
				{-3,     -15,   0},
				{-3.25,  -13,   0},
				{-3.5,   -11,   0},
				{-3.75,  -9,    0},
				{-4,     -6,    0},
				{-4,     -3,    0},
				{-11,    -3,    0},
				{-28,    -7.75, 0},
				{-28.75, -7,    0},
				{-28,    -4,    0},
				{-11.75, 4.25,  0},
				{-11,    8,     0},
				{-8.75,  8,     0},
				{-8.75,  6,     0},
				{-3.75,  9.5,   0},
				{-4,     15,    0},
				{-3.5,   18,    0},
				{-3,     21,    0},
				{-2.5,   23.5,  0},
				{-2,     26,    0},
				{-1.5,   27.5,  0},
				{-1,     28.5,  0},
				{-0.5,   30,    0},
				{0,      30.5,  0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void Create747Symbol()
		{
			openglDisplayList747 = Tessellate747Symbol();
		}

		protected static void CreateOutlined747Symbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlined747 = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlined747, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0,     31.5,  0},
				{0.5,   31,    0},
				{1,     30,    0},
				{1.5,   28.5,  0},
				{2,     27,    0},
				{2.5,   24.5,  0},
				{3,     22,    0},
				{3,     13,    0},
				{9,     7,     0},
				{9,     8,     0},
				{8.5,   8,     0},
				{8.5,   10.5,  0},
				{11,    10.5,  0},
				{11,    8,     0},
				{10.5,  8,     0},
				{10.5,  6,     0},
				{19,    -0.5,  0},
				{19,    0.5,   0},
				{18.5,  0.5,   0},
				{18.5,  3,     0},
				{21,    3,     0},
				{21,    0.5,   0},
				{20.5,  0.5,   0},
				{20.5,  -1.5,  0},
				{28,    -8,    0},
				{28,    -13,   0},
				{12,    -2,    0},
				{3,     -0.25, 0},
				{3,     -14,   0},
				{2.5,   -16,   0},
				{2,     -19,   0},
				{1.75,  -22,   0},
				{10,    -29,   0},
				{10,    -31.5, 0},
				{1,     -28.5, 0},
				{0,     -32,   0},
				{-0,    -32,   0},
				{-1,    -28.5, 0},
				{-10,   -31.5, 0},
				{-10,   -29,   0},
				{-1.75, -22,   0},
				{-2,    -19,   0},
				{-2.5,  -16,   0},
				{-3,    -14,   0},
				{-3,    -0.25, 0},
				{-12,   -2,    0},
				{-28,   -13,   0},
				{-28,   -8,    0},
				{-20.5, -1.5,  0},
				{-20.5, 0.5,   0},
				{-21,   0.5,   0},
				{-21,   3,     0},
				{-18.5, 3,     0},
				{-18.5, 0.5,   0},
				{-19,   0.5,   0},
				{-19,   -0.5,  0},
				{-10.5, 6,     0},
				{-10.5, 8,     0},
				{-11,   8,     0},
				{-11,   10.5,  0},
				{-8.5,  10.5,  0},
				{-8.5,  8,     0},
				{-9,    8,     0},
				{-9,    7,     0},
				{-3,    13,    0},
				{-3,    22,    0},
				{-2.5,  24.5,  0},
				{-2,    27,    0},
				{-1.5,  28.5,  0},
				{-1,    30,    0},
				{-0.5,  31,    0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateC172Symbol()
		{
			openglDisplayListC172 = TessellateC172Symbol();
		}

		protected static void CreateOutlinedC172Symbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlinedC172 = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlinedC172, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0,     21,    0},
				{0.5,   19.5,  0},
				{2,     19,    0},
				{2.25,  18,    0},
				{2.5,   16,    0},
				{2.75,  15,    0},
				{3,     13,    0},
				{3,     11,    0},
				{14,    11,    0},
				{29,    9.5,   0},
				{29,    4,     0},
				{15,    2.25,  0},
				{3,     2.25,  0},
				{1,     -13.5, 0},
				{9,     -15,   0},
				{9,     -18.5, 0},
				{1,     -20,   0},
				{0.5,   -18.5, 0},
				{0,     -19.5, 0},
				{-0.5,  -18.5, 0},
				{-1,    -20,   0},
				{-9,    -18.5, 0},
				{-9,    -15,   0},
				{-1,    -13.5, 0},
				{-3,    2.25,  0},
				{-15,   2.25,  0},
				{-29,   4,     0},
				{-29,   9.5,   0},
				{-14,   11,    0},
				{-3,    11,    0},
				{-3,    13,    0},
				{-2.75, 15,    0},
				{-2.5,  16,    0},
				{-2.25, 18,    0},
				{-2,    19,    0},
				{-0.5,  19.5,  0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateLearJetSymbol()
		{
			openglDisplayListLearJet = TessellateLearJetSymbol();
		}

		protected static void CreateOutlinedLearJetSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlinedLearJet = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlinedLearJet, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0,      25.25, 0},
				{0.5,    25,    0},
				{1,      24.75, 0},
				{1.25,   24,    0},
				{1.5,    23.5,  0},
				{1.75,   23,    0},
				{2,      22,    0},
				{2,      9.25,  0},
				{24,     -8,    0},
				{25.75,  -11,   0},
				{23,     -9,    0},
				{8,      -2.5,  0},
				{2,      -2.5,  0},
				{2,      -5.5,  0},
				{5,      -5.5,  0},
				{5.25,   -8,    0},
				{5,      -10,   0},
				{4.75,   -14,   0},
				{4.5,    -14.5, 0},
				{1,      -15.5, 0},
				{0.75,   -17,   0},
				{8.5,    -24,   0},
				{8.5,    -26,   0},
				{0,      -22.5, 0},
				{-8.5,   -26,   0},
				{-8.5,   -24,   0},
				{-0.75,  -17,   0},
				{-1,     -15.5, 0},
				{-4.5,   -14.5, 0},
				{-4.75,  -14,   0},
				{-5,     -10,   0},
				{-5.25,  -8,    0},
				{-5,     -5.5,  0},
				{-2,     -5.5,  0},
				{-2,     -2.5,  0},
				{-8,     -2.5,  0},
				{-23,    -9,    0},
				{-25.75, -11,   0},
				{-24,    -8,    0},
				{-2,     9.25,  0},
				{-2,     22,    0},
				{-1.75,  23,    0},
				{-1.5,   23.5,  0},
				{-1.25,  24,    0},
				{-1,     24.75, 0},
				{-0.5,   25,    0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateSAABSymbol()
		{
			openglDisplayListSAAB = TessellateSAABSymbol();
		}

		protected static void CreateOutlinedSAABSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListOutlinedSAAB = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListOutlinedSAAB, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Initialize the vertices
			double[,] v = 
			{
				{0, 26.5,       0},
				{0.5, 26,       0},
				{1, 25.5,       0},
				{1.5,   25,     0},
				{2,     24,     0},
				{2.5,   23,     0},
				{2.75,  21,     0},
				{3,     20,     0},
				{3,     6,      0},
				{7.5,   5.5,    0},
				{8,     11,     0},
				{9,     12,     0},
				{10,    11,     0},
				{10.5,  5,      0},
				{28,    3,      0},
				{28.5,  2.5,    0},
				{29,    2,      0},
				{29,    0,      0},
				{3,     -1.5,   0},
				{3,     -16,    0},
				{2.25,  -17,    0},
				{11,    -18.75, 0},
				{12,    -19.25, 0},
				{12.5,  -20,    0},
				{12.5,  -22,    0},
				{1.5,   -23,    0},
				{1.25,  -24,    0},
				{1,     -25,    0},
				{0.5,   -26,    0},
				{0,     -26.5,  0},
				{-0.5,  -26,    0},
				{-1,    -25,    0},
				{-1.25, -24,    0},
				{-1.5,  -23,    0},
				{-12.5, -22,    0},
				{-12.5, -20,    0},
				{-12,   -19.25, 0},
				{-11,   -18.75, 0},
				{-2.25, -17,    0},
				{-3,    -16,    0},
				{-3,    -1.5,   0},
				{-29,   0,      0},
				{-29,   2,      0},
				{-28.5, 2.5,    0},
				{-28,   3,      0},
				{-11.5, 5,      0},
				{-11,   11,     0},
				{-10,   12,     0},
				{-9,    11,     0},
				{-8.5,  5.5,    0},
				{-3,    6,      0},
				{-3,    20,     0},
				{-2.75, 21,     0},
				{-2.5,  23,     0},
				{-2,    24,     0},
				{-1.5,  25,     0},
				{-1,    25.5,   0},
				{-0.5,  26,     0}
			};

			// Draw the unfilled symbol
			Gl.glLineWidth(0.5f);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < v.GetLength(0); i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		private static void CreateCircle(int radius)
        {
            int nPoints = 360;
            uint centerY = 0;
            uint centerX = 0;

			Gl.glBegin(Gl.GL_POLYGON);
            double angleUnit = 360 / nPoints * Math.PI / 180;
            for (int i = 0; i < nPoints; i++)
            {
                double angle = angleUnit * i;
                double x = (Math.Cos(angle) * radius) + centerX;
                double y = (Math.Sin(angle) * radius) + centerY;

                Gl.glVertex2d(x, y);
            }
            Gl.glEnd();
        }
		
		private static void CreateCircle(double radius, bool fill, bool border)
		{
			int nPoints = 360;
			uint centerY = 0;
			uint centerX = 0;

			if (fill)
			{
				Gl.glBegin(Gl.GL_POLYGON);
				double angleUnit = 360 / nPoints * Math.PI / 180;
				for (int i = 0; i < nPoints; i++)
				{
					double angle = angleUnit * i;
					double x = (Math.Cos(angle) * radius) + centerX;
					double y = (Math.Sin(angle) * radius) + centerY;

					Gl.glVertex2d(x, y);
				}
				Gl.glEnd();
			}

			if (border)
			{
				Gl.glBegin(Gl.GL_LINE_LOOP);
				double angleUnit = 360 / nPoints * Math.PI / 180;
				for (int i = 0; i < nPoints; i++)
				{
					double angle = angleUnit * i;
					double x = (Math.Cos(angle) * radius) + centerX;
					double y = (Math.Sin(angle) * radius) + centerY;

					Gl.glVertex2d(x, y);
				}
				Gl.glEnd();
			}
		}

		private static void BindTexture(int index)
		{
			Gl.glEnable(Gl.GL_TEXTURE_2D); // Enable Texture Mapping
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[index]);

			Gl.glBegin(Gl.GL_QUADS);
			Gl.glTexCoord2f(0.0f, 0.0f);
			Gl.glVertex2f(-1.0f, -1.0f);
			Gl.glTexCoord2f(1.0f, 0.0f);
			Gl.glVertex2f(1.0f, -1.0f);
			Gl.glTexCoord2f(1.0f, 1.0f);
			Gl.glVertex2f(1.0f, 1.0f);
			Gl.glTexCoord2f(0.0f, 1.0f);
			Gl.glVertex2f(-1.0f, 1.0f);
			Gl.glEnd();

			Gl.glDisable(Gl.GL_TEXTURE_2D);
		}

		private static bool LoadTextures(SymbolType type)
		{
			Bitmap image = null;
			string fileName = string.Empty;
			int index;

			switch (type)
			{
				case SymbolType.WSIHurricaneN:
					fileName = "hurricaneN.png";
					index = 0;
					break;
				case SymbolType.WSIHurricaneS:
					fileName = "hurricaneS.png";
					index = 1;
					break;
				case SymbolType.WSITropicalStormN:
					fileName = "tropicalN.png";
					index = 2;
					break;
				case SymbolType.WSITropicalStormS:
					fileName = "tropicalS.png";
					index = 3;
					break;
				case SymbolType.ParkedAircraft:
					fileName = "ParkedAircraft.png";
					index = 4;
					break;
				case SymbolType.Pushpin:
					fileName = "Pushpin.png";
					index = 5;
					break;
                case SymbolType.BrakeActionGood:
                    fileName = "ABF_Blue.png";
                    index = 6;
                    break;
                case SymbolType.BrakeActionFair:
                    fileName = "ABF_Green.png";
                    index = 7;
                    break;
                case SymbolType.BrakeActionMedium:
                    fileName = "ABF_Yellow.png";
                    index = 8;
                    break;
                case SymbolType.BrakeActionPoor:
                    fileName = "ABF_Orange.png";
                    index = 9;
                    break;
                case SymbolType.BrakeActionNA:
                    fileName = "ABF_Gray.png";
                    index = 10;
                    break;
                case SymbolType.BrakeActionNil:
                    fileName = "ABF_Red.png";
                    index = 11;
                    break;
				default:
					fileName = "hurricaneN.png";
					index = 0;
					break;
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

					Gl.glGenTextures(1, out texture[index]);

					// Create Linear Filtered Texture
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[index]);
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
			catch {	}

			return false;
		}

		protected static void CreateDoubleTriangleSymbol()
		{
			// Create an OpenGL display list
			openglDisplayListDoubleTriangle = Gl.glGenLists(1);
			Gl.glNewList(openglDisplayListDoubleTriangle, Gl.GL_COMPILE);


			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = new double[3, 3];
			v[0, 0] = 0; v[0, 1] = 15; v[0, 2] = 0;
			v[1, 0] = -12; v[1, 1] = -15; v[1, 2] = 0;
			v[2, 0] = 12; v[2, 1] = -15; v[2, 2] = 0;
						
			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_TRIANGLES);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			//v[0, 0] = 0; v[0, 1] = 1; v[0, 2] = 0;
			//Gl.glBegin(Gl.GL_TRIANGLES);
			//for (int i = 0; i < 3; i++)
			//    Gl.glVertex2d(v[i, 0], v[i, 1]);
			//Gl.glEnd();

			// End the GL list
			Gl.glEndList();
		}

		protected static void CreateOutlineDoubleTriangleSymbol()
		{
			// Create an OpenGL display list
			openglOutlineDisplayListDoubleTriangle = Gl.glGenLists(1);
			Gl.glNewList(openglOutlineDisplayListDoubleTriangle, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			double[,] v = new double[3, 3];
			v[0, 0] = 0; v[0, 1] = 15; v[0, 2] = 0;
			v[1, 0] = -12; v[1, 1] = -15; v[1, 2] = 0;
			v[2, 0] = 12; v[2, 1] = -15; v[2, 2] = 0;

			Gl.glLineWidth(1);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();

			v[0, 0] = 0; v[0, 1] = 2; v[0, 2] = 0;
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();
			
			v[0, 0] = 0; v[0, 1] = -8; v[0, 2] = 0;
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (int i = 0; i < 3; i++)
				Gl.glVertex2d(v[i, 0], v[i, 1]);
			Gl.glEnd();
			//End the GL list
			Gl.glEndList();
		}
	}
}

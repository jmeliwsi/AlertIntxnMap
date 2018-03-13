using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Windows.Forms;
using Tao.OpenGl;
using FUL;
using Microsoft.Win32;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.IO;

namespace WSIMap
{
	/**
	 * \class MapGL
	 * \brief The WSI MapGL user control
	 */
	public sealed class MapGL : OpenGlControl
	{
		#region Data Members
		public event MapRectangleChangedEventHandler MapRectangleChanged;
        public bool panToFoundObject = false;
        public bool enablePanToFoundObject = true;
		private LayerCollection layers = null;
		private float scaleFactor = 1.0f;
		private const double orthoLeft = -180, orthoRight = 180, orthoBottom = -90, orthoTop = 90;
		private double westExtent = -180, eastExtent = 180, southExtent = -90, northExtent = 90;
		const int WM_KEYDOWN = 0x100;
		const int WM_SYSKEYDOWN = 0x104;
		internal bool trackingRectangle = false;
		internal bool panning = false;
		internal bool rasterFix = false;
		internal bool scrollOptimization = false;
		internal bool pooled = false;
		private int mouseDownX, mouseDownY;
		private int mouseUpX, mouseUpY;
		private int mouseMoveX, mouseMoveY;
		private int scrollWinX = int.MinValue;
		private int scrollWinY = int.MinValue;
		private BoundingBox._RectType3 rectToDraw = new BoundingBox._RectType3();
		private PointD trackingRectP1, trackingRectP2;
		private int viewportX, viewportY, viewportWidth, viewportHeight;
		private WSIFusionToolTip toolTip;
		private bool useToolTips = true;
		private bool latLonGrid = false;
		private static float latLonGridLineWidth = 1.0f;
		private static Color latLonGridColor = Color.Gray;
		private Font font = null;
		private float digitCharWidth;
		public BoundingBox boundingBox;
		private LinkedList<RectangleD> mapViews = null;
		private byte maxMapViews = 5;
		private Feature highlightFeature = null;
		private FeatureCollection highlightFeatures = null;
		private bool ctrlPressed;
		private bool shiftPressed;
		private bool statusBarMargin = false;
		private DateTime mouseTimeStamp = DateTime.UtcNow;
		private MapProjections mapProjection = MapProjections.CylindricalEquidistant;
		private short centralLongitude = Projection.DefaultCentralLongitude;
		private bool disableDateLinePanning = false;
        //public Dictionary<string, int> TextureCache = new Dictionary<string, int>();
		#endregion

        // Use this to store map panning offset. Used to keep Smart Labels in place during panning;
        public int? WinXMove { get; set; }
        public int? WinYMove { get; set; }
            
		#region Initialization
		public enum Direction { N, NE, E, SE, S, SW, W, NW };

		public MapGL()
		{
			InitializeComponent();
			layers = new LayerCollection();
			mapViews = new LinkedList<RectangleD>();
			//try
			//{
			//    RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\WSI\\Fusion");
			//    if (key != null)
			//    {
			//        object value = key.GetValue("RasterFix");
			//        if (value != null)
			//            rasterFix = Convert.ToBoolean(value);
			//    }
			//}
			//catch { }
			rasterFix = FusionSettings.Map.RasterFix;
			scrollOptimization = FusionSettings.Client.ScrollOptimization;
		}

		protected override void Dispose(bool disposing)
		{
			// Clean up all OpenGL display lists
			for (int i = 0; i < layers.Count; i++)
			{
				if (layers[i] != null && layers[i].Features != null)
					layers[i].Features.Clear(false);
			}

			// Don't complete the cleanup if this instance is currently pooled
			if (pooled)
				return;

			// Clean up the font
			//if (font != null)
			//    font.Dispose();

			base.Dispose(disposing);
		}

		public new void InitializeContexts()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap InitializeContexts()");
#endif

			base.InitializeContexts();
			if (ContextState.RenderingContext != IntPtr.Zero)
				Tao.Platform.Windows.Wgl.wglShareLists(ContextState.RenderingContext, Tao.Platform.Windows.Wgl.wglGetCurrentContext());
			SetViewport();
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glOrtho(orthoLeft, orthoRight, orthoBottom, orthoTop, -1, 1);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glEnable(Gl.GL_STENCIL_TEST);
			try
			{
				font = new Font("Arial", 10, true);
				digitCharWidth = font.abcf['0'].abcfA + font.abcf['0'].abcfB + font.abcf['0'].abcfC;
			}
			catch { }
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.toolTip = new WSIFusionToolTip();
			this.toolTip.AutoPopDelay = 32747;
			//this.toolTip.InitialDelay = 1000;
			//this.toolTip.ReshowDelay = 500;
			this.toolTip.OwnerDraw = true;
			this.toolTip.ShowAlways = true;

			this.SuspendLayout();
			// 
			// MapGL
			// 
			this.Name = "MapGL";
			this.StencilBits = ((byte)(1));
			//this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.MapGLMouseWheel);
			this.MouseLeave += new System.EventHandler(this.MapGL_MouseLeave);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.MapGLPaint);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MapGLMouseMove);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MapGL_KeyUp);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MapGLMouseDown);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MapGLMouseUp);
			this.MouseEnter += new System.EventHandler(this.MapGL_MouseEnter);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MapGL_KeyDown);
			this.ResumeLayout(false);

		}
		#endregion
		#endregion

		#region Painting
		private void MapGLPaint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			// Do nothing if the map has width or height of 0 or scaleFactor is 0
			if (Width == 0 || Height == 0 || scaleFactor == 0)
				return;

			//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			//sw.Start();

			// Set the current rendering context
			MakeCurrent();

			// Set the viewport
			SetViewport();
			TranslateTest();

			// Set the bounding box
			boundingBox.Ortho.left = orthoLeft;
			boundingBox.Ortho.right = orthoRight;
			boundingBox.Ortho.bottom = orthoBottom;
			boundingBox.Ortho.top = orthoTop;
			boundingBox.Viewport.x = viewportX;
			boundingBox.Viewport.y = viewportY;
			boundingBox.Viewport.width = viewportWidth;
			boundingBox.Viewport.height = viewportHeight;
			boundingBox.Map.left = GetMapLeftDeg();
			boundingBox.Map.normLeft = NormalizeLongitude(boundingBox.Map.left);
			boundingBox.Map.right = GetMapRightDeg();
			boundingBox.Map.normRight = NormalizeLongitude(boundingBox.Map.right);
			boundingBox.Map.bottom = GetMapBottomDeg();
			boundingBox.Map.top = GetMapTopDeg();
			boundingBox.Window.x = this.Left;
			boundingBox.Window.y = this.Top;
			boundingBox.Window.width = this.Width;
			boundingBox.Window.height = this.Height;

			if (scrollOptimization && scrollWinX != int.MinValue && scrollWinY != int.MinValue)
			{
				// optimized painting for scrolling
				drawLayersForExposedRegion();
			}
			else
			{
				// normal painting
				rectToDraw.left = rectToDraw.right = rectToDraw.top = rectToDraw.bottom = rectToDraw.normLeft = rectToDraw.normRight = double.MinValue;
				drawLayers();
			}

			//sw.Stop();
			//Console.WriteLine("Paint: " + sw.ElapsedMilliseconds);
		}

		private void drawLayersForExposedRegion()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap drawLayersForExposedRegion()");
#endif

			// copy image from front buffer to back buffer while translating according to scrolling
			int toX = scrollWinX > 0 ? scrollWinX : 0;
			int toY = scrollWinY < 0 ? this.Height + scrollWinY : this.Height; // for to, Y in Windows coordinates (top = 0)
			int fromX = scrollWinX < 0 ? -scrollWinX : 0;
			int fromY = scrollWinY > 0 ? scrollWinY : 0; // for from, Y in opengl coordinates (bottom = 0)
			int copyWidth = this.Width - Math.Abs(scrollWinX);
			int copyHeight = this.Height - Math.Abs(scrollWinY);
			SetRasterPosWin(toX, toY);
			Gl.glPixelZoom((float)1.0, (float)1.0);
			Gl.glDisable(Gl.GL_STENCIL_TEST);
			Gl.glDisable(Gl.GL_BLEND);
			Gl.glReadBuffer(Gl.GL_FRONT);
			Gl.glCopyPixels(fromX, fromY, copyWidth, copyHeight, Gl.GL_COLOR);
			Gl.glEnable(Gl.GL_STENCIL_TEST);
			Gl.glEnable(Gl.GL_BLEND);

			if (scrollWinX != 0)
			{
				// render exposed columns
				double scrollX = scrollWinX * GetMapWidthDeg() / Width;
				rectToDraw.top = BoundingBox.Map.top + 0.4;
				rectToDraw.bottom = BoundingBox.Map.bottom - 0.4;
				if (scrollWinX > 0)
				{
					// scroll left, expose region on left
					rectToDraw.left = BoundingBox.Map.left - 0.4;
					rectToDraw.right = rectToDraw.left + scrollX + 0.8;
				}
				else
				{
					// scroll right, expose region on right
					rectToDraw.right = BoundingBox.Map.right + 0.4;
					rectToDraw.left = rectToDraw.right + scrollX - 0.8;
				}
				rectToDraw.normLeft = NormalizeLongitude(rectToDraw.left);
				rectToDraw.normRight = NormalizeLongitude(rectToDraw.right);
				Gl.glEnable(Gl.GL_SCISSOR_TEST);
				Gl.glScissor(scrollWinX > 0 ? 0 : this.Width + scrollWinX, 0, Math.Abs(scrollWinX), this.Height);
				drawLayers();
				Gl.glDisable(Gl.GL_SCISSOR_TEST);
				rectToDraw.left = rectToDraw.right = rectToDraw.top = rectToDraw.bottom = rectToDraw.normLeft = rectToDraw.normRight = double.MinValue;
			}

			if (scrollWinY != 0)
			{
				// render exposed rows
				double scrollY = scrollWinY * GetMapHeightDeg() / Height;
				rectToDraw.left = BoundingBox.Map.left - 0.4;
				rectToDraw.right = BoundingBox.Map.right + 0.4;
				if (scrollWinY > 0)
				{
					// scroll up, expose region at top
					rectToDraw.top = BoundingBox.Map.top + 0.4;
					rectToDraw.bottom = rectToDraw.top - scrollY - 0.8;
				}
				else
				{
					// scroll down, expose region at bottom
					rectToDraw.bottom = BoundingBox.Map.bottom - 0.4;
					rectToDraw.top = rectToDraw.bottom - scrollY + 0.8;
				}
				rectToDraw.normLeft = NormalizeLongitude(rectToDraw.left);
				rectToDraw.normRight = NormalizeLongitude(rectToDraw.right);
				Gl.glEnable(Gl.GL_SCISSOR_TEST);
				Gl.glScissor(0, scrollWinY > 0 ? this.Height - scrollWinY : 0, this.Width, Math.Abs(scrollWinY));
				drawLayers();
				Gl.glDisable(Gl.GL_SCISSOR_TEST);
				rectToDraw.left = rectToDraw.right = rectToDraw.top = rectToDraw.bottom = rectToDraw.normLeft = rectToDraw.normRight = double.MinValue;
			}
		}

		private void drawLayers()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap drawLayers()");
#endif

			// Set matrix mode and background color
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glClearColor(((float)BackColor.R) / 255f, ((float)BackColor.G) / 255f, ((float)BackColor.B) / 255f, 0f);
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

			// Draw all the layers
			lock (layers)
			{
				for (int i = layers.Count - 1; i >= 0; i--)
				{
					Layer layer = layers[i];
					Gl.glStencilFunc(Gl.GL_ALWAYS, 1, 1);
					if (layer.Visible)
					{
						if (layer.Dirty)
							layer.Refresh(this.mapProjection, this.centralLongitude);
						layer.Draw(this);
					}
				}
			}

			// Draw the lat/lon grid if its turned on
			if (latLonGrid)
				DrawLatLonGrid();

			// Draw the tracking rectangle if its turned on
			if (trackingRectangle)
				DrawTrackingRectangle();
		}
		#endregion

		#region Internal Properties
		public double ScaleX
		{
			get { return scaleFactor * ((float)viewportWidth / (float)(orthoRight - orthoLeft)); }
		}

		public double ScaleY
		{
			get { return scaleFactor * ((float)viewportHeight / (float)(orthoTop - orthoBottom)); }
		}

		internal float ScaleFactor
		{
			get { return scaleFactor; }
		}

		internal BoundingBox BoundingBox
		{
			get { return boundingBox; }
		}

		internal BoundingBox._RectType3 RectToDraw
		{
			get { return rectToDraw; }
		}
		#endregion

		#region Public Properties
		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public short CentralLongitude
		{
			get { return centralLongitude; }
		}

		public double MinViewableLatitude
		{
			get
			{
				if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal)
					return Projection.MinAzimuthalLatitude;
				else
					return southExtent;
			}
		}

		public double MaxViewableLatitude
		{
			get { return northExtent; }
		}

		public int FeatureCount
		{
			get
			{
				int featureCount = 0;
				for (int i = 0; i < layers.Count; i++)
					featureCount += layers[i].Features.Count;
				return featureCount;
			}
		}

		public bool UseToolTips
		{
			get { return useToolTips; }
			set { useToolTips = value; }
		}

		public Feature HighlightFeature
		{
			get { return highlightFeature; }
		}

		public FeatureCollection HighlightFeatures
		{
			get { return highlightFeatures; }
		}

		public int BackCount
		{
			get { return mapViews.Count; }
		}

		public byte MapViewListSize
		{
			get { return maxMapViews; }
			set
			{
				if (value >= maxMapViews)
					maxMapViews = value;
				else
				{
					// Reduce the size of the current list
					for (int i = 0; i < maxMapViews - value; i++)
					{
						if (mapViews.Count == 0)
							break;
						mapViews.RemoveFirst();
					}
					maxMapViews = value;
				}
			}
		}

		public bool LatLonGrid
		{
			get { return latLonGrid; }
			set { latLonGrid = value; }
		}

		public static uint LatLonGridLineWidth
		{
			get { return Convert.ToUInt32(latLonGridLineWidth); }
			set { latLonGridLineWidth = Convert.ToSingle(value); }
		}

		public static Color LatLonGridColor
		{
			get { return latLonGridColor; }
			set { latLonGridColor = value; }
		}

		public bool MapStatusBarMargin
		{
			set { statusBarMargin = value; }
		}

		public FUL.Utils.ZoomLevelType ZoomLevel
		{
			get
			{
				if (this.DesignMode)
					return FUL.Utils.ZoomLevelType.None;
				double zoomLevel = (GetMapHeightDeg() / (double)this.Height) * (double)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
				if (zoomLevel > 110.0)
					return FUL.Utils.ZoomLevelType.Global;
				else if (zoomLevel > 10.0 && zoomLevel <= 110.0)
					return FUL.Utils.ZoomLevelType.Continental;
				else if (zoomLevel > 2.0 && zoomLevel <= 10.0)
					return FUL.Utils.ZoomLevelType.State;
				else if (zoomLevel > 0.25 && zoomLevel <= 2.0)
					return FUL.Utils.ZoomLevelType.County;
				else if (zoomLevel > 0.1 && zoomLevel <= 0.25)
					return FUL.Utils.ZoomLevelType.Terminal;
				else if (zoomLevel <= 0.1)
					return FUL.Utils.ZoomLevelType.Airport;
				else
					return FUL.Utils.ZoomLevelType.None;
			}
		}

		public bool DisableDateLinePanning
		{
			get { return disableDateLinePanning; }
			set { disableDateLinePanning = value; }
		}

		public LayerCollection Layers
		{
			get { return layers; }
		}

		public WSIFusionToolTip MapToolTip
		{
			get { return toolTip; }
		}
		#endregion

		#region Public Methods
		public void SetMapProjection(MapProjections mapProjection, short centralLongitude)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetMapProjection()");
#endif

			// Set the current rendering context
			MakeCurrent();

			// Get the current map center in world coordinates
			PointD mapCenter = GetMapCenter();
			double cxw = mapCenter.X;
			double cyw = mapCenter.Y;

			// Project the map center point to the new projection
			double px, py;
			Projection.ProjectPoint(mapProjection, cxw, cyw, centralLongitude, out px, out py);

			// Get the current map center in current projection coordinates
			double cxp = (GetMapRightDeg() + GetMapLeftDeg()) / 2;
			double cyp = (GetMapTopDeg() + GetMapBottomDeg()) / 2;

			// Set the new map projection on the map control and on all features in the map
			this.mapProjection = mapProjection;
			this.centralLongitude = centralLongitude;
			foreach (Layer layer in layers)
				layer.Refresh(this.mapProjection, this.centralLongitude);

			// Center the map on the current center point in the new projection
			Gl.glTranslated(cxp - px, cyp - py, 0.0);
			TranslateTest();

			// Trigger MapRectangleChanged event
			OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

			// Redraw the map
			Refresh();
		}

		public void Zoom(double zoomFactor)
		{
			if (!string.IsNullOrEmpty(toolTip.ToolTipText))
				toolTip.SetToolTip(this, string.Empty);
			Zoom(zoomFactor, true);
		}

		public void Zoom(double zoomFactor, bool refresh)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap Zoom()");
#endif

			if (zoomFactor == 0)
				return;

			// Set the current rendering context
			MakeCurrent();

			// Save the current view on the view list
			SaveCurrentMapView();

			// Scale the map based using the zoomFactor
			scaleFactor *= (float)zoomFactor;
			PointD p1 = GetMapCenterExt();
			Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			ScaleTest(zoomFactor);

			// Translate the map back to the original center point
			PointD p2 = GetMapCenterExt();
			Gl.glTranslated(p2.X - p1.X, p2.Y - p1.Y, 0.0);
			TranslateTest();

			if (refresh)
			{
				// Trigger MapRectangleChanged event
				OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

				// Redraw the map
				Refresh();
			}
		}

		public void Pan(Direction d)
		{
			Pan(d, 0.125);
		}

		public void Pan(Direction d, double mapFraction)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap Pan()");
#endif

			try
			{
				// Calculate the number of degrees to pan in each direction
				// Attempt to pan a multiple of screen pixels
				int panWinX = (int)(Width * mapFraction);
				int panWinY = (int)(Height * mapFraction);
				double panX = panWinX * GetMapWidthDeg() / Width;
				double panY = panWinY * GetMapHeightDeg() / Height;

				scrollWinX = 0;
				scrollWinY = 0;
				if (d == Direction.E || d == Direction.SE || d == Direction.NE)
					scrollWinX = -panWinX;
				else if (d == Direction.W || d == Direction.SW || d == Direction.NW)
					scrollWinX = panWinX;
				if (d == Direction.N || d == Direction.NE || d == Direction.NW)
				{
					// avoid scrolling off top of map
					double northEdge = GetMapTopDeg();
					if (northEdge + panY > northExtent)
					{
						panWinY = (int)((northExtent - northEdge) * Height / GetMapHeightDeg());
						panY = panWinY * GetMapHeightDeg() / Height;
					}
					scrollWinY = panWinY;
				}
				else if (d == Direction.S || d == Direction.SE || d == Direction.SW)
				{
					// avoid scrolling off bottom of map
					double southEdge = GetMapBottomDeg();
					if (southEdge - panY < southExtent)
					{
						panWinY = (int)((southEdge - southExtent) * Height / GetMapHeightDeg());
						panY = panWinY * GetMapHeightDeg() / Height;
					}
					scrollWinY = -panWinY;
				}

				if (scrollWinX == 0 && scrollWinY == 0)
				{
					scrollWinX = int.MinValue;
					scrollWinY = int.MinValue;
					return;
				}

				// Set the current rendering context
				MakeCurrent();

				// Save the current view on the view list
				SaveCurrentMapView();

				// Pan in the specified direction
				if (d == Direction.E)
					Gl.glTranslated(-panX, 0.0, 0.0);
				else if (d == Direction.NE)
					Gl.glTranslated(-panX, -panY, 0.0);
				else if (d == Direction.SE)
					Gl.glTranslated(-panX, panY, 0.0);
				else if (d == Direction.W)
					Gl.glTranslated(panX, 0.0, 0.0);
				else if (d == Direction.NW)
					Gl.glTranslated(panX, -panY, 0.0);
				else if (d == Direction.SW)
					Gl.glTranslated(panX, panY, 0.0);
				else if (d == Direction.N)
					Gl.glTranslated(0.0, -panY, 0.0);
				else if (d == Direction.S)
					Gl.glTranslated(0.0, panY, 0.0);

				// Trigger MapRectangleChanged event
				OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

				// synchronous map paint
				Refresh();
			}
			catch { }
			finally
			{
				// reset scrolling variables so paints will not do scroll optimization
				scrollWinX = int.MinValue;
				scrollWinY = int.MinValue;
			}
		}

		public PointD ToMapPointExt(int winx, int winy)
		{
			// Returns a PointD in map (world) coordinates. (e.g. If you pass in the Windows coordinates
			// for Boston, MA, you get the lat/lon of Boston, MA regardless of the map projection.)

			double ux, uy;
			PointD point = ToOpenGLPoint(winx, winy);
			Projection.UnprojectPoint(mapProjection, point.X, point.Y, centralLongitude, out ux, out uy);
			point.X = ux;
			point.Y = uy;
			return point;
		}

		public PointD ToMapPoint(int winx, int winy)
		{
			PointD point = ToMapPointExt(winx, winy);
			point.X = NormalizeLongitude(point.X);
			return point;
		}

		public PointD ToOpenGLPoint(int winx, int winy)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ToOpenGLPoint()");
#endif

			// Returns a PointD in OpenGL coordinates (-180 <= x <= 180, -90 <= y <= 90).  If the map is using
			// the Cylindrical Equidistant projection, ToMapPointExt and this method return the same point.

			double mapx, mapy, mapz;
			double[] modelMatrix = new double[16];
			double[] projMatrix = new double[16];
			int[] viewport = new int[4];

			// Set the current rendering context
			MakeCurrent();

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
			Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
			Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

			winy = Height - winy;	// Windows: UL corner is (0,0); OpenGL: LL corner is (0,0)

			Glu.gluUnProject(winx, winy, 0, modelMatrix, projMatrix, viewport, out mapx, out mapy, out mapz);

			return new PointD(mapx, mapy);
		}

		public double ToMapXDistanceDeg(double winDeltaX)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ToMapXDistanceDeg()");
#endif

			double mapx1, mapx2, mapy, mapz;
			double[] modelMatrix = new double[16];
			double[] projMatrix = new double[16];
			int[] viewport = new int[4];

			// Set the current rendering context
			MakeCurrent();

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
			Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
			Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

			Glu.gluUnProject(0, 0, 0, modelMatrix, projMatrix, viewport, out mapx1, out mapy, out mapz);
			Glu.gluUnProject(winDeltaX, 0, 0, modelMatrix, projMatrix, viewport, out mapx2, out mapy, out mapz);

			return Math.Abs(mapx1 - mapx2);
		}

		public double ToMapYDistanceDeg(double winDeltaY)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ToMapYDistanceDeg()");
#endif

			double mapx, mapy1, mapy2, mapz;
			double[] modelMatrix = new double[16];
			double[] projMatrix = new double[16];
			int[] viewport = new int[4];

			// Set the current rendering context
			MakeCurrent();

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
			Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
			Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

			Glu.gluUnProject(0, 0, 0, modelMatrix, projMatrix, viewport, out mapx, out mapy1, out mapz);
			Glu.gluUnProject(0, winDeltaY, 0, modelMatrix, projMatrix, viewport, out mapx, out mapy2, out mapz);

			return Math.Abs(mapy1 - mapy2);
		}

        public Point ToWinPointFromMap(double x, double y)
        {
            double px, py;
            Projection.ProjectPoint(MapProjection, x, y, CentralLongitude, out px, out py);
            px = DenormalizeLongitude(px);
            return ToWinPoint(px, py);
        }

        public Point ToWinPoint(double x, double y)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ToWinPoint()");
#endif

			double winx, winy, winz;
			double[] modelMatrix = new double[16];
			double[] projMatrix = new double[16];
			int[] viewport = new int[4];

			// Set the current rendering context
			MakeCurrent();

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
			Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
			Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

			Glu.gluProject(x, y, 0, modelMatrix, projMatrix, viewport, out winx, out winy, out winz);

			winy = Height - winy;	// Windows: UL corner is (0,0); OpenGL: LL corner is (0,0)

			return new Point((int)winx, (int)winy);
		}

		public void SetMapWidth(double mapWidth, FUL.Utils.DistanceUnits units)
		{
			try
			{
				double mapWidthInUnits = 0;
				switch (units)
				{
					case Utils.DistanceUnits.km:
						mapWidthInUnits = mapWidth / 1.609344;
						break;
					case Utils.DistanceUnits.mi:
						mapWidthInUnits = mapWidth;
						break;
					case Utils.DistanceUnits.nm:
						mapWidthInUnits = mapWidth / 0.868976242;
						break;
					default:
						mapWidthInUnits = mapWidth;
						break;
				}

				double WidthHeightRation = Math.Abs((GetMapLeftDeg() - GetMapRightDeg()) / (GetMapTopDeg() - GetMapBottomDeg()));
				PointD mapCenter = GetMapCenter();
				double mapWidthDeg = mapWidthInUnits / (Math.Cos(mapCenter.Latitude * FUL.Utils.deg2rad) * FUL.Utils.MilesPerDegLon);
				double HalfWidth = mapWidthDeg / 2;

				double Left = mapCenter.X - HalfWidth;
				double Right = mapCenter.X + HalfWidth;

				double HalfHeight = HalfWidth / WidthHeightRation;
				double Bottom = mapCenter.Y - HalfHeight;
				double Top = mapCenter.Y + HalfHeight;

				WSIMap.RectangleD Rectangle = new WSIMap.RectangleD(Bottom, Top, Left, Right);
				SetMapRectangleAvg(Rectangle, false);
			}
			catch { }

		}

		public double GetMapWidth(FUL.Utils.DistanceUnits units)
		{
			double mapWidthDeg = GetMapWidthDeg();
			PointD mapCenter = GetMapCenterExt();
			double mapWidthMiles = Math.Cos(mapCenter.Latitude * FUL.Utils.deg2rad) * mapWidthDeg * FUL.Utils.MilesPerDegLon;
			double mapWidthInUnits = 0;
			switch (units)
			{
				case Utils.DistanceUnits.km:
					mapWidthInUnits = mapWidthMiles * 1.609344;
					break;
				case Utils.DistanceUnits.mi:
					mapWidthInUnits = mapWidthMiles;
					break;
				case Utils.DistanceUnits.nm:
					mapWidthInUnits = mapWidthMiles * 0.868976242;
					break;
				default:
					mapWidthInUnits = mapWidthMiles;
					break;
			}

			return mapWidthInUnits;
		}

		public PointD GetMapCenterExt()
		{
			// Returns a PointD in map (world) coordinates. (e.g. If the map is centered on Boston, MA,
			// you get the lat/lon of Boston, MA regardless of the map projection.)

			double x = (GetMapRightDeg() + GetMapLeftDeg()) / 2;
			double y = (GetMapTopDeg() + GetMapBottomDeg()) / 2;
			double ux, uy;
			Projection.UnprojectPoint(mapProjection, x, y, centralLongitude, out ux, out uy);
			return new PointD(ux, uy);
		}

		public PointD GetMapCenter()
		{
			PointD point = GetMapCenterExt();
			point.X = NormalizeLongitude(point.X);
			return point;
		}

		public void SetMapCenter(int winx, int winy)
		{
			PointD p = ToMapPoint(winx, winy);
			SetMapCenter(p);
		}

        /// <summary>
        /// exponential easing function, get incremental difference based on step
        /// </summary>
        /// <param name="step">step we need the increment for, starting at 1</param>
        /// <param name="delta">total change</param>
        /// <param name="numSteps">number of total increments needed</param>
        /// <returns>increment at current step</returns>
        public static double EaseOut(double step, double delta, double numSteps)
        {
            if (step == numSteps)
                return delta - PositionAtStep(step - 1, delta, numSteps);
            else
                return PositionAtStep(step, delta, numSteps) - PositionAtStep(step - 1, delta, numSteps);
        }

        /// <summary>
        /// exponential easing, returns absolute position at a given step
        /// </summary>
        /// <param name="step">step we need increment for, starting at 1</param>
        /// <param name="delta">total change</param>
        /// <param name="numSteps">number of total increments</param>
        /// <returns></returns>
        public static double PositionAtStep(double step, double delta, double numSteps)
        {
            return delta * (1 - Math.Pow(2, -10 * step / numSteps));
        }

		public void SetMapCenter(IMapPoint p)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetMapCenter()");
#endif

			// Expects a PointD in map (world) coordinates. (e.g. If you want the map centered on Boston, MA,
			// you should pass in the lat/lon of Boston, MA regardless of the map projection.)

			// Set the current rendering context
			MakeCurrent();

			// Save the current view on the view list
			SaveCurrentMapView();

			// Project the points using the map's current projection
			PointD p1 = GetMapCenterExt();
			double px, py, p1x, p1y;
			Projection.ProjectPoint(mapProjection, p.X, p.Y, centralLongitude, out px, out py);
			Projection.ProjectPoint(mapProjection, p1.X, p1.Y, centralLongitude, out p1x, out p1y);

            if (enablePanToFoundObject && panToFoundObject)
            {
                // reposition map in steps
                int numSteps = 8;
                double pdx, pdy, dx, dy;
                pdx = p1x - px;
                pdy = p1y - py;
                for (int i = 1; i < numSteps + 1; i++)
                {
                    dx = EaseOut(i, pdx, numSteps);
                    dy = EaseOut(i, pdy, numSteps);
                    Gl.glTranslated(dx, dy, 0.0);
                    Refresh();
                }
                TranslateTest();
                panToFoundObject = false;
                OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));
            }
            else
            {
                // Set the map center to point p
                Gl.glTranslated(p1x - px, p1y - py, 0.0);
                TranslateTest();

                // Trigger MapRectangleChanged event
                OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

                // Redraw the map
                Refresh();
            }
		}

		public RectangleD GetMapRectangleExt()
		{
			return new RectangleD(GetMapBottomDeg(), GetMapTopDeg(), GetMapLeftDeg(), GetMapRightDeg());
		}

		public RectangleD GetMapRectangle()
		{
			RectangleD rect = GetMapRectangleExt();
			rect.Left = NormalizeLongitude(rect.Left);
			rect.Right = NormalizeLongitude(rect.Right);
			return rect;
		}

		public void GetMapBoundingBox(out double minLat, out double maxLat, out double minLon, out double maxLon)
		{
			// Prevents an infinite loop below if window is minimized
			if (Height == 0 || Width == 0)
			{
				minLat = 0;
				minLon = 0;
				maxLat = 0;
				maxLon = 0;
				return;
			}

			// Find the min and max latitude and longitude in the map control.  For projections other than
			// cylindrical equidistant, sample points in the map control to find the min and max values.
			minLat = 999;
			minLon = 999;
			maxLat = -999;
			maxLon = -999;
			PointD p = null;

			// For cylindrical projections, just return the map rectangle to improve performance
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Cylindrical)
			{
				p = ToMapPointExt(0, this.Height);
				minLat = p.Y;
				p = ToMapPointExt(this.Width, 0);
				maxLon = p.X;
				p = ToMapPointExt(0, 0);
				minLon = p.X;
				maxLat = p.Y;
				return;
			}

			// Sample points in the map control
			bool idl = false;
			int jstep = (Height / 5) == 0 ? 1 : (Height / 5); // prevent infinite loop
			int istep = (Width / 5) == 0 ? 1 : (Width / 5); // prevent infinite loop
			for (int j = 0; j <= Height; j += jstep)
			{
				for (int i = 0; i <= Width; i += istep)
				{
					p = ToMapPoint(i, j);
					if (!idl && minLon != 999 && Math.Sign(p.X) != Math.Sign(minLon) && (p.X < -90 || p.X > 90))
						idl = true;
					if (p.X < minLon)
						minLon = p.X;
					if (p.X > maxLon)
						maxLon = p.X;
					if (p.Y < minLat)
						minLat = p.Y;
					if (p.Y > maxLat)
						maxLat = p.Y;
				}
			}

			// If the map contains the international date line, reset min and max longitudes.  The resulting
			// bounding box isn't strictly correct (it covers too much of the map), but is a simple solution
			// to the date line problem.
			if (idl)
			{
				minLon = -180;
				maxLon = 180;
			}

			// If the map contains the north pole, set the max latitude to 90
			double px, py;
			Projection.ProjectPoint(mapProjection, 0, 90, centralLongitude, out px, out py);
			Point pt = ToWinPoint(px, py);
			if (pt.X >= 0 && pt.X < Width && pt.Y >= 0 && pt.Y < Height)
				maxLat = 90;

			// Set the min latitude to the projection's min latitude if it is less than that value
			if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && minLat < Projection.MinAzimuthalLatitude)
				minLat = Projection.MinAzimuthalLatitude;
		}

		public RectangleD GetMapBoundingBox()
		{
			double minLat, maxLat, minLon, maxLon;
			GetMapBoundingBox(out minLat, out maxLat, out minLon, out maxLon);
			RectangleD rect = new RectangleD(minLat, maxLat, minLon, maxLon);
			return rect;
		}

		public void SetMapRectangle(RectangleD rect)
		{
			SetMapRectangleAvg(rect, true);
		}

		public void SetMapRectangle(RectangleD rect, bool saveCurrentMapView, bool useMaxRectDimension)
		{
			if (useMaxRectDimension)
				SetMapRectangleMin(rect, saveCurrentMapView);
			else
				SetMapRectangleAvg(rect, saveCurrentMapView);
		}

		public double DenormalizeLongitude(double x)
		{
			// find a longitude for x that is within a symmetric 360 degree range around the Map center
			double mapMidPointLon = (this.BoundingBox.Map.left + this.BoundingBox.Map.right) / 2.0;
			double leftBound = mapMidPointLon - 180.0;
			double rightBound = mapMidPointLon + 180.0;

			while (x < leftBound)
				x += 360.0;

			while (x > rightBound)
				x -= 360.0;

			// ml <= x <= mr
			return x;
		}

		public void ScrollMap(int winX, int winY, double zoomFactor)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ScrollMap()");
#endif

			if (zoomFactor == 0)
				return;

			// Set the current rendering context
			MakeCurrent();

			// Save the current view on the view list
			SaveCurrentMapView();

			// Scale the map based using the zoomFactor
			scaleFactor *= (float)zoomFactor;

			PointD p1 = ToOpenGLPoint(winX, winY);

			Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			ScaleTest(zoomFactor);

			// Translate the map back to the original center point
			PointD p2 = ToOpenGLPoint(winX, winY);
			Gl.glTranslated(p2.X - p1.X, p2.Y - p1.Y, 0.0);
			TranslateTest();

			// Trigger MapRectangleChanged event
			OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

			// Redraw the map
			Refresh();
		}

		public void Back()
		{
			if (mapViews.Count > 0)
			{
				// Go back to the previous map view
				SetMapRectangleAvg(mapViews.Last.Value, false);

				// Remove the map view that is now the current map view
				mapViews.RemoveLast();
			}
		}

		public void ClearHistory()
		{
			mapViews.Clear();
		}

		public RectangleD GetMapExtents()
		{
			return new RectangleD(southExtent, northExtent, westExtent, eastExtent);
		}

		public void SetMapExtents(RectangleD rect)
		{
			// Don't allow malformed rectangles
			if (rect == null || rect.Width == 0 || rect.Height == 0)
			{
				Refresh();
				return;
			}

			// Check for badly specified extents
			if (rect.Left > rect.Right || rect.Bottom > rect.Top)
			{
				Refresh();
				return;
			}
			if (rect.Top > orthoTop || rect.Bottom < orthoBottom || rect.Right > orthoRight || rect.Left < orthoLeft)
			{
				Refresh();
				return;
			}

			// Set the map extents
			southExtent = rect.Bottom;
			northExtent = rect.Top;
			westExtent = rect.Left;
			eastExtent = rect.Right;
			SetMapRectangleAvg(rect, false);
		}

		public double GetMapTolerance()
		{
			// This constant is an aribtrary value that makes the tooltips work in a reasonable way
			double TOLERANCE_SCALE_VALUE = 300.0;

			// Set the search distance
			if (scaleFactor > 0)
				return (1.0 / scaleFactor) * TOLERANCE_SCALE_VALUE;
			else
				return TOLERANCE_SCALE_VALUE;
		}

		public RectangleD TrackRectangle(bool tracking)
		{
			trackingRectangle = tracking;
			if (!trackingRectangle)
			{
				PointD p1 = ToOpenGLPoint(mouseDownX, mouseDownY);
				PointD p2 = ToOpenGLPoint(mouseUpX, mouseUpY);

				if (mouseDownX < mouseUpX && mouseDownY < mouseUpY)
					return new RectangleD(p2.Y, p1.Y, p1.X, p2.X);
				else if (mouseDownX < mouseUpX && mouseDownY > mouseUpY)
					return new RectangleD(p1.Y, p2.Y, p1.X, p2.X);
				else if (mouseDownX > mouseUpX && mouseDownY > mouseUpY)
					return new RectangleD(p1.Y, p2.Y, p2.X, p1.X);
				else if (mouseDownX > mouseUpX && mouseDownY < mouseUpY)
					return new RectangleD(p2.Y, p1.Y, p2.X, p1.X);
				else
					return null;
			}
			else
				return (RectangleD)null;
		}

		public void Panning(bool panning)
		{
			this.panning = panning;
			if (this.panning)
			{
                WinXMove = 0;
                WinYMove = 0;
                SaveCurrentMapView();
            }
				
			if (!this.panning && (mouseMoveX != mouseDownX || mouseMoveY != mouseDownY))
				OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));
		}

		public string GetToolTip()
		{
			return toolTip.ToolTipText;
		}

		public double MapRatio()
		{
			double ratio = GetMapWidthDeg() / GetMapHeightDeg();
			return ratio;
		}

		public bool SaveAsImage(string imageFilename, ImageFormat imageFormat)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SaveAsImage()");
#endif

			Bitmap b = null;
			try
			{
				this.AutoSwapBuffers = false;
				this.Refresh();
				b = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
				BitmapData bd = b.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				Gl.glReadBuffer(Gl.GL_BACK);
				Gl.glReadPixels(0, 0, this.Width, this.Height, Gl.GL_BGRA_EXT, Gl.GL_UNSIGNED_BYTE, bd.Scan0);
				b.UnlockBits(bd);
				b.RotateFlip(RotateFlipType.Rotate180FlipX);
				b.Save(imageFilename, imageFormat);
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				if (b != null)
					b.Dispose();
				this.AutoSwapBuffers = true;
			}
		}

		/// <summary>
		/// Embed screen capture in and save as a pdf document
		/// </summary>
		/// <param name="filename">Name of the .pdf file to save.</param>
		/// <returns></returns>
		public bool SaveAsPdf(string filename)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SaveAsPdf()");
#endif

			Bitmap b = null;
			Double pagePixelWidth;
			Double pagePixelHeight;
			PageOrientation orientation;
			Double scale;
			try
			{
				this.AutoSwapBuffers = false;
				this.Refresh();
				b = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
				BitmapData bd = b.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				Gl.glReadBuffer(Gl.GL_BACK);
				Gl.glReadPixels(0, 0, this.Width, this.Height, Gl.GL_BGRA_EXT, Gl.GL_UNSIGNED_BYTE, bd.Scan0);
				b.UnlockBits(bd);
				b.RotateFlip(RotateFlipType.Rotate180FlipX);
				PdfDocument s_document = new PdfDocument();
				MemoryStream strm = new MemoryStream();
				b.Save(strm, ImageFormat.Bmp);
				Image img = Image.FromStream(strm);
				XImage image = XImage.FromGdiPlusImage(img);
				PdfPage page = s_document.AddPage();
				//Set Image Scaling and Orientation
				if (b.Height >= b.Width)
				{
					pagePixelWidth = 96 * 8.5;
					pagePixelHeight = 96 * 11;
					orientation = PageOrientation.Portrait;
				}
				else
				{
					pagePixelWidth = 96 * 11;
					pagePixelHeight = 96 * 8.5;
					orientation = PageOrientation.Landscape;
				}

				if (pagePixelHeight / b.Height < pagePixelWidth / b.Width)
				{
					scale = pagePixelHeight / image.PixelHeight;
				}
				else
				{
					scale = pagePixelWidth / b.Width;
				}
				page.Orientation = orientation;
				XGraphics gfx = XGraphics.FromPdfPage(page);
				gfx.ScaleTransform(scale);
				gfx.DrawImage(image, 5 , 5);
				s_document.Save(filename);
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				if(b != null)
				{
					b.Dispose();
				}
				this.AutoSwapBuffers = true;
			}
		}

		public string OpenGLVersion()
		{
			try
			{
#if TRACK_OPENGL_DISPLAY_LISTS
				Feature.ConfirmMainThread("WSIMap OpenGLVersion()");
#endif

				IntPtr ptr = Gl.glGetString(Gl.GL_VERSION);
				string version = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
				return version;
			}
			catch
			{
				return "Unknown";
			}
		}

		public int OpenGLMaxTextureSize()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap OpenGLMaxTextureSize()");
#endif

			try
			{
				int maxSize = 0;
				Gl.glGetIntegerv(Gl.GL_MAX_TEXTURE_SIZE, out maxSize);
				return maxSize;
			}
			catch
			{
				return int.MinValue;
			}
		}
		#endregion

		#region Public Static Methods
		public static double NormalizeLongitude(double x)
		{
			if (x < -180.0)
			{
				x = x % -360.0;
				if (x < -180.0)
					x += 360.0;
			}
			else if (x > 180.0)
			{
				x = x % 360.0;
				if (x > 180.0)
					x -= 360.0;
			}

			return x;
		}

		public static double Distance(IMapPoint p1, IMapPoint p2, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(p1.Y, p1.X, p2.Y, p2.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(p1.Y, p1.X, p2.Y, p2.X, Utils.DistanceUnits.mi);
		}
		#endregion

		#region Internal Static Methods
		internal static int GetNumberOfCrossingIDL(double left, double right)
		{
			int crossings = 0;  // number of times IDL has been crossed
			int sign = 0;

			if (left < -180)
			{
				crossings = (Math.Abs((int)left / 180) + 1) / 2;
				sign = -1;
			}
			else if (right > 180)
			{
				crossings = (((int)right / 180) + 1) / 2;
				sign = 1;
			}

			return crossings * sign;
		}

		internal static void DrawDisplayListWithShift(int openglDisplayList, double left, double right, bool isCrossIDL)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap DrawDisplayListWithShift()");
#endif

			int crossings = GetNumberOfCrossingIDL(left, right);

			if (isCrossIDL)
			{
				// Draw left and right
				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				Gl.glCallList(openglDisplayList);
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
				Gl.glCallList(openglDisplayList);
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 2), 0.0f, 0.0f);
				Gl.glCallList(openglDisplayList);
				Gl.glPopMatrix();

			}
			else
			{
				DrawDisplayListWithShift(openglDisplayList, left, right);
			}
		}

		internal static void DrawDisplayListWithShift(int openglDisplayList, double left, double right)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap DrawDisplayListWithShift()");
#endif

			int crossings = GetNumberOfCrossingIDL(left, right);

			double l = NormalizeLongitude(left);
			double r = NormalizeLongitude(right);

			// Crossing dateline
			if ((l - r) > -0.001)   // ignore small differences between l & r
			{
				// Draw left and right
				Gl.glPushMatrix();
				if (crossings > 0)
					Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
				else
					Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
				Gl.glCallList(openglDisplayList);
				Gl.glPopMatrix();

				Gl.glPushMatrix();
				Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
				Gl.glCallList(openglDisplayList);
				Gl.glPopMatrix();
			}
			else
			{
				if (crossings > 0)
				{
					if (l > (360 * (crossings - 1) + 180) && r < (360 * crossings + 180))
					{
						// draw right
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * (crossings + 1), 0.0f, 0.0f);
						Gl.glCallList(openglDisplayList);
						Gl.glPopMatrix();
					}
					else
					{
						// draw left
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
						Gl.glCallList(openglDisplayList);
						Gl.glPopMatrix();
					}
				}
				else
				{
					if (l > (360 * crossings - 180) && r > (360 * (crossings + 1) - 180))
					{
						// draw left
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * crossings, 0.0f, 0.0f);
						Gl.glCallList(openglDisplayList);
						Gl.glPopMatrix();
					}
					else
					{
						// draw right
						Gl.glPushMatrix();
						Gl.glTranslatef(360f * (crossings - 1), 0.0f, 0.0f);
						Gl.glCallList(openglDisplayList);
						Gl.glPopMatrix();
					}
				}
			}
		}
		#endregion

		#region Private Methods
		private void SetMapRectangleAvg(RectangleD rect, bool saveCurrentMapView)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetMapRectangleAvg()");
#endif

			// Don't allow malformed rectangles
			if (rect == null || rect.Width == 0 || rect.Height == 0)
			{
				Refresh();
				return;
			}

			// Set the viewport
			SetViewport();

			// Determine an average zoom factor for the input rectangle
			double xZoomFactor = GetMapWidthDeg() / rect.Width;
			double yZoomFactor = GetMapHeightDeg() / rect.Height;
			double zoomFactor = (xZoomFactor + yZoomFactor) / 2;
			if (zoomFactor == 0)
			{
				Refresh();
				return;
			}

			// Set the current rendering context
			MakeCurrent();

			// Save the current view on the view list
			if (saveCurrentMapView)
				SaveCurrentMapView();

			// Scale the map based using the zoomFactor
			scaleFactor *= (float)zoomFactor;
			Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			ScaleTest(zoomFactor);

			// Project the points using the map's current projection
			PointD p1 = GetMapCenterExt();
			double p1x, p1y;
			Projection.ProjectPoint(mapProjection, p1.X, p1.Y, centralLongitude, out p1x, out p1y);

			// Translate the map using the rectangle center
			Gl.glTranslated(p1x - rect.Center.X, p1y - rect.Center.Y, 0.0);
			TranslateTest();

			// Trigger MapRectangleChanged event
			OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

			// Redraw the map
			Refresh();
		}

		private void SetMapRectangleMin(RectangleD rect, bool saveCurrentMapView)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetMapRectangleMin()");
#endif

			// Don't allow malformed rectangles
			if (rect == null || rect.Width == 0 || rect.Height == 0)
			{
				Refresh();
				return;
			}

			// Set the viewport
			SetViewport();

			// Determine a minimum zoom factor (max dimension) for the input rectangle
			double xZoomFactor = GetMapWidthDeg() / rect.Width;
			double yZoomFactor = GetMapHeightDeg() / rect.Height;
			double zoomFactor = 0;
			if (xZoomFactor <= yZoomFactor)
				zoomFactor = xZoomFactor;
			else
				zoomFactor = yZoomFactor;
			if (zoomFactor == 0)
			{
				Refresh();
				return;
			}

			// Set the current rendering context
			MakeCurrent();

			// Save the current view on the view list
			if (saveCurrentMapView)
				SaveCurrentMapView();

			// Scale the map based using the zoomFactor
			scaleFactor *= (float)zoomFactor;
			Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			ScaleTest(zoomFactor);

			// Project the points using the map's current projection
			PointD p1 = GetMapCenterExt();
			double p1x, p1y;
			Projection.ProjectPoint(mapProjection, p1.X, p1.Y, centralLongitude, out p1x, out p1y);

			// Translate the map using the rectangle center
			Gl.glTranslated(p1x - rect.Center.X, p1y - rect.Center.Y, 0.0);
			TranslateTest();

			// Trigger MapRectangleChanged event
			OnMapRectangleChanged(new MapRectangleChangedEventArgs(this.GetMapRectangle()));

			// Redraw the map
			Refresh();
		}

		private void SaveCurrentMapView()
		{
			// Just return if the list size is 0
			if (maxMapViews == 0)
				return;

			// Save the current view on the map view list
			if (mapViews.Count < maxMapViews)
				mapViews.AddLast(this.GetMapRectangleExt());
			else
			{
				mapViews.RemoveFirst();
				mapViews.AddLast(this.GetMapRectangleExt());
			}
		}

		private void DrawTrackingRectangle()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap DrawTrackingRectangle()");
#endif

			if (trackingRectP1 == null || trackingRectP2 == null)
				return;
			Gl.glColor4f(((float)Color.Yellow.R) / 255f, ((float)Color.Yellow.G) / 255f, ((float)Color.Yellow.B) / 255f, 0.5f);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(trackingRectP1.X, trackingRectP1.Y);
			Gl.glVertex2d(trackingRectP2.X, trackingRectP1.Y);
			Gl.glVertex2d(trackingRectP2.X, trackingRectP2.Y);
			Gl.glVertex2d(trackingRectP1.X, trackingRectP2.Y);
			Gl.glEnd();
		}

		private void DrawLatLonGrid()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap DrawLatLonGrid()");
#endif

			// Make sure font was initialized
			if (font == null)
				return;

			// Configuration
			double xstep, ystep;
			double wl = GetMapLeftDeg();
			double wr = GetMapRightDeg();
			double wt = GetMapTopDeg();
			double wb = GetMapBottomDeg();
			double digitWidth = ToMapXDistanceDeg(digitCharWidth);
			int startLon, endLon;
			startLon = (int)Math.Floor(wl / 180.0) * 180;
			endLon = (int)Math.Ceiling(wr / 180.0) * 180;
			double ww = wr - wl;
			double wh = wt - wb;
			if (ww > 30)
			{
				xstep = 10;
				ystep = 10;
			}
			else if (ww < 30 && ww >= 10)
			{
				xstep = 5;
				ystep = 5;
			}
			else
			{
				xstep = 1;
				ystep = 1;
			}
			Gl.glLineWidth(latLonGridLineWidth);
			Gl.glColor4f(((float)latLonGridColor.R) / 255f, ((float)latLonGridColor.G) / 255f, ((float)latLonGridColor.B) / 255f, 1f);

			// Draw latitude lines
			if (mapProjection == MapProjections.CylindricalEquidistant)
			{
				for (double y = -90; y <= 90; y += ystep)
				{
					// don't draw outside the map window
					if (y < wb || y > wt)
						continue;

					string lat = y.ToString();

					// draw lines
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Gl.glVertex2d(wl + ((lat.Length + 1) * digitWidth), y);
					Gl.glVertex2d(wr, y);
					Gl.glEnd();

					// draw labels
					Gl.glPushAttrib(Gl.GL_LIST_BIT);
					Gl.glListBase(font.OpenGLDisplayListBase);
					Gl.glRasterPos2d(wl + (digitWidth * 0.6), y - (digitWidth * 0.5));
					Gl.glCallLists(lat.Length, Gl.GL_UNSIGNED_BYTE, lat);
					Gl.glPopAttrib();
				}
			}
            else if (mapProjection == MapProjections.Mercator)
            {
                for (double y = -70; y <= 70; y += ystep)
                {
                    // don't draw outside the map window
                    if (y < wb || y > wt)
                        continue;

                    string lat = y.ToString();

                    // draw lines
                    Gl.glBegin(Gl.GL_LINE_STRIP);
                    double px1, px2, py1, py2;
                    // x coord is a left border + offset for a label, i.e. 0, +-10, +-20 etc
                    Projection.ProjectPoint(MapProjections.Mercator, wl + ((lat.Length + 1) * digitWidth), y, centralLongitude, out px1, out py1);
                    Projection.ProjectPoint(MapProjections.Mercator, wr, y, centralLongitude, out px2, out py2);
                    Gl.glVertex2d(px1, py1);
                    Gl.glVertex2d(px2, py2);
                    Gl.glEnd();

                    // draw labels
                    Gl.glPushAttrib(Gl.GL_LIST_BIT);
                    Gl.glListBase(font.OpenGLDisplayListBase);
                    double xl, yl;
                    Projection.ProjectPoint(MapProjections.Mercator, wl, y, centralLongitude, out xl, out yl);
                    Gl.glRasterPos2d(xl + (digitWidth * 0.6), yl - (digitWidth * 0.5));
                    Gl.glCallLists(lat.Length, Gl.GL_UNSIGNED_BYTE, lat);
                    Gl.glPopAttrib();
                }
            }
            else if (mapProjection == MapProjections.Stereographic || mapProjection == MapProjections.Orthographic)
			{
				int nPoints = 120;
				double cx = 0;	// center of lat circles x value
				double cy = 89.9999 * FUL.Utils.deg2rad; // center of lat circles y value
				int t = 360 / nPoints;
				double sincy = Math.Sin(cy);
				double coscy = Math.Cos(cy);

				for (double lat = 0; lat <= 90; lat += ystep)
				{
					double d = (lat * FUL.Utils.MilesPerDegLat) / FUL.Utils.EarthRadius_sm;
					double sind = Math.Sin(d);
					double cosd = Math.Cos(d);

					// draw lines
					Gl.glBegin(Gl.GL_LINE_STRIP);
					for (int i = 0; i <= nPoints; i++)
					{
						double siny = sincy * cosd + coscy * sind * Math.Cos(t * i * FUL.Utils.deg2rad);
						double y = Math.Asin(siny);
						double dlon = Math.Atan2(Math.Sin(t * i * FUL.Utils.deg2rad) * sind * coscy, cosd - sincy * siny);
						double x = ((cx - dlon + Math.PI) % (2 * Math.PI)) - Math.PI;

						x = -1 * x / FUL.Utils.deg2rad;
						y = y / FUL.Utils.deg2rad;

						double px, py;
						Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
					Gl.glEnd();
				}
			}

            int margin = 0;
			if (statusBarMargin)
				margin = 4;

			// Draw longitude lines 
			if (mapProjection == MapProjections.CylindricalEquidistant || mapProjection == MapProjections.Mercator)
			{
				for (double x = startLon; x <= endLon; x += xstep)
				{
					// don't draw outside the map window
					if (x < wl || x > wr)
						continue;

					string lon = NormalizeLongitude(x).ToString();

					// draw lines
					Gl.glBegin(Gl.GL_LINE_STRIP);
					Gl.glVertex2d(x, wb + (digitWidth * (2.0 + margin)));
					Gl.glVertex2d(x, wt);
					Gl.glEnd();

					// draw labels
					Gl.glPushAttrib(Gl.GL_LIST_BIT);
					Gl.glListBase(font.OpenGLDisplayListBase);
					Gl.glRasterPos2d(x - (lon.Length * digitWidth * 0.45), wb + (digitWidth * (0.6 + margin)));
					Gl.glCallLists(lon.Length, Gl.GL_UNSIGNED_BYTE, lon);
					Gl.glPopAttrib();
				}
			}
            else if (mapProjection == MapProjections.Stereographic || mapProjection == MapProjections.Orthographic)
			{
				for (double x = -180; x <= 180; x += xstep)
				{
					// draw lines
					Gl.glBegin(Gl.GL_LINE_STRIP);
					double px, py;
					Projection.ProjectPoint(mapProjection, x, 80, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
					Projection.ProjectPoint(mapProjection, x, 0, centralLongitude, out px, out py);
					Gl.glVertex2d(px, py);
					Gl.glEnd();
				}
			}
        }

		private void SetRasterPosWin(int x, int y)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetRasterPosWin()");
#endif

			// save state
			Gl.glPushAttrib(Gl.GL_TRANSFORM_BIT | Gl.GL_VIEWPORT_BIT);
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();

			// set up viewport & projection and set the position
			Gl.glViewport(0, 0, this.Width, this.Height);
			Gl.glOrtho(0.0, this.Width, 0.0, this.Height, 0.0, 1.0);
			Gl.glRasterPos2i(x, this.Height - y);  // convert y-origin from top to bottom

			// restore state
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPopMatrix();
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPopMatrix();
			Gl.glPopAttrib();
		}

		private void GetLabelStartInfo(out double startAdr, out int start_label, double wl, double wr, double xstep)
		{
			startAdr = -wr % 360;
			start_label = 0;

			while (startAdr <= wl)
			{
				startAdr += (int)xstep;
				start_label += (int)xstep;
				if (start_label > 180)
					start_label = (int)(-180 + xstep);
			}
			start_label -= 180;
			if (start_label < -180)
				start_label += 360;
		}

		private double GetMapWidthDeg()
		{
			return GetMapRightDeg() - GetMapLeftDeg();
		}

		private double GetMapHeightDeg()
		{
			return GetMapTopDeg() - GetMapBottomDeg();
		}

		private double GetMapRightDeg()
		{
			PointD pt = ToOpenGLPoint(this.Width, 0);
			return Math.Round(pt.X, 3); // prevent values close to but slightly greater than 180
		}

		private double GetMapLeftDeg()
		{
			PointD pt = ToOpenGLPoint(0, 0);
			return Math.Round(pt.X, 3); // prevent values close to but slightly less than -180
		}

		private double GetMapBottomDeg()
		{
			PointD pt = ToOpenGLPoint(0, this.Height);
			return pt.Y;
		}

		private double GetMapTopDeg()
		{
			PointD pt = ToOpenGLPoint(0, 0);
			return pt.Y;
		}

		private FeatureCollection GetFeaturesOverMouse(IMapPoint point)
		{
			PointD mousePoint = new PointD(point.X, point.Y);

			FeatureCollection featureList = new FeatureCollection();

			// Set the search distance
			double tolerance = GetMapTolerance();

			LayerCollection tooltipLayers = new LayerCollection();
			foreach (Layer layer in layers)
			{
				if (!layer.UseToolTips || !layer.Visible || !layer.Drawn)
					continue;

				int index = tooltipLayers.Count - 1;
				for (int i = index; i >= 0; i--)
				{
					if (layer.TooltipOrder >= tooltipLayers[i].TooltipOrder)
					{
						index = i + 1;
						break;
					}

					index = i;
				}

				if ((index > -1) && (index < tooltipLayers.Count))
					tooltipLayers.Insert(layer, index);
				else
					tooltipLayers.Add(layer);
			}

			// Order tropical tooltip
			int tropicalIndex = -1;
			foreach (Layer tooltipLayer in tooltipLayers)
				tooltipLayer.FindClosestFeaturesWithin(mousePoint, tolerance, true, featureList, ref tropicalIndex);

			return featureList;
		}

		private void SetViewport()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap SetViewport()");
#endif

			if (this.Width >= this.Height && this.Height < (this.Width / 2))
			{
				viewportWidth = this.Width;
				viewportHeight = this.Width / 2;
			}
			else
			{
				viewportHeight = this.Height;
				viewportWidth = 2 * this.Height;
			}

			viewportX = (this.Width - viewportWidth) / 2;
			viewportY = (this.Height - viewportHeight) / 2;

			Gl.glViewport(viewportX, viewportY, viewportWidth, viewportHeight);
		}

		private void ScaleTest(double zoom)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ScaleTest()");
#endif

			double zoomFactor, wZoomFactor, hZoomFactor;

			// Set the current rendering context
			MakeCurrent();

			// If the map is too small, rescale the map
			double mapWidth = GetMapWidthDeg();
			if (mapWidth > 0 && mapWidth <= 0.01 && zoom > 1)
			{
				zoomFactor = 1.0 / zoom;
				scaleFactor *= (float)zoomFactor;
				Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			}

			// If the map height or width is greater than the maximum extents, rescale the map
			wZoomFactor = GetMapWidthDeg() / (eastExtent - westExtent);
			hZoomFactor = GetMapHeightDeg() / (northExtent - southExtent);
			if ((wZoomFactor > 1 || hZoomFactor > 1) && zoom < 1)
			{
				if (wZoomFactor >= hZoomFactor)
					zoomFactor = wZoomFactor;
				else
					zoomFactor = hZoomFactor;
				scaleFactor *= (float)zoomFactor;
				Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			}
		}

		private void ScaleTest()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap ScaleTest()");
#endif

			double zoomFactor, wZoomFactor, hZoomFactor;

			// Set the current rendering context
			MakeCurrent();

			// If the map is too small, rescale the map
			double mapWidth = GetMapWidthDeg();
			if (mapWidth > 0 && mapWidth <= 0.01)
			{
				zoomFactor = (100 * (eastExtent - westExtent)) / scaleFactor;
				scaleFactor *= (float)zoomFactor;
				Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			}

			// If the map height or width is greater than the maximum extents, rescale the map
			wZoomFactor = GetMapWidthDeg() / (eastExtent - westExtent);
			hZoomFactor = GetMapHeightDeg() / (northExtent - southExtent);
			if (wZoomFactor > 1 || hZoomFactor > 1)
			{
				if (wZoomFactor >= hZoomFactor)
					zoomFactor = wZoomFactor;
				else
					zoomFactor = hZoomFactor;
				scaleFactor *= (float)zoomFactor;
				Gl.glScaled(zoomFactor, zoomFactor, 1.0);
			}
		}

		private void TranslateTest()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap TranslateTest()");
#endif

			// Set the current rendering context
			MakeCurrent();

			// Make sure we don't zoom outside the map extents
			double panX = 0, panY = 0;
			double northEdge = GetMapTopDeg();
			double southEdge = GetMapBottomDeg();
			if (southEdge < southExtent)
				panY = southEdge - southExtent;
			else if (northEdge > northExtent)
				panY = northEdge - northExtent;
			else
				panY = 0;
			if (disableDateLinePanning)
			{
				double westEdge = GetMapLeftDeg();
				double eastEdge = GetMapRightDeg();
				if (westEdge < westExtent)
					panX = westEdge - westExtent;
				else if (eastEdge > eastExtent)
					panX = eastEdge - eastExtent;
				else
					panX = 0;
			}
			Gl.glTranslated(panX, panY, 0.0);
		}
		#endregion

		#region Events
		private void OnMapRectangleChanged(MapRectangleChangedEventArgs e)
		{
			if (MapRectangleChanged != null)
				MapRectangleChanged(this, e);
		}

		public void MapResize()
		{
			ScaleTest();
		}

		private void MapGL_KeyUp(object sender, KeyEventArgs e)
		{
			ctrlPressed = e.Control;
			shiftPressed = e.Shift;
		}

		private void MapGL_KeyDown(object sender, KeyEventArgs e)
		{
			ctrlPressed = e.Control;
			shiftPressed = e.Shift;

			if (ctrlPressed && !shiftPressed && e.KeyCode == Keys.F)
				ctrlPressed = false;
		}

		private void MapGL_MouseLeave(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(toolTip.ToolTipText))
				toolTip.SetToolTip(this, string.Empty);
			highlightFeature = null;
			highlightFeatures = null;
			this.MouseWheel -= new System.Windows.Forms.MouseEventHandler(this.MapGLMouseWheel);
			ctrlPressed = false;
			shiftPressed = false;
		}

		private void MapGL_MouseEnter(object sender, EventArgs e)
		{
			this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.MapGLMouseWheel);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			bool processed = true;
			if ((msg.Msg == WM_KEYDOWN) || (msg.Msg == WM_SYSKEYDOWN))
			{
				switch (keyData)
				{
					case Keys.Down:
						Pan(Direction.S, 0.125);
						break;
					case Keys.Control | Keys.Down:
						Pan(Direction.S, 0.03125);
						break;
					case Keys.Up:
						Pan(Direction.N, 0.125);
						break;
					case Keys.Control | Keys.Up:
						Pan(Direction.N, 0.03125);
						break;
					case Keys.Left:
						Pan(Direction.W, 0.125);
						break;
					case Keys.Control | Keys.Left:
						Pan(Direction.W, 0.03125);
						break;
					case Keys.Right:
						Pan(Direction.E, 0.125);
						break;
					case Keys.Control | Keys.Right:
						Pan(Direction.E, 0.03125);
						break;
					case Keys.PageUp:
						Zoom(0.5);
						break;
					case Keys.Control | Keys.PageUp:
						Zoom(0.8);
						break;
					case Keys.PageDown:
						Zoom(2.0);
						break;
					case Keys.Control | Keys.PageDown:
						Zoom(1.25);
						break;
					case Keys.Control | Keys.Shift | Keys.F2:
						scrollOptimization = !scrollOptimization;
						break;
					default:
						processed = base.ProcessCmdKey(ref msg, keyData);
						break;
				}
			}
			else
			{
				processed = base.ProcessCmdKey(ref msg, keyData);
			}
			return processed;
		}

		private void MapGLMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			mouseDownX = e.X;
			mouseDownY = e.Y;
			mouseMoveX = e.X;
			mouseMoveY = e.Y;
			if (!string.IsNullOrEmpty(toolTip.ToolTipText))
				toolTip.SetToolTip(this, string.Empty);
		}

		private void MapGLMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			mouseUpX = e.X;
			mouseUpY = e.Y;
		}

		private void MapGLMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Delta == 0 || e.X < 0 || e.X > boundingBox.Window.width || e.Y < 0 || e.Y > boundingBox.Window.height)
				return;

			if (ctrlPressed && shiftPressed)
				return;

			if (ctrlPressed)
			{
				if (e.Delta > 0)
					Pan(WSIMap.MapGL.Direction.N, .125);
				else
					Pan(WSIMap.MapGL.Direction.S, .125);
			}
			else if (shiftPressed)
			{
				if (e.Delta > 0)
					Pan(WSIMap.MapGL.Direction.E, .03125);
				else
					Pan(WSIMap.MapGL.Direction.W, .03125);
			}
			else
			{
				if (e.Delta > 0)
					ScrollMap(e.X, e.Y, 1.25);
				else
					ScrollMap(e.X, e.Y, 0.8);
			}
		}

		private void MapGLMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			Feature.ConfirmMainThread("WSIMap MapGLMouseMove()");
#endif

            // Draw a tracking rectangle from the mouse down position to the position in e
            if (trackingRectangle)
			{
				trackingRectP1 = ToOpenGLPoint(mouseDownX, mouseDownY);
				trackingRectP2 = ToOpenGLPoint(e.X, e.Y);

				// Redraw the map
				Refresh();
			}
			else if (panning)
			{
				try
				{
                    // Calculate the distance to pan and translate the map
					scrollWinX = e.X - mouseMoveX;
					scrollWinY = e.Y - mouseMoveY;

                    double scrollX = scrollWinX * GetMapWidthDeg() / Width;
					double scrollY = scrollWinY * GetMapHeightDeg() / Height;
					mouseMoveX = e.X;
					mouseMoveY = e.Y;

					if (scrollWinY > 0)
					{
						// avoid scrolling off top of map
						double northEdge = GetMapTopDeg();
						if (northEdge + scrollY > northExtent)
						{
							scrollWinY = (int)((northExtent - northEdge) * Height / GetMapHeightDeg());
							scrollY = scrollWinY * GetMapHeightDeg() / Height;
                        }
					}
					else if (scrollWinY < 0)
					{
						// avoid scrolling off bottom of map
						double southEdge = GetMapBottomDeg();
						if (southEdge + scrollY < southExtent)
						{
							scrollWinY = (int)((southExtent - southEdge) * Height / GetMapHeightDeg());
							scrollY = scrollWinY * GetMapHeightDeg() / Height;
                        }
					}

                    WinXMove = WinXMove + scrollWinX;
                    WinYMove = WinYMove + scrollWinY;

                    Gl.glTranslated(scrollX, -scrollY, 0.0);

					// synchronous map paint
					Refresh();

				}
				catch { }
				finally
				{
					// reset scrolling variables so paints will not do scroll optimization
					scrollWinX = int.MinValue;
					scrollWinY = int.MinValue;
				}
			}
			else if (useToolTips)
			{
				if (!this.Focused)
				{
					if (!string.IsNullOrEmpty(toolTip.ToolTipText))
						toolTip.SetToolTip(this, string.Empty);
					return;
				}

				// Find features at the mouse's location
				PointD mousePoint = ToMapPoint(e.X, e.Y);
				FeatureCollection features = GetFeaturesOverMouse(mousePoint);

				// If we found a feature and the tooltip is on, display the tooltip
				if (features.Count > 0)
				{
					string tti = features.GetToolTipInfo();

					if (toolTip.ToolTipText != tti)
					{
						toolTip.SetToolTip(this, features, tti);
						highlightFeature = features[0];
						highlightFeatures = features;
					}
				}
				else
				{
					if (!string.IsNullOrEmpty(toolTip.ToolTipText))
						toolTip.SetToolTip(this, string.Empty);
					highlightFeature = null;
					highlightFeatures = null;
				}
			}
		}
		#endregion

    }

	public delegate void MapRectangleChangedEventHandler(object sender, MapRectangleChangedEventArgs e);

	public class MapRectangleChangedEventArgs : EventArgs
	{
		private RectangleD mapRect;

		public MapRectangleChangedEventArgs(RectangleD mapRect)
		{
			this.mapRect = mapRect;
		}

		public RectangleD MapRect
		{
			get { return mapRect; }
		}
	}
}

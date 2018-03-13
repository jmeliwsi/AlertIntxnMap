using System;
using System.Drawing;
using Tao.OpenGl;
using FUL;

namespace WSIMap
{
	/**
	 * \class Label
	 * \brief Represents a label on the map
	 */
	public class Label : Feature, IMapPoint, IProjectable
	{
		#region Data Members
		public enum AlignmentType { Left, Center, Right };
		protected double x;
		protected double y;
		protected Color color;
		protected string text;
		protected double xOffset;
		protected double yOffset;
        protected Font font;
		protected bool highlight;
		protected Color highlightColor;
        protected uint highlightOpacity;
        protected uint highlightBorderWidth;
        protected Color highlightBorderColor;
        protected uint declutterLineWidth;
        protected bool fastDraw;
        protected bool useFontColor;
        protected const double f1 = 1.4, f2 = 1.40, f3 = f2 - 1;
		protected double width;
		protected double height;
		protected Color leaderLineColor;
		protected ushort fontSize;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		protected AlignmentType alignment;
		#endregion

        public double? WinX { get; set; }
        public double? WinY { get; set; }

		#region Props for label dragging

		public enum PositionType { Default = 0, Fixed = 1, Dynamic = 2 };
        
        // Cartesian-like quadrants with origin located in the centre of the window
        private enum Quadrant { TopRight = 1, TopLeft, BottomLeft, BottomRight }

        private class Coordinates
        {
            public Coordinates()
            {
            }

            public Coordinates(double x, double y)
            {
                X = x;
                Y = y;
            }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public bool IsMovedByUser { get; set; }
        public bool IsMovingByUser { get; set; }
		public PositionType Position { get; set; }
		
        // Flight coords, if set
        public double? FlightX { get; set; }
        public double? FlightY { get; set; }

        // I want to store coords of a bounding rectangle
        private double RectTop { get; set; }
        private double RectRight { get; set; }
        private double RectBottom { get; set; }
        private double RectLeft { get; set; }

        public double DragOffsetX { get; set; }
        public double DragOffsetY { get; set; }

        private double FixedX { get; set; }
        private double FixedY { get; set; }

        private double? QuadrX { get; set; }
        private double? QuadrY { get; set; }

        private Quadrant CurrentQuadrant { get; set; }

        #endregion

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

		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public uint Size
        {
            get { return font.PointSize; }
        }
        
        public string Text
		{
			get { return text; }
			set { 
				if (value != text)
				{
					width = double.MinValue; 
					height = double.MinValue; 
				}
				text = value; 
			}
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

		public bool Highlight
		{
			get { return highlight; }
			set { highlight = value; }
		}

        public Color HighlightColor
        {
            get { return highlightColor; }
            set { highlightColor = value; }
        }

		public Color LeaderLineColor
		{
			get { return leaderLineColor; }
			set { leaderLineColor = value; }
		}

        public bool LeaderLine
		{
            get; set;
		}

        public uint HighlightOpacity
        {
            get { return highlightOpacity; }
            set
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;
                highlightOpacity = value;
            }
        }

        public uint HighlightBorderWidth
        {
            get { return highlightBorderWidth; }
            set { highlightBorderWidth = value; }
        }

        public Color HighlightBorderColor
        {
            get { return highlightBorderColor; }
            set { highlightBorderColor = value; }
        }

        public uint DeclutterLineWidth
        {
            get { return declutterLineWidth; }
            set { declutterLineWidth = value; }
        }

        public bool UseFontColor
        {
            get { return useFontColor; }
            set { useFontColor = value; }
        }

		public AlignmentType Alignment
		{
			get { return alignment; }
			set { alignment = value; }
		}

		public double Width
		{
			get
			{
				if (width <= 0.0)
					computeWidthAndHeight();
				return width;
			}
		}

		public double Height
		{
			get
			{
				if (height <= 0.0)
					computeWidthAndHeight();
				return height;
			}
		}

		internal override RectangleD GetBoundingRect(MapGL parentMap)
		{
			try
			{
				// This Label must have a parent map for this to work
                if (parentMap == null)
					return null;
				
				// Make sure the scale factors are non-zero
                if (parentMap.ScaleX == 0 || parentMap.ScaleY == 0)
					return null;

                if(Position == PositionType.Fixed)
                {
                    var coords = GetFixedCoordsWin(parentMap);
                    var pgl = parentMap.ToOpenGLPoint((int)coords.X, (int)coords.Y);
                    pgl.X = MapGL.NormalizeLongitude(pgl.X);
                    return new RectangleD(pgl.Y, pgl.Y + this.Height / parentMap.ScaleY, pgl.X, pgl.X + this.Width / parentMap.ScaleX);
                }

                if (WinX.HasValue && WinY.HasValue)
                {
                    var pgl = parentMap.panning && Position != PositionType.Dynamic
                        ? parentMap.ToOpenGLPoint((int)(WinX.Value + parentMap.WinXMove.GetValueOrDefault(0)), (int)(WinY.Value + parentMap.WinYMove.GetValueOrDefault(0)))
                        : parentMap.ToOpenGLPoint((int)(WinX.Value), (int)(WinY.Value));
                    pgl.X = MapGL.NormalizeLongitude(pgl.X);
                    return new RectangleD(pgl.Y, pgl.Y + this.Height / parentMap.ScaleY, pgl.X, pgl.X + this.Width / parentMap.ScaleX);
                }

                var initialRect = new RectangleD(this.Latitude, this.Latitude + this.Height / parentMap.ScaleY, this.Longitude, this.Longitude + this.Width / parentMap.ScaleX);
                return initialRect;

            }
			catch
			{
				return null;
			}
		}

        public RectangleD GetBoundingRectWin(MapGL parentMap)
        {
            try
            {
                // This Label must have a parent map for this to work
                if (parentMap == null)
                    return null;

                // Make sure the scale factors are non-zero
                if (parentMap.ScaleX == 0 || parentMap.ScaleY == 0)
                    return null;

                double px, py;
                Projection.ProjectPoint(parentMap.MapProjection, Longitude, Latitude, parentMap.CentralLongitude, out px, out py);
                px = parentMap.DenormalizeLongitude(px);
                var currentPosition = parentMap.ToWinPoint(px, py);
                return new RectangleD(currentPosition.Y, currentPosition.Y - Height, currentPosition.X, currentPosition.X + Width);
            }
            catch
            {
                return null;
            }
        }

        public PointD ToMapPointFromWin(MapGL parentMap, double x, double y)
        {
            var gl = parentMap.ToOpenGLPoint((int)x, (int)y);
            double px, py;
            Projection.UnprojectPoint(MapProjection, gl.X, gl.Y, centralLongitude, out px, out py);
            return new PointD(px, py);
        }

        public bool FastDraw
        {
            get { return fastDraw; }
            set { fastDraw = value; }
        }

		public Label(Font font, string text, Color color, PointD location)
            : this(font, text, color, location.Latitude, location.Longitude, 0, 0, false, SystemColors.Info, string.Empty, string.Empty)
		{
		}

        public Label(Font font, string text, Color color, double latitude, double longitude)
            : this(font, text, color, latitude, longitude, 0, 0, false, SystemColors.Info, string.Empty, string.Empty)
		{
		}

        public Label(Font font, string text, Color color, PointD location, double xOffset, double yOffset)
            : this(font, text, color, location.Latitude, location.Longitude, xOffset, yOffset, false, SystemColors.Info, string.Empty, string.Empty)
		{
		}

        public Label(Font font, string text, Color color, double latitude, double longitude, double xOffset, double yOffset)
            : this(font, text, color, latitude, longitude, xOffset, yOffset, false, SystemColors.Info, string.Empty, string.Empty)
		{
		}

		public Label(Font font, string text, Color color, PointD location, double xOffset, double yOffset, bool highlight, Color highlightColor)
            : this(font, text, color, location.Latitude, location.Longitude, xOffset, yOffset, highlight, highlightColor, string.Empty, string.Empty)
		{
		}

		public Label(Font font, string text, Color color, double latitude, double longitude, double xOffset, double yOffset, bool highlight, Color highlightColor)
            : this(font, text, color, latitude, longitude, xOffset, yOffset, highlight, highlightColor, string.Empty, string.Empty)
		{
		}

		public Label(Font font, string text, Color color, PointD location, double xOffset, double yOffset, bool highlight, Color highlightColor, string featureName, string featureInfo)
            : this(font, text, color, location.Latitude, location.Longitude, xOffset, yOffset, highlight, highlightColor, featureName, featureInfo)
		{
		}

		public Label(Font font, string text, Color color, double latitude, double longitude, double xOffset, double yOffset, bool highlight, Color highlightColor, string featureName, string featureInfo)
		{
			this.font = font;
			this.text = text;
			this.Latitude = latitude;
			this.Longitude = longitude;
			this.color = color;
			this.xOffset = xOffset;
			this.yOffset = yOffset;
			this.highlight = highlight;
			this.highlightColor = highlightColor;
            this.highlightOpacity = 75;
            this.highlightBorderWidth = 0;
            this.highlightBorderColor = Color.Black;
            this.declutterLineWidth = 1;
			this.featureName = featureName;
			this.featureInfo = featureInfo;
            this.fastDraw = false;
            this.useFontColor = false;
			this.width = double.MinValue;
			this.height = double.MinValue;
			this.fontSize = font.PointSize;
			this.mapProjection = MapProjections.CylindricalEquidistant;
			this.alignment = AlignmentType.Right;
		}

        public Label(Label label) : this(label.font, label.text, label.color, label.Latitude, label.Longitude, label.xOffset, label.yOffset, label.highlight, label.highlightColor, label.featureName, label.featureInfo)
        {
            // Copy dragged label state.
            Position = label.Position;
            IsMovedByUser = label.IsMovedByUser;
            IsMovingByUser = label.IsMovingByUser;
            CurrentQuadrant = label.CurrentQuadrant;
            DragOffsetX = label.DragOffsetX;
            DragOffsetY = label.DragOffsetY;
            RectBottom = label.RectBottom;
            RectLeft = label.RectLeft;
            RectRight = label.RectRight;
            RectTop = label.RectTop;
            QuadrX = label.QuadrX;
            QuadrY = label.QuadrY;
            FlightX = label.FlightX;
            FlightY = label.FlightY;
        }

		public double DistanceTo(IMapPoint p, bool kilometers)
		{
			if (kilometers)
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.km);
			else
				return Utils.Distance(this.Y, this.X, p.Y, p.X, Utils.DistanceUnits.mi);
		}

        public bool PointIsInsideLabel(MapGL map, Layer l, int x, int y)
        {
            if(WinX.HasValue && WinY.HasValue)
            {
                var xIsOk1 = x >= WinX.Value && x <= WinX.Value + Width;
                var yIsOk1 = y <= WinY.Value && y >= WinY.Value - Height;
                return xIsOk1 && yIsOk1;
            }
            var topRight = map.ToWinPointFromMap(RectRight, RectTop);
            var bottomRight = map.ToWinPointFromMap(RectRight, RectBottom);
            var bottomLeft = map.ToWinPointFromMap(RectLeft, RectBottom);
            var topLeft = map.ToWinPointFromMap(RectLeft, RectTop);
            var xIsOk = x >= topLeft.X && x <= topRight.X;
            var yIsOk = y <= bottomLeft.Y && y >= topLeft.Y;
            return xIsOk && yIsOk;
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Label Draw()");
#endif

            // Was this Label filtered out by the declutter algorithm?
            if (!this.draw) return;

            // Set the map projection
            this.mapProjection = parentMap.MapProjection;
            this.centralLongitude = parentMap.CentralLongitude;

            if (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal && Position != PositionType.Fixed && (FlightY < Projection.MinAzimuthalLatitude || y < Projection.MinAzimuthalLatitude))
                return;

            // Don't draw the label if the font wasn't properly initialized
            if (!font.Initialized) return;

            if (font.PointSize != this.fontSize)
            {
                fontSize = font.PointSize;
                width = double.MinValue;
                height = double.MinValue;
            }

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Set the matrix mode
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            if (IsMovedByUser && Position == PositionType.Fixed)
            {
                DrawFixed(parentMap, parentLayer);
                return;
            }

            double _x = x;
            double _y = y;

            // Calculate the rendering scale factors
            float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

            double px, py;
            _x += this.xOffset / xFactor;
            _y += this.yOffset / yFactor;
            Projection.ProjectPoint(mapProjection, _x, _y, centralLongitude, out px, out py);

            if (IsMovedByUser && !IsMovingByUser && Position == PositionType.Dynamic && FlightX.HasValue && FlightY.HasValue)
            {
                var p = parentMap.ToWinPointFromMap(FlightX.Value, FlightY.Value);
                WinX = p.X + this.xOffset;
                WinY = p.Y + this.yOffset;
            }

            // Draw the text
            Gl.glPushAttrib(Gl.GL_LIST_BIT);
            Gl.glListBase(font.OpenGLDisplayListBase);
            string[] _text = text.Split('\n');
            lock (_text)
            {
                var ax = px;

                using (var highlightRect = GetBoundingRect(parentMap))
                {
                    if (highlightRect != null)
                    {
                        if (!WinX.HasValue || !WinY.HasValue)
                        {
                            ax = AlignXPos(px, highlightRect);

                            highlightRect.MoveLowerLeftTo(new PointD(ax, py));
                        }
                        RectTop = highlightRect.Top;
                        RectRight = highlightRect.Right;
                        RectBottom = highlightRect.Bottom;
                        RectLeft = highlightRect.Left;
                    }

                    // Draw a background rectangle
                    if (!fastDraw)
                    {
                        if (highlight)
                        {
                            DrawHighlightRect(parentMap, parentLayer, highlightRect);
                        }

                        DrawLine(parentMap, parentLayer, highlightRect);
                    }

                    // Project the position
                    if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                    {
                        _x = parentMap.DenormalizeLongitude(x);
                        Projection.ProjectPoint(mapProjection, _x, y, centralLongitude, out px, out py);
                        px += this.xOffset / xFactor;
                        py += this.yOffset / yFactor;
                        ax = AlignXPos(px, GetBoundingRect(parentMap));
                    }
                }

                SetTextColor();
                if (WinX.HasValue && WinY.HasValue)
                {
                    PointD openglCoords;

                    if (parentMap.panning && Position != PositionType.Dynamic)
                    {
                        openglCoords = parentMap.ToOpenGLPoint((int)(WinX.Value + parentMap.WinXMove.GetValueOrDefault(0)), (int)(WinY.Value + parentMap.WinYMove.GetValueOrDefault(0)));
                    }
                    else
                    {
                        openglCoords = parentMap.ToOpenGLPoint((int)WinX.Value, (int)WinY.Value);
                    }
                    openglCoords.X = parentMap.DenormalizeLongitude(openglCoords.X);
                    openglCoords.X = AlignXPos(openglCoords.X, GetBoundingRect(parentMap));
                    for (int i = _text.Length - 1; i >= 0; i--)
                    {
                        SetRasterPosFixed(openglCoords.X, openglCoords.Y);

                        Gl.glCallLists(_text[i].Length, Gl.GL_UNSIGNED_BYTE, _text[i]);
                        openglCoords.Y += ((font.PointSize / f1) / yFactor) * f2; // f2 creates line spacing
                    }
                }

                else
                {
                    // Draw the text
                    for (int i = _text.Length - 1; i >= 0; i--)
                    {
                        if (fastDraw)
                            Gl.glRasterPos3d(ax, py, 0.0);
                        else
                            SetRasterPos(ax, py);

                        Gl.glCallLists(_text[i].Length, Gl.GL_UNSIGNED_BYTE, _text[i]);
                        py += ((font.PointSize / f1) / yFactor) * f2; // f2 creates line spacing
                    }
                }
            }
            Gl.glPopAttrib();
        }

        private void DrawHighlightRect(MapGL parentMap, Layer parentLayer, RectangleD r)
        {
            if (r == null)
                return;

            if (highlightBorderColor == Color.Empty)
                highlightBorderWidth = 0;
            if (highlightColor == Color.Empty)
                highlightOpacity = 0;
            r.BorderColor = highlightBorderColor;
            r.BorderWidth = highlightBorderWidth;
            r.FillColor = highlightColor;
            r.Opacity = highlightOpacity;
            r.StretchByPixels(4, parentMap);

            r.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);

            r.Draw(parentMap, parentLayer);
        }

        public void SetFixedDragOfset(MapGL parentMap, int winX, int winY)
        {
            if (!QuadrX.HasValue || !QuadrY.HasValue)
            {
                // if we already have win coords (i.e. decluttering is on), use them
                if (WinX.HasValue && WinY.HasValue)
                {
                    FixedX = WinX.Value;
                    FixedY = WinY.Value;
                }
                else
                {
                    // translate from Flight coordinates to Label coordinates. I.e. translate X and Y from Flight symbol to bottom left corner of a label;
                    double px, py;
                    Projection.ProjectPoint(MapProjections.CylindricalEquidistant, RectLeft, RectBottom, centralLongitude, out px, out py);
                    var currentPosition = parentMap.ToWinPoint(px, py);
                    FixedX = currentPosition.X;
                    FixedY = currentPosition.Y;
                }

                UpdateQuadrantCoordsFromFixed(parentMap);
            }
            
            var currentFixedCoords = GetFixedCoordsWin(parentMap);

            DragOffsetX = currentFixedCoords.X - winX;
            DragOffsetY = currentFixedCoords.Y - winY;
        }

        public void SetWinCoords(MapGL map, int x, int y)
        {
            FixedX = x + DragOffsetX;
            FixedY = y + DragOffsetY;
            UpdateQuadrantCoordsFromFixed(map);
        }

        public void ResetPosition()
        {
            DragOffsetX = 0;
            DragOffsetY = 0;

            XOffset = 0;
            YOffset = 0;

            QuadrX = null;
            QuadrY = null;

            WinX = null;
            WinY = null;
        }

        private void DrawFixed(MapGL parentMap, Layer parentLayer)
        {
            // Draw the text
            Gl.glPushAttrib(Gl.GL_LIST_BIT);
            Gl.glListBase(font.OpenGLDisplayListBase);
            string[] _text = text.Split('\n');
            lock (_text)
            {
                using (var highlightRect = GetBoundingRect(parentMap))
                {
                    if (highlightRect != null)
                    {
                        RectTop = highlightRect.Top;
                        RectRight = highlightRect.Right;
                        RectBottom = highlightRect.Bottom;
                        RectLeft = highlightRect.Left;
                    }

                    if (!fastDraw)
                    {
                        if (highlight)
                        {
                            DrawHighlightRect(parentMap, parentLayer, highlightRect);
                        }

                        DrawLine(parentMap, parentLayer, highlightRect);
                    }
                }

                // Set the text color
                SetTextColor();
                // Calculate the rendering scale factors
                var yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

                var winCoords = GetFixedCoordsWin(parentMap, 0, 0);
                var openglCoords = parentMap.ToOpenGLPoint((int)winCoords.X, (int)winCoords.Y);

                // Draw the text
                for (int i = _text.Length - 1; i >= 0; i--)
                {
                    SetRasterPosFixed(openglCoords.X, openglCoords.Y);

                    Gl.glCallLists(_text[i].Length, Gl.GL_UNSIGNED_BYTE, _text[i]);
                    openglCoords.Y += ((font.PointSize / f1) / yFactor) * f2; // f2 creates line spacing
                }
            }
            Gl.glPopAttrib();
        }

        private double AlignXPos(double px, RectangleD boundingRect)
        {
            var result = px;

            if (alignment == AlignmentType.Left)
                result = px - boundingRect.Width;
            else if (alignment == AlignmentType.Center)
                result = px - (boundingRect.Width / 2);

            return result;
        }

        private void SetTextColor()
        {
            if (useFontColor)
                Gl.glColor3d(glc(font.Color.R), glc(font.Color.G), glc(font.Color.B));
            else
                Gl.glColor3d(glc(color.R), glc(color.G), glc(color.B));
        }

        private void DrawLine(MapGL parentMap, Layer parentLayer, RectangleD rect)
        {
            if(rect == null)
                return;

            if (parentMap != null && (parentLayer.Declutter || IsMovedByUser))
            {
                if (leaderLineColor == Color.Empty)
                    leaderLineColor = highlightColor;
                WSIMap.Line line;
                double p1x, p1y;

                if (IsMovedByUser && FlightX.HasValue && FlightY.HasValue)
                {
                    Projection.ProjectPoint(mapProjection, FlightX.Value, FlightY.Value, centralLongitude, out p1x, out p1y);
                }
                else
                {
                    Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out p1x, out p1y);
                }

                var pt = FindIntersection(rect);
                pt.X = MapGL.NormalizeLongitude(pt.X);

                var flightPoint = new PointD(p1x, p1y);
                pt.Refresh(MapProjection, centralLongitude);
                line = new Line(pt, flightPoint, leaderLineColor, declutterLineWidth);
                
                line.InterpolationMethod = Curve.InterpolationMethodType.Linear;
                line.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
                line.Draw(parentMap, parentLayer);
                line.Dispose();


            }
        }

        private PointD GetFixedCoords(MapGL map, double offsetX = 0, double offsetY = 0)
        {
            if(CurrentQuadrant == 0)
            {
                // Quadrant not set
                return new PointD(x, y);
            }

            var coords = TranslateQuadrantsToGlobalCoords(offsetX, offsetY, map.BoundingBox.Window.width, map.BoundingBox.Window.height);
            var p = map.ToMapPoint((int)coords.X, (int)coords.Y);
            return new PointD(p.X, p.Y);
        }

        private Coordinates GetFixedCoordsWin(MapGL map, double offsetX = 0, double offsetY = 0)
        {
            if (CurrentQuadrant == 0)
            {
                // Quadrant not set
                return new Coordinates();
            }

            var coords = TranslateQuadrantsToGlobalCoords(offsetX, offsetY, map.BoundingBox.Window.width, map.BoundingBox.Window.height);
            WinX = coords.X;
            WinY = coords.Y;
            return coords;
        }

        private Coordinates TranslateQuadrantsToGlobalCoords(double offsetX, double offsetY, double windowWidth, double windowHeight)
        {
            double qx, qy;
            switch (CurrentQuadrant)
            {
                case Quadrant.TopRight:
                    qx = windowWidth - QuadrX.Value;
                    qy = windowHeight - QuadrY.Value + offsetY;
                    return new Coordinates(qx, qy);
                case Quadrant.BottomRight:
                    qx = windowWidth - QuadrX.Value;
                    qy = QuadrY.Value + offsetY;
                    return new Coordinates(qx, qy);
                case Quadrant.BottomLeft:
                    qx = QuadrX.Value;
                    qy = QuadrY.Value + offsetY;
                    return new Coordinates(qx, qy);
                case Quadrant.TopLeft:
                    qx = QuadrX.Value;
                    qy = windowHeight - QuadrY.Value + offsetY;
                    return new Coordinates(qx, qy);
            }
            return new Coordinates(0, 0);
        }
                            
        // Use if label was moved by user in Fixed mode
        protected void SetRasterPosFixed(double x, double y)
        {
            double winx, winy, winz;
            double[] modelMatrix = new double[16];
            double[] projMatrix = new double[16];
            int[] viewport = new int[4];
            byte[] bitmap = new byte[1];

            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            viewport[0] = 0;
            viewport[1] = 0;

            Glu.gluProject(x, y, 0.0, modelMatrix, projMatrix, viewport, out winx, out winy, out winz);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glOrtho(viewport[0], viewport[2], viewport[1], viewport[3], 0.0, 1.0);
            Gl.glRasterPos3d(0.0, 0.0, -winz);
            Gl.glBitmap(0, 0, 0, 0, (float)winx, (float)winy, bitmap);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }

        private void UpdateQuadrantCoordsFromFixed(MapGL parentMap)
        {
            var windowWidth = parentMap.BoundingBox.Window.width;
            var windowHeight = parentMap.BoundingBox.Window.height;

            if (FixedX > windowWidth / 2)
            {
                QuadrX = windowWidth - FixedX;
                if (FixedY > windowHeight / 2)
                {
                    CurrentQuadrant = Quadrant.TopRight;
                    QuadrY = windowHeight - FixedY;
                }

                else
                {
                    CurrentQuadrant = Quadrant.BottomRight;
                    QuadrY = FixedY;
                }
            }
            else
            {
                QuadrX = FixedX;
                if (FixedY > windowHeight / 2)
                {
                    CurrentQuadrant = Quadrant.TopLeft;
                    QuadrY = windowHeight - FixedY;
                }
                else
                {
                    CurrentQuadrant = Quadrant.BottomLeft;
                    QuadrY = FixedY;
                }
            }
        }

        // Changes origin from flight symbol to bottom left corner of bounding rect
        private void ChangeCoordinatesOrigin(MapGL parentMap)
        {
            double px, py;
            Projection.ProjectPoint(parentMap.MapProjection, RectLeft, RectBottom, parentMap.CentralLongitude, out px, out py);
            var winPoint = parentMap.ToWinPoint(px, py);

            FixedX = winPoint.X;
            FixedY = winPoint.Y;

            UpdateQuadrantCoordsFromFixed(parentMap);
        }

        protected void SetRasterPos(double newX, double newY)
        {
            double winx, winy, winz;
            double[] modelMatrix = new double[16];
            double[] projMatrix = new double[16];
            int[] viewport = new int[4];
            byte[] bitmap = new byte[1];
            
            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

            viewport[0] = 0;
            viewport[1] = 0;

            Glu.gluProject(newX, newY, 0.0, modelMatrix, projMatrix, viewport, out winx, out winy, out winz);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glOrtho(viewport[0], viewport[2], viewport[1], viewport[3], 0.0, 1.0);
            Gl.glRasterPos3d(0.0, 0.0, -winz);
            Gl.glBitmap(0, 0, 0, 0, (float)winx, (float)winy, bitmap);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }

        protected PointD FindIntersection(RectangleD rect)
        {
            try
            {
                FUL.Coordinate a1, a2, b1, b2, intersection;

                // Line from rect center to label point
                b1.Lat = rect.Center.Latitude;
                b1.Lon = rect.Center.Longitude;
             //   b2.Lat = !FlightY.HasValue ? this.Latitude : FlightY.Value;
             //   b2.Lon = !FlightX.HasValue ? this.Longitude : FlightX.Value;
                b2.Lat = this.Latitude;
                b2.Lon =  this.Longitude;

                if(Position == PositionType.Fixed)
                {
                    double fx, fy;
                    RectangleD rect2;
                    Projection.ProjectRect(mapProjection, centralLongitude, rect, out rect2);

                    Projection.ProjectPoint(mapProjection, b2.Lon, b2.Lat, centralLongitude, out fx, out fy);

                    b1.Lat = rect.Center.Latitude;
                    b1.Lon = rect.Center.Longitude;
                    b2.Lat = fy;// !FlightY.HasValue ? this.Latitude : FlightY.Value;
                    b2.Lon = fx; //!FlightX.HasValue ? this.Longitude : FlightX.Value;

                    // Bottom segment of rect
                    a1.Lat = rect.Bottom;
                    a1.Lon = rect.Right;
                    a2.Lat = rect.Bottom;
                    a2.Lon = rect.Left;

                    if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                        return new PointD(intersection.Lon, intersection.Lat);

                    // Left segment of rect
                    a1.Lat = rect.Bottom;
                    a1.Lon = rect.Left;
                    a2.Lat = rect.Top;
                    a2.Lon = rect.Left;

                    if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                        return new PointD(intersection.Lon, intersection.Lat);

                    // Top segment of rect
                    a1.Lat = rect.Top;
                    a1.Lon = rect.Left;
                    a2.Lat = rect.Top;
                    a2.Lon = rect.Right;

                    if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                        return new PointD(intersection.Lon, intersection.Lat);

                    // Right segment of rect
                    a1.Lat = rect.Top;
                    a1.Lon = rect.Right;
                    a2.Lat = rect.Bottom;
                    a2.Lon = rect.Right;

                    if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                        return new PointD(intersection.Lon, intersection.Lat);

                    return rect.Center;
                }

                // Bottom segment of rect
                a1.Lat = rect.Bottom;
                a1.Lon = rect.Right;
                a2.Lat = rect.Bottom;
                a2.Lon = rect.Left;

                if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                    return new PointD(intersection.Lon, intersection.Lat);

                // Left segment of rect
                a1.Lat = rect.Bottom;
                a1.Lon = rect.Left;
                a2.Lat = rect.Top;
                a2.Lon = rect.Left;

                if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                    return new PointD(intersection.Lon, intersection.Lat);

                // Top segment of rect
                a1.Lat = rect.Top;
                a1.Lon = rect.Left;
                a2.Lat = rect.Top;
                a2.Lon = rect.Right;

                if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                    return new PointD(intersection.Lon, intersection.Lat);

                // Right segment of rect
                a1.Lat = rect.Top;
                a1.Lon = rect.Right;
                a2.Lat = rect.Bottom;
                a2.Lon = rect.Right;

                if (FUL.Utils.LineSegmentsIntersect(a1, a2, b1, b2, true, out intersection))
                    return new PointD(intersection.Lon, intersection.Lat);

                return rect.Center;
            }
            catch
            {
                return rect.Center;
            }
        }

		private void computeWidthAndHeight()
		{
			try
			{
				// Calculate height and width of enclosing rectangle
				width = 0.0;
				double w;
				string[] _text = text.Split('\n');
				lock (_text)
				{
					for (int i = 0; i < _text.Length; i++)
					{
						w = 0;
						foreach (char c in _text[i])
							w += (font.abcf[c].abcfA + font.abcf[c].abcfB + font.abcf[c].abcfC);
						if (w > width)
							width = w;
					}
				}
				// rectangle height = ((scaled font point size) * (number of text lines) * (inter-line spacing factor)) - (inter-line spacing factor for a single line of text)
				height = ((font.PointSize / f1) * _text.Length * f2) - (f3 * (font.PointSize / f1));
			}
			catch
			{
				width = 0.0;
				height = 0.0;
			}
		}
	}
}

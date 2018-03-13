using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
    /**
     * \class FTLabel
     * \brief Represents a label on the map.  Uses FTFont for text rendering.
     */
    public class FTLabel : PointD
    {
        #region Data Members
        protected string text;
        protected double xOffset;
        protected double yOffset;
        protected FTFont font;
        protected uint opacity;
        protected bool highlight;
        protected Color highlightColor;
        protected uint highlightBorderWidth;
        protected uint highlightOpacity;
        protected const double lineSpacingFactor = 1.1;
        #endregion

        public string Text
        {
            get { return text; }
            set { text = value; }
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

        public uint Opacity
        {
            get { return opacity; }
            set
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;
                opacity = value;
            }
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

        public uint HighlightBorderWidth
        {
            get { return highlightBorderWidth; }
            set { highlightBorderWidth = value; }
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

        public override RectangleD BoundingRect
        {
            get
            {
                if (this.container != null && this.container.container != null)
                {
                    double w = 0, h = 0, dx = 0, dy = 0, adj = 0;
                    font.PointSize = (int)size;
                    string[] _text = text.Split('\n');
                    lock (_text)
                    {
                        for (int i = 0; i < _text.Length; i++)
                        {
                            RectangleF bbox = font.GetBoundingBox(_text[i]);
                            double _w = this.container.container.ToMapXDistanceDeg(bbox.Width);
                            double _h = this.container.container.ToMapYDistanceDeg(bbox.Height);
                            dx = Math.Sign(bbox.Left) * this.container.container.ToMapXDistanceDeg(bbox.Left);
                            dy = Math.Sign(bbox.Top) * this.container.container.ToMapYDistanceDeg(bbox.Top);
                            if (_w > w) w = _w;
                            h += (_h * lineSpacingFactor);
                            if (i == 0) adj = _h * 0.25; // adjustment to bottom of rectangle
                        }
                    }

                    return new RectangleD(this.y - adj + dy, this.y + dy + h, this.x + dx, this.x + dx + w);
                }
                else
                    return null;
            }
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, PointD location, double xOffset, double yOffset)
            : this(font, text, color, opacity, pointSize, location.Latitude, location.Longitude, xOffset, yOffset, false, Color.White, string.Empty, string.Empty)
        {
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, double latitude, double longitude, double xOffset, double yOffset)
            : this(font, text, color, opacity, pointSize, latitude, longitude, xOffset, yOffset, false, Color.White, string.Empty, string.Empty)
        {
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, PointD location, double xOffset, double yOffset, bool highlight, Color highlightColor)
            : this(font, text, color, opacity, pointSize, location.Latitude, location.Longitude, xOffset, yOffset, highlight, highlightColor, string.Empty, string.Empty)
        {
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, double latitude, double longitude, double xOffset, double yOffset, bool highlight, Color highlightColor)
            : this(font, text, color, opacity, pointSize, latitude, longitude, xOffset, yOffset, highlight, highlightColor, string.Empty, string.Empty)
        {
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, PointD location, double xOffset, double yOffset, bool highlight, Color highlightColor, string featureName, string featureInfo)
            : this(font, text, color, opacity, pointSize, location.Latitude, location.Longitude, xOffset, yOffset, highlight, highlightColor, featureName, featureInfo)
        {
        }

        public FTLabel(FTFont font, string text, Color color, uint opacity, uint pointSize, double latitude, double longitude, double xOffset, double yOffset, bool highlight, Color highlightColor, string featureName, string featureInfo)
        {
            this.font = font;
            this.text = text;
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.color = color;
            this.Opacity = opacity;
            this.size = pointSize;
            this.highlight = highlight;
            this.highlightColor = highlightColor;
            this.highlightBorderWidth = 1;
            this.HighlightOpacity = 75;
            this.featureName = featureName;
            this.featureInfo = featureInfo;
        }

        public override void Refresh()
        {
        }

        internal override void Create()
        {
        }

        private double GetTextHeight(string s)
        {
            if (this.container != null && this.container.container != null)
            {
                font.PointSize = (int)size;
                RectangleF bbox = font.GetBoundingBox(s);
                return this.container.container.ToMapYDistanceDeg(bbox.Height);
            }
            else
                return 0;
        }

        internal override void Draw(float scaleFactor, BoundingBox box)
        {
            // Was this label filtered out by the declutter algorithm?
            if (!this.draw) return;

            // Some OpenGL initialization
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glShadeModel(Gl.GL_FLAT);

            // Calculate the rendering scale factors
            float xFactor = scaleFactor * ((float)box.Viewport.width / (float)(box.Ortho.right - box.Ortho.left));
            float yFactor = scaleFactor * ((float)box.Viewport.height / (float)(box.Ortho.top - box.Ortho.bottom));

            // Calculate the position of the text
            double _x = x + (xOffset / xFactor);
            double _y = y + (yOffset / yFactor);

            // Draw the text
            string[] _text = text.Split('\n');
            lock (_text)
            {
                // Draw a background rectangle
                if (highlight)
                {
                    RectangleD highlightRect = this.BoundingRect;

                    if (highlightRect != null)
                    {
                        double left = highlightRect.Left + (xOffset / xFactor);
                        double bottom = highlightRect.Bottom + (yOffset / yFactor);
                        highlightRect.MoveLowerLeftTo(new PointD(left, bottom));
                        highlightRect.BorderColor = Color.Black;
                        highlightRect.BorderWidth = highlightBorderWidth;
                        highlightRect.FillColor = highlightColor;
                        highlightRect.Opacity = highlightOpacity;
                        highlightRect.Stretch(1.1);
                        highlightRect.Refresh();
                        if (container != null && container.Declutter)
                        {
                            WSIMap.Line line = new Line(this, highlightRect.Center, highlightColor, 2);
                            line.Refresh();
                            line.Draw(scaleFactor, box);
                            line.Dispose();
                        }
                        highlightRect.Draw(scaleFactor, box);
                        highlightRect.Dispose();
                    }
                }

                // Set the font size
                font.PointSize = (int)size;

                // Set the text color
                Gl.glColor4d(glc(color.R), glc(color.G), glc(color.B), (float)opacity / 100);

                double y0 = _y;
                for (int i = _text.Length - 1; i >= 0; i--)
                {
                    if (i != _text.Length - 1)
                        y0 += (GetTextHeight(_text[i]) * lineSpacingFactor);
                    SetRasterPos(_x, y0);
                    font.Draw(_text[i]);
                }
            }
        }
        
        protected void SetRasterPos(double x, double y)
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
    }
}

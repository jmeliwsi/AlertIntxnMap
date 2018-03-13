using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class RectangleD
	 * \brief Represents a rectangle on the map
	 */
	public class RectangleD : Polygon
	{
		#region Data Members
		protected double bottom;
		protected double top;
		protected double left;
		protected double right;
		#endregion

		protected void CreatePointList()
		{
			pointList.Clear();
			pointList.Add(new PointD(left,top));
			pointList.Add(new PointD(right,top));
			pointList.Add(new PointD(right,bottom));
			pointList.Add(new PointD(left,bottom));
		}

		protected void ModifyPointList()
		{
			pointList[0].X = left; pointList[0].Y = top;
			pointList[1].X = right; pointList[1].Y = top;
			pointList[2].X = right; pointList[2].Y = bottom;
			pointList[3].X = left; pointList[3].Y = bottom;

			// Check for segments that cross the international date line
			int nCrossings = NumDatelineCrossings();

			// If the polygon crosses the date line, adjust the longitudes
			isCrossDateline = nCrossings > 0 ? true : false;

            Updated = true;
		}

		public RectangleD() : base()
		{
			bottom = 0;
			top = 0;
			left = 0;
			right = 0;
			CreatePointList();
		}

		public RectangleD(double rectBottom, double rectTop, double rectLeft, double rectRight) : base()
		{
			bottom = rectBottom;
			top = rectTop;
			left = rectLeft;
			right = rectRight;
			CreatePointList();
		}

		public double Bottom
		{
			get { return bottom; }
			set
			{
				bottom = value;
				ModifyPointList();
			}
		}

		public double Top
		{
			get { return top; }
			set
			{
				top = value;
				ModifyPointList();
			}
		}

		public double Left
		{
			get { return left; }
			set
			{
				left = value;
				ModifyPointList();
			}
		}

		public double Right
		{
			get { return right; }
			set
			{
				right = value;
				ModifyPointList();
			}
		}

		public double Width
		{
			get
            {
                if (left > right)   // the date line intersects the rectangle
                {
                    return (180 - left) + (180 + right);
                }
                else
                    return right-left;
            }
		}

		public double Height
		{
			get { return top-bottom; }
		}

		public PointD Center
		{
			get { return new PointD(left+(Width/2),bottom+(Height/2)); }
		}

		public bool FastIntersect(RectangleD rect)
		{
			return FastIntersect(rect.bottom, rect.top, rect.left, rect.right);
		}

		public bool FastIntersect(double rectBottom, double rectTop, double rectLeft, double rectRight)
		{
			// Assumes rectangles are aligned with x & y axes
			if (rectLeft > this.right)
				return false;
			if (rectRight < this.left)
				return false;
			if (rectBottom > this.top)
				return false;
			if (rectTop < this.bottom)
				return false;

			return true;
		}

		public void MoveLowerLeftTo(double latitude, double longitude)
		{
			double deltaX = longitude - left;
			double deltaY = latitude - bottom;

			left += deltaX;
			right += deltaX;
			bottom += deltaY;
			top += deltaY;

			ModifyPointList();
		}

		public void MoveLowerLeftTo(PointD p)
		{
			MoveLowerLeftTo(p.Latitude,p.Longitude);
		}

        public void Stretch(double factor)
        {
            double deltaX = Width * (1 - factor);
            double deltaY = Height * (1 - factor);

            left = left + (deltaX / 2);
            right = right - (deltaX / 2);
            top = top - (deltaY / 2);
            bottom = bottom + (deltaY / 2);

            ModifyPointList();
        }

        public void Stretch(double xFactor, double yFactor)
        {
            double deltaX = Width * (1 - xFactor);
            double deltaY = Height * (1 - yFactor);

            left = left + (deltaX / 2);
            right = right - (deltaX / 2);
            top = top - (deltaY / 2);
            bottom = bottom + (deltaY / 2);

            ModifyPointList();
        }

        public override string ToString()
        {
            return string.Format("{0,6:##.00}, {1,6:##.00}, {2,7:###.00}, {3,7:###.00}", bottom, top, left, right);
        }

        public new void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			SetMapProjection(mapProjection, centralLongitude);
			if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
				CreateDisplayList();
		}

		private void SetMapProjection(MapProjections mapProjection, short centralLongitude)
		{
			// Only set the map projection if it's changing. This prevents unnecessary regeneration of the display list.
			if (mapProjection != this.mapProjection || centralLongitude != this.centralLongitude)
			{
				this.mapProjection = mapProjection;
				this.centralLongitude = centralLongitude;
				Updated = true;
			}
		}

		internal void StretchByPixels(int nPixels, MapGL parentMap)
        {
            double d;
            if (parentMap != null)
            {
                d = parentMap.ToMapXDistanceDeg(nPixels);
                left = left - (d / 2);
                right = right + (d / 2);
                d = parentMap.ToMapYDistanceDeg(nPixels);
                top = top + (d / 2);
                bottom = bottom - (d / 2);
                ModifyPointList();
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RectangleD Draw()");
#endif

			if (openglDisplayList == -1) return;

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);            
            else
                Gl.glCallList(openglDisplayList);
		}
	}
}

using System;

namespace WSIMap
{
	/**
	 * \class BoundingBox
	 * \brief Describes a map bounding box in geographic and window coordinates
	 */
	public struct BoundingBox
	{
        public _RectType3 Map;
        public _RectType2 Viewport;
        public _RectType1 Ortho;
        public _RectType2 Window;

		public struct _RectType1
		{
			public double left;
			public double right;
			public double bottom;
			public double top;
		}

		public struct _RectType2
		{
			public int x;
			public int y;
			public int width;
			public int height;
		}

        public struct _RectType3
        {
			public double left;
            public double normLeft;
			public double right;
            public double normRight;
			public double bottom;
			public double top;
        }
	}
}

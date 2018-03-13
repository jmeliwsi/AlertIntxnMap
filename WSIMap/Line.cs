using System;
using System.Drawing;
using Tao.OpenGl;
using System.Collections;

namespace WSIMap
{
	/**
	 * \class Line
	 * \brief Represents a line segment on the map
	 */
    [Serializable] public class Line : Curve
	{
		public Line() : this(new PointD(), new PointD(), Color.White, 1)
		{
		}

		public Line(PointD pt1, PointD pt2) : this(pt1, pt2, Color.White, 1)
		{
		}

		public Line(PointD pt1, PointD pt2, Color color) : this(pt1, pt2, color, 1)
		{
		}

		public Line(PointD pt1, PointD pt2, Color color, uint width) : base(new ArrayList(new object[] { pt1, pt2} ), color, width, CurveType.Solid)
		{
		}

		// Hides Curve.Type
		public new CurveType Type
		{
			get { return type; }
			set { type = value; }
		}

		// Hides Curve.Add
		public new int Add(PointD pt)
		{
			return -1;
		}

		// Hides Curve.Remove
		public new void Remove(PointD pt)
		{
			return;
		}

	}
}

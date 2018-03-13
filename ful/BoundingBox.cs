using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
	public class BoundingBox
	{
		private double top;
		public double Top
		{
			get { return top; }
			set { top = value; }
		}

		private double left;
		public double Left
		{
			get { return left; }
			set { left = value; }
		}

		private double bottom;
		public double Bottom
		{
			get { return bottom; }
			set { bottom = value; }
		}

		private double right;
		public double Right
		{
			get { return right; }
			set { right = value; }
		}

		public BoundingBox()
		{

		}

		public BoundingBox(double top, double left, double bottom, double right)
		{
			this.top = top;
			this.left = left;
			this.bottom = bottom;
			this.right = right;
		}
	}
}

using System;

namespace WSIMap
{
	public interface IMapPoint
	{
		double X
		{
			get;
			set;
		}

		double Y
		{
			get;
			set;
		}

		double Latitude
		{
			get;
			set;
		}

		double Longitude
		{
			get;
			set;
		}

		double DistanceTo(IMapPoint p, bool kilometers);
	}
}

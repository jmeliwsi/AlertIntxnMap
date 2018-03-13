using System;

namespace WSIMap
{
	public enum MapProjections { CylindricalEquidistant, Stereographic, Orthographic, Mercator/*, Lambert*/ };

	// Types are from http://egsc.usgs.gov/isb/pubs/MapProjections/projections.html
	public enum MapProjectionTypes { Unknown, Cylindrical, Pseudocylindrical, Azimuthal };
	
	public static class Projection
	{
		public const short DefaultCentralLongitude = -90;
		internal const double MinAzimuthalLatitude = 0;
		private const double deg2rad = FUL.Utils.deg2rad;
		private const double scaleFactor = 90;
		private const double mercatorScaleFactor = 50;
		private const double lambertScaleFactor = 46;
		private const double lambertRefLat = 40;
		private const double lambertStdParallel1 = 33;
		private const double lambertStdParallel2 = 45;

		public static void ProjectPoint(MapProjections mapProjection, double x, double y, double centralLon, out double px, out double py)
		{
			switch (mapProjection)
			{
				case MapProjections.Stereographic:
					Stereographic(x, y, centralLon, out px, out py);
					break;
				case MapProjections.Orthographic:
					Orthographic(x, y, centralLon, out px, out py);
					break;
				case MapProjections.Mercator:
					Mercator(x, y, centralLon, out px, out py);
					break;
				//case MapProjections.Lambert:
				//	Lambert(x, y, centralLon, out px, out py);
				//	break;
				case MapProjections.CylindricalEquidistant:
				default:
					px = x;
					py = y;
					break;
			}
		}

		public static void UnprojectPoint(MapProjections mapProjection, double px, double py, double centralLon, out double x, out double y)
		{
			switch (mapProjection)
			{
				case MapProjections.Stereographic:
					InverseStereographic(px, py, centralLon, out x, out y);
					break;
				case MapProjections.Orthographic:
					InverseOrthographic(px, py, centralLon, out x, out y);
					break;
				case MapProjections.Mercator:
					InverseMercator(px, py, centralLon, out x, out y);
					break;
				//case MapProjections.Lambert:
				//	InverseLambert(px, py, centralLon, out x, out y);
				//	break;
				case MapProjections.CylindricalEquidistant:
				default:
					x = px;
					y = py;
					break;
			}
		}

		public static void ProjectDirection(MapProjections mapProjection, double x, double y, double dir, double centralLon, out double pdir)
		{
			switch (mapProjection)
			{
				case MapProjections.Stereographic:
					pdir = dir - (x - centralLon);
					break;
				case MapProjections.Orthographic:
					pdir = dir - (x - centralLon);
					break;
				case MapProjections.Mercator:
					pdir = dir;
					break;
				//case MapProjections.Lambert:
				//	pdir = dir - ((x - centralLon) / (Math.PI / 2.0));
				//	break;
				case MapProjections.CylindricalEquidistant:
				default:
					pdir = dir;
					break;
			}
		}

		public static void ProjectRect(MapProjections mapProjection, double centralLon, RectangleD rect, out RectangleD prect)
		{
			double b, t, l, r;

			Projection.ProjectPoint(mapProjection, rect.Left, rect.Bottom, centralLon, out l, out b);
			Projection.ProjectPoint(mapProjection, rect.Right, rect.Top, centralLon, out r, out t);

			prect = new RectangleD(b, t, l, r);
		}

		public static void UnprojectRect(MapProjections mapProjection, double centralLon, RectangleD rect, out RectangleD urect)
		{
			double b, t, l, r;

			Projection.UnprojectPoint(mapProjection, rect.Left, rect.Bottom, centralLon, out l, out b);
			Projection.UnprojectPoint(mapProjection, rect.Right, rect.Top, centralLon, out r, out t);

			urect = new RectangleD(b, t, l, r);
		}

		public static MapProjectionTypes GetProjectionType(MapProjections mapProjection)
		{
			MapProjectionTypes type;

			switch (mapProjection)
			{
				case MapProjections.Stereographic:
					type = MapProjectionTypes.Azimuthal;
					break;
				case MapProjections.Orthographic:
					type = MapProjectionTypes.Azimuthal;
					break;
				case MapProjections.Mercator:
					type = MapProjectionTypes.Cylindrical;
					break;
				case MapProjections.CylindricalEquidistant:
					type = MapProjectionTypes.Cylindrical;
					break;
				//case MapProjections.Lambert:
				//	type = MapProjectionTypes.Azimuthal;
				//	break;
				default:
					type = MapProjectionTypes.Unknown;
					break;
			}

			return type;
		}

		private static void InverseStereographic(double px, double py, double centralLon, out double x, out double y)
		{
			// Assumes a central latitude of 90 degrees
			double rho = Math.Sqrt(Math.Pow(px, 2) + Math.Pow(py, 2));
			double c = 2 * Math.Atan2(rho, scaleFactor);
			y = Math.Asin(Math.Cos(c)) / deg2rad;
			x = centralLon + (Math.Atan2(px, -py) / deg2rad);
		}

		private static void Stereographic(double x, double y, double centralLon, out double px, out double py)
		{
			// Assumes a central latitude of 90 degrees
			double k = scaleFactor / (1 + Math.Sin(y * deg2rad));
			px = k * Math.Cos(y * deg2rad) * Math.Sin((x - centralLon) * deg2rad);
			py = -k * Math.Cos(y * deg2rad) * Math.Cos((x - centralLon) * deg2rad);
		}

		private static void InverseOrthographic(double px, double py, double centralLon, out double x, out double y)
		{
			// Assumes a central latitude of 90 degrees
			double rho = Math.Sqrt(Math.Pow(px, 2) + Math.Pow(py, 2));
			double c = Math.Asin(rho / scaleFactor);
			if (double.IsNaN(c)) c = Math.PI / 2;
			y = Math.Asin(Math.Cos(c)) / deg2rad;
			x = centralLon + (Math.Atan2(px, -py) / deg2rad);
		}

		private static void Orthographic(double x, double y, double centralLon, out double px, out double py)
		{
			// Assumes a central latitude of 90 degrees
			px = scaleFactor * Math.Cos(y * deg2rad) * Math.Sin((x - centralLon) * deg2rad);
			py = -scaleFactor * Math.Cos(y * deg2rad) * Math.Cos((x - centralLon) * deg2rad);
		}

		private static void InverseMercator(double px, double py, double centralLon, out double x, out double y)
		{
			x = px;
			y = ((2 * Math.Atan(Math.Pow(Math.E, py / mercatorScaleFactor))) - (Math.PI / 2)) / deg2rad;
		}

		private static void Mercator(double x, double y, double centralLon, out double px, out double py)
		{
			px = x;
			py = mercatorScaleFactor * Math.Log(Math.Tan(y * deg2rad) + (1d / Math.Cos(y * deg2rad)));
		}

		/* Lambert projection methods
		private static void InverseLambert(double px, double py, double centralLon, out double x, out double y)
		{
			double sp1 = lambertStdParallel1 * deg2rad;
			double sp2 = lambertStdParallel2 * deg2rad;

			double n = (Math.Log(Math.Cos(sp1) * (1.0 / Math.Cos(sp2))))
				/ (Math.Log(Math.Tan((0.25 * Math.PI) + (0.5 * sp2)) * (1.0 / Math.Tan((0.25 * Math.PI) + (0.5 * sp1)))));
			double F = (Math.Cos(sp1) * Math.Pow(Math.Tan((0.25 * Math.PI) + (0.5 * sp1)), n)) / n;
			double rho0 = F * Math.Pow((1.0 / Math.Tan((0.25 * Math.PI) + (0.5 * lambertRefLat * deg2rad))), n);
			double rho = Math.Sign(n) * Math.Sqrt(Math.Pow(px, 2) + Math.Pow(rho0 - py, 2));
			double theta = Math.Atan2(px, rho0 - py) / deg2rad;

			y = ((2.0 * Math.Atan(Math.Pow(F / (rho / lambertScaleFactor), 1.0 / n))) - (0.5 * Math.PI)) / deg2rad;
			x = centralLon + (theta / n);

			// Adjust x values to something reasonable in the "gap" of the projection.
			// WARNING: Does not work for centralLon values outside the range of -90 to 90.
			if (centralLon <= 0)
			{
				if (x > 180 || x <= (-180 / n))
					x = 180;
				else if (x < -180 && x > (-180 / n))
					x = -180;
			}
			else
			{
				if (x > 180 && x <= (180 / n))
					x = 180;
				else if (x < -180 || x > (180 / n))
					x = -180;
			}
		}

		private static void Lambert(double x, double y, double centralLon, out double px, out double py)
		{
			double sp1 = lambertStdParallel1 * deg2rad;
			double sp2 = lambertStdParallel2 * deg2rad;

			double n = (Math.Log(Math.Cos(sp1) * (1.0 / Math.Cos(sp2))))
				/ (Math.Log(Math.Tan((0.25 * Math.PI) + (0.5 * sp2)) * (1.0 / Math.Tan((0.25 * Math.PI) + (0.5 * sp1)))));
			double F = (Math.Cos(sp1) * Math.Pow(Math.Tan((0.25 * Math.PI) + (0.5 * sp1)), n)) / n;
			double rho0 = F * Math.Pow((1.0 / Math.Tan((0.25 * Math.PI) + (0.5 * lambertRefLat * deg2rad))), n);
			double rho = F * Math.Pow((1.0 / Math.Tan((0.25 * Math.PI) + (0.5 * y * deg2rad))), n);

			px = lambertScaleFactor * rho * Math.Sin(n * ((x - centralLon) * deg2rad));
			py = rho0 - (lambertScaleFactor * rho * Math.Cos(n * ((x - centralLon) * deg2rad)));
		}
		*/
	}
}

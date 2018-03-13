using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace WSIMap
{
	public class JetStream : MultipartFeature, IProjectable, IRefreshable
	{
		#region Data Members
		private Curve curve;
		private Symbol arrowHead;
		private const int curveWidth = 8;
		private const int arrowHeadSize = 10;
		#endregion

		public JetStream(List<PointD> pointList, Color color, Color outlineColor)
		{
			// Create the curve part of the jet stream
			curve = new Curve(pointList, color, curveWidth, Curve.CurveType.Solid);
			curve.Outlined = true;
			curve.OutlineColor = outlineColor;

			// Create the arrow head
			double dir = CalculateArrowHeadDir(MapProjections.CylindricalEquidistant);
			arrowHead = new Symbol(SymbolType.Triangle, color, arrowHeadSize, pointList.Last().Latitude, pointList.Last().Longitude, dir);
			arrowHead.Outlined = true;
			arrowHead.OutlineColor = outlineColor;

			// Add the features the MultipartFeature
			this.Add(curve);
			this.Add(arrowHead);
		}

		public new void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			arrowHead.Direction = CalculateArrowHeadDir(mapProjection);
			base.Refresh(mapProjection, centralLongitude);
		}

		private double CalculateArrowHeadDir(MapProjections mp)
		{
			try
			{
				int count = curve.PointList.Count;
				double lat1 = curve.PointList[count - 2].Latitude;
				double lat2 = curve.PointList[count - 1].Latitude;
				double lon1 = curve.PointList[count - 2].Longitude;
				double lon2 = curve.PointList[count - 1].Longitude;

				// Calculate the direction between the two points
				double range = 0, direction = 0;
				FUL.Utils.RangeBearing(lat1, lon1, lat2, lon2, FUL.Utils.DistanceUnits.mi, ref range, ref direction);

				// Adjust the direction for the latitude so it "looks right" for the projection
				if (Projection.GetProjectionType(mp) != MapProjectionTypes.Azimuthal)
				{
					double x = Math.Sin(direction * deg2rad);
					double y = Math.Cos(direction * deg2rad);
					x /= Math.Cos(lat2 * deg2rad);
					direction = Math.Atan2(x, y) / deg2rad;
				}

				return direction;
			}
			catch
			{
				return 0;
			}
		}
	}
}

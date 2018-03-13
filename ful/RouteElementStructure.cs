using System;
using System.Collections.Generic;
using System.Text;

namespace FUL
{
	/// <summary>
	/// Route Point Structure
	/// </summary>
	[Serializable]
	public struct RouteElementStruct
	{
		public FUL.Coordinate Location;
		public FUL.NavDataType NavType;
		public string ItemName;
		public string PointName;
		public string AreaCode;

        public string BuildInfoText()
        {
            var sb = new StringBuilder();

            switch (NavType)
            {
                case NavDataType.Jetway_A:
                case NavDataType.Jetway_E:
                case NavDataType.Jetway_N:
                case NavDataType.Point:
                    return sb.Append("Waypoint: ").Append(PointName).ToString();
                case NavDataType.Sid:
                    return sb.Append("SID: ").Append(PointName).ToString();
                case NavDataType.Star:
                    return sb.Append("STAR: ").Append(PointName).ToString();
                case NavDataType.Station:
                    return sb.Append("Station: ").Append(PointName).ToString();

                default:
                    return sb.Append(string.IsNullOrWhiteSpace(PointName) ? ItemName : PointName).ToString();
            }
        }

        /// <summary>
        /// Returns short description.
        /// Be carefull, calling this method will lead to a boxing value type.
        /// </summary>
        /// <returns>Short description.</returns>
        public override string ToString()
        {
            return BuildInfoText();
        }
    }
}

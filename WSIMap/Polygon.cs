using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;
using System.Linq;

namespace WSIMap
{
	public enum PolygonStipplePattern { None, Line, DashedLine, DensityNA, DensityGood, DensityFair, DensityMedium, DensityPoor, DensityNil, LineL2R, LineVert, LineHorz};
	/*  Delta UpperAirCharts PolygonStipplePattern Mapping
	 *		SPARSEDOT		->	DensityFair
	 *		MEDIUMDOT		->	DensityMedium
	 *		DENSEDOT		->	DensityNil
	 *		L2RHASH			->	LineL2R
	 *		R2LHASH			->	Line
	 *		VERTICALHASH	->	LineVert
	 *		HORIZONTALHASH	->	LineHorz
	 */


	/**
	 * \class Polygon
	 * \brief Represents a polygon on the map
	 */
	[Serializable] public class Polygon : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		private const int MAX_INTERPOLATED_POINTS = 500;
		protected List<PointD> pointList;		// list of points set by the user
		protected List<PointD> pointListToDraw;	// user point list plus points added to support azimuthal projections
		protected Color borderColor;
		protected Color fillColor;
		protected Color stippleColor = Color.Transparent;
		protected uint opacity;
		protected uint borderWidth;
		protected bool isCrossDateline;
		private int isSimple;		// -1 means calculation hasn't been done. 0 is non-simple. 1 is simple.
        protected PolygonBorderType borderType;
        protected int stippleFactor;
        protected ushort stipplePattern;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		protected bool cubicSplineFit;
		private bool endpointFix;
		private bool endpointFixApplied;
		private PolygonStipplePattern fillPattern = PolygonStipplePattern.None;

		#region Polygon stipple patterns
		private static byte[] linePattern = {
									0x08, 0x08, 0x08, 0x08,
									0x04, 0x04, 0x04, 0x04,
									0x02, 0x02, 0x02, 0x02,
									0x01, 0x01, 0x01, 0x01,
									0x80, 0x80, 0x80, 0x80,
									0x40, 0x40, 0x40, 0x40,
									0x20, 0x20, 0x20, 0x20,
									0x10, 0x10, 0x10, 0x10,
									0x08, 0x08, 0x08, 0x08,
									0x04, 0x04, 0x04, 0x04,
									0x02, 0x02, 0x02, 0x02,
									0x01, 0x01, 0x01, 0x01,
									0x80, 0x80, 0x80, 0x80,
									0x40, 0x40, 0x40, 0x40,
									0x20, 0x20, 0x20, 0x20,
									0x10, 0x10, 0x10, 0x10,
									0x08, 0x08, 0x08, 0x08,
									0x04, 0x04, 0x04, 0x04,
									0x02, 0x02, 0x02, 0x02,
									0x01, 0x01, 0x01, 0x01,
									0x80, 0x80, 0x80, 0x80,
									0x40, 0x40, 0x40, 0x40,
									0x20, 0x20, 0x20, 0x20,
									0x10, 0x10, 0x10, 0x10,
									0x08, 0x08, 0x08, 0x08,
									0x04, 0x04, 0x04, 0x04,
									0x02, 0x02, 0x02, 0x02,
									0x01, 0x01, 0x01, 0x01,
									0x80, 0x80, 0x80, 0x80,
									0x40, 0x40, 0x40, 0x40,
									0x20, 0x20, 0x20, 0x20,
									0x10, 0x10, 0x10, 0x10
								};
		private static byte[] lineL2RPattern = {
									0x10, 0x10, 0x10, 0x10,
									0x20, 0x20, 0x20, 0x20,
									0x40, 0x40, 0x40, 0x40,
									0x80, 0x80, 0x80, 0x80,
									0x01, 0x01, 0x01, 0x01,
									0x02, 0x02, 0x02, 0x02,
									0x04, 0x04, 0x04, 0x04,
									0x08, 0x08, 0x08, 0x08,
									0x10, 0x10, 0x10, 0x10,
									0x20, 0x20, 0x20, 0x20,
									0x40, 0x40, 0x40, 0x40,
									0x80, 0x80, 0x80, 0x80,
									0x01, 0x01, 0x01, 0x01,
									0x02, 0x02, 0x02, 0x02,
									0x04, 0x04, 0x04, 0x04,
									0x08, 0x08, 0x08, 0x08,
									0x10, 0x10, 0x10, 0x10,
									0x20, 0x20, 0x20, 0x20,
									0x40, 0x40, 0x40, 0x40,
									0x80, 0x80, 0x80, 0x80,
									0x01, 0x01, 0x01, 0x01,
									0x02, 0x02, 0x02, 0x02,
									0x04, 0x04, 0x04, 0x04,
									0x08, 0x08, 0x08, 0x08,
									0x10, 0x10, 0x10, 0x10,
									0x20, 0x20, 0x20, 0x20,
									0x40, 0x40, 0x40, 0x40,
									0x80, 0x80, 0x80, 0x80,
									0x01, 0x01, 0x01, 0x01,
									0x02, 0x02, 0x02, 0x02,
									0x04, 0x04, 0x04, 0x04,
									0x08, 0x08, 0x08, 0x08	
								};
		private static byte[] lineVertPattern = {
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01,
									0x01, 0x01, 0x01, 0x01
								};
		private static byte[] lineHorzPattern = {
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0xFF, 0xFF, 0xFF, 0xFF,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0xFF, 0xFF, 0xFF, 0xFF,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0xFF, 0xFF, 0xFF, 0xFF,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0xFF, 0xFF, 0xFF, 0xFF
								};
		private static byte[] dashedLinePattern = {
									0x00, 0x80, 0x00, 0x80,
									0x00, 0x40, 0x00, 0x40,
									0x00, 0x20, 0x00, 0x20,
									0x00, 0x10, 0x00, 0x10,
									0x00, 0x08, 0x00, 0x08,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x80, 0x00, 0x80, 0x00,
									0x40, 0x00, 0x40, 0x00,
									0x20, 0x00, 0x20, 0x00,
									0x10, 0x00, 0x10, 0x00,
									0x08, 0x00, 0x08, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x80, 0x00, 0x80,
									0x00, 0x40, 0x00, 0x40,
									0x00, 0x20, 0x00, 0x20,
									0x00, 0x10, 0x00, 0x10,
									0x00, 0x08, 0x00, 0x08,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x80, 0x00, 0x80, 0x00,
									0x40, 0x00, 0x40, 0x00,
									0x20, 0x00, 0x20, 0x00,
									0x10, 0x00, 0x10, 0x00,
									0x08, 0x00, 0x08, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00,
									0x00, 0x00, 0x00, 0x00
							   };
        private static byte[] DensityNAPattern =  {
                                    0x00, 0x00, 0x00, 0x01, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0xC0, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x80, 0x00, 0x00, 0x00
                                };
        private static byte[] DensityGoodPattern =  {
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x03, 0x00, 0x03, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x03, 0x00, 0x03, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00
                                };
        private static byte[] DensityFairPattern =  {
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x03, 0x00, 0x03, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0xC0, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x03, 0x00, 0x03, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00
                                };
        private static byte[] DensityMediumPattern =  {
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x18, 0x00, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x00, 0x18, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x18, 0x00, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x00, 0x18, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x18, 0x00, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x00, 0x18, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x18, 0x00, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x00, 0x18, 0x00
                                };
        private static byte[] DensityPoorPattern =  {
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18
                                };
        private static byte[] DensityNilPattern =  {
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00, 
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x18, 0x18, 0x18, 0x18
                                };

		#endregion
		private const string TRACKING_CONTEXT = "Polygon";
		#endregion

		[DllImport("tessellate.dll", EntryPoint = "TessellatePolygon", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void TessellatePolygon(double[] x, double[] y, int nPoints, int fillColorR, int fillColorG, int fillColorB, int opacity, bool isSimple, MapProjections mapProjection, double centralLongitude);

		public enum PolygonBorderType { Solid, LongDashed, ShortDashed, Dotted, DashDot, Custom };

        public Polygon() : this(new List<PointD>(), Color.White, 1, Color.Black, 0)
		{
		}

		public Polygon(Color borderColor, uint borderWidth, Color fillColor, uint opacity) : this(new List<PointD>(), borderColor, borderWidth, fillColor, opacity)
		{
		}

        public Polygon(ArrayList pointList, Color borderColor, uint borderWidth, Color fillColor, uint opacity)
			: this(new List<PointD>(pointList.Cast<PointD>()), borderColor, borderWidth, fillColor, opacity)
		{
		}

		public Polygon(List<PointD> pointList, Color borderColor, uint borderWidth, Color fillColor, uint opacity)
		{
			pointListToDraw = new List<PointD>();
            PointList = pointList;
			this.borderColor = borderColor;
			this.borderWidth = borderWidth;
			this.fillColor = fillColor;
			this.opacity = opacity;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
            this.borderType = PolygonBorderType.Solid;
            this.stippleFactor = 1;
            this.stipplePattern = 0xFFFF;
			this.isCrossDateline = NumDatelineCrossings() > 0 ? true : false;
			this.endpointFix = false;
			this.endpointFixApplied = false;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public Polygon(List<PointD> pointList, Color borderColor, uint borderWidth, Color fillColor, Color stippleColor, uint opacity)
		{
			pointListToDraw = new List<PointD>();
			PointList = pointList;
			this.borderColor = borderColor;
			this.borderWidth = borderWidth;
			this.fillColor = fillColor;
			this.stippleColor = stippleColor;
			this.opacity = opacity;
			this.featureInfo = string.Empty;
			this.featureName = string.Empty;
			this.borderType = PolygonBorderType.Solid;
			this.stippleFactor = 1;
			this.stipplePattern = 0xFFFF;
			this.isCrossDateline = NumDatelineCrossings() > 0 ? true : false;
			this.endpointFix = false;
			this.endpointFixApplied = false;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public int Count
		{
			get { return pointList.Count; }
		}

		public PointD this[int index]
		{
			get
			{
				if (index < 0 || index >= pointList.Count)
					return null;
				else
					return (PointD)pointList[index];
			}
		}

		public List<PointD> PointList
		{
			get { return pointList; }
            set
            {
                pointList = new List<PointD>(value);
                Updated = true;

				// Check for segments that cross the international date line
				int nCrossings = NumDatelineCrossings();

				// If the polygon crosses the date line, adjust the longitudes
				isCrossDateline = nCrossings > 0 ? true : false;
            }
		}

		public Color BorderColor
		{
			get { return borderColor; }
            set { borderColor = value; Updated = true; }
		}

		public uint BorderWidth
		{
			get { return borderWidth; }
            set { borderWidth = value; Updated = true; }
		}

		public Color FillColor
		{
			get { return fillColor; }
            set { fillColor = value; Updated = true; }
		}

		public Color StippleColor
		{
			get { return stippleColor; }
			set { stippleColor = value; Updated = true; }
		}

		public uint Opacity
		{
			get { return opacity; }
			set
			{
				if (value < 0) value = 0;
				if (value > 100) value = 100;
				opacity = value;
                Updated = true;
			}
		}

		public bool CubicSplineFit
		{
			get { return cubicSplineFit; }
			set { cubicSplineFit = value; }
		}

		public bool EndpointFix
		{
			get { return endpointFix; }
			set { endpointFix = value; }
		}

		public PolygonBorderType BorderType
        {
            get { return borderType; }
            set { borderType = value; Updated = true; }
        }

        public int StippleFactor
        {
            get { return stippleFactor; }
            set { stippleFactor = value; Updated = true; }
        }

        public ushort StipplePattern
        {
            get { return stipplePattern; }
            set { stipplePattern = value; Updated = true; }
        }

		public PolygonStipplePattern FillPattern
		{
			set { fillPattern = value; Updated = true; }
		}

        public int Add(PointD pt)
		{
			isSimple = -1;
            Updated = true;
			pointList.Add(pt);
			int index = pointList.Count - 1;
			if (!isCrossDateline && index > 0)
			{
				PointD temp1, temp2;

				if (Curve.CrossesIDL(pointList[index - 1], pointList[index], out temp1, out temp2))
					isCrossDateline = true;
			}

            return index; // return index of added point for backward compatibility
		}

		public void Remove(PointD pt)
		{
			isSimple = -1;
            Updated = true;
			pointList.Remove(pt);

			// Check for segments that cross the international date line
			int nCrossings = NumDatelineCrossings();

			// If the polygon crosses the date line, adjust the longitudes
			isCrossDateline = nCrossings > 0 ? true : false;
		}

        protected bool IsSimple(bool forceCheck)
        {
            if (Count < 4)
                return true;	// Don't even worry about isSimple at this point.

            if (forceCheck)
                isSimple = -1;

            if (isSimple >= 0)
                return isSimple == 1;

            int maxIndex = (this[0].Latitude == this[Count - 1].Latitude && this[0].Longitude == this[Count - 1].Longitude) ? Count - 1 : Count;

            // We need to calculate isSimple.
            for (int i = 0; i < maxIndex - 2; i++)
                for (int j = i + 2; j < maxIndex; j++)
                {
                    int ix4 = (j == maxIndex - 1) ? 0 : j + 1;

                    if (i == 0 && ix4 == 0)
                        continue;

                    if (Intersects(this[i], this[i + 1], this[j], this[ix4]))
                    {
                        isSimple = 0;
                        return false;
                    }
                }

            isSimple = 1;
            return true;
        }

		/// <summary>
		/// This method determines the intersection of 2 line segments.
		/// The algorithm is based on the 2d line intersection method from "comp.graphics.algorithms
		/// </summary>
		/// <param name="LineSegmentA_Point1">Line Segment 1 (1st point)</param>
		/// <param name="LineSegmentA_Point2">Line Segment 1 (2nd point)</param>
		/// <param name="LineSegmentB_Point1">Line Segment 2 (1st point)</param>
		/// <param name="LineSegmentB_Point2">Line Segment 2 (2nd point)</param>
		/// <returns>Result of intersection</returns>
		public static bool Intersects(PointD LineSegmentA_Point1, PointD LineSegmentA_Point2, PointD LineSegmentB_Point1, PointD LineSegmentB_Point2)
		{
			double dx = LineSegmentA_Point2.Longitude - LineSegmentA_Point1.Longitude;
			double dy = LineSegmentA_Point2.Latitude - LineSegmentA_Point1.Latitude;
			double da = LineSegmentB_Point2.Longitude - LineSegmentB_Point1.Longitude;
			double db = LineSegmentB_Point2.Latitude - LineSegmentB_Point1.Latitude;
			if ((da * dy - db * dx) == 0) //The segments are parallel, thus will never intersect.
				return false;

			double s = (dx * (LineSegmentB_Point1.Latitude - LineSegmentA_Point1.Latitude) + dy * (LineSegmentA_Point1.Longitude - LineSegmentB_Point1.Longitude)) / (da * dy - db * dx);
			double t = (da * (LineSegmentA_Point1.Latitude - LineSegmentB_Point1.Latitude) + db * (LineSegmentB_Point1.Longitude - LineSegmentA_Point1.Longitude)) / (db * dx - da * dy);

			return (s >= 0 && s <= 1 && t >= 0 && t <= 1);
		}

		public bool IsPointIn(PointD point)
		{
			// From: http://www.alienryderflex.com/polygon/
			// The function will return true if the point is inside the
			// polygon or false if it is not. If the point is exactly on
			// the edge of the polygon, then the function may return true
			// or false.

			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			// CAUTION: THIS METHOD ONLY WORKS AFTER Refresh() HAS BEEN CALLED.
			// Create() (called by Refresh()) sets isCrossDateline and adjusts
			// the longitudes of the polygon's point list.
			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

			List<PointD> tempPointList;
			if (pointListToDraw.Count > 0)
				tempPointList = pointListToDraw;
			else
				tempPointList = pointList;

			PointD pt = new PointD(point.X, point.Y);
			int i, j = 0;
			bool  oddNodes = false;

			// Adjust the longitude if the polygon crosses the dateline
			if (isCrossDateline && pt.Longitude > 0)
                pt.Longitude -= 360;

			// Point in polygon test
			for (i=0; i<tempPointList.Count; i++)
			{
				j++;
				if (j == tempPointList.Count) j=0;
				double polyYi = ((PointD)tempPointList[i]).Y;
				double polyYj = ((PointD)tempPointList[j]).Y;
				double polyXi = ((PointD)tempPointList[i]).X;
				double polyXj = ((PointD)tempPointList[j]).X;
				if (polyYi < pt.Y && polyYj >= pt.Y || polyYj < pt.Y && polyYi >= pt.Y) 
				{
					if (polyXi+(pt.Y-polyYi)/(polyYj-polyYi)*(polyXj-polyXi) < pt.X) 
						oddNodes = !oddNodes;
				}
			}

			return oddNodes;
		}

		public void GetBoundingBox(ref double minLat, ref double maxLat, ref double minLon, ref double maxLon)
		{
			List<PointD> tempPointList;
			if (pointListToDraw.Count > 0)
				tempPointList = pointListToDraw;
			else
				tempPointList = pointList;

			minLat = 90;
			maxLat = -90;
			minLon = 180;
			maxLon = -180;
			bool crossesIDL = CrossesDateline();

			for (int i = 0; i < tempPointList.Count; i++)
			{
				PointD p = (PointD)tempPointList[i];
				if (p.Latitude < minLat)
					minLat = p.Latitude;
				if (p.Latitude > maxLat)
					maxLat = p.Latitude;
				if (crossesIDL)
				{
					if (p.Longitude >= 0 && p.Longitude < minLon)
						minLon = p.Longitude;
					if (p.Longitude < 0 && p.Longitude > maxLon)
						maxLon = p.Longitude;
				}
				else
				{
					if (p.Longitude < minLon)
						minLon = p.Longitude;
					if (p.Longitude > maxLon)
						maxLon = p.Longitude;
				}
			}
		}

        public PointD GetMidpointConsideringDateLine()
        {
            double minLat = 0, minLon = 0, maxLat = 0, maxLon = 0;
            GetBoundingBox(ref minLat, ref maxLat, ref minLon, ref maxLon);

            double latitude = (maxLat + minLat) / 2.0;
            double longitude = (minLon + maxLon) / 2.0;

            if (CrossesDateline())
            {
                if (longitude == 0)
                    longitude = 180;
                else if (longitude < 0)
                    longitude += 180;
                else if (longitude > 0)
                    longitude -= 180;
            }

            return new PointD(longitude, latitude);
        }

		public bool CrossesDateline()
		{
			// Check for segments that cross the international dateline
			for (int i = 1; i < Count; i++)
			{
				if (Curve.CrossesIDL(pointList[i - 1].Longitude, pointList[i].Longitude))
					return true;
			}

			return false;
		}

        public void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			SetMapProjection(mapProjection, centralLongitude);
			if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
				CreateDisplayList();
		}

		private bool AllPointsOutsideProjection()
		{
			if (Projection.GetProjectionType(mapProjection) != MapProjectionTypes.Azimuthal)
				return false;

			foreach (PointD pt in pointList)
				if (pt.Y > 0.0)
					return false;

			return true;
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

		protected void CreateDisplayList()
		{
            if ((openglDisplayList == -1) || Updated)
            {
				double px, py;

                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

                // Is there anything to draw?
                if (Count == 0 || (opacity == 0 && borderWidth == 0) || AllPointsOutsideProjection())
                {
                    Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;	// nothing to draw
                }

                // Some OpenGL initialization
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_FLAT);

				// If the polygon crosses the date line, adjust the longitudes
				if (isCrossDateline)
				{
					for (int i = 0; i < pointList.Count; i++)
					{
						if (pointList[i].X > 0)
							pointList[i].X -= 360;
					}
				}

				// Add up the vertices
                numVertices += pointList.Count;

				// Hack for large polygons where the endpoints are far apart in longitude (e.g. ozone)
				if (endpointFix)
				{
					endpointFixApplied = false;
					bool wide = false;
					bool veryWide = false;
					PointD firstPoint = pointList[0];
					PointD lastPoint = pointList[pointList.Count - 1];
					if (Math.Abs(firstPoint.Longitude - lastPoint.Longitude) >= 40)
						wide = true;
					if (Math.Abs(firstPoint.Longitude - lastPoint.Longitude) >= 180)
						veryWide = true;

					// hokey rules to deal with weird polygons
					bool applyFix = false;
					applyFix = (wide && (!veryWide || !(Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal)))
						|| (veryWide && (Math.Abs(Math.Abs(firstPoint.Longitude) - Math.Abs(lastPoint.Longitude)) > 10) && (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal));

					if (applyFix)
					{
						// add new first & last points at pole
						pointList.Insert(0, new PointD(firstPoint.Longitude, Math.Sign(firstPoint.Latitude) * 90));
						pointList.Add(new PointD(lastPoint.Longitude, Math.Sign(lastPoint.Latitude) * 90));
						endpointFixApplied = true;
					}
				}

				// Generate interpolated intermediate points. NOTE: This clears pointListToDraw before generating interpolated points.
				GenerateInterpolatedPoints();

				// Hack to remove points added above for large polygons
				if (endpointFix && endpointFixApplied)
				{
					pointList.RemoveAt(pointList.Count - 1);
					pointList.RemoveAt(0);
				}

				// If the user specified that they need the video card fix where we don't draw polygons (FIR) quite as
				// accurately, then do that.
				if (FusionSettings.Map.SimplePolygonsFix)
				{
					if (pointListToDraw.Count > FusionSettings.Map.SimplePolygonsMaxPoints)
						pointListToDraw = DouglasPeucker.DouglasPeuckerReduction(pointListToDraw, FusionSettings.Map.SimplePolygonsTolerance);
				}

				// Turn on polygon stippling
				if (fillPattern != PolygonStipplePattern.None)
				{
					Gl.glEnable(Gl.GL_POLYGON_STIPPLE);
					switch (fillPattern)
					{
						case PolygonStipplePattern.Line:
							Gl.glPolygonStipple(linePattern);
							break;
						case PolygonStipplePattern.LineL2R:
							Gl.glPolygonStipple(lineL2RPattern);
							break;
						case PolygonStipplePattern.LineVert:
							Gl.glPolygonStipple(lineVertPattern);
							break;
						case PolygonStipplePattern.LineHorz:
							Gl.glPolygonStipple(lineHorzPattern);
							break;
						case PolygonStipplePattern.DashedLine:
							Gl.glPolygonStipple(dashedLinePattern);
							break;
                        case PolygonStipplePattern.DensityNA:
                            Gl.glPolygonStipple(DensityNAPattern);
							break;
                        case PolygonStipplePattern.DensityGood:
                            Gl.glPolygonStipple(DensityGoodPattern);
                            break;
                        case PolygonStipplePattern.DensityFair:
                            Gl.glPolygonStipple(DensityFairPattern);
                            break;
                        case PolygonStipplePattern.DensityMedium:
                            Gl.glPolygonStipple(DensityMediumPattern);
                            break;
                        case PolygonStipplePattern.DensityPoor:
                            Gl.glPolygonStipple(DensityPoorPattern);
                            break;
                        case PolygonStipplePattern.DensityNil:
                            Gl.glPolygonStipple(DensityNilPattern);
                            break;
					}
				}
				
                // Render the polygon fill
                if (opacity > 0 && fillColor != Color.Transparent)
                {
                    Gl.glDepthRange(0.1, 1.0);
					double[] x = new double[pointListToDraw.Count];
					double[] y = new double[pointListToDraw.Count];
					for (int i = 0; i < pointListToDraw.Count; i++)
                    {
						x[i] = pointListToDraw[i].X;
						y[i] = pointListToDraw[i].Y;
                    }

					// if stipple color is defined, then draw background color first
					// then draw second layer with colored pattern
 					// otherwise draw normal single layer polygon
					if (stippleColor != Color.Transparent)
						Gl.glDisable(Gl.GL_POLYGON_STIPPLE);
					TessellatePolygon(x, y, pointListToDraw.Count, fillColor.R, fillColor.G, fillColor.B, (int)opacity, false, mapProjection, centralLongitude);
                    Gl.glDepthRange(0.0, 1.0);

					if (stippleColor != Color.Transparent)
					{
						Gl.glEnable(Gl.GL_POLYGON_STIPPLE);
						TessellatePolygon(x, y, pointListToDraw.Count, stippleColor.R, stippleColor.G, stippleColor.B, (int)opacity, false, mapProjection, centralLongitude);
						Gl.glDepthRange(0.0, 1.0);
					}
                }

				// Turn off polygon stippling
				if (fillPattern != PolygonStipplePattern.None)
					Gl.glDisable(Gl.GL_POLYGON_STIPPLE);

                // Render the polygon border
                if (borderWidth > 0)
                {
                    // Setup
                    Gl.glEnable(Gl.GL_LINE_SMOOTH);
                    Gl.glDepthRange(0.0, 0.9);
                    Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
                    Gl.glLineWidth(borderWidth);
                    if (borderType != PolygonBorderType.Solid)
                    {
                        switch (borderType)
                        {
                            case PolygonBorderType.LongDashed:
                                stipplePattern = 0x00FF;
                                break;
                            case PolygonBorderType.ShortDashed:
                                stipplePattern = 0x0F0F;
                                break;
                            case PolygonBorderType.Dotted:
                                stipplePattern = 0xCCCC;
                                break;
							case PolygonBorderType.DashDot:
								stipplePattern = 0x18FF;
                                break;
                            case PolygonBorderType.Custom:
                                // stipple pattern set by user
                                break;
                            default:
                                break;
                        }
                        Gl.glLineStipple(stippleFactor, stipplePattern);
                        Gl.glEnable(Gl.GL_LINE_STIPPLE);
                    }

                    // Draw the border for the polygon
					MapProjectionTypes mpType = Projection.GetProjectionType(mapProjection);
                    Gl.glBegin(Gl.GL_LINE_LOOP);
					for (int i = 0; i < pointListToDraw.Count; i++)
					{
						if (mpType == MapProjectionTypes.Azimuthal && pointListToDraw[i].Y < Projection.MinAzimuthalLatitude)
							Projection.ProjectPoint(mapProjection, pointListToDraw[i].X, Projection.MinAzimuthalLatitude, centralLongitude, out px, out py);
						else
							Projection.ProjectPoint(mapProjection, pointListToDraw[i].X, pointListToDraw[i].Y, centralLongitude, out px, out py);
						Gl.glVertex2d(px, py);
					}
                    Gl.glEnd();

                    // Cleanup
                    Gl.glDepthRange(0.0, 1.0);
                    Gl.glDisable(Gl.GL_LINE_SMOOTH);
                    if (borderType != PolygonBorderType.Solid)
                        Gl.glDisable(Gl.GL_LINE_STIPPLE);
                }

                // End the OpenGL display list
                Gl.glEndList();

                if (Updated)
                    Updated = false;
            }
		}

		private void GenerateInterpolatedPoints()
		{
			if (cubicSplineFit)
				GenerateCubicSplinePoints();
			else
				GenerateLinearPoints();
		}

		private void GenerateLinearPoints()
		{
			// Generate interpolated intermediate points.  This results in
			// segments that are properly curved in azimuthal projections.
			pointListToDraw.Clear();
			pointList.Add(pointList[0]); // temporarily add first point to end
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				pointListToDraw.Add(pointList[i]);
				int nPointsToAdd = 0;
				double lonStep = 0, latStep = 0;
				if (Curve.CrossesIDL(pointList[i + 1].Longitude, pointList[i].Longitude))
				{
					double delta1 = 180 - Math.Abs(pointList[i + 1].Longitude);
					double delta2 = 180 - Math.Abs(pointList[i].Longitude);
					nPointsToAdd = (int)((delta1 + delta2) / 2);
					if (nPointsToAdd > 0)
					{
						lonStep = (delta1 + delta2) / nPointsToAdd;
						latStep = (pointList[i + 1].Latitude - pointList[i].Latitude) / nPointsToAdd;
					}
				}
				else
				{
					nPointsToAdd = (int)Math.Abs((pointList[i + 1].Longitude - pointList[i].Longitude) / 2);
					lonStep = (pointList[i + 1].Longitude - pointList[i].Longitude) / nPointsToAdd;
					latStep = (pointList[i + 1].Latitude - pointList[i].Latitude) / nPointsToAdd;
				}
				if (nPointsToAdd > 1 && nPointsToAdd < MAX_INTERPOLATED_POINTS)
				{
					for (int j = 1; j < nPointsToAdd; j++)
					{
						PointD p = new PointD(pointList[i].X + (j * lonStep), pointList[i].Y + (j * latStep));
						if ((p.Y * pointListToDraw[pointListToDraw.Count - 1].Y) < 0)
						{
							// segment crosses equator; add point at equator
							PointD eqp = GetSegmentIntersection(p, pointListToDraw[pointListToDraw.Count - 1], new PointD(-180, 0), new PointD(180, 0));
							if (!PointD.IsNullOrEmpty(eqp))
							{
								eqp.Latitude = 0.0; // force lat to be exactly 0
								pointListToDraw.Add(eqp);
							}
						}
						pointListToDraw.Add(p);
					}
				}
				else
				{
					if ((pointList[i].Y * pointList[i + 1].Y) < 0)
					{
						// segment crosses equator; add point at equator
						PointD eqp = GetSegmentIntersection(pointList[i], pointList[i + 1], new PointD(-180, 0), new PointD(180, 0));
						if (!PointD.IsNullOrEmpty(eqp))
						{
							eqp.Latitude = 0.0; // force lat to be exactly 0
							pointListToDraw.Add(eqp);
						}
					}
				}
			}
			pointListToDraw.Add(pointList[pointList.Count - 1]); // add last point to pointListToDraw
			pointList.RemoveAt(pointList.Count - 1); // remove temporary point
		}

		private void GenerateCubicSplinePoints()
		{
			List<List<PointD>> listOfLists = new List<List<PointD>>();
			List<PointD> list = new List<PointD>();

			// If the border crosses the IDL, split the list of points into segments
			for (int i = 0; i < pointList.Count - 1; i++)
			{
				//Skip adjacent identical points which cause null reference exception when spline
				if (pointList[i].IsSamePoint(pointList[i + 1]))
					continue;

				list.Add(pointList[i]);
				if (Curve.CrossesIDL(pointList[i].Longitude, pointList[i + 1].Longitude))
				{
					listOfLists.Add(list);
					list = new List<PointD>();
				}
			}
			list.Add(pointList[pointList.Count - 1]); // add the last point
			listOfLists.Add(list);

			// Do a spline fit for each segment and add it to the overall point list to draw
			foreach (List<PointD> l in listOfLists)
				pointListToDraw.AddRange(Spline.CubicSplineFit(l));
		}

		private PointD GetSegmentIntersection(PointD s1p1, PointD s1p2, PointD s2p1, PointD s2p2)
		{
			PointD point = PointD.Empty;
			FUL.Coordinate _s1p1 = new FUL.Coordinate(s1p1.Longitude, s1p1.Latitude);
			FUL.Coordinate _s1p2 = new FUL.Coordinate(s1p2.Longitude, s1p2.Latitude);
			FUL.Coordinate _s2p1 = new FUL.Coordinate(s2p1.Longitude, s2p1.Latitude);
			FUL.Coordinate _s2p2 = new FUL.Coordinate(s2p2.Longitude, s2p2.Latitude);
			FUL.Coordinate iPoint = new FUL.Coordinate(true);

			bool intersect = FUL.Utils.LineSegmentsIntersect(_s1p1, _s1p2, _s2p1, _s2p2, true, out iPoint);
			if (intersect)
				point = new PointD(iPoint.Lon, iPoint.Lat);

			return point;
		}

		protected int NumDatelineCrossings()
		{
			// Check for segments that cross the international dateline
			int nCrossings = 0;
			PointD temp1, temp2;
			for (int i=1; i<Count; i++)
			{
				if (Curve.CrossesIDL(pointList[i-1],pointList[i],out temp1,out temp2))
					nCrossings++;
			}
			return nCrossings;
		}

		protected void CreateNewPointLists(out List<PointD> pl1, out List<PointD> pl2)
		{
			// Check for empty point list
			if (Count == 0)
			{
				pl1 = null;
				pl2 = null;
				return;
			}

			// The polygon crosses the dateline, so form new point lists
            int crossings = 0, idx = 0;
			pl1 = new List<PointD>();
			pl2 = new List<PointD>();
			PointD newPt1, newPt2;
			pl1.Add(pointList[0]);
			for (int i=1; i<=Count; i++)
			{
                idx = i;
                if (idx == Count) idx = 0;
				if (!Curve.CrossesIDL(pointList[i-1],pointList[idx],out newPt1,out newPt2))
				{
					if (crossings == 1)
						pl2.Add(pointList[idx]);
					else
						pl1.Add(pointList[idx]);	
				}
				else
				{
					crossings++;
					if (crossings == 1)
					{
						pl1.Add(newPt1);
						pl2.Add(newPt2);
						pl2.Add(pointList[idx]);
					}
					else
					{
						pl2.Add(newPt1);
						pl1.Add(newPt2);
						pl1.Add(pointList[idx]);
					}
				}
			}
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Polygon Draw()");
#endif

			if (openglDisplayList == -1) return;

            bool crossIDL = false;
            foreach (PointD p in pointList)
            {
                if ((p.Longitude > 180) || (p.Longitude < -180))
                {
                    crossIDL = true;
                    break;
                }
            }

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180 || crossIDL)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right, crossIDL);
            else
                Gl.glCallList(openglDisplayList);
		}
	}
}

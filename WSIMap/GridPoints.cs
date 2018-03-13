using System;
using System.Drawing;
using System.Text;

namespace WSIMap
{
	public class GridPoints : MultipartFeature
	{
		#region Data Members
		private SymbolType symbolType;
		private System.Drawing.Color symbolColor;
		private uint symbolSize;
		#endregion

		public GridPoints()
		{
			this.symbolType = SymbolType.Plus;
			this.symbolColor = Color.Black;
			this.symbolSize = 2;
		}

		public GridPoints(SymbolType symbolType, Color symbolColor, uint symbolSize)
		{
			this.symbolType = symbolType;
			this.symbolColor = symbolColor;
			this.symbolSize = symbolSize;
		}

		public SymbolType SymbolType
		{
			get { return symbolType; }
			set { symbolType = value; }
		}

		public Color SymbolColor
		{
			get { return symbolColor; }
			set { symbolColor = value; }
		}

		public uint SymbolSize
		{
			get { return symbolSize; }
			set { symbolSize = value; }
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("GridPoints Draw()");
#endif

			// Get the map rectangle
			double minLat, maxLat, minLon, maxLon;
			parentMap.GetMapBoundingBox(out minLat, out maxLat, out minLon, out maxLon);
			int l = (int)minLon;
			int r = (int)maxLon;
			int t = (int)maxLat;
			int b = (int)minLat;
			
			// Set the resolution of the grid based on the map width
			int res = 1;
			if (parentMap.ScaleFactor < 5)
				res = 10;
			else if (parentMap.ScaleFactor >= 5 && parentMap.ScaleFactor < 35)
				res = 5;
			else
				res = 1;

			// Round l, r, t & b to match the resolution (prevents the grid from "sliding" wrt map)
			l = l / res * res;
			r = r / res * res;
			t = t / res * res;
			b = b / res * res;

			// Clear existing symbols
			this.Clear(false);

			// Add new symbols only within the map rectangle
			for (int i = l; i <= r; i+=res)
			{
				for (int j = b; j <= t; j+=res)
				{
					Symbol symbol = new Symbol(symbolType, symbolColor, symbolSize, j, i);
					symbol.ToolTip = true;
					symbol.FeatureInfo = j.ToString() + "," + MapGL.NormalizeLongitude(i).ToString();
					this.Add(symbol);
				}
			}

			// Draw the symbols
			for (int i = 0; i < this.Count; i++)
				features[i].Draw(parentMap, parentLayer);
		}
	}
}

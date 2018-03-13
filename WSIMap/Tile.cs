using System;
using System.Collections.Generic;
using System.Text;

namespace WSIMap
{
    /**
	 * \class Tile
	 * \brief Represents a tile - DEM data covering the data with 1km resolutions
	 */
    [Serializable]
    internal class Tile
    {
        #region Data Members
        private double  longitude;
        private double  latidude;
        private string  fileName;
        private int     transparency; 
        private DEM     dem;
        private double  shift;
        private FeatureCollection feature_collection; 
        #endregion

        public Tile(string file_path, double longitude, double latidude, int transparency, FeatureCollection feature_collection, ColorTables.ByteQuad[] color_table, int level)
        {
            this.longitude = longitude;
            this.latidude = latidude;
            this.fileName = file_path;
            this.transparency = transparency;
            this.feature_collection = feature_collection;
            this.shift = longitude;

            dem = new DEM(file_path, color_table, file_path, String.Empty, level, shift);
            dem.Transparency = transparency;
            dem.Refresh();
            feature_collection.Add(dem);
        }
      
        public virtual void Dispose()
        {
            feature_collection.Remove(dem);
            dem.Dispose();
        }

        public double Longitude
        {
            get { return longitude; }             
        }

        public double Latidude
        {
            get { return latidude; }
        }
        
        public string FileName
        {
            get { return fileName; }             
        }

        public double Shift
        {
            set 
            {
                shift = value;
                dem.Shift = shift;            
            }
        }

		public void UpdateColor(ColorTables.ByteQuad[] new_color_table)
        {
            dem.UpdateColor(new_color_table);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WSIMap
{
    /**
	 * \class TilesCollection
	 * \brief A collection class that holds a set of tiles
	 */ 
    internal class TilesCollection : System.Collections.CollectionBase
    {
        #region Data Members 
        public TilesInfo    tiles_info = null;
        ColorTables.ByteQuad[] color_table = ColorTables.ColorTable_elevgbsncap;

		private string files_path_1km = FusionSettings.Map.Directory + "WSI Fusion Client\\DEM_Data\\DEM_Globe";      // textures path with 1km resolution 
        private int         transparency = 180;
        private Ant_Tile[]  Antarctica_Tiles;
        private const int   Antarctica_tiles_count = 6;
       
        private FeatureCollection features = null;
            
        private int previous_view_width = 0;
        private int previous_view_height = 0;
        private double previous_left = 0;
        private double previous_top = 0;

        private double left;
        private double right;
        private double top;
        private double bottom;

        private double scaleFactor;
        private int view_port_width = 0;
        private int view_port_height = 0;
        private int previous_view_port_width = 0;
        private int previous_view_port_height = 0;

        #endregion

        public struct ViewRegion
        {
            public double min_longitude;
            public double max_longitude;
            public double min_latitude;
            public double max_latitude;
        }
        private struct Ant_Tile
        {
            public string name;
            public int left;
        };

        public TilesCollection()
		{            
            TilesInfoInitialization();            
		}
        
        public int Transparency
        {
            get { return transparency; }
            set { transparency = value; }
        }

		public ColorTables.ByteQuad[] ColorTable
        {
            set
            {
                color_table = value;
                if (this.Count != 0)
                {
                    UpdateTiles();
                }
            }
        } 
         
        public FeatureCollection Features
        {
            set { features = value; }
        }

        public int Add(Tile tile)
		{
			int index;
			lock (this.List.SyncRoot)
			{
                index = this.List.Add(tile);
			}
			return index;
		}
 
        public void Remove(Tile tile)
        {
            lock (this.List.SyncRoot)
            {
                tile.Dispose();
                this.List.Remove(tile);
            }
        }
        
        public Tile this[string name]
        {
            get
            {
                Tile tile = (Tile)null;
                bool found = false;
                lock (this.List.SyncRoot)
                {
                    for (int i = 0; i < this.List.Count; i++)
                    {
                        tile = (Tile)this.List[i];
                        if (found = string.Equals(tile.FileName, name))
                            break;
                    }
                }
                if (found)
                    return tile;
                else
                    return null;
            }
        }
 
        public Tile this[int index]
        {
            get
            {
                if (index < 0 || index >= this.List.Count)
                    return null;
                else
                    return (Tile)this.List[index];
            }
        }
       
        private bool ExistTile(string file_name, double shift)
        {
            bool result = false;
            lock (this.List.SyncRoot)
            {
                if (this.List.Count > 0)
                    foreach (Tile tile in this)
                    {
                        if (tile.FileName == file_name)
                        {
                            result = true;
                            tile.Shift = shift;
                            break;
                        }
                    }
            }
            return result;
        }

        public void UpdateTiles()
        {
            int count = this.List.Count;
            if (count > 0)
                lock (this.List.SyncRoot)
                {
                    Tile tile;
                    for (int i = 0; i != count; ++i)
                    {
                        tile = this[count - 1 - i];
                        tile.UpdateColor(color_table);
                    }
                }
        }
        
        public void ClearTiles()
        {
            int count = this.List.Count;
            if (count > 0)
                lock (this.List.SyncRoot)
                {
                    Tile tile;
                    for (int i = 0; i != count; ++i)
                    {
                        tile = this[count - 1 - i];
                        this.Remove(tile);
                    }
                }
        }

        private bool TileIsVisible(ref ViewRegion vr, double longitude_left, int latitude_top, bool Antarctica, int step)
        {
            bool res = false;
            double latitude_height = 50/step;
            double longitude_tile_width = 40 / step;
            int shift_x = 6 / step + 1, shift_y = 8 / step + 1;
            if (Antarctica)
            {                 
                longitude_tile_width = 60.0 / step;
                shift_x = 10 / step + 1;                
            }

            if ((longitude_left - shift_x < vr.min_longitude && longitude_left + longitude_tile_width + shift_x > vr.max_longitude &&
                 vr.max_latitude + shift_x > latitude_top - latitude_height && vr.min_latitude - shift_x < latitude_top) ||
                //left border
                (vr.min_longitude - shift_x < longitude_left && longitude_left < vr.max_longitude + shift_x) &&
                ((latitude_top > vr.min_latitude - shift_y && latitude_top < vr.max_latitude + shift_y) ||
                (latitude_top - latitude_height > vr.min_latitude - shift_y && latitude_top - latitude_height < vr.max_latitude + shift_y) ||
                (latitude_top > vr.max_latitude - shift_y - 1 && latitude_top - latitude_height < vr.min_latitude + shift_y + 1)) ||
                // right border
                (vr.min_longitude - shift_x < longitude_left + longitude_tile_width && longitude_left + longitude_tile_width < vr.max_longitude + shift_x) &&
                ((latitude_top > vr.min_latitude - shift_y && latitude_top < vr.max_latitude + shift_y) ||
                (latitude_top - latitude_height > vr.min_latitude - shift_y && latitude_top - latitude_height < vr.max_latitude + shift_y) ||
                (latitude_top > vr.max_latitude - shift_y && latitude_top - latitude_height < vr.min_latitude + shift_y))
               )
                res = true;

            return res;
        }

        private void UpdateAntarcticaTile(ref ViewRegion vr, string file_name, int level, int step, double Antarctica_tile_left, int Antarctica_tile_top, MapGL parentMap)
        {
            double tileLeftEdge = Antarctica_tile_left;
            int crossing = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
            {
                //if (parentMap.BoundingBox.Map.left > 0 && parentMap.BoundingBox.Map.left < 90)
                //{
                //    int ratio = (int)parentMap.BoundingBox.Map.right / 180;
                //    int delta = (int)parentMap.BoundingBox.Map.right % 180;
                //    tileLeftEdge = Antarctica_tile_left + 180 * (ratio + 1);
                //    tileLeftEdge -= delta;
                //}
                //else
                //if (parentMap.BoundingBox.Map.left < -180)
                //{
                //    double delta = 0;
                //    int ratio = 0;

                //    ratio = Math.Abs((int)parentMap.BoundingBox.Map.left / 180);
                //    delta = Math.Abs(parentMap.BoundingBox.Map.left % 180);
                //    tileLeftEdge += (int)delta;
                //    if (parentMap.BoundingBox.Map.left < -360 && parentMap.BoundingBox.Map.left > -450)
                //        tileLeftEdge -= 180;
                //    else
                //        if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                //            tileLeftEdge -= (int)(180 * (ratio + 1));
                //}
                //else
                //    if (parentMap.BoundingBox.Map.right > 180)
                //        tileLeftEdge = MapGL.GetShiftedLongtitude(Antarctica_tile_left, parentMap);

                //if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                //    tileLeftEdge += 360;
                //if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                //    tileLeftEdge -= 360;

                //tileLeftEdge = parentMap.DenormalizeLongitude(tileLeftEdge);

                tileLeftEdge += crossing * 360;
            }
           
            if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                tileLeftEdge += 360;
            if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                tileLeftEdge -= 360;

            bool visible = TileIsVisible(ref vr, tileLeftEdge, Antarctica_tile_top, true, step);
            bool tile_exists = ExistTile(file_name, tileLeftEdge);
             
            if (visible && !tile_exists && System.IO.File.Exists(file_name))
                this.Add(new Tile(file_name, tileLeftEdge, Antarctica_tile_top, transparency, features, color_table, level));
            else
                if (tile_exists && !visible)
                    this.Remove(this[file_name]);
        }

        private void UpdateAllAntarcticaTiles(int level, int sublevel, ref ViewRegion vr, MapGL parentMap)
        {             
            string file_name;                   

            for (int i = 0; i != Antarctica_tiles_count; ++i)
            {                
                string current_file_name = Antarctica_Tiles[i].name;

                file_name = files_path_1km + level + "_" + sublevel + "\\" + current_file_name + "_" + level;
                if (sublevel > 1)
                {
                    bool sphere_switching = false;
                    file_name += "_" + sublevel + ".BDF";
                    int step = (int)Math.Sqrt(sublevel);
                     
                        for (int n = 0; n != step; ++n)
                        {
                            if (sphere_switching)
                            {
                                current_file_name = current_file_name.Remove(0, 1);
                                current_file_name = current_file_name.Insert(0, "E");
                                sphere_switching = false;
                            }

                            double tile_left = (int)(Antarctica_Tiles[i].left + 60.0 / step * n);
                            double long_num = Math.Abs(tile_left);
                            current_file_name = current_file_name.Remove(1, 3);
                            int ind = 1;
                            if (long_num == 0)
                            {
                                current_file_name = current_file_name.Insert(ind, "00");
                                ind += 2;
                                sphere_switching = true;
                            }
                            else
                                if (long_num < 100)
                                {
                                    current_file_name = current_file_name.Insert(ind, "0");
                                    ++ind;
                                }
                            current_file_name = current_file_name.Insert(ind, long_num.ToString());

                            int[] AntarcticaTilesNumbers = new int[] { -60, -63, -67, -71, -75, -78, -82, -86 };
                            int tiles_count = AntarcticaTilesNumbers.Length;

                            for (int j = 0; j != tiles_count; ++j)
                            {
                                if (current_file_name.Length > 0)
                                {
                                    current_file_name = current_file_name.Remove(current_file_name.Length - 2, 2);
                                    current_file_name += Math.Abs(AntarcticaTilesNumbers[j]);
                                    file_name = files_path_1km + level + "_" + sublevel + "\\" + current_file_name + "_" + level + "_" + sublevel + ".BDF";

                                    UpdateAntarcticaTile(ref vr, file_name, level, step, tile_left, AntarcticaTilesNumbers[j], parentMap);
                                }
                            }
                        }
                }
                else
                {
                    file_name += ".BDF";
                    UpdateAntarcticaTile(ref vr, file_name, level, (int)Math.Sqrt(sublevel), Antarctica_Tiles[i].left, -60, parentMap);
                }
            }  
        }
       
        private void UpdateVisibleTilesOfSmallerSize(int level, int step, int longitude, string sphere, int latitude, string hemisphere,
                                                     ref ViewRegion vr, MapGL parentMap)
        {
            const double sphere_w = 40;
            const double sphere_h = 50;
            const double atlantica_w = 60;
            const double atlantica_h = 30;

            double tile_w = sphere_w;
            double tile_h = sphere_h;
            if (hemisphere == "S" && latitude == 60)
            {
                tile_w = atlantica_w;
                tile_h = atlantica_h;
            }

            int crossing = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
            int new_latitude = 0, new_longitude = 0;
            for (int i = 0; i != step; ++i)
                for (int j = 0; j != step; ++j)
                {
                    new_longitude = (int)(longitude + tile_w / step * j);
                    
                    if (new_longitude > 0)
                        sphere = "E"; 
                    else
                        sphere = "W";

                    string file_name = files_path_1km + level + "_" + step * step + "\\" + sphere;
                    

                    if (new_longitude == 0)
                        file_name += "00";
                    else
                        if (Math.Abs(new_longitude) < 100)
                            file_name += "0";

                    new_latitude = (int)((latitude - tile_h) + tile_h / step * (i + 1));

                    file_name += Math.Abs(new_longitude) + hemisphere + Math.Abs(new_latitude) + "_" + level + "_" + step * step + ".BDF";

                   double tileLeftEdge = new_longitude;
                   if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                   {                       
                       //if (parentMap.BoundingBox.Map.left > 0 && parentMap.BoundingBox.Map.left < 90)
                       //{
                       //    int ratio = (int)parentMap.BoundingBox.Map.right / 180;
                       //    int delta = (int)parentMap.BoundingBox.Map.right % 180;
                       //    tileLeftEdge = new_longitude + 180 * (ratio + 1);
                       //    tileLeftEdge -= delta;
                       //}
                       //else
                       //    if (parentMap.BoundingBox.Map.left < -180)
                       //    {
                       //        double delta = 0;
                       //        int ratio = 0;

                       //        ratio = Math.Abs((int)parentMap.BoundingBox.Map.left / 180);
                       //        delta = Math.Abs(parentMap.BoundingBox.Map.left % 180);
                       //        tileLeftEdge += delta;
                       //        if (parentMap.BoundingBox.Map.left < -360 && parentMap.BoundingBox.Map.left > -450)
                       //            tileLeftEdge -= 180;
                       //        else
                       //            if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                       //                tileLeftEdge -= 180 * (ratio + 1);
                       //    }
                       //    else
                       //        if (parentMap.BoundingBox.Map.right > 180)
                       //            tileLeftEdge = MapGL.GetShiftedLongtitude(new_longitude, parentMap);


                       //if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                       //    tileLeftEdge += 360;
                       //if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                       //    tileLeftEdge -= 360;

                       //tileLeftEdge = parentMap.DenormalizeLongitude(tileLeftEdge);

                       tileLeftEdge += crossing * 360;
                   }
                   if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                       tileLeftEdge += 360;
                   if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                       tileLeftEdge -= 360;
                    bool visible = TileIsVisible(ref vr, tileLeftEdge, new_latitude, false, step);
                    bool exist = ExistTile(file_name, tileLeftEdge);

                    if (visible && !exist && new_latitude > -60 && System.IO.File.Exists(file_name))
                        this.Add(new Tile(file_name, tileLeftEdge, new_latitude, transparency, features, color_table, level));
                    else
                        if (exist && !visible)
                            this.Remove(this[file_name]);
                }
        }

        public void UpdateVisibleTilesOfEntireGlobe(int level, int sublevel, ref ViewRegion vr, MapGL parentMap)
        {
            string file_name = "";                
            string latitude_code = "N";
            int latitude_num = 0;
            int latitude_top = 0;
            int longitude_left = 0;

            int crossings = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
          
            for (int i = 0; i != 5; i++)
            {
                for (int j = 0; j != 4; j++)
                {
                    switch (latitude_num)
                    {
                        case 0:
                        case 60:
                            latitude_num = 40;
                            latitude_top = 40;
                            latitude_code = "N";
                            break;
                        case 40:
                            latitude_num = 90;
                            latitude_top = 90;
                            break;
                        case 90:
                            latitude_num = 10;
                            latitude_top = -10;
                            latitude_code = "S";
                            break;
                        case 10:
                            latitude_num = 60;
                            latitude_top = -60;
                            break;
                    }

                    file_name = "W"; // for western hemisphere
                    string sphere_code = "W";

                    for (int k = 0; k != 2; ++k)
                    {
                        int _longitude = i * 40 + 20;
                        if (_longitude < 100)                             
                            file_name += "0";                                 
                        
                        string long_code_lat = _longitude + latitude_code + latitude_num;
                        file_name += long_code_lat;  
                        
                        if( k == 0)
                            longitude_left = -(_longitude);
                        else
                            longitude_left = _longitude;
 
                        file_name = files_path_1km + level + "_" + sublevel + "\\" + file_name + "_" + level;
                        if (sublevel > 1)
                            file_name += "_" + sublevel;
                        file_name += ".BDF";

                        double tileLeftEdge = longitude_left;

                        if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                        {
                            //if (parentMap.BoundingBox.Map.left > 0 && parentMap.BoundingBox.Map.left < 90)
                            //{  
                            //    int ratio = (int)parentMap.BoundingBox.Map.right / 180;
                            //    int delta = (int)parentMap.BoundingBox.Map.right % 180;
                            //    tileLeftEdge = longitude_left + 180 * (ratio + 1);
                            //    tileLeftEdge -= delta;   
                            //}
                            //else
                            //    if (parentMap.BoundingBox.Map.left < -180)
                            //    {
                            //        double delta = 0;
                            //        int ratio = 0; 

                            //        ratio = Math.Abs((int)parentMap.BoundingBox.Map.left / 180);
                            //        delta = Math.Abs(parentMap.BoundingBox.Map.left % 180);
                            //        tileLeftEdge += (int)delta;
                            //        if (parentMap.BoundingBox.Map.left < -360 && parentMap.BoundingBox.Map.left > -450)
                            //            tileLeftEdge -= 180;
                            //        else
                            //            if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                            //                tileLeftEdge -= (int)(180 * (ratio + 1));
                            //    }
                            //    else
                            //        if(parentMap.BoundingBox.Map.right > 180)
                            //            tileLeftEdge = MapGL.GetShiftedLongtitude(longitude_left, parentMap);

                            //if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                            //    tileLeftEdge += 360;
                            //if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                            //    tileLeftEdge -= 360;  
                            //tileLeftEdge = parentMap.DenormalizeLongitude(tileLeftEdge);

                            tileLeftEdge += crossings * 360;

                            //Console.WriteLine("left {0} VR {1} {2}", tileLeftEdge, vr.min_longitude, vr.max_longitude);
                        }

                        if (tileLeftEdge < parentMap.BoundingBox.Map.left)
                            tileLeftEdge += 360;
                        if (tileLeftEdge > parentMap.BoundingBox.Map.right)
                            tileLeftEdge -= 360;  

                        bool visible = true;
                        if (scaleFactor > 1.0)
                            visible = TileIsVisible(ref vr, tileLeftEdge, latitude_top, false, 1);

                        bool exist = ExistTile(file_name, tileLeftEdge);
                        if (sublevel > 1 && visible)
                            UpdateVisibleTilesOfSmallerSize(level, (int)Math.Sqrt(sublevel), longitude_left, sphere_code, latitude_top, latitude_code, ref vr,parentMap);
                        else
                        {
                            if (visible && !exist && System.IO.File.Exists(file_name))
                                this.Add(new Tile(file_name, tileLeftEdge, latitude_top, transparency, features, color_table, level));
                            else
                                if (exist && !visible)
                                    this.Remove(this[file_name]);
                        }
 
                        sphere_code = "E";
                        file_name = "E";
                    }
                }
            }
            // The files of Antarctica   
            UpdateAllAntarcticaTiles(level, sublevel, ref vr, parentMap);
        }

        public void UpdateVisibleTiles(MapGL parentMap)
        {
            this.left = parentMap.BoundingBox.Map.left;
            this.right = parentMap.BoundingBox.Map.right;
            this.top = parentMap.BoundingBox.Map.top;
            this.bottom = parentMap.BoundingBox.Map.bottom;            
            this.scaleFactor = parentMap.ScaleFactor;
            this.view_port_width = parentMap.Size.Width;
            this.view_port_height = parentMap.Size.Height;
            if (IsScaled())
                ScaleTerrainMap(parentMap);
            else
                if (IsPositionChanged())
                    TransformTerrainMap(parentMap);                
        }
        
        private bool IsScaled()
        {
            bool result = false;

            int h = (int)(top - bottom);
            int w = (int)(right - left);

            if (previous_view_height != h || previous_view_width != w || 
                previous_view_port_width != view_port_width || previous_view_port_height != view_port_height)
            {
                previous_view_height = h;
                previous_view_width = w;
                previous_view_port_width = view_port_width;
                previous_view_port_height = view_port_height;
                result = true;
            }

            return result;
        }

        private void ScaleTerrainMap(MapGL parentMap)
        {
            ClearTiles();

            previous_left = left;
            int level = 0;
            int sublevel = 0;

            GetResolutionLevel(out level, out sublevel);
            ViewRegion vr;
            vr.min_longitude = left;
            vr.max_longitude = right;
            vr.min_latitude = bottom;
            vr.max_latitude = top;
            UpdateVisibleTilesOfEntireGlobe(level, sublevel, ref vr, parentMap);
        }

        private bool IsPositionChanged()
        {
            bool result = false;
 
            if (previous_left != left || previous_top != top)
            {
                previous_left = left;
                previous_top = top;
                result = true;
            }

            return result;
        }

        private void TransformTerrainMap(MapGL parentMap)
        {
            int level = 0;
            int sublevel = 0;
            GetResolutionLevel(out level, out sublevel);
            ViewRegion vr;
            vr.min_longitude = left;
            vr.max_longitude = right;
            vr.min_latitude = bottom;
            vr.max_latitude = top;
            UpdateVisibleTilesOfEntireGlobe(level, sublevel, ref vr, parentMap);
        }

        private void GetResolutionLevel(out int resolution, out int sublevel)
        {
            resolution = 3;
            sublevel = 1;

            if (tiles_info != null && tiles_info.Count > 0)
                for (int i = 0; i != tiles_info.Count; i++)
                {
                    resolution = tiles_info[i].resolution;
                    sublevel = tiles_info[i].sublevel;
                    if (scaleFactor < tiles_info[i].scale_factor/* && view_port_width < tiles_info[i].view_port_width*/)
                        break;
                }
        }

        private void TilesInfoInitialization()
        {
            int delta = 50;
            int vp_width = 640 + delta;
            int vp_height = 320 + delta;

            if (tiles_info == null)
                tiles_info = new TilesInfo();
                      
            tiles_info.Add(new Item(2.1, vp_width, vp_height, 4, 1));            
            tiles_info.Add(new Item(2.1, vp_width * 100, vp_height * 100, 3, 4));                    
            
            tiles_info.Add(new Item(4.1, vp_width, vp_height, 3, 4));
            tiles_info.Add(new Item(4.1, vp_width * 100, vp_height * 100, 2, 16));      
     
            tiles_info.Add(new Item(8.1, vp_width, vp_height, 3, 4));  
            tiles_info.Add(new Item(8.1, vp_width * 100, vp_height * 100, 2, 16));            
                          
            tiles_info.Add(new Item(16.1, vp_width, vp_height, 2, 16));            
            tiles_info.Add(new Item(16.1, vp_width * 100, vp_height * 100, 1, 64)); 
        
            tiles_info.Add(new Item(32.1, vp_width, vp_height, 1, 64));      
            tiles_info.Add(new Item(32.1, vp_width * 100, vp_height * 100, 0, 64)); 
               
            tiles_info.Add(new Item(64.1, vp_width * 100, vp_height * 100, 0, 64));  
                   
            tiles_info.Add(new Item(128.1, vp_width * 100, vp_height * 100, 0, 64));        
 
            tiles_info.Add(new Item(256.1, vp_width * 100, vp_height * 100, 0, 64)); 
            tiles_info.Add(new Item(512.1, vp_width * 100, vp_height * 100, 0, 64));      
            tiles_info.Add(new Item(1024.1, vp_width * 100, vp_height * 100, 0, 64));     
            tiles_info.Add(new Item(3600.1, vp_width * 100, vp_height * 100, 0, 64));    

            Antarctica_Tiles = new Ant_Tile[Antarctica_tiles_count];
            Antarctica_Tiles[0].name = "E120S60"; Antarctica_Tiles[0].left = 120;
            Antarctica_Tiles[1].name = "W120S60"; Antarctica_Tiles[1].left = -120;
            Antarctica_Tiles[2].name = "W000S60"; Antarctica_Tiles[2].left = 0;
            Antarctica_Tiles[3].name = "W180S60"; Antarctica_Tiles[3].left = -180;
            Antarctica_Tiles[4].name = "W060S60"; Antarctica_Tiles[4].left = -60;
            Antarctica_Tiles[5].name = "E060S60"; Antarctica_Tiles[5].left = 60;
        }
    }
    /**
   * \class TilesInfo
   * \brief A collection class that holds a set of info about resolutions and corresponding scale factors 
   */
    public struct Item
    {
        public double scale_factor;
        public int view_port_width;
        public int view_port_height;

        public int resolution;
        public int sublevel;

        public Item(double scale_factor, int view_port_width, int view_port_height, int resolution, int level)
        {
            this.scale_factor = scale_factor;
            this.view_port_width = view_port_width;
            this.view_port_height = view_port_height;
            this.resolution = resolution;
            this.sublevel = level;            
        }
    }

    public class TilesInfo : System.Collections.CollectionBase
    {
        public virtual void Add(Item NewItem)
        {
            this.List.Add(NewItem);
        }

        //this is the indexer (readonly)
        public virtual Item this[int Index]
        {
            get
            {
                return (Item)this.List[Index];
            }
        }
    }


}
     
 


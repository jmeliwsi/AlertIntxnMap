using System;
using System.Collections.Generic;
using Tao.OpenGl;

namespace WSIMap
{
    /**
     * \class Terrain
     * \brief Supports display of custom formatted terrain tiles
     */
    public class Terrain : Feature
    {
        #region Data Members
        protected ColorTables.ByteQuad[] terrainColorTable;
        private TilesCollection tiles_collection;
        #endregion

        public Terrain(FeatureCollection parentFeatureCollection)
        {
            tiles_collection = new TilesCollection();
            tiles_collection.Features = parentFeatureCollection;
            terrainColorTable = ColorTables.ColorTable_elevgbsncap;
            tiles_collection.ColorTable = terrainColorTable;
        }

        public ColorTables.ByteQuad[] TerrainColorTable
        {
            get { return terrainColorTable; }
            set
            {
                terrainColorTable = value;
                tiles_collection.ColorTable = terrainColorTable;
            }
        }

        public override void Refresh()
        {
        }

        public override void Dispose()
        {
            this.Clear();
        }

        public void Clear()
        {
            tiles_collection.ClearTiles();
        }

        internal override void Create()
        {
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
            //-----------------------------------------------------------------
            // NOTE: This method doesn't actually draw the terrain tiles.  It
            // updates the collection of visible tiles, loading and removing
            // tiles as needed.  Then it sets up stenciling.  Upon return
            // from this method, the tiles are drawn because the DEM contained
            // in each tile has been added to the FeatureCollection of the
            // same Layer that contains this Terrain object.  This implies
            // that this Terrain object MUST be the first Feature in the
            // parent Layer's FeatureCollection.  It always is because the
            // Terrain object is added to the FeatureCollection before any
            // of the DEM objects.
            //-----------------------------------------------------------------

            // Update the tiles
            tiles_collection.UpdateVisibleTiles(parentMap);

            // Set the stencil buffer to only allow writing to the
            // screen when the value of the stencil buffer is not 1
            Gl.glStencilFunc(Gl.GL_EQUAL, 1, 1);
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP);
        }
    }
}

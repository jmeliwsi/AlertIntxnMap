using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using Tao.OpenGl;

namespace WSIMap
{
    /**
     * \class VectorFile
     * \brief Supports display of vector files such as ESRI shapefiles
     */
    public class DynamicVectorFile : Feature
    {
        #region Data Members
        protected enum FillType { Land, Water };
        protected string fileName;		// name of this vector file
        protected int numFeatures;		// number of features in this file
        protected Color borderColor;	// the border color
        protected Color waterFillColor;	// the water fill color
        protected Color landFillColor;  // the land fill color
        protected bool drawBorder;		// if true, draw a border on the features
        protected bool fillFeatures;	// if true, fill in the features
        protected double dataleft, dataright, datatop, databottom;
        protected bool dirty;
        protected bool stencil;         // if true, use stencil buffer
        protected bool drawLand;        // if true, draw land vector files
        protected bool drawWater;       // if true, draw water vector files 
		private const string TRACKING_CONTEXT = "DynamicVectorFile";
		#endregion

		[DllImport("tessellate.dll", EntryPoint = "TessellateVectorFile", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void TessellateVectorFile(string vectorFileName, int[] fillColorRGB, bool useTwoColors, int[] fillColor2RGB, int opacity, MapProjections mapProjection);

		private static string Directory = FusionSettings.Map.Directory;

        public DynamicVectorFile()
            : this(string.Empty, string.Empty)
        {
        }

        public DynamicVectorFile(string featureName, string featureInfo)
        {
			if (!FusionSettings.Client.Developer)
				Directory = @".\Basemaps\";

            // Set fields
            this.featureName = featureName;
            this.featureInfo = featureInfo;
            this.numFeatures = 0;
            this.numVertices = 0;
            this.borderColor = Color.Black;
            this.waterFillColor = Color.White;
            this.landFillColor = Color.White;
            this.fillFeatures = false;
            this.drawBorder = true;
            this.dataleft = 0.0;
            this.dataright = 0.0;
            this.datatop = 0.0;
            this.databottom = 0.0;
            this.dirty = true;
            this.stencil = false;
            this.drawLand = true;
            this.drawWater = true; 
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; dirty = true; }
        }

        public Color WaterFillColor
        {
            get { return waterFillColor; }
            set { waterFillColor = value; dirty = true; }
        }

        public Color LandFillColor
        {
            get { return landFillColor; }
            set { landFillColor = value; dirty = true;  }
        }

        public bool Border
        {
            get { return drawBorder; }
            set { drawBorder = value; dirty = true; }
        }

        public bool Fill
        {
            get { return fillFeatures; }
            set { fillFeatures = value; dirty = true; }
        }

        public bool Stencil
        {
            get { return stencil; }
            set { stencil = value; }
        }

        public bool DrawLand
        {
            get { return drawLand; }
            set { drawLand = value; }
        }

        public bool DrawWater
        {
            get { return drawWater; }
            set { drawWater = value; }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
            double left = parentMap.BoundingBox.Map.left - 1;
            double right = parentMap.BoundingBox.Map.right + 1;
            double bottom = parentMap.BoundingBox.Map.bottom - 1;
            double top = parentMap.BoundingBox.Map.top + 1;

#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("DynamicVectorFile Draw()");
#endif

			// Test whether the current map is outside the data already read. If so, then read new data.
            if (dirty || (dataleft > left) || (dataright < right) || (datatop < top) || (databottom > bottom))
            {
                dirty = false; 
                // Create an OpenGL display list for this file
				CreateOpenGLDisplayList(TRACKING_CONTEXT);
                Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

                // Some OpenGL initialization
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_FLAT);

                int xmin = 10 * (int)(left / 10.0);
                int xmax = 10 * (int)(right / 10.0);
                int ymin = 10 * (int)(bottom / 10.0);
                int ymax = 10 * (int)(top / 10.0);
                if (ymax == 80) ymax = 70;  // tiles stop at 80 deg
                if (ymin == -80) ymin = -70;// tiles stop at -80 deg
                if (left < 0.0) xmin -= 10;
                if (right < 0.0) xmax -= 10;                 
                if (bottom < 0.0) ymin -= 10;
                if (top < 0.0) ymax -= 10;

                dataleft = (double)xmin;
                dataright = (double)(xmax + 10);
                databottom = (double)ymin;
                datatop = (double)(ymax + 10);

                int x, y, xx, yy;
                char ns, ew;

                if (xmin < -180 || xmin > 180)
                    xmin = (int)MapGL.NormalizeLongitude(xmin);
                if (xmax > 180 || xmax < -180)
                    xmax = (int)MapGL.NormalizeLongitude(xmax);

                int _xmin = xmin, _xmax = xmax;
                for (int i = 0; i <= 1; i++)
                {
                    if (_xmin > _xmax)
                    {
                        // the date line does intersect the map, so split the
                        // loops below into two pieces
                        if (i == 0)
                        {
                            // 1st time for the loops below go from xmin to 179
                            xmin = _xmin;
                            xmax = 179;
                        }
                        else
                        {
                            // 2nd time for the loops below go from -180 to xmax
                            xmin = -180;
                            xmax = _xmax;
                        }
                    }
                    else
                    {
                        // the date line doesn't intersect the map,
                        // so only do the loops below once and leave
                        // xmin and xmax as they are
                        if (i == 1)
                            break;
                    }

                    for (x = xmin; x <= xmax; x += 10)
                    {
                        xx = x;

                        if (x < 0)
                        {
                            xx = -x;
                            ew = 'W';
                        }
                        else ew = 'E';

                        for (y = ymin; y <= ymax; y += 10)
                        {
                            yy = y;
                            if (y < 0)
                            {
                                yy = -y;
                                ns = 'S';
                            }
                            else ns = 'N';

                            if (drawLand)
                            {
                                if (xx == 180)
                                    this.fileName = Directory + @"HighResMap\W180" + ns + yy + "1.shp";
                                else
									this.fileName = Directory + @"HighResMap\" + ew + xx + ns + yy + "1.shp";

                                LoadFromFile(FillType.Land);
                                if (openglDisplayList == -1)
                                    return;
                            }

                            if (drawWater)
                            {
                                if (xx == 180)
									this.fileName = Directory + @"HighResMap\W180" + ns + yy + "2.shp";
                                else
									this.fileName = Directory + @"HighResMap\" + ew + xx + ns + yy + "2.shp";

                                LoadFromFile(FillType.Water);
                                if (openglDisplayList == -1)
                                    return;
                            }
                        }
                    }
                }

                // End the OpenGL display list
                Gl.glEndList();
            }

            if (openglDisplayList == -1) return;

            if (stencil)
            {
                // Set the clear value for the stencil buffer, then clear it
                Gl.glClearStencil(0);
                Gl.glClear(Gl.GL_STENCIL_BUFFER_BIT);

                // Set the stencil buffer to write a 1 in every time
                // a pixel is written to the screen
                Gl.glStencilFunc(Gl.GL_ALWAYS, 1, 1);
                Gl.glStencilOp(Gl.GL_REPLACE, Gl.GL_REPLACE, Gl.GL_REPLACE);
            }

            if (parentMap.BoundingBox.Map.left < -180 || parentMap.BoundingBox.Map.right > 180)
                MapGL.DrawDisplayListWithShift(openglDisplayList, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);            
            else                            
                Gl.glCallList(openglDisplayList);                      
        }

        private unsafe void LoadFromFile(FillType fillType)
        {
            // TODO: This works with ESRI shapefiles.  It hasn't been tested with other vector formats.

            // Reset the total number of vertices and features for this file
            numVertices = 0;
            numFeatures = 0;

            //------------------------------------------------------------
            // First, fill the polygons by tessellating them
            //------------------------------------------------------------
            Color fillColor;
            if (fillType == FillType.Land)
                fillColor = landFillColor;
            else
                fillColor = waterFillColor;
			if (fillFeatures && fillColor != Color.Transparent)
			{
				int[] fillColorRGB = new int[3];
				int[] fillColor2RGB = new int[3];
				fillColorRGB[0] = fillColor.R; fillColorRGB[1] = fillColor.G; fillColorRGB[2] = fillColor.B;
				fillColor2RGB[0] = Color.Empty.R; fillColor2RGB[1] = Color.Empty.G; fillColor2RGB[2] = Color.Empty.B;
				TessellateVectorFile(fileName, fillColorRGB, false, fillColor2RGB, 100, MapProjections.CylindricalEquidistant);
			}

            //------------------------------------------------------------
            // Second, read the file again to draw the polygon borders
            //------------------------------------------------------------
            if (!drawBorder)
            {
                return;
            }
            try
            {
                // Register all OGR drivers
                GDAL.OGRRegisterAll();

                // Open the vector file
                void* driver = null;
                void* dataSource = GDAL.OGROpen(fileName, 0, ref driver);
                if (dataSource == null)
                {
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;
                }

                // Get the vector layer from the data source
                void* vectorLayer = null;
                int numLayers = GDAL.OGR_DS_GetLayerCount(dataSource);
                if (numLayers != 1)
                {
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                    return;
                }
                vectorLayer = GDAL.OGR_DS_GetLayer(dataSource, 0);
                GDAL.OGR_L_ResetReading(vectorLayer);

                // Set the border color and width
                Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
                Gl.glLineWidth(1f);

                // Extract the features
                void* feature = null;
                while ((feature = GDAL.OGR_L_GetNextFeature(vectorLayer)) != null)
                {
                    // Count the number of features
                    numFeatures++;

                    // Get the geometry object
                    void* geometry = null;
                    geometry = GDAL.OGR_F_GetGeometryRef(feature);

                    // Process the geometry object
                    if (geometry != null)
                    {
                        void* shape = null;
                        int numGeometries;

                        // Determine the type and number of geometries
                        int geometryType = GDAL.OGR_G_GetGeometryType(geometry);
                        numGeometries = GDAL.OGR_G_GetGeometryCount(geometry);

                        // Handle wkbLineString geometry type
                        // TODO: this needs to be tested for wkbMultiLineString
                        if (numGeometries == 0 && (geometryType == (int)GDAL.OGRwkbGeometryType.wkbLineString ||
                            geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiLineString))
                        {
                            int iPoints;
                            Gl.glBegin(Gl.GL_LINE_STRIP);
                            shape = geometry;

                            // Iterate the points of this feature
                            int nPoints = GDAL.OGR_G_GetPointCount(shape);
                            double lastx = 999.0, lasty = 999.0;
                            for (iPoints = 0; iPoints < nPoints; iPoints++)
                            {
                                double x = 0, y = 0, z = 0;

                                // Get the next point
                                GDAL.OGR_G_GetPoint(shape, iPoints, ref x, ref y, ref z);
                                if ((x == lastx) || (y == lasty))
                                {
                                    Gl.glEnd();
                                    Gl.glBegin(Gl.GL_LINE_STRIP);
                                }
                                Gl.glVertex2d(x, y);
                                numVertices++;
      
                                lastx = x;
                                lasty = y;
                            }

                            // End the GL polygon
                            Gl.glEnd();
                        }

                        // Iterate over the geometries
                        for (int i = 0; i < numGeometries; i++)
                        {
                            // Pull out the shape to process
                            shape = GDAL.OGR_G_GetGeometryRef(geometry, i);
                            if (geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiPolygon ||
                                geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiLineString ||
                                geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiPoint)
                                shape = GDAL.OGR_G_GetGeometryRef(shape, 0);

                            if (shape != null)
                            {
                                int iPoints;

                                // Begin the GL shape for this feature
                                switch (geometryType)
                                {
                                    case (int)GDAL.OGRwkbGeometryType.wkbPoint:
                                    case (int)GDAL.OGRwkbGeometryType.wkbMultiPoint:
                                        // TODO: this case needs to be tested
                                        Gl.glBegin(Gl.GL_POINTS);
                                        break;
                                    case (int)GDAL.OGRwkbGeometryType.wkbPolygon:
                                    case (int)GDAL.OGRwkbGeometryType.wkbMultiPolygon:
                                        Gl.glBegin(Gl.GL_LINE_STRIP);
                                        break;
                                    default:
                                        break;
                                }

                                // Iterate the points of this feature
                                int nPoints = GDAL.OGR_G_GetPointCount(shape);
                                double lastx = 999.0, lasty = 999.0;
                                for (iPoints = 0; iPoints < nPoints; iPoints++)
                                {
                                    double x = 0, y = 0, z = 0;

                                    // Get the next point
                                    GDAL.OGR_G_GetPoint(shape, iPoints, ref x, ref y, ref z);
                                    if (((x == lastx) && (x == 10.0 * (int)(x / 10.0)))
                                        || ((y == lasty) && (y == 10.0 * (int)(y / 10.0))))
                                    {
                                        Gl.glEnd();
                                        Gl.glBegin(Gl.GL_LINE_STRIP);
                                    }
                                    Gl.glVertex2d(x, y);
                                    numVertices++;
          
                                    lastx = x;
                                    lasty = y;
                                }

                                // End the GL polygon
                                Gl.glEnd();
                            }
                        }
                    }
                    GDAL.OGR_F_Destroy(feature);
                }
                GDAL.OGR_DS_Destroy(dataSource);
            }
            catch
            {
				DeleteOpenGLDisplayList(TRACKING_CONTEXT);
                return;
            }
        }
    }
}


using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
    /**
     * \class DEM
     * \brief Represents a Digital Elevation Model raster
     */
    public class DEMRaster : Feature
    {
        #region Data Members
        protected const short noData = -9999;
        protected const float dataRange = 9159;
        protected const short minElev = -407;
        protected string fileName;			// data file name
        protected int width;				// width of the contained raster image
        protected int height;				// height of the contained raster image
        protected byte[] image;			    // the image pixels
        protected byte alphaBlend;			// 0=transparent, 255=opaque
        protected Color transparentColor;	// this color is not rendered
        protected double[] geoTransform;	// from GDAL; holds contents of ESRI World File
        protected bool drawable;			// indicates whether the image can be drawn
        #endregion

        public DEMRaster(string rasterFileName) : this(rasterFileName, string.Empty, string.Empty)
        {
        }

        public DEMRaster(string rasterFileName, string featureName, string featureInfo)
        {
            // Check input file name
            if (Object.Equals(rasterFileName, null))
                throw new WSIMapException("Raster file name is null");
            if (rasterFileName.Equals(string.Empty))
                throw new WSIMapException("Raster file name is empty");

            // Set fields
            this.fileName = rasterFileName;
            this.featureName = featureName;
            this.featureInfo = featureInfo;
            this.numVertices = 0;
            this.alphaBlend = 255;
            this.transparentColor = Color.FromArgb(0, 0, 0);
            this.drawable = false;
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public byte[] Image
        {
            get { return image; }
        }

        public int Transparency
        {
            get { return alphaBlend; }
            set
            {
                if (value < byte.MinValue) value = byte.MinValue;
                if (value > byte.MaxValue) value = byte.MaxValue;
                alphaBlend = Convert.ToByte(value);
                SetAlpha();
            }
        }

        public Color Transparent
        {
            get { return transparentColor; }
            set { transparentColor = value; SetAlpha(); }
        }

        public string FileName
        {
            get { return fileName; }
        }

        protected void SetAlpha()
        {
            for (long i = 3; i < width * height * 4; i += 4)
            {
                if (image[i - 3] == transparentColor.R && image[i - 2] == transparentColor.G && image[i - 1] == transparentColor.B)
                    image[i] = 0;
                else
                    image[i] = alphaBlend;
            }
        }

        protected unsafe bool LoadFromFile()
        {
            // Register the GDAL drivers
            GDAL.GDALAllRegister();

            // Open the raster file
            void* dataSet = GDAL.GDALOpen(fileName, 0);
            if (dataSet == null)
            {
                drawable = false;
                return false;
            }

            // Get the geotransform information
            geoTransform = new double[6];
            fixed (double* pGeoTransform = geoTransform)
            {
                int result = GDAL.GDALGetGeoTransform(dataSet, pGeoTransform);
                if (result != 0)
                {
                    drawable = false;
                    return false;
                }
            }

            // Get handles to the raster band (assumes there is only one)
            void* rasterBandR = GDAL.GDALGetRasterBand(dataSet, 1);

            // Get the dimensions of the raster bands
            width = GDAL.GDALGetRasterBandXSize(rasterBandR);
            height = GDAL.GDALGetRasterBandYSize(rasterBandR);

            // Read the raster band
            int error;
            short[] R = new short[width * height];
            fixed (void* r = R)
            {
                error = GDAL.GDALRasterIO(rasterBandR, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, r, width, height, GDAL.GDALDataType.GDT_Int16, 0, 0);
                if (error != 0)
                {
                    drawable = false;
                    return false;
                }
            }

            // Make the raster band into RGBA pixels
            long index, n = 0;
            byte value;
            image = new byte[width * height * 4];
            for (long j = height; j > 0; j--)
            {
                for (long i = 0; i < width; i++)
                {
                    index = ((j - 1) * width) + i;
                    if (R[index] != noData)
                        value = Convert.ToByte(((float)(R[index] - minElev) / dataRange) * 255f);
                    else
                        value = 0;
                    image[n * 4] = ColorTables.ColorTable_elevgbsncap[value].R;
                    image[(n * 4) + 1] = ColorTables.ColorTable_elevgbsncap[value].G;
                    image[(n * 4) + 2] = ColorTables.ColorTable_elevgbsncap[value].B;
                    if (R[index] == noData)
                        image[(n * 4) + 3] = 0;
                    else
                        image[(n * 4) + 3] = alphaBlend;	// alpha blending
                    n++;
                }
            }

            // Done
            GDAL.GDALClose(dataSet);
            drawable = true;
            return true;
        }

        public override void Refresh()
        {
            LoadFromFile();
        }

        internal override void Create()
        {
            LoadFromFile();
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
            if (!drawable) return;

            // Position the image
            double lrX = geoTransform[0];
            double lrY = geoTransform[3] + (geoTransform[5] * height);
            SetRasterPos(lrX, lrY);

            // Image degrees per pixel in x & y directions
            float dx = (float)geoTransform[1];
            float dy = (float)Math.Abs(geoTransform[5]);

            // Calculate the rendering scale factors
            float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

            // Draw the pixels
            Gl.glPixelZoom(xFactor * dx, yFactor * dy);
            Gl.glDrawPixels(width, height, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);
        }

        private void SetRasterPos(double x, double y)
        {
            double winx, winy, winz;
            double[] modelMatrix = new double[16];
            double[] projMatrix = new double[16];
            int[] viewport = new int[4];
            byte[] bitmap = new byte[1];

            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelMatrix);
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projMatrix);
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
            viewport[0] = 0;
            viewport[1] = 0;

            Glu.gluProject(x, y, 0.0, modelMatrix, projMatrix, viewport, out winx, out winy, out winz);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Gl.glOrtho(viewport[0], viewport[2], viewport[1], viewport[3], 0.0, 1.0);
            Gl.glRasterPos3d(0.0, 0.0, -winz);
            Gl.glBitmap(0, 0, 0, 0, (float)winx, (float)winy, bitmap);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }
    }
}

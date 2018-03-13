using System;
using System.Drawing;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class Raster
	 * \brief Represents a raster image
	 */
    public class Raster : Feature, IDisposable
	{
		#region Data Members
		protected bool loadFromFile;		// is this raster from a file?
		protected string fileName;			// if it is, here's the name
		protected int width;				// width of the contained raster image
		protected int height;				// height of the contained raster image
		protected byte[] image;				// the image pixels
		protected byte alphaBlend;			// 0=transparent, 255=opaque
		protected Color transparentColor;	// this color is not rendered
		protected double[] geoTransform;	// from GDAL; holds contents of ESRI World File
		protected bool drawable;			// indicates whether the image can be drawn
        private double top;					// top of the image (deg lat)
        private double left;				// left of the image (deg lon)
        private double bottom;				// bottom of the image (deg lat)
        private double right;				// right of the image (deg lon)
        private bool textured;
        private int texture = -1;
        private bool isGrib2;
        private bool useStenciling;
        protected int[] fullImage;
        protected int fullImageSize;
		#endregion

        public Raster()
        {
            this.loadFromFile = false;
            this.fileName = string.Empty;
            this.featureName = string.Empty;
            this.featureInfo = string.Empty;
            this.numVertices = 0;
            this.alphaBlend = 255;
            //this.transparentColor = Color.FromArgb(0, 0, 0);
            this.transparentColor = Color.FromArgb(ColorTables.TRANSPARENT_COLOR.R, ColorTables.TRANSPARENT_COLOR.G, ColorTables.TRANSPARENT_COLOR.B);
            this.drawable = false;
            this.textured = false;
            this.useStenciling = false;
        }

		public Raster(string rasterFileName) : this(rasterFileName, string.Empty, string.Empty)
		{
		}

		public Raster(string rasterFileName, string featureName, string featureInfo)
		{
			// Check input file name
			if (Object.Equals(rasterFileName,null))
				throw new WSIMapException("Raster file name is null");
			if (rasterFileName.Equals(string.Empty))
				throw new WSIMapException("Raster file name is empty");

			// Set fields
			this.loadFromFile = true;
			this.fileName = rasterFileName;
			this.featureName = featureName;
			this.featureInfo = featureInfo;
			this.numVertices = 0;
			this.alphaBlend = 255;
			this.transparentColor = Color.FromArgb(0,0,0);
			this.drawable = false;
            this.textured = true;
            this.useStenciling = false;
		}

		public Raster(byte[] rgba, int width, int height, double[] geoInfo) : this(rgba, width, height, geoInfo, string.Empty, string.Empty)
		{
		}

		public Raster(byte[] rgba, int width, int height, double[] geoInfo, string featureName, string featureInfo)
		{
			// Check the inputs
			if (Object.Equals(rgba,null))
				throw new WSIMapException("Image array is null");
			if ((width * height * 4) != rgba.Length)
				throw new WSIMapException("Image array is the wrong size");
			if (Object.Equals(geoInfo,null))
				throw new WSIMapException("Geographic information array is null");

			// Set fields
			this.loadFromFile = false;
			this.image = rgba;
			this.width = width;
			this.height = height;
			this.geoTransform = geoInfo;
			this.fileName = string.Empty;
			this.featureName = featureName;
			this.featureInfo = featureInfo;
			this.numVertices = 0;
			this.alphaBlend = 255;
			this.transparentColor = Color.FromArgb(0,0,0);            
			this.drawable = true;
            this.useStenciling = false;
		}

        public Raster(byte[] rgba, int[] fullImagergba, int width, int height, double[] geoInfo, string featureName, string featureInfo)
        {
            //Check the inputs
            if (Object.Equals(rgba, null))
                throw new WSIMapException("Image array is null");
            if ((width * height * 4) != rgba.Length)
                throw new WSIMapException("Image array is the wrong size");
            if (Object.Equals(geoInfo, null))
                throw new WSIMapException("Geographic information array is null");

            // Set fields
            this.loadFromFile = false;
            this.image = rgba;
            this.fullImage = fullImagergba;
            this.width = width;
            this.height = height;
            this.geoTransform = geoInfo;
            this.fileName = string.Empty;
            this.featureName = featureName;
            this.featureInfo = featureInfo;
            this.numVertices = 0;
            this.alphaBlend = 255;
            this.transparentColor = Color.FromArgb(0, 0, 0);
            this.drawable = true;
            this.useStenciling = false;
        }

		public int Width
		{
            set { width = value; }
			get { return width; }
		}

		public int Height
		{
            set { height = value; }
			get { return height; }
		}

        public double Top
        {
            set { top = value; }
        }

        public double Bottom
        {
            set { bottom = value; }
        }

        public double Left
        {
            set { left = value; }
        }

        public double Right
        {
            set { right = value; }
        }

		public byte[] Image
		{
            //get { return image; }
            //set { image = value; }

            get { return image; }
            set { 
                    if(value.Length == this.fullImageSize)
                    {
                        Array.Copy(value, this.image, value.Length);
                    }
                    else
                    {
                        this.fullImageSize = value.Length;
                        this.image = value;
                    }       
                }
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
			set	{ transparentColor = value; SetAlpha(); }
		}

		public string FileName
		{
			get { return fileName; }
		}

        public int[] FullImage
        {
            get { return fullImage; }
            set { 
                    if(value.Length == this.fullImageSize)
                    {
                        Array.Copy(value, this.fullImage, value.Length);
                    }
                    else
                    {
                        this.fullImageSize = value.Length;
                        this.fullImage = value;
                    }       
                }
        }

        public double[] GeoInform
        {
            get { return this.geoTransform; }
            set { this.geoTransform = value;}
        }

        public bool IsGrib2
        {
            get { return isGrib2; }
            set { this.isGrib2 = value; }
        }

        public bool UpdateFromArray(byte[] rgba)
		{
			if (rgba.Length == this.image.Length)
			{
				this.loadFromFile = false;
				this.image = rgba;
				this.fileName = string.Empty;
				return true;
			}
			else
				return false;
		}

		public bool UpdateFromArray(byte[] rgba, int width, int height, double[] geoInfo)
		{
			this.loadFromFile = false;
			this.image = rgba;
			this.width = width;
			this.height = height;
			this.geoTransform = geoInfo;
			this.fileName = string.Empty;
			return true;
		}

		public bool UpdateFromFile()
		{
			this.loadFromFile = true;
			return LoadFromFile();
		}

		public bool UpdateFromFile(string rasterFileName)
		{
			this.loadFromFile = true;
			this.fileName = rasterFileName;
			return LoadFromFile();
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

        public bool UpdateFromPNGFile1()
        {
            this.loadFromFile = true;
            this.textured = true;
            return LoadFromPNGFile();
        }

        public void UpdateFromPNGFile()
        {
            //Bitmap image = null;
            //try
            //{
            //    // If the file doesn't exist or can't be found, an ArgumentException is thrown instead of
            //    // just returning null
            //    image = new Bitmap(fileName);
            //}
            //catch (System.ArgumentException)
            //{
            //    image = null;
            //}

            //try
            //{
            //    if (image != null)
            //    {
            //        //image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            //        //System.Drawing.Imaging.BitmapData bitmapdata;
            //        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            //        System.Drawing.Imaging.PixelFormat format = image.PixelFormat;
            //        if (fileName.EndsWith("bmp"))
            //            format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

            //        bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, format);

            //        //Gl.glGenTextures(1, out texture);

            //        //// Create Linear Filtered Texture
            //        //Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
            //        //Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            //        //Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);

            //        //int iFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_RGBA : Gl.GL_RGB;
            //        //int eFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_BGRA : Gl.GL_BGR;

            //        //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, iFormat, image.Width, image.Height, 0, eFormat, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

            //        //image.UnlockBits(bitmapdata);
            //        //image.Dispose();
            //        drawable = true;
            //        textured = true;
            //    }
            //}
            //catch (Exception)
            //{

            //}
        }

        protected unsafe bool LoadFromPNGFile()
        {
            if (!loadFromFile) 
                return true;

			try
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
                
				// Determine the number of raster bands in the file
				int bandCount = GDAL.GDALGetRasterCount(dataSet);
                if (bandCount != 4)
                {
                    drawable = false;
                    return false;
                }

                // Get a handle to the raster band R
                void* rasterBand1 = GDAL.GDALGetRasterBand(dataSet, 1);
                GDAL.GDALDataType dataType1 = GDAL.GDALGetRasterDataType(rasterBand1);
                if (dataType1 != GDAL.GDALDataType.GDT_Byte) // handle only GDT_Byte for now
                {
                    GDAL.GDALClose(dataSet);
                    drawable = false;
                    return false;
                }
                width = GDAL.GDALGetRasterBandXSize(rasterBand1);
                height = GDAL.GDALGetRasterBandYSize(rasterBand1);
               
                byte[] r = new byte[width * height];
                fixed (void* p = r)
                {
                    int error = GDAL.GDALRasterIO(rasterBand1, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, p, width, height, GDAL.GDALDataType.GDT_Byte, 0, 0);
                    if (error != 0)
                    {
                        GDAL.GDALClose(dataSet);
                        drawable = false;
                        return false;
                    }
                }

                // Get a handle to the raster band G
                void* rasterBand2 = GDAL.GDALGetRasterBand(dataSet, 2);
                GDAL.GDALDataType dataType2 = GDAL.GDALGetRasterDataType(rasterBand2);
                if (dataType2 != GDAL.GDALDataType.GDT_Byte) // handle only GDT_Byte for now
                {
                    GDAL.GDALClose(dataSet);
                    drawable = false;
                    return false;
                }
                byte[] g = new byte[width * height];
                fixed (void* p = g)
                {
                    int error = GDAL.GDALRasterIO(rasterBand2, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, p, width, height, GDAL.GDALDataType.GDT_Byte, 0, 0);
                    if (error != 0)
                    {
                        GDAL.GDALClose(dataSet);
                        drawable = false;
                        return false;
                    }
                }

                // Get a handle to the raster band B
                void* rasterBand3 = GDAL.GDALGetRasterBand(dataSet, 3);
                GDAL.GDALDataType dataType3 = GDAL.GDALGetRasterDataType(rasterBand3);
                if (dataType3 != GDAL.GDALDataType.GDT_Byte) // handle only GDT_Byte for now
                {
                    GDAL.GDALClose(dataSet);
                    drawable = false;
                    return false;
                }
                byte[] b = new byte[width * height];
                fixed (void* p = b)
                {
                    int error = GDAL.GDALRasterIO(rasterBand3, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, p, width, height, GDAL.GDALDataType.GDT_Byte, 0, 0);
                    if (error != 0)
                    {
                        GDAL.GDALClose(dataSet);
                        drawable = false;
                        return false;
                    }
                }

                // Get a handle to the raster band A
                void* rasterBand4 = GDAL.GDALGetRasterBand(dataSet, 4);
                GDAL.GDALDataType dataType4 = GDAL.GDALGetRasterDataType(rasterBand4);
                if (dataType4 != GDAL.GDALDataType.GDT_Byte) // handle only GDT_Byte for now
                {
                    GDAL.GDALClose(dataSet);
                    drawable = false;
                    return false;
                }
                byte[] a = new byte[width * height];
                fixed (void* p = a)
                {
                    int error = GDAL.GDALRasterIO(rasterBand4, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, p, width, height, GDAL.GDALDataType.GDT_Byte, 0, 0);
                    if (error != 0)
                    {
                        GDAL.GDALClose(dataSet);
                        drawable = false;
                        return false;
                    }
                }
				// Done
				GDAL.GDALClose(dataSet);

                int index, n = 0;
                image = new byte[width * height * 4];
                for (int j = height; j > 0; j--)
                {
                    for (int i = 0; i < width; i++)
                    {
                        index = ((j - 1) * width) + i;
                        //GDAL.GDALGetColorEntryAsRGB(colorTable, pixels[index], &color);
                        image[n * 4] = r[index];
                        image[(n * 4) + 1] = b[index];
                        image[(n * 4) + 2] = g[index];
                        image[(n * 4) + 3] = a[index];
                        n++;
                    }
                }

				drawable = true;
				return true;
			}
			catch
			{
				drawable = false;
				return false;
			}
		}

		protected unsafe bool LoadFromFile()
		{
			// NOTE: This method works with PNG & GIF; other formats have not been tested.

			// Don't try to load it from a file if the image data was set in the constructor
			if (!loadFromFile) return true;

			try
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

				// Read the world file
				geoTransform = new double[6];
                fixed (double* pGeoTransform = geoTransform)
                {
                    int result = GDAL.GDALReadWorldFile(fileName, null, pGeoTransform);
                    if (result == 0)
                    {
                        GDAL.GDALClose(dataSet);
                        drawable = false;
                        return false;
                    }
                }
                
				// Determine the number of raster bands in the file
				int bandCount = GDAL.GDALGetRasterCount(dataSet);

				// Read the file based on the number of raster bands
				if (bandCount == 4)	// e.g. PNG
				{
					// Get a handle to the raster band
					void* rasterBand1 = GDAL.GDALGetRasterBand(dataSet, 1);

					// Determine the data type contained in the raster band (see the GDALDataType enumeration)
					GDAL.GDALDataType dataType = GDAL.GDALGetRasterDataType(rasterBand1);
					if (dataType != GDAL.GDALDataType.GDT_Byte) // handle only GDT_Byte for now
					{
						GDAL.GDALClose(dataSet);
						drawable = false;
						return false;
					}

					// Get the color table for the image
					void* colorTable = GDAL.GDALGetRasterColorTable(rasterBand1);
					GDAL.GDALPaletteInterp palette = GDAL.GDALGetPaletteInterpretation(colorTable);
					if (palette != GDAL.GDALPaletteInterp.GPI_RGB) // handle only RGB for now
					{
						GDAL.GDALClose(dataSet);
						drawable = false;
						return false;
					}

					// Get the dimensions of the raster band
					width = GDAL.GDALGetRasterBandXSize(rasterBand1);
					height = GDAL.GDALGetRasterBandYSize(rasterBand1);

					// Get the image pixels
					int error;
					byte[] pixels = new byte[width * height];
					fixed (void* p = pixels)
					{
						error = GDAL.GDALRasterIO(rasterBand1, GDAL.GDALRWFlag.GF_Read, 0, 0, width, height, p, width, height, GDAL.GDALDataType.GDT_Byte, 0, 0);
						if (error != 0)
						{
							GDAL.GDALClose(dataSet);
							drawable = false;
							return false;
						}
					}

					// Convert the image pixels into RGBA pixels using the color table
					int index, n = 0;
					GDAL.GDALColorEntry color;
					image = new byte[width * height * 4];
					for (int j = height; j > 0; j--)
					{
						for (int i = 0; i < width; i++)
						{
							index = ((j - 1) * width) + i;
							GDAL.GDALGetColorEntryAsRGB(colorTable, pixels[index], &color);
							image[n * 4] = (byte)color.c1;
							image[(n * 4) + 1] = (byte)color.c2;
							image[(n * 4) + 2] = (byte)color.c3;
							image[(n * 4) + 3] = (byte)color.c4;
							n++;
						}
					}

				}
				else
				{
					GDAL.GDALClose(dataSet);
					drawable = false;
					return false;
				}

				// Done
				GDAL.GDALClose(dataSet);
				drawable = true;
				return true;
			}
			catch
			{
				drawable = false;
				return false;
			}
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Raster Draw()");
#endif

			Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            if (textured)
                DrawImageAsTexture(parentMap);
            else if (isGrib2)              
                DrawDecodedRaster(parentMap, image, width, height, 1);
            else
                DisplayImage(parentMap);

            Gl.glDisable(Gl.GL_BLEND);
        }

        private void DisplayImage(MapGL parentMap)
        {
			// Position the image
            double lrX = geoTransform[0];
            double lrY = geoTransform[3] + (geoTransform[5] * height);
			SetRasterPos(lrX,lrY);

			// Image degrees per pixel in x & y directions
			float dx = (float)geoTransform[1];
			float dy = (float)Math.Abs(geoTransform[5]);

			// Calculate the rendering scale factors
            float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

			// Draw the pixels
			Gl.glPixelStorei(Gl.GL_UNPACK_ALIGNMENT, 1);
            Gl.glPixelStorei(Gl.GL_PACK_ROW_LENGTH, width);
            Gl.glPixelZoom(xFactor * dx, yFactor * dy);
			Gl.glDrawPixels(width, height, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);
		}       

        private void DrawDecodedRaster(MapGL parentMap, byte[] pixels, int w, int h, int m)
        {
            double lrX = geoTransform[0];
            double lrY = geoTransform[3] + (geoTransform[5] * height);

            // Position the image
            double _x = parentMap.DenormalizeLongitude(lrX);
            if (_x > parentMap.BoundingBox.Map.right)
                _x -= 360.0;

            SetRasterPos(_x, lrY);

            // Image degrees per pixel in x & y directions
            float dx = (float)geoTransform[1] * m;
            float dy = (float)Math.Abs(geoTransform[5]) * m;

            // disable stenciling to improve performance
            if (useStenciling)
            {
                Gl.glEnable(Gl.GL_STENCIL_TEST);
                Gl.glStencilFunc(Gl.GL_EQUAL, 1, 1);
                Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP);
            }
            else
                Gl.glDisable(Gl.GL_STENCIL_TEST);

            // Draw the pixels
            Gl.glPixelStorei(Gl.GL_UNPACK_ALIGNMENT, 1);
            Gl.glPixelZoom((float)parentMap.ScaleX * dx, (float)parentMap.ScaleY * dy);
            Gl.glDrawPixels(w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);

            if (_x + 360.0 < parentMap.BoundingBox.Map.right)
            {
                // display the image again offset to the right by 360 degrees
                _x += 360.0;
                SetRasterPos(_x, lrY);
                Gl.glDrawPixels(w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);
            }
            else if (_x - 360.0 + width * geoTransform[1] > parentMap.BoundingBox.Map.left)
            {
                // display the image again offset to the left by 360 degrees
                _x -= 360;
                SetRasterPos(_x, lrY);
                Gl.glDrawPixels(w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);
            }

            // re-endable stenciling for other features that may need it
            if (!useStenciling)
                Gl.glEnable(Gl.GL_STENCIL_TEST);
        }

        private void SetRasterPos(double x, double y)
		{
			double winx, winy, winz;
			double[] modelMatrix = new double[16];
			double[] projMatrix = new double[16];
			int[] viewport = new int[4];
			byte[] bitmap = new byte[1];

			Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX,modelMatrix);
			Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX,projMatrix);
			Gl.glGetIntegerv(Gl.GL_VIEWPORT,viewport);
			viewport[0] = 0;
			viewport[1] = 0;

			Glu.gluProject(x,y,0.0,modelMatrix,projMatrix,viewport,out winx,out winy,out winz);

			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPushMatrix();
			Gl.glLoadIdentity();
			
            Gl.glOrtho(viewport[0], viewport[2], viewport[1], viewport[3], 0.0, 1.0);
			Gl.glRasterPos3d(0.0, 0.0, -winz);
			Gl.glBitmap(0,0,0,0,(float)winx,(float)winy,bitmap);

			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPopMatrix();
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glPopMatrix();
		}

        private void DrawImageAsTexture(MapGL parentMap)
        {
            Console.WriteLine("{0}, {1}", featureName, texture);
            if (texture == -1)
            {
                Bitmap image = null;
                try
                {
                    // If the file doesn't exist or can't be found, an ArgumentException is thrown instead of
                    // just returning null
                    if (fileName.StartsWith("http"))
                    {
                        System.Net.WebRequest request = System.Net.WebRequest.Create(fileName);
                        System.Net.WebResponse response = request.GetResponse();
                        System.IO.Stream responseStream = response.GetResponseStream();
                        image = new Bitmap(responseStream);
                    }
                    else
                        image = new Bitmap(fileName);
                }
                catch (System.ArgumentException)
                {
                    image = null;
                }

                try
                {
                    if (image != null)
                    {
                        image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        //System.Drawing.Imaging.BitmapData bitmapdata;
                        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                        System.Drawing.Imaging.PixelFormat format = image.PixelFormat;
                        if (fileName.EndsWith("bmp"))
                            format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

                        System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, format);

                        //Gl.glShadeModel(Gl.GL_SMOOTH);
                        //if (texture == -1)
                        Gl.glGenTextures(1, out texture);
                        //Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);

                        Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);

                        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
                        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                        Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
                        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
                        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

                        //int iFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_RGBA : Gl.GL_RGB;
                        //int eFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_BGRA : Gl.GL_BGR;

                        Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, image.Width, image.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

                        image.UnlockBits(bitmapdata);
                        image.Dispose();

                        if (texture == -1)
                            return;
                    }
                }
                catch (Exception)
                {

                }
            }

            int crossings = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
            // Is the International Date Line in the map?
            bool idlInMap = false;
            if (parentMap.BoundingBox.Map.normLeft > parentMap.BoundingBox.Map.normRight)
                idlInMap = true;

            double l = left;
            double r = right;
            if (crossings != 0)
            {
                l = left + crossings * 360;
                r = right + crossings * 360;

                if (idlInMap)
                {
                    if (l > parentMap.BoundingBox.Map.right || r < parentMap.BoundingBox.Map.left)
                    {
                        if (crossings > 0)
                        {
                            l -= 360;
                            r -= 360;
                        }
                        else
                        {
                            l += 360;
                            r += 360;
                        }
                    }
                }
            }
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);

            //glTexSubImage2D(GL_TEXTURE_2D, 0, 12, 44, subImageWidth, subImageHeight, GL_RGBA, GL_UNSIGNED_BYTE, subImage);

            Gl.glEnable(Gl.GL_TEXTURE_2D);									// Enable Texture Mapping
            Gl.glShadeModel(Gl.GL_SMOOTH);									// Enable Smooth Shading

            Gl.glBegin(Gl.GL_QUADS);
            Gl.glTexCoord2f(0.0f, 0.0f);
            Gl.glVertex3d(l, bottom, 0.0);
            Gl.glTexCoord2f(1.0f, 0.0f);
            Gl.glVertex3d(r, bottom, 0.0);
            Gl.glTexCoord2f(1.0f, 1.0f);
            Gl.glVertex3d(r, top, 0.0);
            Gl.glTexCoord2f(0.0f, 1.0f);
            Gl.glVertex3d(l, top, 0.0);
            Gl.glEnd();

            Gl.glDisable(Gl.GL_TEXTURE_2D);
        }

		//private void DrawImageAsTexture1(MapGL parentMap)
		//{
		//	if (image != null)
		//	{
		//		int crossings = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
		//		// Is the International Date Line in the map?
		//		bool idlInMap = false;
		//		if (parentMap.BoundingBox.Map.normLeft > parentMap.BoundingBox.Map.normRight)
		//			idlInMap = true;
                
		//		double l = left;
		//		double r = right;
		//		if (crossings != 0)
		//		{
		//			l = left + crossings * 360;
		//			r = right + crossings * 360;

		//			if (idlInMap)
		//			{
		//				if (l > parentMap.BoundingBox.Map.right || r < parentMap.BoundingBox.Map.left)
		//				{
		//					if (crossings > 0)
		//					{
		//						l -= 360;
		//						r -= 360;
		//					}
		//					else
		//					{
		//						l += 360;
		//						r += 360;
		//					}
		//				}
		//			}
		//		}

		//		Gl.glShadeModel(Gl.GL_SMOOTH);
		//		if (texture == -1)
		//			Gl.glGenTextures(1, out texture);
		//		Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
		//		Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);

		//		Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
		//		Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
		//		Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
		//		Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
		//		Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
		//		Gl.glEnable(Gl.GL_TEXTURE_2D);
		//		Gl.glBegin(Gl.GL_QUADS);
		//		Gl.glTexCoord2f(0.0f, 0.0f);
		//		Gl.glVertex3d(l, bottom, 0.0);
		//		Gl.glTexCoord2f(1.0f, 0.0f);
		//		Gl.glVertex3d(r, bottom, 0.0);
		//		Gl.glTexCoord2f(1.0f, 1.0f);
		//		Gl.glVertex3d(r, top, 0.0);
		//		Gl.glTexCoord2f(0.0f, 1.0f);
		//		Gl.glVertex3d(l, top, 0.0);
		//		Gl.glEnd();

		//		Gl.glDisable(Gl.GL_TEXTURE_2D);

		//		//Gl.glDeleteTextures(1, texture);
		//	}
		//}

        public void Dispose()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Raster Dispose()");
#endif

			// Deletes undrawn textures; if deleteAll is true it deletes all existing textures
            if (textured && texture != -1)
                Gl.glDeleteTextures(1, ref texture);
        }
	}
}

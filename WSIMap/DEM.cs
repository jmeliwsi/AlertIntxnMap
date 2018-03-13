using System;
using System.Drawing;
using System.Threading;
using Tao.OpenGl;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;

namespace WSIMap
{
    /**
     * \class DEM
     * \brief Represents a Digital Elevation Model raster
     */
    internal class DEM : Feature
    {  
        #region Data Members
        protected string    fileName;			// data file name
        protected int       width;				// width of the contained raster image
        protected int       height;				// height of the contained raster image
        protected byte[]    image;				// the image pixels
        protected byte      alphaBlend;			// 0=transparent, 255=opaque
        protected Color     transparentColor;	// this color is not rendered
        protected bool      drawable;			// indicates whether the image can be drawn
        private ColorTables.ByteQuad[] color_table;
        private uint[]      texture;
        private double      leftShift;
        private double      previousLeftShift;
        private double      left;
        private double      bottom;
        private double      right;
        private double      top;
        private float       dx, dy;
        private int         current_level;        
        #endregion

        public DEM(string rasterFileName, ColorTables.ByteQuad[] color_table, int level, double left_with_shift)
            : this(rasterFileName, color_table, string.Empty, string.Empty, level, left_with_shift)
        {
        }

        public DEM(string rasterFileName, ColorTables.ByteQuad[] color_table, string featureName, string featureInfo, int level, double left_with_shift)
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
            this.alphaBlend = 180;
            this.transparentColor = Color.FromArgb(0, 0, 0);            
            this.drawable = false;

            this.current_level = level;

            this.texture = null;
            this.openglDisplayList = -1;

            this.color_table = color_table;
            this.leftShift = left_with_shift;
            this.previousLeftShift = left_with_shift;
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

        public double Shift
        {             
            set { leftShift = value; }
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

        internal override void Create()
        {
        }

        public override void Refresh()
        { 
            image = LoadFromBinFile(fileName);
            drawable = true;          
        }       

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
            if (!drawable) return;
            CreateAndDisplayTexture(parentMap.ScaleFactor, parentMap.BoundingBox.Map.right);
            //DrawImageByRaster(parentMap);            
        }

        private void DrawImageByRaster(MapGL parentMap)
        {
            // Position the image          
            SetRasterPos(left, bottom);

            // Calculate the rendering scale factors
            float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

            // Draw the pixels    
            double pixel_scale_w = xFactor * (right - left) / width;
            double pixel_scale_h = xFactor * (top - bottom) / height;

            Gl.glPixelZoom((float)pixel_scale_w, (float)pixel_scale_h);
            Gl.glDrawPixels(width, height, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);            
        }
         
        private byte[] LoadFromBinFile(string file)
        {
            FileStream inStream = File.OpenRead(file);

            // use a BinaryReader to read formatted data and dump it to the screen
            BinaryReader binary_reader = new BinaryReader(inStream);

            //Header
            left = (double)binary_reader.ReadDecimal();
            bottom = (double)binary_reader.ReadDecimal();
            right = (double)binary_reader.ReadDecimal();
            top = (double)binary_reader.ReadDecimal();

            dx = (float)binary_reader.ReadDecimal();
            dy = (float)binary_reader.ReadDecimal();

            width = binary_reader.ReadInt32();
            height = binary_reader.ReadInt32();
            byte[] image = null;

            // load pixels of image
            int count = binary_reader.ReadInt32();
            image = new byte[width * height * 4];

            count = width * height;

            int n = 0;
            byte value = 0;
            for (int i = 0; i != count; i++)
            {
                value = (byte)inStream.ReadByte();

                // Decode the pixel value to a color
                ColorTables.ByteQuad color = color_table[value];
                if (value == 0) color = color_table[253];

                image[n * 4] = color.R;
                image[(n * 4) + 1] = color.G;
                image[(n * 4) + 2] = color.B;
                image[(n * 4) + 3] = Math.Min(color.A, alphaBlend); // alpha blending                  

                ++n;
            }

            binary_reader.Close();
            inStream.Close();
            inStream.Dispose();

            return image;
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

        private void CreateAndDisplayTexture(double scale_factor, double rightBorder)
        {
            if (image != null)
            {             
                double d_left = leftShift;
                double d_right = d_left + (right - left);
                previousLeftShift = leftShift;

                Gl.glShadeModel(Gl.GL_SMOOTH);

                texture = new uint[1];
                Gl.glGenTextures(1, texture);
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[0]);
                Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);

                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

                Gl.glEnable(Gl.GL_TEXTURE_2D);
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3d(d_left, bottom, 0.0);
                Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3d(d_right, bottom, 0.0);
                Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3d(d_right, top, 0.0);
                Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3d(d_left, top, 0.0);
                Gl.glEnd();

                if (scale_factor < 2.1 && d_right > rightBorder)
                {
                    d_left -= 360.0;
                    d_right -= 360.0;
                    Gl.glBegin(Gl.GL_QUADS);
                    Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3d(d_left, bottom, 0.0);
                    Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3d(d_right, bottom, 0.0);
                    Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3d(d_right, top, 0.0);
                    Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3d(d_left, top, 0.0);
                    Gl.glEnd();
                }
                    
                Gl.glDisable(Gl.GL_TEXTURE_2D);

                Gl.glDeleteTextures(1, texture);
                texture = null;
            }
        }

		public void UpdateColor(ColorTables.ByteQuad[] new_color_table)
        {
            color_table = new_color_table;
        }
    }    
        
}



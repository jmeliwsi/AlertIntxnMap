using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Tao.OpenGl;	

namespace WSIMap
{
    public class RasterTile : Feature, IDisposable
    {
        #region Data Members
        protected string fileName;			// if it is, here's the name
        protected bool drawable;			// indicates whether the image can be drawn
        private double top;					// top of the image (deg lat)
        private double left;				// left of the image (deg lon)
        private double bottom;				// bottom of the image (deg lat)
        private double right;				// right of the image (deg lon)
        private int texture = -1;
        private int opacity;
		private byte[] imageData;
		private bool convertPng;
        #endregion

        public RasterTile()
            : this(string.Empty, string.Empty, string.Empty)
        {

        }

        public RasterTile(string file)
            : this(file, string.Empty, string.Empty)
        {

        }

        public RasterTile(string file, string name, string info)
        {
            fileName = file;
            featureInfo = info;
            featureName = name;
            drawable = false;
			convertPng = false;
        }

		public RasterTile(byte[] imageData, string name, string info)
		{
			this.imageData = imageData;
			featureInfo = info;
			featureName = name;
			drawable = false;
			fileName = String.Empty;
			convertPng = true;
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

        public int Transparency
        {
            get { return opacity; }
            set { opacity = value; }
        }

        public void UpdateTile(WSIMap.RasterTile tile)
        {
            this.featureName = tile.FeatureName;
            this.FeatureInfo = tile.FeatureInfo;
            this.left = tile.left;
            this.right = tile.right;
            this.top = tile.top;
            this.bottom = tile.bottom;
            this.fileName = tile.fileName;

            drawable = false;
        }

        private void UpdateImageTexture()
        {
            if (texture == -1)
                return;

            drawable = false;
            Bitmap image = null;
            try
            {
                // If the file doesn't exist or can't be found, an ArgumentException is thrown instead of
                // just returning null
                if (fileName.StartsWith("http"))
                {
                    //Console.WriteLine("Load " + fileName);
                    System.Net.WebRequest request = System.Net.WebRequest.Create(fileName);
                    System.Net.WebResponse response = request.GetResponse();
                    using (System.IO.Stream responseStream = response.GetResponseStream())
                    {
                        image = new Bitmap(responseStream);
                    }
                }
                else
                    image = new Bitmap(fileName);
            }
            catch (Exception)
            {
                image = null;
                drawable = false;
                return;
            }

            try
            {
                if (image != null)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    System.Drawing.Imaging.PixelFormat format = image.PixelFormat;
                    if (fileName.EndsWith("bmp"))
                        format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

                    System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, format);
                    Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
					Gl.glTexSubImage2D(Gl.GL_TEXTURE_2D, 0, 0, 0, image.Width, image.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

                    int error = Gl.glGetError();
                    if (error == 0)
                        drawable = true;

                    image.UnlockBits(bitmapdata);
                }
            }
            catch
            {
                drawable = false;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RasterTile Draw()");
#endif

			if (texture == -1 && !convertPng)
                LoadImageTextureFromFile(parentMap);
			if (texture == -1 && convertPng)
				LoadImageTextureFromData(parentMap);
			else if (!drawable && !convertPng)
				UpdateImageTexture();

            if (drawable)
                DrawImageAsTexture(parentMap);
        }

        private void LoadImageTextureFromFile(MapGL parentMap)
        {
            if (texture != -1)
                return;

            //if (parentMap.TextureCache.ContainsKey(featureName))
            //{
            //    texture = parentMap.TextureCache[featureName];
            //    drawable = true;
            //    return;
            //}

            Bitmap image = null;
            try
            {
				if (imageData == null)
				{
					// If the file doesn't exist or can't be found, an ArgumentException is thrown instead of
					// just returning null
					if (fileName.StartsWith("http"))
					{
						System.Net.WebRequest request = System.Net.WebRequest.Create(fileName);
						System.Net.WebResponse response = request.GetResponse();
						using (System.IO.Stream responseStream = response.GetResponseStream())
						{
							image = new Bitmap(responseStream);
						}
					}
					else
						image = new Bitmap(fileName);
				}
				else
				{
					System.IO.Stream imageStream = new System.IO.MemoryStream(imageData);
					image = new Bitmap(imageStream);
				}
            }
            catch (Exception)
            {
                image = null;
                drawable = false;
                return;
            }

            try
            {
                if (image != null)
                {
                    //Gl.glShadeModel(Gl.GL_SMOOTH);
                    Gl.glGenTextures(1, out texture);
                    if (texture == -1)
                    {
                        drawable = false;
                        return;
                    }

                    Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);

					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
                    //Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
                    Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

                    //int iFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_RGBA : Gl.GL_RGB;
                    //int eFormat = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? Gl.GL_BGRA : Gl.GL_BGR;

                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                    System.Drawing.Imaging.PixelFormat format = image.PixelFormat;
                    if (fileName.EndsWith("bmp"))
                        format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

                    System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, format);

					Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, image.Width, image.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

                    int error = Gl.glGetError();
                    drawable = true;
                    image.UnlockBits(bitmapdata);

                    //parentMap.TextureCache.Add(featureName, texture);
                }
            }
            catch
            {
                drawable = false;
            }
        }

		private void LoadImageTextureFromData(MapGL parentMap)
		{
			if (texture != -1)
				return;

			Bitmap image = null;
			try
			{
				System.IO.Stream imageStream = new System.IO.MemoryStream(this.imageData);
				image = new Bitmap(imageStream);
			}
			catch (Exception)
			{
				this.imageData = null;
				image = null;
				drawable = false;
				return;
			}

			try
			{
				if (image != null)
				{
					Gl.glGenTextures(1, out texture);
					if (texture == -1)
					{
						drawable = false;
						return;
					}
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
					Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
					Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
					image.RotateFlip(RotateFlipType.RotateNoneFlipY);
					Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

					//Clone image data into a format that we can use as an OpenGL texture.
					System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);
					Bitmap clonedBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					System.Drawing.Imaging.BitmapData clonedbitmapdata = clonedBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, clonedBitmap.Width, clonedBitmap.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, clonedbitmapdata.Scan0);
					image.UnlockBits(bitmapdata);
					clonedBitmap.UnlockBits(clonedbitmapdata);

					drawable = true;
					this.imageData = null;

					image.Dispose();
					clonedBitmap.Dispose();
				}
			}
			catch
			{
				drawable = false;
			}
		}

        private void DrawImageAsTexture(MapGL parentMap)
        {
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

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            double alpha = 1;
            if (opacity > 0)
                alpha = (float)opacity / 255;
            Gl.glColor4d(1, 1, 1, alpha);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
            
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
            Gl.glDisable(Gl.GL_BLEND);

			//Gl.glBegin(Gl.GL_LINE_LOOP);
			//Gl.glVertex3d(l, bottom, 0.0);
			//Gl.glVertex3d(r, bottom, 0.0);
			//Gl.glVertex3d(r, top, 0.0);
			//Gl.glVertex3d(l, top, 0.0);
			//Gl.glEnd();
        }

        public void Dispose()
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("RasterTile Dispose()");
#endif

			if (texture != -1)
                Gl.glDeleteTextures(1, ref texture);
        }
    }
}

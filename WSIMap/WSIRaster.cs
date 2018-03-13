using System;
using Tao.OpenGl;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace WSIMap
{
	/**
	 * \class Raster
	 * \brief Represents a custom WSI raster image
	 */
    [Serializable]
	public class WSIRaster : Feature
	{
		#region Data Members
        protected Color transparentColor;   // this color is rendered with alpha = 0
        protected int colorTableIndex;		// used to determine color table
		protected int height;				// height of input image
		protected int width;				// width of input image
		protected double[] geoInform;		// image location information
		protected byte[] imageBody;			// the RLE image pixels
		protected byte[] reduced;			// reduced resolution pixels
		protected byte[] native;			// native resolution pixels
		protected byte alphaBlend;			// 0=transparent, 255=opaque
        protected Color threshold;          // colors < threshold are transparent
        private bool updating;				// is the raster being updated?
		private bool reducedIsValid;		// whether the reduced resolution pixels are valid
		private bool useStenciling;			// use the stencil buffer when drawing pixels?
        //protected uint[] texture;
		private bool thresholdRain;
		private bool thresholdMix;
		private bool thresholdSnow;
		#endregion

		public WSIRaster()
		{
            this.transparentColor = Color.FromArgb(ColorTables.TRANSPARENT_COLOR.R, ColorTables.TRANSPARENT_COLOR.G, ColorTables.TRANSPARENT_COLOR.B);
			this.colorTableIndex = 4;
			this.height = 0;
			this.width = 0;
			this.geoInform = null;
			this.imageBody = null;
			this.reduced = null;
			this.native = null;
			this.alphaBlend = 255;
            this.threshold = Color.Empty;
            this.updating = false;
			this.reducedIsValid = false;
			this.useStenciling = false;
            //this.texture = null;
			thresholdRain = false;
			thresholdSnow = false;
			thresholdMix = false;
		}

		public void Update(int colorTableIndex, int height, int width, double[] geoInform, byte[] imageBody, int Transparency, Color threshold)
		{
            try
            {
                updating = true;

                // Initialize fields
                this.colorTableIndex = colorTableIndex;
                this.height = height;
                this.width = width;
                this.geoInform = geoInform;
                this.imageBody = imageBody;
                if (Transparency < byte.MinValue)
                    alphaBlend = byte.MinValue;
                else if (Transparency > byte.MaxValue)
                    alphaBlend = byte.MaxValue;
                else
                    alphaBlend = Convert.ToByte(Transparency);
                this.threshold = threshold;
				this.reducedIsValid = false;
			}
            catch { }
            finally
            {
                updating = false;
            }
		}

		public void Update(int colorTableIndex, int height, int width, double[] geoInform, byte[] imageBody, int Transparency, Color threshold, bool applyToRain, bool applyToMix, bool applyToSnow)
		{
			try
			{
				updating = true;

				// Initialize fields
				this.colorTableIndex = colorTableIndex;
				this.height = height;
				this.width = width;
				this.geoInform = geoInform;
				this.imageBody = imageBody;
				if (Transparency < byte.MinValue)
					alphaBlend = byte.MinValue;
				else if (Transparency > byte.MaxValue)
					alphaBlend = byte.MaxValue;
				else
					alphaBlend = Convert.ToByte(Transparency);
				thresholdRain = applyToRain;
				thresholdSnow = applyToSnow;
				thresholdMix = applyToMix;
				this.threshold = threshold;
				//TODO:
				this.reducedIsValid = false;
			}
			catch { }
			finally
			{
				updating = false;
			}
		}

		public bool UpdateFromFile(string filename, int colorTableIndex, int height, int width, double[] geoInform, int Transparency, Color threshold)
		{
            try
            {
                updating = true;

                // Initialize fields
                this.colorTableIndex = colorTableIndex;
                this.height = height;
                this.width = width;
                this.geoInform = geoInform;
                this.imageBody = System.IO.File.ReadAllBytes(filename);
				if (Transparency < byte.MinValue)
                    alphaBlend = byte.MinValue;
                else if (Transparency > byte.MaxValue)
                    alphaBlend = byte.MaxValue;
                else
                    alphaBlend = Convert.ToByte(Transparency);
                this.threshold = threshold;
				this.reducedIsValid = false;
			}
            catch
			{
				return false;
			}
            finally
            {
                updating = false;
            }

			return true;
		}

        public void SetAlpha(int transparency)
        {
            if (transparency < byte.MinValue) transparency = byte.MinValue;
            if (transparency > byte.MaxValue) transparency = byte.MaxValue;
            alphaBlend = Convert.ToByte(transparency);
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

        public Color Threshold
        {
            get { return threshold; }
            set
            {
                threshold = value;
                ApplyThreshold();
            }
        }

		public bool ThresholdRain
		{
			set { thresholdRain = value; }
		}

		public bool ThresholdSnow
		{
			set { thresholdSnow = value; }
		}

		public bool ThresholdMix
		{
			set { thresholdMix = value; }
		}

		public bool Stencil
		{
			get { return useStenciling; }
			set { useStenciling = value; }
		}

		public int ColorTableIndex
		{
			get { return colorTableIndex; }
			set
			{
				colorTableIndex = value;
				reducedIsValid = false;
			}
		}

		public bool IsDisplayed
		{
			get { return imageBody != null; }
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WSIRaster Draw()");
#endif

			// If the raster is being updated, don't try to draw it
            if (updating) return;

			if (ImageIsCompletelyOutsideDrawableArea(parentMap))
				return;

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

			// Calculate the current width of a screen pixel
            double degPerPixel = (parentMap.BoundingBox.Map.right - parentMap.BoundingBox.Map.left) / parentMap.BoundingBox.Window.width;
 			// Display reduced or native resolution?
			if (degPerPixel < geoInform[1])
				DrawHighResRaster(parentMap, 1);
			else if (degPerPixel < geoInform[1] * 2.0)
				DrawHighResRaster(parentMap, 2);
			else
				DrawLowResRaster(parentMap);

            Gl.glDisable(Gl.GL_BLEND);
		}

		private void DrawHighResRaster(MapGL parentMap, int sf)
		{
			// Adjust the map bounding box to provide a buffer
            BoundingBox box;
            box = parentMap.BoundingBox;
            box.Map.left -= 0.2;
            box.Map.bottom -= 0.2;
            box.Map.top += 0.2;
            box.Map.right += 0.2;

			if (parentMap.RectToDraw.left != double.MinValue)
				// if RectToDraw defines an actual rect, just draw that rect to save time
				box.Map = parentMap.RectToDraw;

			// make sure normalized longitudes of map bounds are set appropriately
			box.Map.normLeft = MapGL.NormalizeLongitude(box.Map.left);
			box.Map.normRight = MapGL.NormalizeLongitude(box.Map.right);

			// Generate a subset of the image at native resolution
			int pixelsY = (int)((box.Map.top - box.Map.bottom) / Math.Abs(geoInform[5]) / sf) + 1;
			double decodedBottom, decodedLeft;
			if (box.Map.normRight > box.Map.normLeft)
			{
				// map does not cross the IDL
				int pixelsX = (int)((box.Map.right - box.Map.left) / geoInform[1] / sf) + 1;
				setNativeArray(pixelsX * pixelsY * 4);
				bool bDraw = false;
				if (sf == 1)
					bDraw = RLEDecoder.DecodeReduceSize(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], box, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);
				else
					bDraw = RLEDecoder.DecodeReduceSizeRes4(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], box, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);

				if (bDraw)
					DrawDecodedRaster(parentMap, native, pixelsX, pixelsY, sf, decodedLeft, decodedBottom);
			}
			else
			{
				// map crosses the IDL -- may need 2 hi-res images (one on each side of IDL)
				if (geoInform[0] < box.Map.normRight)
				{
					// the "left" of the image is between the IDL and the right of the map
					// decode & render the "left" subimage on the "right" of the map
					BoundingBox boxTemp = box;
					// adjust left of map rightward toward the IDL
					boxTemp.Map.left = boxTemp.Map.left - boxTemp.Map.normLeft + 180.0001;
					boxTemp.Map.normLeft = MapGL.NormalizeLongitude(boxTemp.Map.left);
					int pixelsX = (int)((boxTemp.Map.right - boxTemp.Map.left) / geoInform[1] / sf) + 1;
					setNativeArray(pixelsX * pixelsY * 4);
					bool bDraw = false;
					if (sf == 1)
						bDraw = RLEDecoder.DecodeReduceSize(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], boxTemp, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);
					else
						bDraw = RLEDecoder.DecodeReduceSizeRes4(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], boxTemp, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);

					if (bDraw)
						DrawDecodedRaster(parentMap, native, pixelsX, pixelsY, sf, decodedLeft, decodedBottom);
				}
				if (geoInform[0] + width * geoInform[1] > box.Map.normLeft)
				{
					// the "right" of the image is between the left of the map and the IDL
					// decode & render the "right" subimage on the "left" of the map
					BoundingBox boxTemp = box;
					// adjust right of map leftward toward the IDL
					boxTemp.Map.right = boxTemp.Map.right - boxTemp.Map.normRight - 180.0001;
					boxTemp.Map.normRight = MapGL.NormalizeLongitude(boxTemp.Map.right);
					int pixelsX = (int)((boxTemp.Map.right - boxTemp.Map.left) / geoInform[1] / sf) + 1;
					setNativeArray(pixelsX * pixelsY * 4);
					bool bDraw = false;
					if (sf == 1)
						bDraw = RLEDecoder.DecodeReduceSize(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], boxTemp, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);
					else
						bDraw = RLEDecoder.DecodeReduceSizeRes4(colorTableIndex, width, height, imageBody, native, alphaBlend, threshold, pixelsX, pixelsY, geoInform[3], geoInform[0], geoInform[5], geoInform[1], boxTemp, thresholdRain, thresholdMix, thresholdSnow, out decodedBottom, out decodedLeft);

					if (bDraw)
						DrawDecodedRaster(parentMap, native, pixelsX, pixelsY, sf, decodedLeft, decodedBottom);
				}
			}
		}

		private void setNativeArray(int minSize)
		{
			// Create native image array if not big enough, otherwise, re-use existing
			if (native != null && native.Length >= minSize)
				Array.Clear(native, 0, minSize);
			else
				native = new byte[minSize];
		}

		private void DrawLowResRaster(MapGL parentMap)
		{
			if (imageBody == null)
				return;
			// Use the reduced resolution image
			if (reduced == null || !reducedIsValid)
			{
				if (reduced == null || (height / 4) * (width / 4) * 4 > reduced.Length)
					reduced = new byte[(height / 4) * (width / 4) * 4];
				RLEDecoder.DecodeReduceRes16(colorTableIndex, width, imageBody, reduced, alphaBlend, width / 4, height / 4, threshold, thresholdRain, thresholdMix, thresholdSnow);
				reducedIsValid = true;
			}

			byte[] pixels = reduced;
			int w = width / 4;
			int h = height / 4;
			double lrX = geoInform[0];
			double lrY = geoInform[3] + (geoInform[5] * height);

			// Extract a sub-image if this feature is turned on in the registry
			if (parentMap.rasterFix)
			{
				// Calculate image edges
				double left = geoInform[0];
				double bottom = geoInform[3] + (geoInform[5] * height);
				double right = geoInform[0] + (geoInform[1] * width);
				double top = geoInform[3];

				// Adjust the map bounding box to provide a buffer
				BoundingBox box;
				box = parentMap.BoundingBox;
				box.Map.left -= 0.2;
				box.Map.bottom -= 0.2;
				box.Map.top += 0.2;
				box.Map.right += 0.2;

				// Begin the sub-image extraction
				byte[] array;
				int index = 0;
				int startCol, endCol, startRow, endRow;

				// Calculate number of degrees per pixel in reduced res image
				double degPerPixelX = (right - left) / w;
				double degPerPixelY = (top - bottom) / h;

				// Calculate the bounds of the sub-image
				if (box.Map.left > left)
					startCol = (int)((box.Map.left - left) / degPerPixelX);
				else
					startCol = 0;
				if (right > box.Map.right)
					endCol = w - (int)((right - box.Map.right) / degPerPixelX);
				else
					endCol = w - 1;
				if (endCol == w)
					endCol--;
				if (box.Map.bottom > bottom)
					startRow = (int)((box.Map.bottom - bottom) / degPerPixelY);
				else
					startRow = 0;
				if (top > box.Map.top)
					endRow = h - (int)((top - box.Map.top) / degPerPixelY);
				else
					endRow = h - 1;
				if (endRow == h)
					endRow--;

				// Calculate the new width and height
				w = endCol - startCol + 1;
				h = endRow - startRow + 1;

				// Extract only if we have a valid width and height and the
				// sub-image isn't the same size as the reduced res image
				if (w > 0 && h > 0 && ((w * h) != ((width / 4) * (height / 4))))
				{
					// Calculate the new image corner
					lrX = left + (startCol * degPerPixelX);
					lrY = bottom + (startRow * degPerPixelY);

					// Allocate the array to hold the sub-image
					array = new byte[w * h * 4];

					// Fill the sub-image array
					int nRows = 0, nCols = 0;
					for (int i = 0; i < reduced.Length; i += 4)
					{
						if (nCols >= startCol && nCols <= endCol && nRows >= startRow && nRows <= endRow)
						{
							array[index++] = reduced[i];
							array[index++] = reduced[i + 1];
							array[index++] = reduced[i + 2];
							array[index++] = reduced[i + 3];
						}
						nCols++;
						if (nCols == (width / 4))
						{
							nRows++;
							nCols = 0;
						}
					}
					pixels = array;
				}
			}

			#region Test code for using a texture
			//bool usedTexture = false;
			//// Create and display a texture
			//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			//sw.Start();
			//if (texture == null)
			//{
			//    texture = new uint[1];
			//    Gl.glGenTextures(1, texture);
			//    Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[0]);
			//    Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, w, h, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);
			//}
			//Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture[0]);
			//Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
			//Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
			//Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
			//Gl.glEnable(Gl.GL_TEXTURE_2D);
			//double left = geoInform[0];
			//double bottom = geoInform[3] + (geoInform[5] * height);
			//double right = geoInform[0] + (geoInform[1] * width);
			//double top = geoInform[3];
			//Gl.glBegin(Gl.GL_QUADS);
			//Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3d(left, bottom, 0.0);
			//Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3d(right, bottom, 0.0);
			//Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3d(right, top, 0.0);
			//Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3d(left, top, 0.0);
			//Gl.glEnd();
			//Gl.glDisable(Gl.GL_TEXTURE_2D);
			////Gl.glDeleteTextures(1, texture);
			//usedTexture = true;
			//sw.Stop();
			//Console.WriteLine("texture: " + sw.Elapsed);
			//if (usedTexture) return;
			#endregion

			DrawDecodedRaster(parentMap, pixels, w, h, 4, lrX, lrY);
		}

		private void DrawDecodedRaster(MapGL parentMap, byte[] pixels, int w, int h, int m, double lrX, double lrY)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WSIRaster DrawDecodedRaster()");
#endif

			// Position the image
			double _x = parentMap.DenormalizeLongitude(lrX);
			if (_x > parentMap.BoundingBox.Map.right)
				_x -= 360.0;

			SetRasterPos(_x, lrY);

			// Image degrees per pixel in x & y directions
			float dx = (float)geoInform[1] * m;
			float dy = (float)Math.Abs(geoInform[5]) * m;

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
                Gl.glDrawPixels(w, h, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pixels);
			}
			else if (_x - 360.0 + width * geoInform[1] > parentMap.BoundingBox.Map.left)
			{
				// display the image again offset to the left by 360 degrees
				_x -= 360;
				SetRasterPos(_x, lrY);
                Gl.glDrawPixels(w, h, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, pixels);
			}

			// re-endable stenciling for other features that may need it
			if (!useStenciling)
				Gl.glEnable(Gl.GL_STENCIL_TEST);
		}

		protected void SetRasterPos(double x, double y)
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

        protected void SetAlpha()
		{
			if (reduced == null || !reducedIsValid) return;

            // There are no transparent pixels (alpha=0) for grayscale images
            if (this.colorTableIndex == (int)ColorTables.ColorCode.Default)
            {
                for (long i = 3; i < reduced.Length; i += 4)
                    reduced[i] = alphaBlend;
            }
            else
                SetAlphaChannel();
        }

        protected void ApplyThreshold()
        {
	        if (reduced == null || !reducedIsValid) return;

			SetAlphaChannel();
        }

        protected void SetAlphaChannel()
        {
			Color snow = Color.Empty;
			Color mix = Color.Empty;

			int snowIndex = -1;
			int rainIndex = -1;
			int mixIndex = -1;
			if (thresholdRain || thresholdMix || thresholdSnow)
				ColorTables.GetThresholdColors(colorTableIndex, Threshold,  ref mix, ref snow, out rainIndex, out mixIndex, out snowIndex);

            // Set the alpha value for all pixels.  Applies the user-specified
            // threshold and alpha value to the pixels.
	        for (long i=3; i<reduced.Length; i+=4)
	        {
                Color color = Color.FromArgb(reduced[i-3],reduced[i-2],reduced[i-1]);
		        // Only set the alpha value for non-transparent pixels
				if (reduced[i - 3] == ColorTables.TRANSPARENT_COLOR.R && reduced[i - 2] == ColorTables.TRANSPARENT_COLOR.G && reduced[i - 1] == ColorTables.TRANSPARENT_COLOR.B)
			        reduced[i] = 0;
				else if (reduced[i - 3] == ColorTables.AVIATION_NO_COVERAGE_COLOR.R && reduced[i - 2] == ColorTables.AVIATION_NO_COVERAGE_COLOR.G && reduced[i - 1] == ColorTables.AVIATION_NO_COVERAGE_COLOR.B)
			        reduced[i] = 0;
				else if (ColorTables.LT(colorTableIndex, color, threshold, mix, snow, thresholdRain, thresholdMix, thresholdSnow, rainIndex, mixIndex, snowIndex))
			        reduced[i] = 0;
		        else
			        reduced[i] = alphaBlend;
	        }
        }

		private bool ImageIsCompletelyOutsideDrawableArea(MapGL parentMap)
		{
			// get drawable rect
			BoundingBox._RectType3 rectToBeDrawn = parentMap.BoundingBox.Map;
			if (parentMap.RectToDraw.left != double.MinValue)
				rectToBeDrawn = parentMap.RectToDraw;

			// in order to avoid normalization problems with very wide images (e.g., ~360 degrees), 
			//  check whehter image width + window size/2 is greater than 179 degrees.  This would
			//  cause improper normalization of the longitude and potentially erroneous results
			if ((parentMap.BoundingBox.Map.right - parentMap.BoundingBox.Map.left) / 2.0 + geoInform[1] * width > 179.0)
				return false;

			// get normalized image edges
			double imageLeft = parentMap.DenormalizeLongitude(geoInform[0]);
			double imageBottom = geoInform[3] + (geoInform[5] * height);
			double imageRight = parentMap.DenormalizeLongitude(geoInform[0] + (geoInform[1] * width));
			double imageTop = geoInform[3];

			// compare the image location to the location of the rectagle to be drawn
			if (imageRight < rectToBeDrawn.left)
				return true;
			if (imageLeft > rectToBeDrawn.right)
				return true;
			if (imageTop < rectToBeDrawn.bottom)
				return true;
			if (imageBottom > rectToBeDrawn.top)
				return true;

			// some portion of the image will be drawn
			return false;
		}

   }
}

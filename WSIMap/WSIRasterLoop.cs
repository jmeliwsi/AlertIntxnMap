using System;
using System.Collections.Generic;
using System.Text;
using Tao.OpenGl;
using System.Drawing;
using System.IO;

namespace WSIMap
{
    /**
     * \class Raster
     * \brief Represents a group of custom WSI raster images and supports looping.
     * \brief It caches reduced resolution image data on disk.  All images must
     * \brief be the same size.
     */
    public class WSIRasterLoop : Feature
    {
        #region Data Members
        protected Color transparentColor;   // this color is rendered with alpha = 0
        protected int colorTableIndex;		// used to determine color table
        protected int height;				// height of input image
        protected int width;				// width of input image
        protected double[] geoInform;		// image location information
        protected byte[] pixels;			// pixels to draw
        protected byte[] reduced;			// reduced resolution pixels
        protected byte alphaBlend;			// 0=transparent, 255=opaque
        protected Color threshold;          // colors < threshold are transparent
        protected string info;              // user supplied info about the image
        protected LinkedList<Guid> imageList; // list of images held by this object
        protected LinkedListNode<Guid> currentNode;
        protected Dictionary<Guid,DateTime> timeList; // holds list of image times
        private const string ext = ".dmimg";
        #endregion

        public WSIRasterLoop()
        {
            this.transparentColor = Color.FromArgb(195, 195, 195);
            this.colorTableIndex = 4;
            this.height = 0;
            this.width = 0;
            this.geoInform = null;
            this.reduced = null;
            this.alphaBlend = 255;
            this.threshold = Color.Empty;
            this.info = string.Empty;
            this.imageList = new LinkedList<Guid>();
            this.currentNode = null;
            this.timeList = new Dictionary<Guid, DateTime>();
        }

        public void Goto(DateTime t)
        {
            TimeSpan ts, timeDiff = TimeSpan.MaxValue;
            Guid guid = Guid.Empty;

            // Find the entry closest to, but not after, time t
            foreach (KeyValuePair<Guid, DateTime> kv in timeList)
            {
                ts = t - kv.Value;
                if (ts.TotalSeconds >= 0 && ts < timeDiff)
                {
                    timeDiff = ts;
                    guid = kv.Key;
                }
            }

            if (guid != Guid.Empty)
                currentNode = imageList.Find(guid);
            else
                currentNode = imageList.First; // t is before any value in timeList
        }

        public void Clean()
        {
            // Delete all image files
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo("./");
                FileInfo[] fileList = dirInfo.GetFiles("*" + ext);
                foreach (FileInfo fi in fileList)
                    File.Delete(fi.Name);
            }
            catch { }

            // Empty the image list
            imageList.Clear();
            currentNode = null;

            // Empty the time list
            timeList.Clear();
        }

        public bool IsLast()
        {
            return (currentNode == imageList.Last);
        }

        public bool AddImageFirst(int colorTableIndex, int height, int width, double[] geoInform, byte[] imageBody, int Transparency, Color threshold, string info, DateTime timeStamp)
        {
            return AddImage(colorTableIndex, height, width, geoInform, imageBody, Transparency, threshold, info, timeStamp, true);
        }

        public bool AddImageLast(int colorTableIndex, int height, int width, double[] geoInform, byte[] imageBody, int Transparency, Color threshold, string info, DateTime timeStamp)
        {
            return AddImage(colorTableIndex, height, width, geoInform, imageBody, Transparency, threshold, info, timeStamp, false);
        }

        protected bool AddImage(int colorTableIndex, int height, int width, double[] geoInform, byte[] imageBody, int Transparency, Color threshold, string info, DateTime timeStamp, bool addFirst)
        {
            // Reject images that have dimensions different than last update
            if (reduced != null)
            {
                if (this.height != height || this.width != width)
                    return false;
            }

            // Initialize fields
            this.colorTableIndex = colorTableIndex;
            this.height = height;
            this.width = width;
            this.geoInform = geoInform;
            this.Transparency = Transparency;
            this.threshold = threshold;
            this.info = info;

            // Create the reduced resolution version of the raster
            try
            {
                if (reduced == null)
                    reduced = new byte[(height / 4) * (width / 4) * 4];
                else
                    Array.Clear(reduced, 0, reduced.Length);
                RLEDecoder.DecodeReduceRes16(colorTableIndex, width, imageBody, reduced, alphaBlend, width / 4, height / 4, threshold, false, false, false);
            }
            catch { return false; }

            // Write the raster to disk
            BinaryWriter bw = null;
            try
            {
                Guid guid = Guid.NewGuid();
                bw = new BinaryWriter(new FileStream(guid.ToString() + ext, FileMode.Create));
                bw.Write(transparentColor.ToArgb());
                bw.Write(colorTableIndex);
                bw.Write(height);
                bw.Write(width);
                bw.Write(geoInform[0]);
                bw.Write(geoInform[1]);
                bw.Write(geoInform[2]);
                bw.Write(geoInform[3]);
                bw.Write(geoInform[4]);
                bw.Write(geoInform[5]);
                bw.Write(alphaBlend);
                bw.Write(threshold.ToArgb());
                bw.Write(info);
                bw.Write(timeStamp.ToString());
                bw.Write(reduced);
                bw.Close();
                if (addFirst)
                    imageList.AddFirst(guid);
                else
                    imageList.AddLast(guid);
                timeList.Add(guid, timeStamp);
            }
            catch
            {
                if (bw != null) bw.Close();
                return false;
            }

            if (threshold != Color.Empty) ApplyThreshold();

            return true;
        }

        public void RemoveFirst()
        {
            File.Delete(imageList.First.Value.ToString() + ext);
            timeList.Remove(imageList.First.Value);
            imageList.RemoveFirst();
        }

        public void RemoveLast()
        {
            File.Delete(imageList.Last.Value.ToString() + ext);
            timeList.Remove(imageList.Last.Value);
            imageList.RemoveLast();
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

        public string Info
        {
            // This property is only valid after the image has been drawn
            get { return info; }
        }

        public DateTime TimeStamp
        {
            get
            {
                if (currentNode != null)
                    return timeList[currentNode.Value];
                else
                    return DateTime.MinValue;
            }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("WSIRasterLoop Draw()");
#endif

			// If the image list is empty, there is nothing to draw
            if (imageList.Count == 0) return;

            int w, h, m;
            double lrX, lrY;

            // Retrieve the image data from disk
            BinaryReader br = null;
            try
            {
                br = new BinaryReader(new FileStream(currentNode.Value.ToString() + ext, FileMode.Open));
                transparentColor = Color.FromArgb(br.ReadInt32());
                colorTableIndex = br.ReadInt32();
                height = br.ReadInt32();
                width = br.ReadInt32();
                geoInform[0] = br.ReadDouble();
                geoInform[1] = br.ReadDouble();
                geoInform[2] = br.ReadDouble();
                geoInform[3] = br.ReadDouble();
                geoInform[4] = br.ReadDouble();
                geoInform[5] = br.ReadDouble();
                alphaBlend = br.ReadByte();
                threshold = Color.FromArgb(br.ReadInt32());
                info = br.ReadString();
                DateTime timeStamp = DateTime.Parse(br.ReadString());
                ReadBytes(br, reduced);
                br.Close();
            }
            catch
            {
                if (br != null) br.Close();
                return;
            }

            // Display the reduced resolution image
            pixels = reduced;
            w = width / 4;
            h = height / 4;
            m = 4;
            lrX = geoInform[0];
            lrY = geoInform[3] + (geoInform[5] * height);

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
                if (endCol == w) endCol--;
                if (box.Map.bottom > bottom)
                    startRow = (int)((box.Map.bottom - bottom) / degPerPixelY);
                else
                    startRow = 0;
                if (top > box.Map.top)
                    endRow = h - (int)((top - box.Map.top) / degPerPixelY);
                else
                    endRow = h - 1;
                if (endRow == h) endRow--;

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
        
            // Position the image
            SetRasterPos(lrX, lrY);

            // Image degrees per pixel in x & y directions
            float dx = (float)geoInform[1] * m;
            float dy = (float)Math.Abs(geoInform[5]) * m;

            // Calculate the rendering scale factors
            float xFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.width / (float)(parentMap.BoundingBox.Ortho.right - parentMap.BoundingBox.Ortho.left));
            float yFactor = parentMap.ScaleFactor * ((float)parentMap.BoundingBox.Viewport.height / (float)(parentMap.BoundingBox.Ortho.top - parentMap.BoundingBox.Ortho.bottom));

            // Draw the pixels
            Gl.glPixelZoom(xFactor * dx, yFactor * dy);
            Gl.glDrawPixels(w, h, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);
        }

        protected void SetRasterPos(double x, double y)
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

        protected void SetAlpha()
        {
            if (reduced == null) return;

            for (long i = 3; i < reduced.Length; i += 4)
            {
                // Only set the alpha value for non-transparent pixels
                if (reduced[i - 3] == 195 && reduced[i - 2] == 195 && reduced[i - 1] == 195)
                    reduced[i] = 0;
                else
                    reduced[i] = alphaBlend;
            }
        }

        protected void ApplyThreshold()
        {
            if (reduced == null) return;

            for (long i = 3; i < reduced.Length; i += 4)
            {
                Color color = Color.FromArgb(reduced[i - 3], reduced[i - 2], reduced[i - 1]);
                // Set the alpha value for non-transparent pixels
                if (reduced[i - 3] == 195 && reduced[i - 2] == 195 && reduced[i - 1] == 195)
                    reduced[i] = 0;
                else if (ColorTables.LT(colorTableIndex, color, threshold))
                    reduced[i] = 0;
                else
                    reduced[i] = alphaBlend;
            }
        }

        private void ReadBytes(BinaryReader reader, byte[] data)
        {
            int offset = 0;
            int remaining = data.Length;
            while (remaining > 0)
            {
                int read = reader.Read(data, offset, remaining);
                if (read <= 0) return;
                remaining -= read;
                offset += read;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Tao.OpenGl;

namespace WSIMap
{
	/**
	 * \class TileMgr
	 * \brief Renders raster terrain tiles as textures for a given map rectangle.
	 */
	public sealed class TileMgr : Feature
	{
		#region Data Members
		private const double TOP = 90.0;
		private const double LEFT = -180.0;
		private int level, prevLevel;
		private List<int> tileList, prevTileList;
		private List<RectangleD> rectList;
		private List<LevelOfDetail> lodList;
		private Dictionary<int,TexInfo> texList;
		private byte[] image;
		private byte alphaBlend; // 0=transparent, 255=opaque
		private ColorTables.ByteQuad[] colorTable;
		private bool drawable;
		private bool maxTextureSizeExceeded;
		#endregion

		#region Supporting Classes
		private class LevelOfDetail
		{
			public int widthPix;
			public int heightPix;
			public double widthDeg;
			public double heightDeg;
			public int tileCount;
			public string tileFilePath;

			public LevelOfDetail(int widthPix, int heightPix, double widthDeg, double heightDeg, int tileCount, string tileFilePath)
			{
				this.widthPix = widthPix;
				this.heightPix = heightPix;
				this.widthDeg = widthDeg;
				this.heightDeg = heightDeg;
				this.tileCount = tileCount;
				this.tileFilePath = tileFilePath;
			}
		}

		private class TexInfo
		{
			public int texture;
			public bool drawn;

			public TexInfo(int texture, bool drawn)
			{
				this.texture = texture;
				this.drawn = drawn;
			}
		}
		#endregion

		public TileMgr(string tileDir)
		{
			// Misc initialization
			drawable = false;
			alphaBlend = 255;
			colorTable = ColorTables.ColorTable_elevgbsncap;

			// Create lists for tiles, rectangles, level-of-detail and textures
			tileList = new List<int>();
			rectList = new List<RectangleD>();
			lodList = new List<LevelOfDetail>();
			texList = new Dictionary<int,TexInfo>();

			// Initialize level-of-detail
			lodList.Add(new LevelOfDetail(1024, 1024, 180, 180, 2, tileDir + @"\32km\32kmtile_"));
			lodList.Add(new LevelOfDetail(1024, 1024, 90, 90, 8, tileDir + @"\16km\16kmtile_"));
			lodList.Add(new LevelOfDetail(1024, 1024, 45, 45, 32, tileDir + @"\8km\8kmtile_"));
			lodList.Add(new LevelOfDetail(1024, 1024, 22.5, 22.5, 128, tileDir + @"\4km\4kmtile_"));
			lodList.Add(new LevelOfDetail(1024, 1024, 11.25, 11.25, 512, tileDir + @"\2km\2kmtile_"));

			// Check the maximum texture size
			MaxTextureSizeExceeded();

			// Create the image array - all level-of-detail are the same size
			image = new byte[lodList[0].widthPix * lodList[0].heightPix * 4];
		}

		public byte Transparency
		{
			get { return alphaBlend; }
			set
			{
				alphaBlend = value;
				DeleteTextures(true);
				tileList.Clear();
			}
		}

		public ColorTables.ByteQuad[] ColorTable
		{
			get { return colorTable; }
			set
			{
				colorTable = value;
				DeleteTextures(true);
				tileList.Clear();
			}
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
			#region DEBUG
			//// Clear debugging rectangles out of the layer
			//List<int> ilist = new List<int>();
			//for (int j = 0; j < parentLayer.Features.Count; j++)
			//{
			//    if (parentLayer.Features[j].FeatureName.Contains("DEBUG"))
			//        ilist.Add(j);
			//}
			//for (int j = ilist.Count - 1; j >= 0; j--)
			//    parentLayer.Features.RemoveAt(ilist[j]);
			//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			//sw.Start();
			#endregion

#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("TileMgr Draw()");
#endif

			// If our texture size exceeds the supported max texture size, don't draw anything
			if (maxTextureSizeExceeded)	return;

			// Store the previous level and tile list
			prevLevel = level;
			prevTileList = new List<int>(tileList);

			// Clear the tile and rectangle lists
			tileList.Clear();
			rectList.Clear();

			// Determine which tiles to use
			SelectTiles(parentMap);

			// Mark existing textures as not drawn
			foreach (KeyValuePair<int, TexInfo> kvp in texList)
				kvp.Value.drawn = false;

			// Enable stenciling
			Gl.glEnable(Gl.GL_STENCIL_TEST);
			Gl.glStencilFunc(Gl.GL_EQUAL, 1, 1);
			Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP);

			// Load and render the tiles
			for (int i = 0; i < tileList.Count; i++)
			{
				if (level == prevLevel && prevTileList.Contains(tileList[i]))
				{
					DrawExistingTexture(level, tileList[i], lodList[level].widthPix, lodList[level].heightPix, rectList[i].Top, rectList[i].Bottom, rectList[i].Left, rectList[i].Right);
				}
				else
				{
					LoadFromFile(lodList[level].tileFilePath + tileList[i].ToString(), lodList[level].widthPix, lodList[level].heightPix);
					DrawNewTexture(level, tileList[i], lodList[level].widthPix, lodList[level].heightPix, rectList[i].Top, rectList[i].Bottom, rectList[i].Left, rectList[i].Right);
				}
			}

			// Delete unused textures
			DeleteTextures(false);

			#region DEBUG
			//sw.Stop();
			//Console.WriteLine("Draw: " + sw.Elapsed.ToString());
			//// Draw debugging rectangles
			//for (int k = 0; k < rectList.Count; k++)
			//{
			//    rectList[k].FeatureName = "DEBUG_" + k.ToString();
			//    rectList[k].Opacity = 0;
			//    rectList[k].BorderColor = System.Drawing.Color.Red;
			//    rectList[k].Refresh();
			//    parentLayer.Features.Add(rectList[k]);
			//}
			#endregion
		}

		private void SelectTiles(MapGL parentMap)
		{
			// Get the map rectangle
			RectangleD mapRect = new RectangleD(parentMap.BoundingBox.Map.bottom, parentMap.BoundingBox.Map.top, parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);
			double mapWidth = mapRect.Width;

			// Get the number of International Date Line crossings
			int crossings = MapGL.GetNumberOfCrossingIDL(parentMap.BoundingBox.Map.left, parentMap.BoundingBox.Map.right);

			// Is the International Date Line in the map?
			bool idlInMap = false;
			if (parentMap.BoundingBox.Map.normLeft > parentMap.BoundingBox.Map.normRight)
				idlInMap = true;

			// Determine which level-of-detail to use; a ratio >= 0.5 results
			// in no more than 4 tiles being displayed at any one time
			level = -1;
			foreach (LevelOfDetail lod in lodList)
			{
				level++;
				double ratio = Math.Round((mapWidth / lod.widthDeg), 3);
				if (ratio >= 0.5)
					break;
			}

			// Determine which tiles to display. Upper left corner of tile 1 is (LEFT, TOP).
			double t = 0, l = 0, b = 0, r = 0;
			int cols = (int)(360.0 / lodList[level].widthDeg);
			for (int i = 0; i < lodList[level].tileCount; i++)
			{
				l = LEFT + (lodList[level].widthDeg * (i % cols));
				r = l + lodList[level].widthDeg;

				t = TOP - (lodList[level].heightDeg * (i / cols));
				b = t - lodList[level].heightDeg;

				if (crossings == 0)
				{
					if (mapRect.FastIntersect(b, t, l, r))
					{
						tileList.Add(i);
						rectList.Add(new RectangleD(b, t, l, r));
					}
				}
				else // map crosses International Date Line
				{
					// Adjust tile rect by number of IDL crossings
					l += crossings * 360;
					r += crossings * 360;
					if (mapRect.FastIntersect(b, t, l, r))
					{
						tileList.Add(i);
						rectList.Add(new RectangleD(b, t, l, r));
					}

					// Adjust again if IDL is in the map
					if (idlInMap)
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
						if (mapRect.FastIntersect(b, t, l, r))
						{
							tileList.Add(i);
							rectList.Add(new RectangleD(b, t, l, r));
						}
					}
				}
			}
		}

		private void DrawExistingTexture(int level, int tile, int width, int height, double top, double bottom, double left, double right)
		{
			// Create the key to reference the texture in the Dictionary
			int key = level * 10000 + tile;

			// Set the shade model
			Gl.glShadeModel(Gl.GL_SMOOTH);

			// Bind to the texture
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texList[key].texture);

			// Setup parameters for textures
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

			// Render the texture
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glBegin(Gl.GL_QUADS);
			Gl.glTexCoord2f(0.0f, 0.0f);
			Gl.glVertex3d(left, bottom, 0.0);
			Gl.glTexCoord2f(1.0f, 0.0f);
			Gl.glVertex3d(right, bottom, 0.0);
			Gl.glTexCoord2f(1.0f, 1.0f);
			Gl.glVertex3d(right, top, 0.0);
			Gl.glTexCoord2f(0.0f, 1.0f);
			Gl.glVertex3d(left, top, 0.0);
			Gl.glEnd();
			Gl.glDisable(Gl.GL_TEXTURE_2D);

			// Mark the texture as drawn
			texList[key].drawn = true;
		}

		private void DrawNewTexture(int level, int tile, int width, int height, double top, double bottom, double left, double right)
		{
			if (!drawable) return;

			// Set the shade model
			Gl.glShadeModel(Gl.GL_SMOOTH);

			// Create the texture
			int texture = 0;
			Gl.glGenTextures(1, out texture);
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image);

			// Setup parameters for textures
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

			// Render the texture
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glBegin(Gl.GL_QUADS);
			Gl.glTexCoord2f(0.0f, 0.0f);
			Gl.glVertex3d(left, bottom, 0.0);
			Gl.glTexCoord2f(1.0f, 0.0f);
			Gl.glVertex3d(right, bottom, 0.0);
			Gl.glTexCoord2f(1.0f, 1.0f);
			Gl.glVertex3d(right, top, 0.0);
			Gl.glTexCoord2f(0.0f, 1.0f);
			Gl.glVertex3d(left, top, 0.0);
			Gl.glEnd();
			Gl.glDisable(Gl.GL_TEXTURE_2D);

			// Save the texture for possible later use
			TexInfo ti = new TexInfo(texture, true);
			int key = level * 10000 + tile;
			texList.Add(key, ti);
		}

		private void LoadFromFile(string fileName, int width, int height)
		{
			// Read the file
			byte[] pixels = null;
			try
			{
				pixels = File.ReadAllBytes(fileName);
			}
			catch
			{
				drawable = false;
				return;
			}

			// Decode run-length encoded pixel values to colors
			int idx = 0, runCount = 0;
			ColorTables.ByteQuad color = ColorTables.AVIATION_NO_COVERAGE_COLOR;
			for (int i = 0; i < width * height; i++)
			{
				if (i == runCount)
				{
					color = colorTable[pixels[idx++]];
					runCount += pixels[idx++];
				}
				image[i * 4] = color.R;
				image[(i * 4) + 1] = color.G;
				image[(i * 4) + 2] = color.B;
				image[(i * 4) + 3] = alphaBlend;
			}

			// Ready to draw the image as a texture
			drawable = true;
		}

		private void DeleteTextures(bool deleteAll)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Texture DeleteTextures()");
#endif

			// Deletes undrawn textures; if deleteAll is true it deletes all existing textures
			List<int> deleted = new List<int>();
			foreach (KeyValuePair<int, TexInfo> kvp in texList)
			{
				if (deleteAll || !kvp.Value.drawn)
				{
					Gl.glDeleteTextures(1, ref kvp.Value.texture);
					deleted.Add(kvp.Key);
				}
			}
			foreach (int n in deleted)
				texList.Remove(n);
		}

		private void MaxTextureSizeExceeded()
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Texture MaxTextureSizeExceeded()");
#endif

			maxTextureSizeExceeded = false;
			int maxSize = int.MaxValue;
			try
			{
				Gl.glGetIntegerv(Gl.GL_MAX_TEXTURE_SIZE, out maxSize);
			}
			catch { }
			if (maxSize < lodList[0].widthPix)
				maxTextureSizeExceeded = true;
		}
	}
}

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
	 * \class TextureTile
	 * \brief Represents a raw, 8-bit image that will be displayed as a texture
	 */
	public class Texture : Feature
	{
		#region Data Members
		protected string fileName;			// data file name
		protected int width;				// width of the image
		protected int height;				// height of the image
		private double top;					// top of the image (deg lat)
		private double left;				// left of the image (deg lon)
		private double bottom;				// bottom of the image (deg lat)
		private double right;				// right of the image (deg lon)
		protected byte[] image;				// the image pixels
		protected byte alphaBlend;			// 0=transparent, 255=opaque
		protected Color transparentColor;	// this color is not rendered
		protected bool drawable;			// indicates whether the image can be drawn
		private ColorTables.ByteQuad[] color_table;
		private uint[] texture;
		#endregion

		public Texture(string fileName, ColorTables.ByteQuad[] color_table, int width, int height, double top, double left, double bottom, double right)
		{
			// Check input file name
			if (Object.Equals(fileName, null))
				throw new WSIMapException("Raster file name is null");
			if (fileName.Equals(string.Empty))
				throw new WSIMapException("Raster file name is empty");

			// Set fields
			this.fileName = fileName;
			this.width = width;
			this.height = height;
			this.top = top;
			this.left = left;
			this.bottom = bottom;
			this.right = right;
			this.featureName = String.Empty;
			this.featureInfo = String.Empty;
			this.numVertices = 0;
			this.alphaBlend = 255;
			this.transparentColor = Color.FromArgb(0, 0, 0);
			this.drawable = false;
			this.texture = null;
			this.openglDisplayList = -1;
			this.color_table = color_table;
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
				if (value < byte.MinValue)
					value = byte.MinValue;
				if (value > byte.MaxValue)
					value = byte.MaxValue;
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

		public void LoadFromBinFile(string file)
		{
			// Read the file
			byte[] pixels = File.ReadAllBytes(file);

			// Create the image array
			image = new byte[width * height * 4];

			// Decode the pixel values to colors
			for (int i = 0; i < width * height; i++)
			{
				ColorTables.ByteQuad color = color_table[pixels[i]];
				image[i * 4] = color.R;
				image[(i * 4) + 1] = color.G;
				image[(i * 4) + 2] = color.B;
				image[(i * 4) + 3] = alphaBlend;                  
			}

			drawable = true;
		}

		private void SetAlpha()
		{
			if (image != null)
			{
				for (long i = 3; i < width * height * 4; i += 4)
				{
					if (image[i - 3] == transparentColor.R && image[i - 2] == transparentColor.G && image[i - 1] == transparentColor.B)
						image[i] = 0;
					else
						image[i] = alphaBlend;
				}
			}
		}

		internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("Texture Draw()");
#endif

			if (!drawable)
				return;
			DrawImageAsTexture();
		}

		private void DrawImageAsTexture()
		{
			if (image != null)
			{
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

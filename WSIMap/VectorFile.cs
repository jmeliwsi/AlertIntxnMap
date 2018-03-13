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
	public class VectorFile : Feature, IProjectable, IRefreshable
	{
		#region Data Members
		protected string fileName;		// name of this vector file
		protected int numFeatures;		// number of features in this file
		protected Color borderColor;	// the border color
		protected uint borderWidth;		// the width of the border lines
		protected uint opacity;			// the opacity is applied to the fill
		protected Color fillColor;		// the fill color
		protected Color fillColor2;		// 2nd fill color for nest polygons
		protected bool useTwoColors;	// use the 2nd fill color
		protected bool drawBorder;		// if true, draw a border on the features
		protected bool fillFeatures;	// if true, fill in the feature
        protected bool stencil;         // if true, use stencil buffer
		protected ErrorCodes errorCode;
		public enum ErrorCodes { NoError, SHPFileDoesNotExist, SHXFileDoesNotExist, ErrorOpeningFile, WrongNumberOfLayers, Exception };
		protected MapProjections mapProjection;
		protected short centralLongitude;
		private const string TRACKING_CONTEXT = "VectorFile";
		#endregion

		[DllImport("tessellate.dll", EntryPoint = "TessellateVectorFile", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void TessellateVectorFile(string vectorFileName, int[] fillColorRGB, bool useTwoColors, int[] fillColor2RGB, int opacity, MapProjections mapProjection, double centralLongitude);

		public VectorFile(string vectorFileName) : this(vectorFileName, string.Empty, string.Empty)
		{
		}

		public VectorFile(string vectorFileName, string featureName, string featureInfo)
		{
			// Check input file name
			if (Object.Equals(vectorFileName,null))
				throw new WSIMapException("Vector file name is null");
			if (vectorFileName.Equals(string.Empty))
				throw new WSIMapException("Vector file name is empty");

			// Set fields
			this.fileName = vectorFileName;
			this.featureName = featureName;
			this.featureInfo = featureInfo;
			this.numFeatures = 0;
			this.numVertices = 0;
			this.borderColor = Color.Black;
			this.borderWidth = 1;
			this.opacity = 100;
			this.fillColor = Color.White;
			this.fillColor2 = Color.Empty;
			this.useTwoColors = false;
			this.fillFeatures = false;
			this.drawBorder = true;
            this.stencil = false;
			this.mapProjection = MapProjections.CylindricalEquidistant;
		}

		public void Dispose()
		{
			DeleteOpenGLDisplayList(TRACKING_CONTEXT);
		}

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public ErrorCodes ErrorCode
		{
			get { return errorCode; }
		}

		public Color BorderColor
		{
			get	{ return borderColor; }
			set { borderColor = value; }
		}

		public uint BorderWidth
		{
			get { return borderWidth; }
			set { borderWidth = value; }
		}

		public uint Opacity
		{
			get { return opacity; }
			set
			{
				if (value < 0) value = 0;
				if (value > 100) value = 100;
				opacity = value;
			}
		}

		public Color FillColor
		{
			get	{ return fillColor; }
			set { fillColor = value; }
		}

		public Color FillColor2
		{
			get { return fillColor2; }
			set { fillColor2 = value; }
		}

		public bool UseTwoFillColors
		{
			get { return useTwoColors; }
			set { useTwoColors = value; }
		}

		public bool Border
		{
			get	{ return drawBorder; }
			set { drawBorder = value; }
		}

		public bool Fill
		{
			get	{ return fillFeatures; }
			set { fillFeatures = value; }
		}

        public bool Stencil
        {
            get { return stencil; }
            set { stencil = value; }
        }

		public void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			this.mapProjection = mapProjection;
			this.centralLongitude = centralLongitude;
			if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
				CreateDisplayList();
		}

		private void CreateDisplayList()
		{
			LoadFromFile();
		}

        internal override void Draw(MapGL parentMap, Layer parentLayer)
		{
#if TRACK_OPENGL_DISPLAY_LISTS
			ConfirmMainThread("VectorFile Draw()");
#endif

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

		private unsafe void LoadFromFile()
		{
			// TODO: This works with ESRI shapefiles.  It hasn't been tested with other vector formats.

			// Create an OpenGL display list for this file
			CreateOpenGLDisplayList(TRACKING_CONTEXT);
			Gl.glNewList(openglDisplayList, Gl.GL_COMPILE);

			// Some OpenGL initialization
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);

			// Make sure the .SHP and .SHX files exist
			if (!System.IO.File.Exists(fileName))
			{
				Gl.glEndList();
				errorCode = ErrorCodes.SHPFileDoesNotExist;
				return;
			}
			if (!System.IO.File.Exists(fileName.Remove(fileName.Length - 3) + "shx"))
			{
				Gl.glEndList();
				errorCode = ErrorCodes.SHXFileDoesNotExist;
				return;
			}

			// Determine if this is an azimuthal projection type
			bool isAziumuthalProj = (Projection.GetProjectionType(mapProjection) == MapProjectionTypes.Azimuthal);

			// Reset the total number of vertices and features for this file
			numVertices = 0;
			numFeatures = 0;

			//------------------------------------------------------------
			// First, fill the polygons by tessellating them
			//------------------------------------------------------------
			if (fillFeatures && fillColor != Color.Transparent)
			{
				int[] c1 = new int[3];
				int[] c2 = new int[3];
				c1[0] = fillColor.R; c1[1] = fillColor.G; c1[2] = fillColor.B;
				c2[0] = fillColor2.R; c2[1] = fillColor2.G; c2[2] = fillColor2.B;
				TessellateVectorFile(fileName, c1, useTwoColors, c2, (int)opacity, mapProjection, centralLongitude);
			}

			//------------------------------------------------------------
			// Second, read the file again to draw the polygon borders
			//------------------------------------------------------------
			if (!drawBorder)
			{
				Gl.glEndList();
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
					Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
					errorCode = ErrorCodes.ErrorOpeningFile;
					return;
				}
		
				// Get the vector layer from the data source
				void* vectorLayer = null;
				int numLayers = GDAL.OGR_DS_GetLayerCount(dataSource);
				if (numLayers != 1)
				{
					Gl.glEndList();
					DeleteOpenGLDisplayList(TRACKING_CONTEXT);
					errorCode = ErrorCodes.WrongNumberOfLayers;
					return;
				}
				vectorLayer = GDAL.OGR_DS_GetLayer(dataSource, 0);
				GDAL.OGR_L_ResetReading(vectorLayer);

				// Set the border color and width
				Gl.glColor3f(glc(borderColor.R), glc(borderColor.G), glc(borderColor.B));
				Gl.glLineWidth((float)borderWidth);

				// Extract the features
				void* feature = null;
				while ((feature = GDAL.OGR_L_GetNextFeature(vectorLayer)) != null)
				{
					// Count the number of features
					numFeatures++;

					// Get the geometry object
					void *geometry = null;
					geometry = GDAL.OGR_F_GetGeometryRef(feature);

					// Process the geometry object
					if (geometry != null)
					{
						void* shape = null;

						// Determine the type and number of geometries
						int geometryType = GDAL.OGR_G_GetGeometryType(geometry);
						if (geometryType < 0)
							geometryType = (geometryType << 1) >> 1; // ignore 3D geometry types
						int numGeometries = GDAL.OGR_G_GetGeometryCount(geometry);

						// Draw the shapes
						if (numGeometries > 0)
						{
							// Iterate over the geometries
							for (int i = 0; i < numGeometries; i++)
							{
								// Pull out the shape to process
								shape = GDAL.OGR_G_GetGeometryRef(geometry, i);

								// If shape is a "multi" type, draw all its parts
								if (geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiPolygon ||
									geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiLineString ||
									geometryType == (int)GDAL.OGRwkbGeometryType.wkbMultiPoint)
								{
									int numParts = GDAL.OGR_G_GetGeometryCount(shape);
									for (int j = 0; j < numParts; j++)
									{
										void* part = GDAL.OGR_G_GetGeometryRef(shape, j);
										Render(geometryType, part, isAziumuthalProj);
									}
								}
								else
								{
									Render(geometryType, shape, isAziumuthalProj);
								}
							}
						}
						else
						{
							if (geometryType == (int)GDAL.OGRwkbGeometryType.wkbLineString)
								Render(geometryType, geometry, isAziumuthalProj);
						}
					}
					GDAL.OGR_F_Destroy(feature);
				}
				GDAL.OGR_DS_Destroy(dataSource);
			}
			catch
			{
				Gl.glEndList();
				DeleteOpenGLDisplayList(TRACKING_CONTEXT);
				errorCode = ErrorCodes.Exception;
				return;
			}

			// End the OpenGL display list
			Gl.glEndList();
		}

		private unsafe void Render(int geometryType, void* shape, bool isAzimuthalProjection)
		{
			if (shape == null) return;

			bool bUnknownType = false;

			// Begin the GL shape
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
				case (int)GDAL.OGRwkbGeometryType.wkbLineString:
					Gl.glBegin(Gl.GL_LINE_STRIP);
					break;
				default:
					bUnknownType = true;
					break;
			}

			// If geometry type is unknown, don't render anything
			if (bUnknownType) return;

			// Iterate the points of this shape
			int nPoints = GDAL.OGR_G_GetPointCount(shape);
			double x = 0, y = 0, z = 0;
			for (int iPoints = 0; iPoints < nPoints; iPoints++)
			{
				// Get the next point
				GDAL.OGR_G_GetPoint(shape, iPoints, ref x, ref y, ref z);
				double px, py;
				if (isAzimuthalProjection && y < Projection.MinAzimuthalLatitude)
					continue;
				Projection.ProjectPoint(mapProjection, x, y, centralLongitude, out px, out py);
				Gl.glVertex2d(px, py);
				numVertices++;
			}

			// End the GL shape
			Gl.glEnd();
		}
	}
}

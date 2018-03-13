using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WSIMap
{
	/**
	 * \class GDAL
	 * \brief Wrapper class for the GDAL Library
	 */
	public class GDAL
	{
		public GDAL()
		{
		}

		public enum GDALRWFlag
		{
			/*! Read data */   GF_Read = 0,
			/*! Write data */  GF_Write = 1
		};

		public enum OGRwkbGeometryType
		{
			wkbUnknown = 0,             /* non-standard */
			wkbPoint = 1,               /* rest are standard WKB type codes */
			wkbLineString = 2,
			wkbPolygon = 3,
			wkbMultiPoint = 4,
			wkbMultiLineString = 5,
			wkbMultiPolygon = 6,
			wkbGeometryCollection = 7,
			wkbNone = 100,              /* non-standard, for pure attribute records */
			wkbLinearRing = 101,        /* non-standard, just for createGeometry() */
		};

		public enum GDALDataType
		{
			GDT_Unknown = 0,
			/*! Eight bit unsigned integer */           GDT_Byte = 1,
			/*! Sixteen bit unsigned integer */         GDT_UInt16 = 2,
			/*! Sixteen bit signed integer */           GDT_Int16 = 3,
			/*! Thirty two bit unsigned integer */      GDT_UInt32 = 4,
			/*! Thirty two bit signed integer */        GDT_Int32 = 5,
			/*! Thirty two bit floating point */        GDT_Float32 = 6,
			/*! Sixty four bit floating point */        GDT_Float64 = 7,
			/*! Complex Int16 */                        GDT_CInt16 = 8,
			/*! Complex Int32 */                        GDT_CInt32 = 9,
			/*! Complex Float32 */                      GDT_CFloat32 = 10,
			/*! Complex Float64 */                      GDT_CFloat64 = 11,
			GDT_TypeCount = 12          /* maximum type # + 1 */
		};

		public enum GDALPaletteInterp { GPI_Gray = 0, GPI_RGB = 1, GPI_CMYK = 2, GPI_HLS = 3 };

		public enum GDALColorInterp
		{
			GCI_Undefined = 0, GCI_GrayIndex = 1, GCI_PaletteIndex = 2, GCI_RedBand = 3,
			GCI_GreenBand = 4, GCI_BlueBand = 5, GCI_AlphaBand = 6, GCI_HueBand = 7,
			GCI_SaturationBand = 8, GCI_LightnessBand = 9, GCI_CyanBand = 10, GCI_MagentaBand = 11,
			GCI_YellowBand = 12, GCI_BlackBand = 13, GCI_YCbCr_YBand = 14, GCI_YCbCr_CbBand = 15,
			GCI_YCbCr_CrBand = 16, GCI_Max = 16
		};

		public struct GDALColorEntry
		{
			public short c1;
			public short c2;
			public short c3;
			public short c4;
		}

		// GDAL Library functions
		[DllImport("gdal13.dll", EntryPoint="GDALAllRegister", CallingConvention=CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void GDALAllRegister();
		[DllImport("gdal13.dll", EntryPoint = "GDALOpen", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* GDALOpen(string fileName, int access);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterCount", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALGetRasterCount(void* hDataset);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterBand", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* GDALGetRasterBand(void* hDataset, int band);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterDataType", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern GDALDataType GDALGetRasterDataType(void* hRasterBand);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterColorTable", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* GDALGetRasterColorTable(void* hRasterBand);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetPaletteInterpretation", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern GDALPaletteInterp GDALGetPaletteInterpretation(void* hColorTable);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetColorEntryCount", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALGetColorEntryCount(void* hColorTable);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetColorEntry", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern GDALColorEntry GDALGetColorEntry(void* hColorTable, int index);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetColorEntryAsRGB", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALGetColorEntryAsRGB(void* hColorTable, int index, GDALColorEntry* color);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterBandXSize", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALGetRasterBandXSize(void* hRasterBand);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetRasterBandYSize", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALGetRasterBandYSize(void* hRasterBand);
		[DllImport("gdal13.dll", EntryPoint = "GDALRasterIO", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALRasterIO(void* hRasterBand, GDALRWFlag eRWFlag, int nXOff, int nYOff, int nXSize, int nYSize, void* pData, int nBufXSize, int nBufYSize, GDALDataType eBufType, int nPixelSpace, int nLineSpace);
		[DllImport("gdal13.dll", EntryPoint = "GDALClose", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void GDALClose(void* hDataset);
		[DllImport("gdal13.dll", EntryPoint = "GDALReadWorldFile", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int GDALReadWorldFile(string baseFileName, string extension, double* geoTransform);
		[DllImport("gdal13.dll", EntryPoint = "GDALGetGeoTransform", CallingConvention = CallingConvention.StdCall), SuppressUnmanagedCodeSecurity]
        unsafe public static extern int GDALGetGeoTransform(void* hDataset, double* geoTransform);   

		// OGR-specific GDAL Library functions
		[DllImport("gdal13.dll", EntryPoint = "OGRRegisterAll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void OGRRegisterAll();
		[DllImport("gdal13.dll", EntryPoint = "OGROpen", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* OGROpen(string fileName, int update, ref void* driverList);
		[DllImport("gdal13.dll", EntryPoint = "OGR_DS_Destroy", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int OGR_DS_Destroy(void* dataSource);
		[DllImport("gdal13.dll", EntryPoint = "OGR_DS_GetLayerCount", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int OGR_DS_GetLayerCount(void* dataSource);
		[DllImport("gdal13.dll", EntryPoint = "OGR_DS_GetLayer", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* OGR_DS_GetLayer(void* dataSource, int index);
		[DllImport("gdal13.dll", EntryPoint = "OGR_L_ResetReading", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void OGR_L_ResetReading(void* layer);
		[DllImport("gdal13.dll", EntryPoint = "OGR_L_GetNextFeature", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* OGR_L_GetNextFeature(void* layer);
		[DllImport("gdal13.dll", EntryPoint = "OGR_F_GetGeometryRef", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* OGR_F_GetGeometryRef(void* feature);
		[DllImport("gdal13.dll", EntryPoint = "OGR_F_Destroy", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void OGR_F_Destroy(void* feature);
		[DllImport("gdal13.dll", EntryPoint = "OGR_G_GetGeometryType", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int OGR_G_GetGeometryType(void* geometry);
		[DllImport("gdal13.dll", EntryPoint = "OGR_G_GetGeometryCount", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int OGR_G_GetGeometryCount(void* geometry);
		[DllImport("gdal13.dll", EntryPoint = "OGR_G_GetGeometryRef", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void* OGR_G_GetGeometryRef(void* geometry, int index);
		[DllImport("gdal13.dll", EntryPoint = "OGR_G_GetPointCount", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern int OGR_G_GetPointCount(void* geometry);
		[DllImport("gdal13.dll", EntryPoint = "OGR_G_GetPoint", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		unsafe public static extern void OGR_G_GetPoint(void* geometry, int index, ref double x, ref double y, ref double z);
	}
}

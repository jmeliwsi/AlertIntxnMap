// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the TESSELLATE_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// TESSELLATE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef TESSELLATE_EXPORTS
#define TESSELLATE_API __declspec(dllexport)
#else
#define TESSELLATE_API __declspec(dllimport)
#endif

enum MapProjections { CylindricalEquidistant, Stereographic, Orthographic, Mercator, Lambert };

extern "C" TESSELLATE_API int TessellatePlaneSymbol(void);
extern "C" TESSELLATE_API int TessellateBell206Symbol(void);
extern "C" TESSELLATE_API int Tessellate737Symbol(void);
extern "C" TESSELLATE_API int Tessellate747Symbol(void);
extern "C" TESSELLATE_API int TessellateC172Symbol(void);
extern "C" TESSELLATE_API int TessellateLearJetSymbol(void);
extern "C" TESSELLATE_API int TessellateSAABSymbol(void);
extern "C" TESSELLATE_API int TessellateTriangleSymbol(void);
extern "C" TESSELLATE_API int TessellateCircleSymbol(void);
extern "C" TESSELLATE_API int TessellateSquareSymbol(void);
extern "C" TESSELLATE_API int TessellateArrowSymbol(void);
extern "C" TESSELLATE_API int TessellateShiftedArrowSymbol(void);
extern "C" TESSELLATE_API void TessellateVectorFile(char* vectorFileName, int fillColor[3], bool useTwoColors, int fillColor2[3], int opacity, MapProjections mapProjection = CylindricalEquidistant, double centralLongitude = -90);
extern "C" TESSELLATE_API void TessellatePolygon(double x[], double y[], int nPoints, int fillColorR, int fillColorG, int fillColorB, int opacity, bool isSimple, MapProjections mapProjection = CylindricalEquidistant, double centralLongitude = -90);
extern "C" TESSELLATE_API void ConvertVectorFileToTriangles(char* vectorFileName, char* triangleFileName);
extern "C" TESSELLATE_API void DrawString(char* string);
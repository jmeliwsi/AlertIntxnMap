#include "stdafx.h"
#include "tessellate.h"
#include "glut.h"
#include "ogr_api.h"
#include <stdlib.h>
#include "math.h"
#include <iostream>
using namespace std;

typedef void (__stdcall *GluTessCallbackType)();
int displayList_plane;
int displayList_olplane;
int displayList_bell206;
int displayList_737;
int displayList_747;
int displayList_c172;
int displayList_learjet;
int displayList_saab;
int displayList_triangle;
int displayList_circle;
int displayList_square;
int displayList_arrow;
FILE* outFile;

double const pi = 3.14159265358979323846;
double const MinAzimuthalLatitude = 0.0;
double const deg2rad = pi / 180.;
double const scaleFactor = 90;
double const lambertRefLat = 40;
double const lambertStdParallel1 = 33;
double const lambertStdParallel2 = 45;
double const lambertScaleFactor = 46;

BOOL APIENTRY DllMain( HANDLE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
    return TRUE;
}

void ProjectPoint(double x, double y, MapProjections mapProjection, double centralLongitude, double *px, double *py)
{
	if (mapProjection == Stereographic)
	{
		// Assumes a central latitude of 90 degrees
		double k = scaleFactor / (1 + sin(y * deg2rad));
		*px = k * cos(y * deg2rad) * sin((x - centralLongitude) * deg2rad);
		*py = -k * cos(y * deg2rad) * cos((x - centralLongitude) * deg2rad);
	}
	else if (mapProjection == Orthographic)
	{
		// Assumes a central latitude of 90 degrees
		*px = scaleFactor * cos(y * deg2rad) * sin((x - centralLongitude) * deg2rad);
		*py = -scaleFactor * cos(y * deg2rad) * cos((x - centralLongitude) * deg2rad);
	}
	else if (mapProjection == Mercator)
	{
		*px = x;
		*py = 50 * log(tan(y * deg2rad) + (1 / cos(y * deg2rad)));
	}
	else if (mapProjection == Lambert)
	{
		double sp1 = lambertStdParallel1 * deg2rad;
		double sp2 = lambertStdParallel2 * deg2rad;

		double n = (log(cos(sp1) * (1.0 / cos(sp2))))
			/ (log(tan((0.25 * pi) + (0.5 * sp2)) * (1.0 / tan((0.25 * pi) + (0.5 * sp1)))));
		double F = (cos(sp1) * pow(tan((0.25 * pi) + (0.5 * sp1)), n)) / n;
		double rho0 = F * pow((1.0 / tan((0.25 * pi) + (0.5 * lambertRefLat * deg2rad))), n);
		double rho = F * pow((1.0 / tan((0.25 * pi) + (0.5 * y * deg2rad))), n);

		*px = lambertScaleFactor * rho * sin(n * ((x - centralLongitude) * deg2rad));
		*py = rho0 - (lambertScaleFactor * rho * cos(n * ((x - centralLongitude) * deg2rad)));
	}
	else // CylindricalEquidistant
	{
		*px = x;
		*py = y;
	}
}

void CALLBACK beginCallback(GLenum which)
{
	glBegin(which);
}

void CALLBACK errorCallback(GLenum errorCode)
{
}

void CALLBACK endCallback(void)
{
	glEnd();
}

void CALLBACK vertexCallback(GLvoid *vertex)
{
	const GLdouble *pointer;

	pointer = (GLdouble *) vertex;
	glColor3dv(pointer+3);
	glVertex3dv(pointer);
}

void CALLBACK vertexCallbackTriangles(GLvoid *vertex)
{
	const GLdouble *pointer;

	pointer = (GLdouble *) vertex;
	glVertex3dv(pointer);
	fprintf(outFile,"%f,%f\n",pointer[0],pointer[1]);
}

void CALLBACK combineCallback(GLdouble coords[3], GLdouble *vertex_data[4], GLfloat weight[4], GLdouble **dataOut)
{
	GLdouble *vertex;

	vertex = (GLdouble *) malloc(6 * sizeof(GLdouble));

	vertex[0] = coords[0];
	vertex[1] = coords[1];
	vertex[2] = coords[2];
	*dataOut = vertex;
}

extern "C" TESSELLATE_API int TessellatePlaneSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_plane = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_plane, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[20][3];
	v[0][0] = 4; v[0][1] = 3; v[0][2] = 0;
	v[1][0] = 28; v[1][1] = -7; v[1][2] = 0;
	v[2][0] = 28; v[2][1] = -2; v[2][2] = 0;
	v[3][0] = 4; v[3][1] = 14; v[3][2] = 0;
	v[4][0] = 3; v[4][1] = 23; v[4][2] = 0;
	v[5][0] = 2; v[5][1] = 26; v[5][2] = 0;
	v[6][0] = 0; v[6][1] = 29; v[6][2] = 0;
	v[7][0] = -2; v[7][1] = 26; v[7][2] = 0;
	v[8][0] = -3; v[8][1] = 23; v[8][2] = 0;
	v[9][0] = -4; v[9][1] = 14; v[9][2] = 0;
	v[10][0] = -28; v[10][1] = -2; v[10][2] = 0;
	v[11][0] = -28; v[11][1] = -7; v[11][2] = 0;
	v[12][0] = -4; v[12][1] = 3; v[12][2] = 0;
	v[13][0] = -2; v[13][1] = -20; v[13][2] = 0;
	v[14][0] = -8; v[14][1] = -24; v[14][2] = 0;
	v[15][0] = -8; v[15][1] = -27; v[15][2] = 0;
	v[16][0] = 0; v[16][1] = -24; v[16][2] = 0;
	v[17][0] = 8; v[17][1] = -27; v[17][2] = 0;
	v[18][0] = 8; v[18][1] = -24; v[18][2] = 0;
	v[19][0] = 2; v[19][1] = -20; v[19][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<20; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_plane;
}

extern "C" TESSELLATE_API int TessellateOutlinedPlaneSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_olplane = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_olplane, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[20][3];
	v[0][0] = 4; v[0][1] = 3; v[0][2] = 0;
	v[1][0] = 28; v[1][1] = -7; v[1][2] = 0;
	v[2][0] = 28; v[2][1] = -2; v[2][2] = 0;
	v[3][0] = 4; v[3][1] = 14; v[3][2] = 0;
	v[4][0] = 3; v[4][1] = 23; v[4][2] = 0;
	v[5][0] = 2; v[5][1] = 26; v[5][2] = 0;
	v[6][0] = 0; v[6][1] = 29; v[6][2] = 0;
	v[7][0] = -2; v[7][1] = 26; v[7][2] = 0;
	v[8][0] = -3; v[8][1] = 23; v[8][2] = 0;
	v[9][0] = -4; v[9][1] = 14; v[9][2] = 0;
	v[10][0] = -28; v[10][1] = -2; v[10][2] = 0;
	v[11][0] = -28; v[11][1] = -7; v[11][2] = 0;
	v[12][0] = -4; v[12][1] = 3; v[12][2] = 0;
	v[13][0] = -2; v[13][1] = -20; v[13][2] = 0;
	v[14][0] = -8; v[14][1] = -24; v[14][2] = 0;
	v[15][0] = -8; v[15][1] = -27; v[15][2] = 0;
	v[16][0] = 0; v[16][1] = -24; v[16][2] = 0;
	v[17][0] = 8; v[17][1] = -27; v[17][2] = 0;
	v[18][0] = 8; v[18][1] = -24; v[18][2] = 0;
	v[19][0] = 2; v[19][1] = -20; v[19][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<20; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// Initialize the border vertices
	GLdouble _v[20][3];
	_v[0][0] = 4.5; _v[0][1] = 2.5; _v[0][2] = 0;
	_v[1][0] = 28.5; _v[1][1] = -7.5; _v[1][2] = 0;
	_v[2][0] = 28.5; _v[2][1] = -1.5; _v[2][2] = 0;
	_v[3][0] = 4.5; _v[3][1] = 14; _v[3][2] = 0;
	_v[4][0] = 3.5; _v[4][1] = 23; _v[4][2] = 0;
	_v[5][0] = 2.5; _v[5][1] = 26; _v[5][2] = 0;
	_v[6][0] = 0; _v[6][1] = 29.5; _v[6][2] = 0;
	_v[7][0] = -2.5; _v[7][1] = 26; _v[7][2] = 0;
	_v[8][0] = -3.5; _v[8][1] = 23; _v[8][2] = 0;
	_v[9][0] = -4.5; _v[9][1] = 14; _v[9][2] = 0;
	_v[10][0] = -28.5; _v[10][1] = -1.5; _v[10][2] = 0;
	_v[11][0] = -28.5; _v[11][1] = -7.5; _v[11][2] = 0;
	_v[12][0] = -4.5; _v[12][1] = 2.5; _v[12][2] = 0;
	_v[13][0] = -2.5; _v[13][1] = -20; _v[13][2] = 0;
	_v[14][0] = -8.5; _v[14][1] = -23.5; _v[14][2] = 0;
	_v[15][0] = -8.5; _v[15][1] = -27.5; _v[15][2] = 0;
	_v[16][0] = 0; _v[16][1] = -24.5; _v[16][2] = 0;
	_v[17][0] = 8.5; _v[17][1] = -27.5; _v[17][2] = 0;
	_v[18][0] = 8.5; _v[18][1] = -23.5; _v[18][2] = 0;
	_v[19][0] = 2.5; _v[19][1] = -20; _v[19][2] = 0;

	// Draw a white border around the plane
	glDepthRange(0.0,0.9);
	glColor3f(1,1,1);
	glLineWidth(1);
	glBegin(GL_LINE_LOOP);
	for (int i=0; i<20; i++)
		glVertex2d(_v[i][0],_v[i][1]);
	glEnd();
	glDepthRange(0.0,1.0);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_olplane;
}

extern "C" TESSELLATE_API int TessellateBell206Symbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_bell206 = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_bell206, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[52][3] = 
	{
		{0,     28.5, 0},
		{1,     28,   0},
		{2,     27.5, 0},
		{3,     26.5, 0},
		{4,     25,   0},
		{4.5,   23,   0},
		{4.5,   15,   0},
		{21.5,  31,   0},
		{24,    29,   0},
		{4.5,   10,   0},
		{4.5,   6,    0},
		{24,    -12,  0},
		{21.5,  -15,  0},
		{3,     2,    0},
		{2.75,  1,    0},
		{2.5,   0,    0},
		{2.25,  -1,   0},
		{2,     -2,   0},
		{1.75,  -3,   0},
		{1.5,   -4,   0},
		{1.25,  -5,   0},
		{1,     -6,   0},
		{1,     -12,  0},
		{6,     -12,  0},
		{6,     -15,  0},
		{1,     -15,  0},
		{1,     -29,  0},
		{-1,    -29,  0},
		{-1,    -15,  0},
		{-6,    -15,  0},
		{-6,    -12,  0},
		{-1,    -12,  0},
		{-1,    -6,   0},
		{-1.25, -5,   0},
		{-1.5,  -4,   0},
		{-1.75, -3,   0},
		{-2,    -2,   0},
		{-2.25, -1,   0},
		{-2.5,  0,    0},
		{-2.75, 1,    0},
		{-21.5, -15,  0},
		{-24,   -12,  0},
		{-4.5,  6,    0},
		{-4.5,  10,   0},
		{-24,   29,   0},
		{-21.5, 31,   0},
		{-4.5,  15,   0},
		{-4.5,  23,   0},
		{-4,    25,   0},
		{-3,    26.5, 0},
		{-2,    27.5, 0},
		{-1,    28,   0}
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<52; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_bell206;
}

extern "C" TESSELLATE_API int Tessellate737Symbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_737 = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_737, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[66][3] = 
	{
		{0,      30.5,  0},
		{0.5,    30,    0},
		{1,      28.5,  0},
		{1.5,    27.5,  0},
		{2,      26,    0},
		{2.5,    23.5,  0},
		{3,      21,    0},
		{3.5,    18,    0},
		{4,      15,    0},
		{3.75,   9.5,   0},
		{8.75,   6,     0},
		{8.75,   8,     0},
		{11,     8,     0},
		{11.75,  4.25,  0},
		{28,     -4,    0},
		{28.75,  -7,    0},
		{28,     -7.75, 0},
		{11,     -3,    0},
		{4,      -3,    0},
		{4,      -6,    0},
		{3.75,   -9,    0},
		{3.5,    -11,   0},
		{3.25,   -13,   0},
		{3,      -15,   0},
		{2.75,   -17,   0},
		{2.5,    -18,   0},
		{2.25,   -19,   0},
		{2,      -20,   0},
		{2,      -21,   0},
		{10.75,  -27,   0},
		{11.5,   -28,   0},
		{11,     -30,   0},
		{1,      -28,   0},
		{0,      -28.5, 0},
		{-1,     -28,   0},
		{-11,    -30,   0},
		{-11.5,  -28,   0},
		{-10.75, -27,   0},
		{-2,     -21,   0},
		{-2,     -20,   0},
		{-2.25,  -19,   0},
		{-2.5,   -18,   0},
		{-2.75,  -17,   0},
		{-3,     -15,   0},
		{-3.25,  -13,   0},
		{-3.5,   -11,   0},
		{-3.75,  -9,    0},
		{-4,     -6,    0},
		{-4,     -3,    0},
		{-11,    -3,    0},
		{-28,    -7.75, 0},
		{-28.75, -7,    0},
		{-28,    -4,    0},
		{-11.75, 4.25,  0},
		{-11,    8,     0},
		{-8.75,  8,     0},
		{-8.75,  6,     0},
		{-3.75,  9.5,   0},
		{-4,     15,    0},
		{-3.5,   18,    0},
		{-3,     21,    0},
		{-2.5,   23.5,  0},
		{-2,     26,    0},
		{-1.5,   27.5,  0},
		{-1,     28.5,  0},
		{-0.5,   30,    0},
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<66; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_737;
}

extern "C" TESSELLATE_API int Tessellate747Symbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_747 = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_747, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[71][3] = 
	{
		{0,     31.5,  0},
		{0.5,   31,    0},
		{1,     30,    0},
		{1.5,   28.5,  0},
		{2,     27,    0},
		{2.5,   24.5,  0},
		{3,     22,    0},
		{3,     13,    0},
		{9,     7,     0},
		{9,     8,     0},
		{8.5,   8,     0},
		{8.5,   10.5,  0},
		{11,    10.5,  0},
		{11,    8,     0},
		{10.5,  8,     0},
		{10.5,  6,     0},
		{19,    -0.5,  0},
		{19,    0.5,   0},
		{18.5,  0.5,   0},
		{18.5,  3,     0},
		{21,    3,     0},
		{21,    0.5,   0},
		{20.5,  0.5,   0},
		{20.5,  -1.5,  0},
		{28,    -8,    0},
		{28,    -13,   0},
		{12,    -2,    0},
		{3,     -0.25, 0},
		{3,     -14,   0},
		{2.5,   -16,   0},
		{2,     -19,   0},
		{1.75,  -22,   0},
		{10,    -29,   0},
		{10,    -31.5, 0},
		{1,     -28.5, 0},
		{0,     -32,   0},
		{-0,    -32,   0},
		{-1,    -28.5, 0},
		{-10,   -31.5, 0},
		{-10,   -29,   0},
		{-1.75, -22,   0},
		{-2,    -19,   0},
		{-2.5,  -16,   0},
		{-3,    -14,   0},
		{-3,    -0.25, 0},
		{-12,   -2,    0},
		{-28,   -13,   0},
		{-28,   -8,    0},
		{-20.5, -1.5,  0},
		{-20.5, 0.5,   0},
		{-21,   0.5,   0},
		{-21,   3,     0},
		{-18.5, 3,     0},
		{-18.5, 0.5,   0},
		{-19,   0.5,   0},
		{-19,   -0.5,  0},
		{-10.5, 6,     0},
		{-10.5, 8,     0},
		{-11,   8,     0},
		{-11,   10.5,  0},
		{-8.5,  10.5,  0},
		{-8.5,  8,     0},
		{-9,    8,     0},
		{-9,    7,     0},
		{-3,    13,    0},
		{-3,    22,    0},
		{-2.5,  24.5,  0},
		{-2,    27,    0},
		{-1.5,  28.5,  0},
		{-1,    30,    0},
		{-0.5,  31,    0}
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<71; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_747;
}

extern "C" TESSELLATE_API int TessellateC172Symbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_c172 = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_c172, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[36][3] = 
	{
		{0,     21,    0},
		{0.5,   19.5,  0},
		{2,     19,    0},
		{2.25,  18,    0},
		{2.5,   16,    0},
		{2.75,  15,    0},
		{3,     13,    0},
		{3,     11,    0},
		{14,    11,    0},
		{29,    9.5,   0},
		{29,    4,     0},
		{15,    2.25,  0},
		{3,     2.25,  0},
		{1,     -13.5, 0},
		{9,     -15,   0},
		{9,     -18.5, 0},
		{1,     -20,   0},
		{0.5,   -18.5, 0},
		{0,     -19.5, 0},
		{-0.5,  -18.5, 0},
		{-1,    -20,   0},
		{-9,    -18.5, 0},
		{-9,    -15,   0},
		{-1,    -13.5, 0},
		{-3,    2.25,  0},
		{-15,   2.25,  0},
		{-29,   4,     0},
		{-29,   9.5,   0},
		{-14,   11,    0},
		{-3,    11,    0},
		{-3,    13,    0},
		{-2.75, 15,    0},
		{-2.5,  16,    0},
		{-2.25, 18,    0},
		{-2,    19,    0},
		{-0.5,  19.5,  0}
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<36; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_c172;
}

extern "C" TESSELLATE_API int TessellateLearJetSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_learjet = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_learjet, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[46][3] = 
	{
		{0,      25.25, 0},
		{0.5,    25,    0},
		{1,      24.75, 0},
		{1.25,   24,    0},
		{1.5,    23.5,  0},
		{1.75,   23,    0},
		{2,      22,    0},
		{2,      9.25,  0},
		{24,     -8,    0},
		{25.75,  -11,   0},
		{23,     -9,    0},
		{8,      -2.5,  0},
		{2,      -2.5,  0},
		{2,      -5.5,  0},
		{5,      -5.5,  0},
		{5.25,   -8,    0},
		{5,      -10,   0},
		{4.75,   -14,   0},
		{4.5,    -14.5, 0},
		{1,      -15.5, 0},
		{0.75,   -17,   0},
		{8.5,    -24,   0},
		{8.5,    -26,   0},
		{0,      -22.5, 0},
		{-8.5,   -26,   0},
		{-8.5,   -24,   0},
		{-0.75,  -17,   0},
		{-1,     -15.5, 0},
		{-4.5,   -14.5, 0},
		{-4.75,  -14,   0},
		{-5,     -10,   0},
		{-5.25,  -8,    0},
		{-5,     -5.5,  0},
		{-2,     -5.5,  0},
		{-2,     -2.5,  0},
		{-8,     -2.5,  0},
		{-23,    -9,    0},
		{-25.75, -11,   0},
		{-24,    -8,    0},
		{-2,     9.25,  0},
		{-2,     22,    0},
		{-1.75,  23,    0},
		{-1.5,   23.5,  0},
		{-1.25,  24,    0},
		{-1,     24.75, 0},
		{-0.5,   25,    0}
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<46; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_learjet;
}

extern "C" TESSELLATE_API int TessellateSAABSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_saab = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_saab, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[58][3] = 
	{
		{0, 26.5,       0},
		{0.5, 26,       0},
		{1, 25.5,       0},
		{1.5,   25,     0},
		{2,     24,     0},
		{2.5,   23,     0},
		{2.75,  21,     0},
		{3,     20,     0},
		{3,     6,      0},
		{7.5,   5.5,    0},
		{8,     11,     0},
		{9,     12,     0},
		{10,    11,     0},
		{10.5,  5,      0},
		{28,    3,      0},
		{28.5,  2.5,    0},
		{29,    2,      0},
		{29,    0,      0},
		{3,     -1.5,   0},
		{3,     -16,    0},
		{2.25,  -17,    0},
		{11,    -18.75, 0},
		{12,    -19.25, 0},
		{12.5,  -20,    0},
		{12.5,  -22,    0},
		{1.5,   -23,    0},
		{1.25,  -24,    0},
		{1,     -25,    0},
		{0.5,   -26,    0},
		{0,     -26.5,  0},
		{-0.5,  -26,    0},
		{-1,    -25,    0},
		{-1.25, -24,    0},
		{-1.5,  -23,    0},
		{-12.5, -22,    0},
		{-12.5, -20,    0},
		{-12,   -19.25, 0},
		{-11,   -18.75, 0},
		{-2.25, -17,    0},
		{-3,    -16,    0},
		{-3,    -1.5,   0},
		{-29,   0,      0},
		{-29,   2,      0},
		{-28.5, 2.5,    0},
		{-28,   3,      0},
		{-11.5, 5,      0},
		{-11,   11,     0},
		{-10,   12,     0},
		{-9,    11,     0},
		{-8.5,  5.5,    0},
		{-3,    6,      0},
		{-3,    20,     0},
		{-2.75, 21,     0},
		{-2.5,  23,     0},
		{-2,    24,     0},
		{-1.5,  25,     0},
		{-1,    25.5,   0},
		{-0.5,  26,     0}
	};

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<58; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_saab;
}

extern "C" TESSELLATE_API int TessellateTriangleSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_triangle = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_triangle, GL_COMPILE);

	// Initialize vertices
	GLdouble v[3][3];
	v[0][0] = 0; v[0][1] = 15; v[0][2] = 0;
	v[1][0] = -15; v[1][1] = -15; v[1][2] = 0;
	v[2][0] = 15; v[2][1] = -15; v[2][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<3; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_triangle;
}

extern "C" TESSELLATE_API int TessellateCircleSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_circle = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_circle, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[24][3];
	v[0][0] = 15.000000; v[0][1] = 0.000000; v[0][2] = 0;
	v[1][0] = 14.488887; v[1][1] = 3.882286; v[1][2] = 0;
	v[2][0] = 12.990381; v[2][1] = 7.500000; v[2][2] = 0;
	v[3][0] = 10.606602; v[3][1] = 10.606602; v[3][2] = 0;
	v[4][0] = 7.500000; v[4][1] = 12.990381; v[4][2] = 0;
	v[5][0] = 3.882285; v[5][1] = 14.488887; v[5][2] = 0;
	v[6][0] = -0.000000; v[6][1] = 15.000000; v[6][2] = 0;
	v[7][0] = -3.882286; v[7][1] = 14.488887; v[7][2] = 0;
	v[8][0] = -7.500000; v[8][1] = 12.990381; v[8][2] = 0;
	v[9][0] = -10.606602; v[9][1] = 10.606601; v[9][2] = 0;
	v[10][0] = -12.990381; v[10][1] = 7.499999; v[10][2] = 0;
	v[11][0] = -14.488888; v[11][1] = 3.882285; v[11][2] = 0;
	v[12][0] = -15.000000; v[12][1] = -0.000001; v[12][2] = 0;
	v[13][0] = -14.488887; v[13][1] = -3.882286; v[13][2] = 0;
	v[14][0] = -12.990381; v[14][1] = -7.500001; v[14][2] = 0;
	v[15][0] = -10.606601; v[15][1] = -10.606602; v[15][2] = 0;
	v[16][0] = -7.499999; v[16][1] = -12.990382; v[16][2] = 0;
	v[17][0] = -3.882285; v[17][1] = -14.488888; v[17][2] = 0;
	v[18][0] = 0.000001; v[18][1] = -15.000000; v[18][2] = 0;
	v[19][0] = 3.882287; v[19][1] = -14.488887; v[19][2] = 0;
	v[20][0] = 7.500001; v[20][1] = -12.990380; v[20][2] = 0;
	v[21][0] = 10.606603; v[21][1] = -10.606601; v[21][2] = 0;
	v[22][0] = 12.990382; v[22][1] = -7.499999; v[22][2] = 0;
	v[23][0] = 14.488888; v[23][1] = -3.882284; v[23][2] = 0;

	// Tessellate
	glDepthRange(0.1,1.0);
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<24; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	//// Draw a white border around the circle
	//glDepthRange(0.0,0.9);
	//glColor3f(1,1,1);
	//glLineWidth(1);
	//glBegin(GL_LINE_LOOP);
	//for (int i=0; i<24; i++)
	//	glVertex2d(v[i][0],v[i][1]);
	//glEnd();
	//glDepthRange(0.0,1.0);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_circle;
}

extern "C" TESSELLATE_API int TessellateSquareSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_square = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_square, GL_COMPILE);

	// Initialize the vertices
	GLdouble v[4][4];
	v[0][0] = 15; v[0][1] = -15; v[0][2] = 0;
	v[1][0] = 15; v[1][1] = 15; v[1][2] = 0;
	v[2][0] = -15; v[2][1] = 15; v[2][2] = 0;
	v[3][0] = -15; v[3][1] = -15; v[3][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<4; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_square;
}

extern "C" TESSELLATE_API int TessellateArrowSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_arrow = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_arrow, GL_COMPILE);

	// Initialize vertices
	GLdouble v[7][3];
	v[0][0] = 0; v[0][1] = 30; v[0][2] = 0;
	v[1][0] = -12; v[1][1] = 5; v[1][2] = 0;
	v[2][0] = -3; v[2][1] = 5; v[2][2] = 0;
	v[3][0] = -3; v[3][1] = -30; v[3][2] = 0;
	v[4][0] = 3; v[4][1] = -30; v[4][2] = 0;
	v[5][0] = 3; v[5][1] = 5; v[5][2] = 0;
	v[6][0] = 12; v[6][1] = 5; v[6][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<7; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_arrow;
}

extern "C" TESSELLATE_API int TessellateShiftedArrowSymbol(void)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	displayList_arrow = glGenLists(1);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Start a new GL list to contain all the features
	glNewList(displayList_arrow, GL_COMPILE);

	// Initialize vertices
	GLdouble v[7][3];
	v[0][0] = 0; v[0][1] = 60; v[0][2] = 0;
	v[1][0] = -12; v[1][1] = 35; v[1][2] = 0;
	v[2][0] = -3; v[2][1] = 35; v[2][2] = 0;
	v[3][0] = -3; v[3][1] = 0; v[3][2] = 0;
	v[4][0] = 3; v[4][1] = 0; v[4][2] = 0;
	v[5][0] = 3; v[5][1] = 35; v[5][2] = 0;
	v[6][0] = 12; v[6][1] = 35; v[6][2] = 0;

	// Tessellate
	gluTessBeginPolygon(tobj, NULL);
	gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY, GL_FALSE);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);
	for (int i=0; i<7; i++)
        gluTessVertex(tobj,v[i],v[i]);
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// End the GL list
	glEndList();

	// Delete the tesselation object
	gluDeleteTess(tobj);

	return displayList_arrow;
}

extern "C" TESSELLATE_API void TessellateVectorFile(char* vectorFileName, int fillColorRGB[3], bool useTwoColors, int fillColor2RGB[3], int opacity, MapProjections mapProjection, double centralLongitude) 
{
	double deg2rad = 3.14159265358979323846 / 180.;
	double scaleFactor = 90;

	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);

	// Register all OGR drivers
	OGRRegisterAll();
	
	// Open the OGR data source (shapefile)
	OGRDataSourceH poDS;
	OGRSFDriverH driver;
    poDS = OGROpen(vectorFileName, FALSE, &driver);
    if (poDS == NULL) return;

	// Get the first (and only in this case) layer
	OGRLayerH poLayer;
	int nLayers = OGR_DS_GetLayerCount(poDS);
	if (nLayers < 1) return;
	poLayer = OGR_DS_GetLayer(poDS,0);
	OGR_L_ResetReading(poLayer);

	// Some OpenGL initialization
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glShadeModel(GL_FLAT);
	glColor4f(((float)fillColorRGB[0])/255., ((float)fillColorRGB[1])/255., ((float)fillColorRGB[2])/255., ((float)opacity)/100.);

	// Extract the features
	OGRFeatureH poFeature;
    while ((poFeature = OGR_L_GetNextFeature(poLayer)) != NULL)
    {
        // Get the geometry object
		OGRGeometryH poGeometry;
        poGeometry = OGR_F_GetGeometryRef(poFeature);

        // Process the geometry object
		if (poGeometry != NULL)
        {
			OGRGeometryH poPolygon;
			int numGeometries;

			// Determine the number of geometries
			if (OGR_G_GetGeometryType(poGeometry) == wkbPolygon || OGR_G_GetGeometryType(poGeometry) == wkbMultiPolygon ||
				OGR_G_GetGeometryType(poGeometry) < 0) // < 0 indicates a 3D geometry
				numGeometries = OGR_G_GetGeometryCount(poGeometry);
			else
			{
				// some other geometry we won't deal with right now
				numGeometries = 0;
		        OGR_F_Destroy(poFeature);
				continue;
			}

            // Iterate over the geometries
			for (int i=0; i<numGeometries; i++)
			{
				if (useTwoColors)
				{
					if (i > 0)
						glColor4f(((float)fillColor2RGB[0])/255., ((float)fillColor2RGB[1])/255., ((float)fillColor2RGB[2])/255., ((float)opacity)/100.);
					else
						glColor4f(((float)fillColorRGB[0])/255., ((float)fillColorRGB[1])/255., ((float)fillColorRGB[2])/255., ((float)opacity)/100.);
				}

				// Pull out the polygon to process
				poPolygon = OGR_G_GetGeometryRef(poGeometry, i);
				if (OGR_G_GetGeometryType(poGeometry) == wkbMultiPolygon)
					poPolygon = OGR_G_GetGeometryRef(poPolygon, 0);

				if (poPolygon != NULL)
				{
					int iPoints;

					// Begin the GL polygon and contour for this feature
					gluTessBeginPolygon(tobj, NULL);
					gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY , GL_FALSE);
					gluTessNormal(tobj, 0.0, 0.0, 1.0);
					gluTessBeginContour(tobj);

					// Create the array to hold the points for this feature
					int nPoints = OGR_G_GetPointCount(poPolygon);
					GLdouble (*polypoint)[3] = new GLdouble[nPoints][3];

					// Iterate the points of this feature
					for (iPoints = 0; iPoints < nPoints; iPoints++)
					{
						double _x, _y, _z;

						// Get the next point
						OGR_G_GetPoint(poPolygon, iPoints, &_x, &_y, &_z);

						// Support map projection if requested
						double px, py;
						if ((mapProjection == Stereographic || mapProjection == Orthographic || mapProjection == Lambert) && _y <= MinAzimuthalLatitude)
							continue;
						ProjectPoint(_x, _y, mapProjection, centralLongitude, &px, &py);

						// Add the point to the polygon to be tesselated and rendered
						polypoint[iPoints][0] = px;
						polypoint[iPoints][1] = py;
						polypoint[iPoints][2] = 0.0;
						gluTessVertex(tobj, polypoint[iPoints], polypoint[iPoints]);
					}

					// End the contour and the polygon for this feature
					gluTessEndContour(tobj);
					gluTessEndPolygon(tobj);

					// Cleanup
					delete [] polypoint;
				}
			}
        }
        OGR_F_Destroy(poFeature);
	}

	// OGR Cleanup
	OGR_DS_Destroy(poDS);

	// Delete the tesselation object
	gluDeleteTess(tobj);
}

extern "C" TESSELLATE_API void TessellatePolygon(double x[], double y[], int nPoints, int fillColorR, int fillColorG, int fillColorB, int opacity, bool isSimple, MapProjections mapProjection, double centralLongitude)
{
	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)glVertex3dv);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	if (isSimple)
		gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)NULL);
	else
		gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)combineCallback);

	// Some OpenGL initialization
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glShadeModel(GL_FLAT);

	// Begin the GL polygon and contour for this feature
	glColor4f(((float)fillColorR)/255., ((float)fillColorG)/255., ((float)fillColorB)/255., ((float)opacity)/100.);
	gluTessBeginPolygon(tobj, NULL);
	if (isSimple)
		gluTessProperty(tobj, GLU_TESS_BOUNDARY_ONLY , GL_FALSE);
	else
		gluTessProperty(tobj, GLU_TESS_WINDING_RULE, GLU_TESS_WINDING_NONZERO);
	gluTessNormal(tobj, 0.0, 0.0, 1.0);
	gluTessBeginContour(tobj);

	// Create the array to hold the points for this feature
	GLdouble (*polypoint)[3] = new GLdouble[nPoints][3];

	for (int i=0; i<nPoints; i++)
	{
		double px, py;
		if ((mapProjection == Stereographic || mapProjection == Orthographic || mapProjection == Lambert) && y[i] < MinAzimuthalLatitude)
			ProjectPoint(x[i], 0, mapProjection, centralLongitude, &px, &py);
		else
			ProjectPoint(x[i], y[i], mapProjection, centralLongitude, &px, &py);

		// Get the next point
		polypoint[i][0] = px;
		polypoint[i][1] = py;
		polypoint[i][2] = 0.0;

		// Add the point to the polygon to be tesselated and rendered
		gluTessVertex(tobj, polypoint[i], polypoint[i]);
	}

	// End the contour and the polygon for this feature
	gluTessEndContour(tobj);
	gluTessEndPolygon(tobj);

	// Cleanup
	delete [] polypoint;

	// Delete the tesselation object
	gluDeleteTess(tobj);
}

extern "C" TESSELLATE_API void ConvertVectorFileToTriangles(char* vectorFileName, char* triangleFileName)
{
	// Open the text file that will hold the tessellation triangle vertices
	fopen_s(&outFile,triangleFileName,"w");
	if (outFile == NULL) return;

	// Create the GL tesselation object and rendering list
	GLUtesselator *tobj;
	glClearColor(0.0, 0.0, 0.0, 0.0);
	tobj = gluNewTess();

	// Setup the tesselation callback functions
	gluTessCallback(tobj, GLU_TESS_VERTEX, (GluTessCallbackType)vertexCallbackTriangles);
	gluTessCallback(tobj, GLU_TESS_BEGIN, (GluTessCallbackType)beginCallback);
	gluTessCallback(tobj, GLU_TESS_END, endCallback);
	gluTessCallback(tobj, GLU_TESS_ERROR, (GluTessCallbackType)errorCallback);
	gluTessCallback(tobj, GLU_TESS_COMBINE, (GluTessCallbackType)combineCallback);
	gluTessCallback(tobj, GLU_TESS_EDGE_FLAG, (GluTessCallbackType)glEdgeFlag);

	// Register all OGR drivers
	OGRRegisterAll();
	
	// Open the OGR data source (shapefile)
	OGRDataSourceH poDS;
	OGRSFDriverH driver;
    poDS = OGROpen(vectorFileName, FALSE, &driver);
    if (poDS == NULL) return;

	// Get the first (and only in this case) layer
	OGRLayerH poLayer;
	int nLayers = OGR_DS_GetLayerCount(poDS);
	if (nLayers < 1) return;
	poLayer = OGR_DS_GetLayer(poDS,0);
	OGR_L_ResetReading(poLayer);

	// Some OpenGL initialization
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	glShadeModel(GL_FLAT);

	// Extract the features
	OGRFeatureH poFeature;
    while ((poFeature = OGR_L_GetNextFeature(poLayer)) != NULL)
    {
        // Get the geometry object
		OGRGeometryH poGeometry;
        poGeometry = OGR_F_GetGeometryRef(poFeature);

        // Process the geometry object
		if (poGeometry != NULL)
        {
			OGRGeometryH poPolygon;
			int numGeometries;

			// Determine the number of geometries
			if (OGR_G_GetGeometryType(poGeometry) == wkbPolygon || OGR_G_GetGeometryType(poGeometry) == wkbMultiPolygon)
				numGeometries = OGR_G_GetGeometryCount(poGeometry);
			else
			{
				// some other geometry we won't deal with right now
				numGeometries = 0;
		        OGR_F_Destroy(poFeature);
				continue;
			}

            // Iterate over the geometries
			for (int i=0; i<numGeometries; i++)
			{
				// Pull out the polygon to process
				poPolygon = OGR_G_GetGeometryRef(poGeometry, i);
				if (OGR_G_GetGeometryType(poGeometry) == wkbMultiPolygon)
					poPolygon = OGR_G_GetGeometryRef(poPolygon, 0);

				if (poPolygon != NULL)
				{
					int iPoints;

					// Begin the GL polygon and contour for this feature
					gluTessBeginPolygon(tobj, NULL);
					gluTessProperty(tobj, GLU_TESS_WINDING_RULE, GLU_TESS_WINDING_NEGATIVE);
					gluTessNormal(tobj, 0.0, 0.0, 1.0);
					gluTessBeginContour(tobj);

					// Create the array to hold the points for this feature
					int nPoints = OGR_G_GetPointCount(poPolygon);
					GLdouble (*polypoint)[3] = new GLdouble[nPoints][3];

					// Iterate the points of this feature
					for (iPoints = 0; iPoints < nPoints; iPoints++)
					{
						double _x, _y, _z;

						// Get the next point
						OGR_G_GetPoint(poPolygon, iPoints, &_x, &_y, &_z);
						polypoint[iPoints][0] = _x;
						polypoint[iPoints][1] = _y;
						polypoint[iPoints][2] = 0.0;

						// Add the point to the polygon to be tesselated and rendered
						gluTessVertex(tobj, polypoint[iPoints], polypoint[iPoints]);
					}

					// End the contour and the polygon for this feature
					gluTessEndContour(tobj);
					gluTessEndPolygon(tobj);

					// Cleanup
					delete [] polypoint;
				}
			}
        }
        OGR_F_Destroy(poFeature);
    }

	// OGR Cleanup
	OGR_DS_Destroy(poDS);

	// Delete the tesselation object
	gluDeleteTess(tobj);

	// Close the triangle vertices file
	fclose(outFile);
}

extern "C" TESSELLATE_API void DrawString(char* text)
{
	char *p;
    for (p = text; *p; p++)
		glutStrokeCharacter(GLUT_STROKE_MONO_ROMAN, *p);
}
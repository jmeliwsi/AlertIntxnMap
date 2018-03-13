using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace WSIMap
{
	public class ContouringUtility
	{
		/*
		* This code is base on the work of Nicholas Yue and Paul D. Bourke CONREC.F routine
		*/

		/**
		 * Conrec a straightforward method of contouring some surface represented a regular 
		 * triangular mesh. 
		 *
		 * Ported from the C++ code by Nicholas Yue (see above copyright notice).
		 * @see http://astronomy.swin.edu.au/pbourke/projection/conrec/ for full description
		 * of code and original C++ source.
		 *
		 * @author  Bradley White
		 * @version 1.0 
		 */

		// Note that castab is arranged differently from the FORTRAN code because
		// Fortran and C/C++ arrays are transposed of each other, in this case
		// it is more tricky as castab is in 3 dimension
		private static int[, ,] castab = new int[3, 3, 3]
        {
            {
                {0,0,8},{0,2,5},{7,6,9}
            },
            {
                {0,3,4},{1,3,1},{4,3,0}
            },
            {
                {9,6,7},{5,2,0},{8,0,0}
            }
        };


		/**
		 *  ORIGINAL ALGORITHM'S PARAMETERS.
		 *  Documentation: http://local.wasp.uwa.edu.au/~pbourke/papers/conrec/
		 * 
		 *     contour is a contouring subroutine for rectangularily spaced data 
		 *
		 *     It emits calls to a line drawing subroutine supplied by the user
		 *     which draws a contour map corresponding to real*4data on a randomly
		 *     spaced rectangular grid. The coordinates emitted are in the same
		 *     units given in the x() and y() arrays.
		 *
		 *     Any number of contour levels may be specified but they must be
		 *     in order of increasing value.
		 *
		 *
		 * @param d  - matrix of data to contour
		 * @param ilb,iub,jlb,jub - index bounds of data matrix
		 *
		 *             The following two, one dimensional arrays (x and y) contain the horizontal and
		 *             vertical coordinates of each sample points.
		 * @param x  - data matrix column coordinates
		 * @param y  - data matrix row coordinates
		 * @param nc - number of contour levels
		 * @param z  - contour levels in increasing order.
		 * 
		 */

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data">matrix of data to contour</param>
		/// <param name="xCoords">The x-coordinates for the data points. Corresponds to longitude. Array length should match data's first dimension length.</param>
		/// <param name="yCoords">The y-coordinates for the data points. Corresponds to latitude. Array length should match data's second dimension length.</param>
		/// <param name="contours">The contour levels in increasing order.</param>
		/// <param name="contourCollection">A FeatureCollection to populate with the lines resulting from contouring.</param>
		public static void contour(double[][] data, double[] xCoords, double[] yCoords, double[] contours, ref FeatureCollection contourCollection)
		{
			int m1;
			int m2;
			int m3;
			int case_value;
			double dmin;
			double dmax;
			double x1 = 0.0;
			double x2 = 0.0;
			double y1 = 0.0;
			double y2 = 0.0;
			int i, j, k, m;
			double[] h = new double[5];
			int[] sh = new int[5];
			double[] xh = new double[5];
			double[] yh = new double[5];


			// The indexing of im and jm should be noted as it has to start from zero
			// unlike the fortran counter part
			int[] im = new int[4] { 0, 1, 1, 0 };
			int[] jm = new int[4] { 0, 0, 1, 1 };

			for (j = (yCoords.Length - 2); j >= 0; j--)
			{
				for (i = 0; i <= xCoords.Length - 2; i++)
				{
					double temp1, temp2;
					temp1 = Math.Min(data[i][j], data[i][j + 1]);
					temp2 = Math.Min(data[i + 1][j], data[i + 1][j + 1]);
					dmin = Math.Min(temp1, temp2);
					temp1 = Math.Max(data[i][j], data[i][j + 1]);
					temp2 = Math.Max(data[i + 1][j], data[i + 1][j + 1]);
					dmax = Math.Max(temp1, temp2);

					if (dmax >= contours[0] && dmin <= contours[contours.Length - 1])
					{
						for (k = 0; k < contours.Length; k++)
						{
							if (contours[k] >= dmin && contours[k] <= dmax)
							{
								for (m = 4; m >= 0; m--)
								{
									if (m > 0)
									{
										// The indexing of im and jm should be noted as it has to
										// start from zero
										h[m] = data[i + im[m - 1]][j + jm[m - 1]] - contours[k];
										xh[m] = xCoords[i + im[m - 1]];
										yh[m] = yCoords[j + jm[m - 1]];
									}
									else
									{
										h[0] = 0.25 * (h[1] + h[2] + h[3] + h[4]);
										xh[0] = 0.5 * (xCoords[i] + xCoords[i + 1]);
										yh[0] = 0.5 * (yCoords[j] + yCoords[j + 1]);
									}

									if (h[m] > 0.0)
										sh[m] = 1;
									else if (h[m] < 0.0)
										sh[m] = -1;
									else
										sh[m] = 0;
								}

								//
								// Note: at this stage the relative heights of the corners and the
								// centre are in the h array, and the corresponding coordinates are
								// in the xh and yh arrays. The centre of the box is indexed by 0
								// and the 4 corners by 1 to 4 as shown below.
								// Each triangle is then indexed by the parameter m, and the 3
								// vertices of each triangle are indexed by parameters m1,m2,and
								// m3.
								// It is assumed that the centre of the box is always vertex 2
								// though this isimportant only when all 3 vertices lie exactly on
								// the same contour level, in which case only the side of the box
								// is drawn.
								//
								//
								//      vertex 4 +-------------------+ vertex 3
								//               | \               / |
								//               |   \    m-3    /   |
								//               |     \       /     |
								//               |       \   /       |
								//               |  m=2    X   m=2   |       the centre is vertex 0
								//               |       /   \       |
								//               |     /       \     |
								//               |   /    m=1    \   |
								//               | /               \ |
								//      vertex 1 +-------------------+ vertex 2
								//
								//
								//
								//               Scan each triangle in the box
								//

								for (m = 1; m <= 4; m++)
								{
									m1 = m;
									m2 = 0;
									m3 = (m != 4) ? m + 1 : 1;

									case_value = castab[sh[m1] + 1, sh[m2] + 1, sh[m3] + 1];

									if (case_value != 0)
									{
										switch (case_value)
										{
											case 1: // Line between vertices 1 and 2
												x1 = xh[m1];
												y1 = yh[m1];
												x2 = xh[m2];
												y2 = yh[m2];
												break;
											case 2: // Line between vertices 2 and 3
												x1 = xh[m2];
												y1 = yh[m2];
												x2 = xh[m3];
												y2 = yh[m3];
												break;
											case 3: // Line between vertices 3 and 1
												x1 = xh[m3];
												y1 = yh[m3];
												x2 = xh[m1];
												y2 = yh[m1];
												break;
											case 4: // Line between vertex 1 and side 2-3
												x1 = xh[m1];
												y1 = yh[m1];
												x2 = xsect(m2, m3, h, xh);
												y2 = ysect(m2, m3, h, yh);
												break;
											case 5: // Line between vertex 2 and side 3-1
												x1 = xh[m2];
												y1 = yh[m2];
												x2 = xsect(m3, m1, h, xh);
												y2 = ysect(m3, m1, h, yh);
												break;
											case 6: //  Line between vertex 3 and side 1-2
												x1 = xh[m3];
												y1 = yh[m3];
												x2 = xsect(m1, m2, h, xh);
												y2 = ysect(m1, m2, h, yh);
												break;
											case 7: // Line between sides 1-2 and 2-3
												x1 = xsect(m1, m2, h, xh);
												y1 = ysect(m1, m2, h, yh);
												x2 = xsect(m2, m3, h, xh);
												y2 = ysect(m2, m3, h, yh);
												break;
											case 8: // Line between sides 2-3 and 3-1
												x1 = xsect(m2, m3, h, xh);
												y1 = ysect(m2, m3, h, yh);
												x2 = xsect(m3, m1, h, xh);
												y2 = ysect(m3, m1, h, yh);
												break;
											case 9: // Line between sides 3-1 and 1-2
												x1 = xsect(m3, m1, h, xh);
												y1 = ysect(m3, m1, h, yh);
												x2 = xsect(m1, m2, h, xh);
												y2 = ysect(m1, m2, h, yh);
												break;
											default:
												break;
										}

										// Put your processing code here and comment out the printf
										//printf("%f %f %f %f %f\n",x1,y1,x2,y2,z[k]);

										Curve temp = new Curve();
										temp.Add(new PointD(x1, y1));
										temp.Add(new PointD(x2, y2));
										temp.Tag = contours[k];

										contourCollection.Add(temp);
									}
								}
							}
						}
					}
				}
			}
		}

		private static double xsect(int p1, int p2, double[] h, double[] xh)
		{
			return (h[p2] * xh[p1] - h[p1] * xh[p2]) / (h[p2] - h[p1]);
		}

		private static double ysect(int p1, int p2, double[] h, double[] yh)
		{
			return (h[p2] * yh[p1] - h[p1] * yh[p2]) / (h[p2] - h[p1]);
		}

		//private static void ProcessRectangle(double x,
		//    double y,
		//    double xInc,
		//    double yInc,
		//    double[][] vals,
		//    double level,
		//    ref ArrayList lines)
		//{
		//    if (vals.Length != 4) 
		//        throw new Exception("Not the correct amount of values in vals");

		//    // TODO: process this square
		//    double center = (vals[0][0] + vals[1][0] + vals[0][1] + vals[1][1]) / 4.0;
		//    double centerX = x + (xInc / 2.0);
		//    double centerY = y + (yInc / 2.0);
		//    double[] valsInOrder = new double[4];
		//    valsInOrder[0] = vals[0][0];
		//    valsInOrder[1] = vals[1][0];
		//    valsInOrder[2] = vals[1][1];
		//    valsInOrder[3] = vals[0][1];

		//    // if all the points are either over or under the level, return no lines
		//    if (vals[0][0] <= level && 
		//        vals[0][1] <= level && 
		//        vals[1][0] <= level && 
		//        vals[1][1] <= level)
		//        return;
		//    if (vals[0][0] >= level &&
		//        vals[0][1] >= level &&
		//        vals[1][0] >= level &&
		//        vals[1][1] >= level)
		//        return;

		//    bool centerOnLevel = center == level;

		//    double halfX = xInc/2.0;
		//    double halfY = yInc/2.0;
		//    // bottom triangle
		//    ProcessTriangle(
		//        x, y, vals[0][0],
		//        x + xInc, y, vals[1][0],
		//        x + halfX, y + halfY, center, 
		//        level, lines);
		//    // right triangle
		//    ProcessTriangle(
		//        x + xInc, y, vals[1][0],
		//        x + xInc, y + yInc, vals[1][1],
		//        x + halfX, y + halfY, center,
		//        level, lines);
		//    // top triangle
		//    ProcessTriangle(
		//        x + xInc, y + yInc, vals[1][1],
		//        x, y + yInc, vals[0][1],
		//        x + halfX, y + halfY, center,
		//        level, lines);
		//    // left triangle
		//    ProcessTriangle(
		//        x, y + yInc, vals[0][1],
		//        x, y, vals[0][0],
		//        x + halfX, y + halfY, center,
		//        level, lines);
		//}

		//private static void ProcessTriangle(
		//    double p1x, double p1y, double p1,
		//    double p2x, double p2y, double p2,
		//    double p3x, double p3y, double p3,
		//    double level, ref ArrayList lines)
		//{
		//    // TODO: process all the triangles

		//    // if all three points are on one side of the level, no line necessary
		//    if (p1 <= level && p2 <= level && p3 <= level)
		//        return;
		//    if (p1 >= level && p2 >= level && p3 >= level)
		//        return;

		//    // if there are 2 points on, one off
		//    if (p1 == level && p2 == level)
		//    {
		//        lines.Add(new Line(new PointD(p1x, p1y), new PointD(p2x, p2y)));
		//        return;
		//    }
		//    if (p3 == level && p2 == level)
		//    {
		//        lines.Add(new Line(new PointD(p3x, p3y), new PointD(p2x, p2y)));
		//        return;
		//    }
		//    if (p3 == level && p1 == level)
		//    {
		//        lines.Add(new Line(new PointD(p3x, p3y), new PointD(p1x, p1y)));
		//        return;
		//    }

		//    // if one vertex is on
		//    if (p1 == level)
		//    {
		//        lines.Add(new Line(new PointD(p1x, p1y), 
		//    }
		//    if (p2 == level)
		//    {
		//    }
		//    if (p3 == level)
		//    {
		//    }
		//}

	}
}

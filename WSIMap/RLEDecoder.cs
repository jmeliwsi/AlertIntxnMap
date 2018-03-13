using System;
using System.Collections;
using System.Drawing;

namespace WSIMap
{
	public class RLEDecoder
	{

		/// <summary>
		/// Decode the entire RLE image at its native resolution.  Produces a
		/// byte array containing the pixel values, not an RGBA array. The
		/// resulting image is flipped top to bottom.
		/// </summary>
		public static void Decode(int code, int width, int height, byte[] encoded, byte[] decoded)
		{
			int index = width * (height - 1);
			int total = 0, line = 0;

			// Fill decoded output array; flip top to bottom
			try
			{
				for (int i = 0; i < encoded.Length; i += 2)
				{
					byte pixelValue = encoded[i];
					total += encoded[i + 1];

					for (int j = 0; j < encoded[i + 1]; j++)
						decoded[index++] = pixelValue;
					
					if (total % width == 0)
						index = (height - 1 - ++line) * width;
				}
			}
			catch
			{
				// Something went wrong, so just clear the output array
				Array.Clear(decoded, 0, decoded.Length);
			}
		}

		/// <summary>
		/// Decode the entire RLE image at its native resolution.  The resulting
		/// image is flipped top to bottom.
		/// </summary>
		//public static void Decode(int code, int width, int height, byte[] encoded, byte[] decoded, byte alpha)
		//{
		//    int index = width*(height-1)*4;
		//    int total = 0, line = 0;
		//    ByteQuad color;
		//    ByteQuad[] ColorTable;

		//    // Pick the color table based on the image's color code
		//    try
		//    {
		//        ColorTable = (ByteQuad[])ColorTables[(ColorCode)code];
		//        if (ColorTable == null)
		//            ColorTable = ColorTable_GreyIRSatellite;	// default color table
		//    }
		//    catch
		//    {
		//        ColorTable = ColorTable_GreyIRSatellite;		// default color table
		//    }

		//    // Fill decoded output array; flip top to bottom
		//    try
		//    {
		//        for (int i=0; i<encoded.Length; i+=2)
		//        {
		//            color = ColorTable[encoded[i]];
		//            total += encoded[i+1];
		//            for(int j=0; j<encoded[i+1]; j++)
		//            {
		//                decoded[index++] = color.R;
		//                decoded[index++] = color.G;
		//                decoded[index++] = color.B;
		//                decoded[index++] = Math.Min(color.A,alpha);

		//            }
		//            if (total % width == 0)
		//                index = (height - 1 - ++line) * width * 4;
		//        }
		//    }
		//    catch
		//    {
		//        // Something went wrong, so just clear the output array
		//        Array.Clear(decoded,0,decoded.Length);
		//    }
		//}

		/// <summary>
		/// Decode the entire RLE image, but reduce the resolution by converting
		/// 4x4 pixel blocks to a single pixel.  High pixels are preserved.  The
		/// resulting image is flipped top to bottom.
		/// </summary>
		public static void DecodeReduceRes16(int code, int width, byte[] encoded, byte[] decoded, byte alpha, int decodedWidth, int decodedHeight, Color threshold, bool applyToRain, bool applyToMix, bool applyToSnow)
		{
            int index = decodedWidth * (decodedHeight - 1) * 4;
			int col = 0, nRows = 0, decodedRows = 0;
			ColorTables.ByteQuad color;

			// get the specified color table that incorporates specified threshold and alpha values
			ColorTables.ByteQuad[] ColorTable = ColorTables.GetColorTableWithThresholdAndTransparency((ColorTables.ColorCode)code, threshold, alpha, applyToRain, applyToMix, applyToSnow);

            // Iterate the runs of the encoded array
			try
			{
				byte[] max = new byte[(width+3)/4];

                for (int i = 0; i < encoded.Length; i += 2)
				{
					// Iterate the length of the run filling in the max pixel value
                    if (encoded[i] != 0)
                    {
                        int startCol = col;
                        col += encoded[i + 1];
                        for (int k = startCol / 4; k < (col - 1) / 4 + 1; k++)
                            if (encoded[i] > max[k]) max[k] = encoded[i];
                    }
                    else
                        col += encoded[i + 1];  // no need to iterate if value is 0

                    // Finished filling the row
					if (col == width)
					{
                        // Add another row and reset the column counter
						nRows++;
						col = 0;

						// Another four rows done; fill output array; flip top to bottom
                        if (nRows % 4 == 0)
						{
							decodedRows++;
							for (int j=0; j<decodedWidth; j++)
							{
								color = ColorTable[max[j]];
								decoded[index++] = color.R;
								decoded[index++] = color.G;
								decoded[index++] = color.B;
								decoded[index++] = color.A;
							}
							index = (decodedHeight - 1 - decodedRows) * decodedWidth * 4;

                            // Clear the max array
							Array.Clear(max,0,max.Length);
						}
                    }
				}
            }
			catch
			{
				// Something went wrong, so just clear the output array
				Array.Clear(decoded,0,decoded.Length);
			}
        }

		/// <summary>
		/// Decode part of the RLE image at its native resolution.  The resulting
		/// image is flipped top to bottom.
		/// </summary>
		public static bool DecodeReduceSize(int code, int width, int height, byte[] encoded, byte[] decoded, byte alpha, Color threshold, int destWidth, int destHeight, double top, double left, double dy, double dx, BoundingBox box, bool applyToRain, bool applyToMix, bool applyToSnow, out double decodedBottom, out double decodedLeft)
		{
			int index;
			int col = 0, nRows = 0, destRows = 0;
			ColorTables.ByteQuad color;
			decodedBottom = double.NaN;
			decodedLeft = double.NaN;

			if (box.Map.normLeft > box.Map.normRight)
				return false;

			// get the specified color table that incorporates specified threshold and alpha values
			ColorTables.ByteQuad[] ColorTable = ColorTables.GetColorTableWithThresholdAndTransparency((ColorTables.ColorCode)code, threshold, alpha, applyToRain, applyToMix, applyToSnow);

			// Iterate the runs of the encoded array from the top down
			try
			{
				// Calculate the starting and ending rows and columns 
				// to extract from the full image
				int startRow = (int)((box.Map.top - top) / dy) ;
				int endRow = startRow + destHeight - 1;
                int startCol = (int)((box.Map.normLeft - left) / dx);
				int endCol = startCol + destWidth - 1;

				// Bounding box checks
				if (startRow >= height)	// map is south of image 
					return false;
				if (endRow <= 0)		// map is north of image 
					return false;
				if (startCol >= width)	// map is east of image
					return false;
				if (endCol <= 0)		// map is west of image
					return false;

				// Calculate the bottom edge of the reduced size image
				if (startRow < 0)
					decodedBottom = top + (destHeight * dy);
				else
					decodedBottom = top + ((endRow + 1) * dy);

				// Adjust starting and ending rows and columns if
				// they intersect any image edge
				if (startRow < 0) startRow = 0;
				if (startCol < 0) startCol = 0;
				if (endRow >= height) endRow = height;
				if (endCol >= width) endCol = width;

				// Calculate the left edge of the reduced size image
				decodedLeft = left + (startCol * dx);

				// Allocate array for one full image row
				byte[] row = new byte[width];

				// Iterate the encoded image array
				for (int i=0; i<encoded.Length; i+=2)
				{
					// See if row is outside bounding box
					if (nRows < startRow)		// too far north, skip it
					{
						col += encoded[i+1];
						if (col == row.Length)	// finished filling another row
						{
							nRows++;
							col = 0;
						}
						continue;
					}
					else if (nRows == endRow)	// too far south, done
						return true;

					// Iterate the length of this run filling in the pixel value
                    if (encoded[i] != 0)
                    {
                        for (int j = 0; j < encoded[i + 1]; j++)
                            row[col++] = encoded[i];
                    }
                    else
                        col += encoded[i + 1];  // no need to iterate if value is 0

					// Finished filling another row
					if (col == row.Length)
					{
						nRows++;
						col = 0;

						// Calculate index into output array; flips top to bottom
						index = (destHeight - 1 - destRows++) * destWidth * 4;

						// Traverse the row, put a subset of it into the output array
						for (int j=startCol; j<endCol; j++)
						{
							// Decode the pixel value to a color
							color = ColorTable[row[j]];
							decoded[index++] = color.R;
							decoded[index++] = color.G;
							decoded[index++] = color.B;
							decoded[index++] = color.A;
						}

                        // Clear the row array
                        Array.Clear(row, 0, row.Length);
					}
				}
				return true;
			}
			catch
			{
				// Something went wrong, so just clear the output array
				Array.Clear(decoded,0,decoded.Length);
				return false;
			}
		}

		/// <summary>
		/// Decode part of the RLE image at its native resolution / 2.  The resulting
		/// image is flipped top to bottom.
		/// </summary>
		public static bool DecodeReduceSizeRes4(int code, int width, int height, byte[] encoded, byte[] decoded, byte alpha, Color threshold, int destWidth, int destHeight, double top, double left, double dy, double dx, BoundingBox box, bool applyToRain, bool applyToMix, bool applyToSnow, out double decodedBottom, out double decodedLeft)
		{
			int index;
			int col = 0, nRows = 0, destRows = 0;
			ColorTables.ByteQuad color;
			decodedBottom = double.NaN;
			decodedLeft = double.NaN;

			if (box.Map.normLeft > box.Map.normRight)
				return false;

			// get the specified color table that incorporates specified threshold and alpha values
			ColorTables.ByteQuad[] ColorTable = ColorTables.GetColorTableWithThresholdAndTransparency((ColorTables.ColorCode)code, threshold, alpha, applyToRain, applyToMix, applyToSnow);

			// Iterate the runs of the encoded array from the top down
			try
			{
				// Calculate the starting and ending rows and columns 
				// to extract from the full image
				// Always start on even rows and columns to keep decimation consistent
				int startRow = (((int)((box.Map.top - top) / dy)) / 2) * 2;
				int endRow = startRow + destHeight * 2 - 1;
				int startCol = (((int)((box.Map.normLeft - left) / dx)) / 2) * 2;
				int endCol = startCol + destWidth * 2 - 1;

				// Bounding box checks
				if (startRow >= height)	// map is south of image 
					return false;
				if (endRow <= 0)		// map is north of image 
					return false;
				if (startCol >= width)	// map is east of image
					return false;
				if (endCol <= 0)		// map is west of image
					return false;

				// Calculate the bottom edge of the reduced size image
				if (startRow < 0)
					decodedBottom = top + (destHeight * 2.0 * dy);
				else
					decodedBottom = top + ((endRow + 1) * dy);

				// Adjust starting and ending rows and columns if
				// they intersect any image edge
				if (startRow < 0)
					startRow = 0;
				if (startCol < 0)
					startCol = 0;
				if (endRow >= height)
					endRow = height;
				if (endCol >= width)
					endCol = width;

				// Calculate the left edge of the reduced size image
				decodedLeft = left + (startCol * dx);

				// Allocate array for one full image row
				byte[] row = new byte[width];

				// Iterate the encoded image array
				bool bWriteRow = false;
				for (int i = 0; i < encoded.Length; i += 2)
				{
					// See if row is outside bounding box
					if (nRows < startRow)		// too far north, skip it
					{
						col += encoded[i + 1];
						if (col == row.Length)	// finished filling another row
						{
							nRows++;
							col = 0;
						}
						continue;
					}
					else if (nRows == endRow)	// too far south, done
						return true;

					// Iterate the length of this run filling in the pixel value
					if (encoded[i] != 0)
					{
						for (int j = 0; j < encoded[i + 1]; ++j, ++col)
							if (encoded[i] > row[col])
								row[col] = encoded[i];
					}
					else
						col += encoded[i + 1];  // no need to iterate if value is 0

					// Finished filling another row
					if (col == row.Length)
					{
						nRows++;
						col = 0;

						if (bWriteRow)
						{
							// Calculate index into output array; flips top to bottom
							index = (destHeight - 1 - destRows++) * destWidth * 4;

							// Traverse the row, put a subset of it into the output array
							bool bWriteCol = false;
							int colorIndex = 0;
							for (int j = startCol; j < endCol; j++)
							{
								// Decode the pixel value to a color
								if (bWriteCol)
								{
									if (row[j] > colorIndex)
										colorIndex = row[j];
									color = ColorTable[colorIndex];
									decoded[index++] = color.R;
									decoded[index++] = color.G;
									decoded[index++] = color.B;
									decoded[index++] = color.A;
									bWriteCol = false;
								}
								else
								{
									colorIndex = row[j];
									bWriteCol = true;
								}
							}

							// Clear the row array
							Array.Clear(row, 0, row.Length);
							bWriteRow = false;
						}
						else
							bWriteRow = true;
					}
				}
				return true;
			}
			catch
			{
				// Something went wrong, so just clear the output array
				Array.Clear(decoded, 0, decoded.Length);
				return false;
			}
		}
    }
}

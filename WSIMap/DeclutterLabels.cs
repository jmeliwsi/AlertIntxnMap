using System;
using System.Collections;

namespace WSIMap
{
	public class DeclutterLabels
	{
		private struct OpenBrgStruct
		{
			public int size;
			public int start;
			public int end;
		}

		public DeclutterLabels()
		{
		}

		public static Hashtable Declutter(ArrayList InputClutteredLabelPositionsLayer, WSIMap.PointD MapCenterPoint, float MapScaleFactor, MapGL map)
		{
//			PerfTimer ProcessTime= new PerfTimer();
//			ProcessTime.Start();

            if (InputClutteredLabelPositionsLayer == null || InputClutteredLabelPositionsLayer.Count == 0)
                return null;

			Hashtable NewLabelPositions= new Hashtable();	// return label positions
			ArrayList NewLabelRectangle= new ArrayList();	// placed labels to use for next placement.
			
			LatLongStruct Aircraft = new LatLongStruct();
			ArrayList PlanePositions= new ArrayList();
            double LongestDistance;

			try
			{

				// Extract List of Aircraft Positions
				foreach (WSIMap.RectangleD data in InputClutteredLabelPositionsLayer)
				{
					Aircraft.Latitude= data.Bottom;
					Aircraft.Longitude= data.Left;
					Aircraft.Id = data.Id;
					Aircraft.DistanceFromMapCenter= Math.Sqrt(Math.Pow(Aircraft.Latitude - MapCenterPoint.Latitude, 2) + Math.Pow(Aircraft.Longitude - MapCenterPoint.Longitude, 2));
                    Aircraft.Rect = data;
					PlanePositions.Add(Aircraft);
				}
				// Sort aircraft by cloest to center of display first.
				PlanePositions.Sort(new DistanceFromCenterComparer(SortDirection.Ascending));

				double MaxLookRange; // How far out on the Brg to look for the label box to fit.
				int MaxLookRangeLoopCnt= 0;
				bool LabelboxPlaced= false;
				int AircraftNumber= -1;
				double BearingToPlane= 0;
				bool [] BearingListForSingle= new bool [360];

				// For each plane, find "open" bearings around it and fit the label rectangle between the "open" bearing lines.
				foreach (LatLongStruct AircraftPosition in PlanePositions)
				{
                    // Transform one RectangleD format to my RectangleStruct to get the rectangular label box size.
                    RectangleStruct Rectan = new RectangleStruct();
                    Rectan.PointA.Latitude = AircraftPosition.Rect.Top;
                    Rectan.PointA.Longitude = AircraftPosition.Rect.Left;
                    Rectan.PointB.Latitude = AircraftPosition.Rect.Top;
                    Rectan.PointB.Longitude = AircraftPosition.Rect.Right;
                    Rectan.PointC.Latitude = AircraftPosition.Rect.Bottom;
                    Rectan.PointC.Longitude = AircraftPosition.Rect.Right;
                    Rectan.PointD.Latitude = AircraftPosition.Rect.Bottom;
                    Rectan.PointD.Longitude = AircraftPosition.Rect.Left;

                    // Find label rectangle diagnol
                    double RectDiagnol = Math.Sqrt(Math.Pow(Rectan.PointA.Latitude - Rectan.PointC.Latitude, 2) + Math.Pow(Rectan.PointA.Longitude - Rectan.PointC.Longitude, 2));
                    double RectHeight = Math.Abs(Rectan.PointA.Latitude - Rectan.PointD.Latitude);
                    double RectWidth = Math.Abs(Rectan.PointC.Longitude - Rectan.PointD.Longitude);

                    MaxLookRangeLoopCnt = 1;
					LabelboxPlaced= false;
					// try placing the label box a maximum range from the plane at 3 different lengths
					while ( (LabelboxPlaced == false) && (MaxLookRangeLoopCnt <= 3) )
					{
						MaxLookRange= RectDiagnol * 1.5 * MaxLookRangeLoopCnt;
						AircraftNumber++;
						bool [] BearingList= new bool [360];

                        // FOR TESTING ONLY. SELECT OPEN BEARINGS.
                        //for (var i = 0; i < 360; i++)
                        //{
                        //    BearingList[i] = true;
                        //}
                        //for (var i = 90; i < 135; i++)
                        //{
                        //    BearingList[i] = false;
                        //}

                        // include brgs to other aircraft positions
                        foreach (LatLongStruct plane in PlanePositions)
						{
							if ((plane.Latitude != AircraftPosition.Latitude) || (plane.Longitude != AircraftPosition.Longitude) )	// Don't include own aircraft.
							{
								if (Math.Sqrt(Math.Pow(AircraftPosition.Latitude - plane.Latitude, 2) + Math.Pow(AircraftPosition.Longitude - plane.Longitude, 2)) < MaxLookRange)
								{
									BearingToPlane= CalculateBearing(AircraftPosition, plane);

									if (BearingToPlane > 359.499)
										BearingToPlane= 359;
									BearingList[Convert.ToInt16(BearingToPlane)]= true;
                                    PadBearingsToPlane(ref BearingList, Convert.ToInt16(BearingToPlane), 5);
								}
							}
						}

						// include any label boxes already placed by this Declutter algorithm
						foreach (RectangleStruct Rect in NewLabelRectangle)
						{
                            if (MathFunctions.FindDistanceToLabelBox(AircraftPosition, Rect, false) < MaxLookRange)
							{
                                // for this already placed label, find "closed" bearings from this aircraft.
                                BearingListForSingle = FindBlockedBearings(AircraftPosition, Rect.PointA, Rect.PointB, Rect.PointC, Rect.PointD);

                                // Use this when debugging to see results of FindBlockedBearings call
                                //var bearingsToRect = BearingListForSingle.Select((x, i) => new { v = x, i }).Where(x => x.v).Select(x => x.i).ToArray();

                                FillBearingGaps(ref BearingListForSingle);
								MergeBrgLists(ref BearingList, BearingListForSingle);

							}
						}

						// Get "open","free" barings from this aircraft.
						ArrayList OpenBrgs= FindOpenBearings(BearingList);
						int BrgSize= -1;
						int startbrg;
						int endbrg;
						// find bearing, from aircraft, to center of label's new position
						int PositionBrg= FindPositionBrg(OpenBrgs, out BrgSize, out startbrg, out endbrg);
                       
						if (PositionBrg > -1)
						{
							WSIMap.RectangleD NewPoint = new WSIMap.RectangleD();
							RectangleStruct Rectangle= new RectangleStruct();
							// Calculate new Label box position.
							if (BrgSize < 90)
							{
                                Rectangle = MathFunctions.FindNewRectanglePosition(AircraftPosition, startbrg, endbrg, RectWidth, RectHeight, MaxLookRange, MapScaleFactor, out LongestDistance);
							}
							else // BrgSize >= 90
							{
								int MaxThreshold= 70;
								int BrgThreshold= BrgSize / 2;
								if (BrgThreshold > MaxThreshold)
									BrgThreshold= MaxThreshold;

								startbrg = PositionBrg - BrgThreshold;
								if (startbrg < 0)
									startbrg= startbrg + 360;
								endbrg= PositionBrg + BrgThreshold;
								if (endbrg >= 360)
									endbrg= endbrg - 360;

                                Rectangle = MathFunctions.FindNewRectanglePosition(AircraftPosition, startbrg, endbrg, RectWidth, RectHeight, MaxLookRange, MapScaleFactor, out LongestDistance);
                            }

                            // check whether the new label placement is within range
                            if (LongestDistance <= MaxLookRange)
							{
                                LabelboxPlaced = true;

                                // Include Label box to be returned (drawn on map)
                                NewPoint.Top = Rectangle.PointB.Latitude;
                                NewPoint.Right = Rectangle.PointB.Longitude;
                                NewPoint.Bottom = Rectangle.PointD.Latitude;
                                NewPoint.Left= Rectangle.PointD.Longitude;

                                // Draw Valid Bearing lines. In Win coordinates Y is turned down, co I'm gonna rotate bearing lines to make 'em rendered properly
                                /*startbrg = Rotate90(startbrg);
                                endbrg = Rotate90(endbrg);
                                var initialBrg = PositionBrg;
                                PositionBrg = Rotate90(PositionBrg);

                                double lat2, lon2;
                                GetEndLinePoint(AircraftPosition, MaxLookRange, startbrg, out lat2, out lon2);
                                NewPoint.Line1 = new WSIMap.Line(new WSIMap.PointD(AircraftPosition.Longitude, AircraftPosition.Latitude), new WSIMap.PointD(lon2, lat2), System.Drawing.Color.Orange, 1);
                                GetEndLinePoint(AircraftPosition, MaxLookRange, endbrg, out lat2, out lon2);
                                NewPoint.Line2 = new WSIMap.Line(new WSIMap.PointD(AircraftPosition.Longitude, AircraftPosition.Latitude), new WSIMap.PointD(lon2, lat2), System.Drawing.Color.Orange, 1);
                                GetEndLinePoint(AircraftPosition, MaxLookRange, PositionBrg, out lat2, out lon2);
                                NewPoint.Line3 = new WSIMap.Line(new WSIMap.PointD(AircraftPosition.Longitude, AircraftPosition.Latitude), new WSIMap.PointD(lon2, lat2), System.Drawing.Color.Orange, 1);*/

                                NewLabelPositions.Add(AircraftPosition.Id, NewPoint);   // label position returned for drawing.

                                // Add label rectangle for decluttering next plane's label.
                                NewLabelRectangle.Add(Rectangle);

                            } //  end-if LongestDistance <= MaxLookRange
							else
							{
								//							if ( MaxLookRangeLoopCnt == 3)
								//								Console.WriteLine("Not including Label for plane " + AircraftNumber + ". Label placement is beyond max range.");
							}
						} // end-if PositionBrg is not defined (because brg range was not big enough)
						else
						{
							//						Console.WriteLine("Not including Label for plane " + AircraftNumber + ". Bearing swath too small.");
							break;	// out of range while loop
						}

						MaxLookRangeLoopCnt++;
					} // end-while all 3 outLook Ranges tried

				} // end-foreach plane

				//			ProcessTime.Stop();
				//			Console.WriteLine(InputClutteredLabelPositionsLayer.Count + " planes. Total process time " + ProcessTime.Duration);

			}
			catch //(Exception ex)
			{
			}

            return NewLabelPositions;
		} // End Declutter;

        private static int Rotate90(int bearing)
        {
            return bearing + 90 > 360 ? bearing + 90 - 360 : bearing + 90;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Disable bearings toward plane position to prevent Label from overlapping plane symbol.
        /// </summary>
        public static void PadBearingsToPlane(ref bool[] BearingList, int BrgToPlane, int size)
        {
            int Brg;
            for (int i = 1; i <= size; i++)
            {
                Brg = BrgToPlane + i;
                if (Brg >= 360)
                    Brg = Brg - 360;
                BearingList[Brg] = true;

                Brg = BrgToPlane - i;
                if (Brg < 0)
                    Brg = Brg + 360;
                BearingList[Brg] = true;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Fill in brg gaps between brgs to 4 rectangle points.
        /// </summary>
        public static void FillBearingGaps(ref bool[] BearingList)
		{
			int one= -1;
			int two= -1;
			int three= -1;
			int four= -1;
			for (int i=0; i<360; i++)
			{
				if (BearingList[i] == true)
				{
					if (one == -1)
						one= i;
					else
					{
						if (two == -1)
							two= i;
						else
						{
							if (three == -1)
								three= i;
						}
					}
					four= i;
//					Console.WriteLine("Brg " + i + " is TRUE/blocked.");
				}
			}
			if (four - one > 180)
			{
				for (int i=four+1; i<360; i++)
					BearingList[i]= true;
				for (int i=0; i<one; i++)
					BearingList[i]= true;
				if (two - one > 180)
				{
					for (int i=two+1; i<four; i++)
						BearingList[i]= true;
				}
				else
				{
					for (int i=one+1; i<two; i++)
						BearingList[i]= true;
					if (three - two > 180)
					{
						for (int i=three+1; i<four; i++)
							BearingList[i]= true;
					}
					else
					{
						for (int i=two+1; i<three; i++)
							BearingList[i]= true;
					}
				}
			}
			else
			{
				for (int i=one+1; i<four; i++)
					BearingList[i]= true;
			}

		} // End FillBearingGaps

		// ---------------------------------------------------------------------
		public static void MergeBrgLists(ref bool [] MasterBearingList, bool [] BearingListForSingle)
		{
			for (int i=0; i<360; i++)
			{
				if (BearingListForSingle[i] == true)
					MasterBearingList[i]= true;
			}

		}

		// ---------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="BearingList"></param>
		/// <returns></returns>
		public static ArrayList FindOpenBearings(bool [] BearingList)
		{
			ArrayList OpenBrgsList= new ArrayList();
			OpenBrgStruct OpenBrg= new OpenBrgStruct();
			OpenBrgStruct OpenZeroBrg= new OpenBrgStruct();
			int i= 0;

			while (i < 360)
			{
				while ((i < 360) && (BearingList[i] == true))
					i++;
				if (i < 360)
				{
					OpenBrg.start= i;
					while ((i < 360) && (BearingList[i] == false))
						i++;

					OpenBrg.end= i-1;
					OpenBrg.size= OpenBrg.end - OpenBrg.start + 1;

					if (OpenBrg.start == 0)
						OpenZeroBrg= OpenBrg;

					if ((OpenBrg.size < 360) && (OpenBrg.end == 359) && (OpenZeroBrg.end != 0))
					{
						OpenBrg.end= OpenZeroBrg.end;
						OpenBrg.size= OpenZeroBrg.size + OpenBrg.size;
						OpenBrgsList.Remove(OpenZeroBrg);
					}
					OpenBrgsList.Add(OpenBrg);
				}
			}

			return OpenBrgsList;
		} // End FindOpenBearings

		// ---------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="AircraftPosition"></param>
		/// <param name="RectanglePoint_A"></param>
		/// <param name="RectanglePoint_B"></param>
		/// <param name="RectanglePoint_C"></param>
		/// <param name="RectanglePoint_D"></param>
		/// <returns></returns>
		public static bool[] FindBlockedBearings(LatLongStruct AircraftPosition, LatLongStruct RectanglePoint_A, LatLongStruct RectanglePoint_B, LatLongStruct RectanglePoint_C, LatLongStruct RectanglePoint_D)
		{
			bool [] BearingList= new bool [360];
			double bearing= 0;

			bearing= CalculateBearing(AircraftPosition, RectanglePoint_A);
			if (bearing > 359.4999)
				bearing= 0;
			BearingList[Convert.ToInt16(bearing)]= true;

			bearing= CalculateBearing(AircraftPosition, RectanglePoint_B);
			if (bearing > 359.4999)
				bearing= 0;
			BearingList[Convert.ToInt16(bearing)]= true;

			bearing= CalculateBearing(AircraftPosition, RectanglePoint_C);
			if (bearing > 359.4999)
				bearing= 0;
			BearingList[Convert.ToInt16(bearing)]= true;

			bearing= CalculateBearing(AircraftPosition, RectanglePoint_D);
			if (bearing > 359.4999)
				bearing= 0;
			BearingList[Convert.ToInt16(bearing)]= true;

			return BearingList;

		} // End FindBlockedBearings

		// -------------------------------------------------------------------
		public static int CalculateBearing(LatLongStruct PointA, LatLongStruct PointB)
		{
			int Brg=0;
			double x=0, y=0, BrgDec=0;

			x= PointB.Longitude - PointA.Longitude;
			y= PointB.Latitude - PointA.Latitude;
            BrgDec = MathFunctions.RadiansToDegrees(Math.Atan(Math.Abs(y) / Math.Abs(x)));
            if (BrgDec > 359.4999)
				BrgDec= 0;
			Brg= Convert.ToInt16(BrgDec);

            /*if ((x >= 0) && (y >= 0))	// Quadrant 1
				Brg= 90 - Brg;
			if ((x >= 0) && (y < 0))	// Quadrant 2
				Brg= 90 + Math.Abs(Brg);
			if ((x < 0) && (y < 0))	// Quadrant 3
				Brg= 270 - Brg;
			if ((x < 0) && (y >= 0))	// Quadrant 4
				Brg= 270 + Math.Abs(Brg);*/

            // Using win coords where Y is inverted; cartesian-like quadrants (from top right anticlockwise)
            /*if ((x >= 0) && (y <= 0))   // Quadrant 1
                Brg = Brg;
            if ((x <= 0) && (y <= 0))    // Quadrant 2
                Brg = 180 - Brg;
            if ((x <= 0) && (y >= 0)) // Quadrant 3
                Brg = 180 + Brg;
            if ((x >= 0) && (y >= 0))    // Quadrant 4
                Brg = 360 - Brg;*/

            if ((x >= 0) && (y <= 0))   // Quadrant 1
                return Brg;
            if ((x <= 0) && (y <= 0))    // Quadrant 2
                return 180 - Brg;
            if ((x <= 0) && (y > 0)) // Quadrant 3
                return 180 + Brg;
            if ((x >= 0) && (y > 0))    // Quadrant 4
                return 360 - Brg;

            return Brg;
		}
		// -------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="AircraftPosition"></param>
		/// <param name="RectanglePoint_A"></param>
		/// <param name="RectanglePoint_B"></param>
		/// <param name="RectanglePoint_C"></param>
		/// <param name="RectanglePoint_D"></param>
		/// <returns></returns>
//		public static bool[] FindBlockedBearings_ByIntersectSegmentsAtAllBrgs(LatLongStruct AircraftPosition, LatLongStruct RectanglePoint_A, LatLongStruct RectanglePoint_B, LatLongStruct RectanglePoint_C, LatLongStruct RectanglePoint_D)
//		{
//			bool [] BearingList= new bool [360];
//			LatLongStruct BrgPoint= new LatLongStruct();
//			LatLongStruct IntersectionPoint;
//			int increment= 1;
//			for (int deg=0; deg<360; deg=deg+increment)
//			{
//				MathFunctions.RangeBearingToLatLon(AircraftPosition.Latitude, AircraftPosition.Longitude, 500, deg, ref BrgPoint.Latitude, ref BrgPoint.Longitude);
//				if (MathFunctions.LineSegmentsIntersect(AircraftPosition, BrgPoint, RectanglePoint_A, RectanglePoint_B, false, out IntersectionPoint) == true)
//					BearingList[deg]= true;
//				if (MathFunctions.LineSegmentsIntersect(AircraftPosition, BrgPoint, RectanglePoint_B, RectanglePoint_C, false, out IntersectionPoint) == true)
//					BearingList[deg]= true;
//				if (MathFunctions.LineSegmentsIntersect(AircraftPosition, BrgPoint, RectanglePoint_C, RectanglePoint_D, false, out IntersectionPoint) == true)
//					BearingList[deg]= true;
//				if (MathFunctions.LineSegmentsIntersect(AircraftPosition, BrgPoint, RectanglePoint_D, RectanglePoint_A, false, out IntersectionPoint) == true)
//					BearingList[deg]= true;
//			}
//
//			return BearingList;
//
//		} // End FindBlockedBearings_ByIntersectSegmentsAtAllBrgs

		// ----------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// <param name="OpenBrgsList"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static int FindPositionBrg(ArrayList OpenBrgsList, out int size, out int start, out int end)
		{
			int SmallestAcceptableBrearingRange= 6;
			int PositionBrg= -1;
			int LargestFreeSize= 0;
			start= -1;
			end= -1;
			size= -1;

			// Find largest consecutive free brgs
			foreach (OpenBrgStruct Brgs in OpenBrgsList)
			{
				if (Brgs.size > LargestFreeSize)
				{
					start= Brgs.start;
					end= Brgs.end;
					size= Brgs.size;
					LargestFreeSize= size;
				}
			}

			if (size > SmallestAcceptableBrearingRange)
			{
				// split start/end
				PositionBrg= start + (size/2);
				if (PositionBrg > 359)
					PositionBrg= PositionBrg - 360;
			}

//			Console.WriteLine(start + "-" + end + "  Brg for label placement is " + PositionBrg);

			return PositionBrg;
		}

		// -----------------------------------------------------------------------
		public static void GetEndLinePoint(LatLongStruct Point, double range, int brg, out double lat, out double lon)
		{
			lat=0;
			lon=0;

			if ((brg >= 0) && (brg <= 90))
			{
				lat= Point.Latitude + range * Math.Sin(MathFunctions.DegreesToRadians(90 - brg));
				lon = Point.Longitude + range * Math.Cos(MathFunctions.DegreesToRadians(90 - brg));
			}
			if ((brg > 90) && (brg <= 180))
			{
				lat= Point.Latitude - range * Math.Sin(MathFunctions.DegreesToRadians(brg - 90));
				lon= Point.Longitude + range * Math.Cos(MathFunctions.DegreesToRadians(brg - 90));
			}
			if ((brg > 180) && (brg <= 270))
			{
				lat= Point.Latitude - range * -Math.Cos(MathFunctions.DegreesToRadians(brg));
				lon= Point.Longitude - range * -Math.Sin(MathFunctions.DegreesToRadians(brg));
			}
			if ((brg > 270) && (brg < 360))
			{
				lat= Point.Latitude + range * Math.Sin(MathFunctions.DegreesToRadians(brg + 90));
				lon= Point.Longitude - range * Math.Cos(MathFunctions.DegreesToRadians(brg + 90));
			}

		} // End GetEndLinePoint

		// ----------------------------------------------------------------------

		internal enum SortDirection 
		{
			Ascending,
			Descending
		}

		/// <summary>
		/// Sort PositionStruct by TimeStamp
		/// </summary>
		internal class DistanceFromCenterComparer : IComparer 
		{

			private SortDirection m_direction = SortDirection.Ascending;

			public DistanceFromCenterComparer() : base() { }

			public DistanceFromCenterComparer(SortDirection direction) 
			{
				this.m_direction = direction;
			}

			int IComparer.Compare(object x, object y) 
			{

				LatLongStruct PointX = (LatLongStruct) x;
				LatLongStruct PointY = (LatLongStruct) y;

				return (this.m_direction == SortDirection.Ascending) ?
					PointX.DistanceFromMapCenter.CompareTo(PointY.DistanceFromMapCenter) : 
					PointY.DistanceFromMapCenter.CompareTo(PointX.DistanceFromMapCenter);
			}
		} // End class

	} // End class
} // End namespace

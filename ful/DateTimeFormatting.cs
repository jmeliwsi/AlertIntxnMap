using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FUL
{
    public class DateTimeFormatting
    {
        // --------------------------------------------------------------------------------
        /// <summary>
        /// Convert SWINDS time format to DateTime type.
        /// Example input: 20100515T220100Z
        /// </summary>
        /// <param name="DateTimeString"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTimeFormat1(string DateTimeString)
        {
            DateTime theDateTime = new DateTime(1800, 1, 1);
			if (string.IsNullOrWhiteSpace(DateTimeString))
				return theDateTime;

            try
            {
                int Year = Convert.ToInt32(DateTimeString.Substring(0, 4));
                int Month = Convert.ToInt32(DateTimeString.Substring(4, 2));
                int Day = Convert.ToInt32(DateTimeString.Substring(6, 2));
                int Hour = Convert.ToInt32(DateTimeString.Substring(9, 2));
                int Minute = Convert.ToInt32(DateTimeString.Substring(11, 2));
                int Second = Convert.ToInt32(DateTimeString.Substring(13, 2));
                theDateTime = new DateTime(Year, Month, Day, Hour, Minute, Second, 0);
            }
            catch (Exception ex)
            {
                FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "Failed to parse DateTime1 value: " + DateTimeString + "\r\n" + ex);
            }

            return theDateTime;

        } // ConvertDateTimeFormat1

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Convert swinds gml time format to standard DateTime
        /// Example input: 09Apr10 12:00Z
        /// </summary>
        /// <param name="DateTimeString"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTimeFormat2(string DateTimeString)
        {
            DateTime ResultTime = DateTime.MinValue;
            try
            {
                int Day = Int32.Parse(DateTimeString.Substring(0, 2));
                int Year = 2000 + Int32.Parse(DateTimeString.Substring(DateTimeString.Length - 9, 2));
                int Hour = Int32.Parse(DateTimeString.Substring(DateTimeString.Length - 6, 2));
                int Minute = Int32.Parse(DateTimeString.Substring(DateTimeString.Length - 3, 2));
                int Month = 0;
                switch (DateTimeString.Substring(2, 3).ToLower())
                {
                    case "jan": Month = 1; break;
                    case "feb": Month = 2; break;
                    case "mar": Month = 3; break;
                    case "apr": Month = 4; break;
                    case "may": Month = 5; break;
                    case "jun": Month = 6; break;
                    case "jul": Month = 7; break;
                    case "aug": Month = 8; break;
                    case "sep": Month = 9; break;
                    case "oct": Month = 10; break;
                    case "nov": Month = 11; break;
                    case "dec": Month = 12; break;
                }

                ResultTime = new DateTime(Year, Month, Day, Hour, Minute, 0);
            }
            catch (Exception ex)
            {
                FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "Failed to parse DateTime2 value: " + DateTimeString + "\r\n" + ex);
            }

            return ResultTime;

        } // End ConvertDateTimeFormat2

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Convert 17MAY2013 to 20130517
        /// </summary>
        /// <param name="InputDate"></param>
        /// <returns></returns>
        public static string ConvertDateFormat1(string InputDate)
        {
            string Day = InputDate.Substring(0,2);
            string Month = string.Empty;
            string Year = InputDate.Substring(5, 4);

            switch (InputDate.Substring(2, 3).ToLower())
            {
                case "jan": Month = "01"; break;
                case "feb": Month = "02"; break;
                case "mar": Month = "03"; break;
                case "apr": Month = "04"; break;
                case "may": Month = "05"; break;
                case "jun": Month = "06"; break;
                case "jul": Month = "07"; break;
                case "aug": Month = "08"; break;
                case "sep": Month = "09"; break;
                case "oct": Month = "10"; break;
                case "nov": Month = "11"; break;
                case "dec": Month = "12"; break;
            }

            return string.Concat(Year, Month, Day);
        }
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Convert swinds gml time format to standard DateTime
		/// Example input: 2010-05-22 06:00:00+00 and 2011-02-15T04:45:00Z
        /// </summary>
        /// <param name="DateTimeString"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTimeFormat3(string DateTimeString)
        {
            DateTime theDateTime = new DateTime(1800, 1, 1);
            try
            {
                int Year = Convert.ToInt32(DateTimeString.Substring(0, 4));
                int Month = Convert.ToInt32(DateTimeString.Substring(5, 2));
                int Day = Convert.ToInt32(DateTimeString.Substring(8, 2));
                int Hour = Convert.ToInt32(DateTimeString.Substring(11, 2));
                int Minute = Convert.ToInt32(DateTimeString.Substring(14, 2));
                int Second = Convert.ToInt32(DateTimeString.Substring(17, 2));
                theDateTime = new DateTime(Year, Month, Day, Hour, Minute, Second, 0);
            }
            catch (Exception ex)
            {
                FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "Failed to parse DateTime3 value: " + DateTimeString + "\r\n" + ex);
            }

            return theDateTime;

        } // End ConvertDateTimeFormat3

		
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Convert swinds Product time format to standard DateTime
		/// Example input: 2010-10-15 16:52:39 UTC
        /// </summary>
        /// <param name="DateTimeString"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTimeFormatDashColon(string DateTimeString)
        {
            DateTime theDateTime = DateTime.MinValue;
            try
            {
				string[] DateandTime;
				DateandTime = DateTimeString.Split(' ');
				string[] Date;
				Date = DateandTime[0].Split('-');
				string[] Time;
				Time = DateandTime[1].Split(':');

				theDateTime = new DateTime(Convert.ToInt32(Date[0]), Convert.ToInt32(Date[1]), Convert.ToInt32(Date[2]), Convert.ToInt32(Time[0]), Convert.ToInt32(Time[1]), Convert.ToInt32(Time[2]));
			}
            catch (Exception ex)
            {
				FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "Failed to parse ConvertDateTimeFormatDashColon value: " + DateTimeString + "\r\n" + ex);
            }

            return theDateTime;

		} // End ConvertDateTimeFormatDashColon

		// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Convert swinds Product time format to standard DateTime
		/// Example input: 1010152100
		/// </summary>
		/// <param name="DateTimeString"></param>
		/// <returns></returns>
		public static DateTime ConvertDateTimeFormat10Digit(string DateTimeString)
		{
			DateTime theDateTime = DateTime.MinValue;
			try
			{
				int Year = 2000 + Convert.ToInt32(DateTimeString.Substring(0, 2));
				int Month = Convert.ToInt32(DateTimeString.Substring(2, 2));
				int Day = Convert.ToInt32(DateTimeString.Substring(4, 2));
				int Hour = Convert.ToInt32(DateTimeString.Substring(6, 2));
				int Minute = Convert.ToInt32(DateTimeString.Substring(8, 2));
				theDateTime = new DateTime(Year, Month, Day, Hour, Minute, 0, 0);
			}
			catch (Exception ex)
			{
				FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "Failed to parse ConvertDateTimeFormat10Digit value: " + DateTimeString + "\r\n" + ex);
			}

			return theDateTime;

		} // End ConvertDateTimeFormat10Digit

		public static string ConvertDateTimeFormat4(string inputDateTime)
		{
			// Convert date/time string in "yyMMddHHmmss" format to "yyyy-MM-ddTHH:mm:ssZ" format
			string outputDateTime = string.Empty;
			DateTime dt = DateTime.MinValue;
			bool parsed = DateTime.TryParseExact(inputDateTime, "yyMMddHHmmss", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out dt);
			if (parsed) outputDateTime = dt.ToString("yyyy-MM-ddTHH:mm:ssZ");
			return outputDateTime;
		}

		public static string FormatDateOnly(string inputDate)
		{
			// Convert date string in "yyMMdd" format to "yyyy-MM-dd" format
			string outputDate = "20" + inputDate;
			outputDate = outputDate.Insert(4, "-");
			outputDate = outputDate.Insert(7, "-");
			return outputDate;
		}

		public static string ConvertHoursMinutesToStringDate(DateTime ReferenceTime, string HHMM, bool HourTimeMustBeGreaterThanReference)
		{
			string DateTimeString = string.Empty;

			if (HHMM.Length == 4)
			{
				DateTime TempTime = FillDateTime(ReferenceTime, int.Parse(HHMM.Substring(0, 2)), int.Parse(HHMM.Substring(2, 2)), 0, HourTimeMustBeGreaterThanReference);
				string TempTimeString = TempTime.ToString("yyyy_MM_dd_HH_mm_ss").Replace("_", "");
				DateTimeString = ConvertDateTimeFormat4(TempTimeString.Substring(2, TempTimeString.Length - 2));
			}
			return DateTimeString;
		}

		public static string ConvertDayHoursMinutesToStringDate(DateTime ReferenceTime, string DDHHMMSS)
		{
			//string DateTimeString = string.Empty;
			DateTime TempTime = DateTime.MinValue;

			if (DDHHMMSS.Length == 8)
			{
				TempTime = FUL.DateTimeFormatting.FillDateTime(ReferenceTime, int.Parse(DDHHMMSS.Substring(0, 2)), int.Parse(DDHHMMSS.Substring(2, 2)), int.Parse(DDHHMMSS.Substring(4, 2)), int.Parse(DDHHMMSS.Substring(6, 2)));
				//string TempTimeString = TempTime.ToString("yyyy_MM_dd_HH_mm_ss").Replace("_", "");
				//DateTimeString = ConvertDateTimeFormat4(TempTimeString.Substring(2, TempTimeString.Length - 2));
			}
			return TempTime.ToString();
		}

        // --------------------------------------------------------------------------
        /// <summary>
        /// returns a date/time string, of current time, in format yyyy_MM_dd_hh_mm_ss
        /// </summary>
        /// <returns></returns>
        public static string TimeNowString()
        {
            DateTime TimeNow = DateTime.UtcNow;
            return TimeNow.ToString("yyyy_MM_dd_HH_mm_ss");
        }

        // ------------------------------------------------------------------------
        /// <summary>
        /// Determine the correct DateTime given a reference time and Day, Hour, Minute.
        /// </summary>
        /// <param name="ReferenceTime"></param>
        /// <param name="Day"></param>
        /// <param name="Hour"></param>
        /// <param name="Minute"></param>
        /// <param name="Second"></param>
        /// <returns></returns>
        public static System.DateTime FillDateTime(System.DateTime ReferenceTime, int Day, int Hour, int Minute, int Second)
        {
            System.Globalization.GregorianCalendar myCalendar = new System.Globalization.GregorianCalendar();
            System.DateTime CompleteDateTime = new System.DateTime(1, 1, 1);
            int Month = ReferenceTime.Month - 1;
            int cnt = 0;

            try
            {
                if ((ReferenceTime.Year > 0) && (ReferenceTime.Month > 0) && (Day > 0))
                {
                    while ((Day > myCalendar.GetDaysInMonth(ReferenceTime.Year, ReferenceTime.Month)) || (cnt > 30))
                    {
                        ReferenceTime = ReferenceTime.Subtract(TimeSpan.FromDays(1));
                        cnt++;
                    }

                    CompleteDateTime = new System.DateTime(ReferenceTime.Year, ReferenceTime.Month, Day, Hour, Minute, Second);
                    if (Math.Abs(ReferenceTime.Day - Day) > 15)
                    {
                        if (Day > ReferenceTime.Day)
                        {
                            if (Month == 0)
                                Month = 12;
                            CompleteDateTime = CompleteDateTime.Subtract(TimeSpan.FromDays(myCalendar.GetDaysInMonth(ReferenceTime.Year, Month)));
                        }
                        else
                            CompleteDateTime = CompleteDateTime.AddMonths(1);
                    }
                }
                //else
                //Console.WriteLine(" **** Invalid parameters passed to FillDateTime! " + " Year:" + ReferenceTime.Year + " Month:" + ReferenceTime.Month + " Day:" + Day);

            }
            catch //(Exception ex)
            {
                //Console.WriteLine("Exception Caught in FillDateTime (with Day)" + " Reference " + ReferenceTime + " Day " + Day + " Hour " + Hour + " Minute " + Minute);
                //Console.WriteLine(ex);
            }

            return CompleteDateTime;

        } // End FillDateTime with Day

        // ------------------------------------------------------------------------

        /// <summary>
        /// Determine the correct DateTime given a reference time and Hour, Minute.
        /// </summary>
        /// <param name="ReferenceTime"></param>
        /// <param name="Hour"></param>
        /// <param name="Minute"></param>
        /// <param name="Second"></param>
        /// <returns></returns>
        public static System.DateTime FillDateTime(System.DateTime ReferenceTime, int Hour, int Minute, int Second, bool HourTimeMustBeGreaterThanReference)
        {
            System.DateTime CompleteDateTime = ReferenceTime;
            try
            {
                CompleteDateTime = new System.DateTime(ReferenceTime.Year, ReferenceTime.Month, ReferenceTime.Day, Hour, Minute, Second);

                if (Math.Abs(ReferenceTime.Hour - Hour) > 12)
                {
                    if (Hour > ReferenceTime.Hour)
                        CompleteDateTime = CompleteDateTime.Subtract(TimeSpan.FromDays(1));
                    else
                        CompleteDateTime = CompleteDateTime.AddDays(1);
                }

                if ((HourTimeMustBeGreaterThanReference == true) && (CompleteDateTime < ReferenceTime))
                    CompleteDateTime = CompleteDateTime.AddDays(1);
            }
            catch //(Exception ex)
            {
                //Console.WriteLine("Exception Caught in FillDateTime (w/o Day)" + " Reference " + ReferenceTime + " Hour " + Hour + " Minute " + Minute);
                //Console.WriteLine(ex);
            }

            return CompleteDateTime;

        } // End FillDateTime


    } // End class
} // End namespace

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Data.SqlClient;

namespace FUL
{

    public class ChargeID
    {
        int ChargeIDTableSize = 32;
        public Hashtable ChargeIDTable;
        private System.Threading.Timer timer_ReadChargeIDs;

        // constructor
        public ChargeID()
        {
            ChargeIDTable = new Hashtable(ChargeIDTableSize, 1);

            // Start Timer for fetching latest chargeIDs from DB.
            // Once per hour, the chargeID table (Packages) is read to get any new chargeIDs.
            object Params = null;
            TimeSpan Init = new TimeSpan(0, 0, 0);
            TimeSpan Periodic = new TimeSpan(1, 0, 0);
            timer_ReadChargeIDs = new System.Threading.Timer(new System.Threading.TimerCallback(ReadChargeIDsFromDB), Params, Init, Periodic);
        }

        // ------------------------------------------------------------------------------
        /// <summary>
        /// Read the Charge ID strings and associated bit values into a hash table for Customer Authorization.
        /// </summary>
        public void ReadChargeIDsFromDB(object Params)
        {
            string LocalDBconnectionString = FUL.Utils.Get_DB_ConnectionString("fusion_Admin");
			if (FUL.Utils.RecordWeatherData == true) // For Record/Playback
				LocalDBconnectionString = FUL.Utils.Get_DB_ConnectionString(FUL.Utils.Weather_DB_Name);

            try
            {
                using (SqlConnection LocalDBconnection = new SqlConnection(LocalDBconnectionString))
                {
                    SqlCommand ReadChargeIDsCommand = new SqlCommand("dbo.GetPackages", LocalDBconnection);
                    LocalDBconnection.Open();

                    using (SqlDataReader myReaderChargeIDs = ReadChargeIDsCommand.ExecuteReader())
                    {
                        ChargeIDTable.Clear();

                        while (myReaderChargeIDs.Read())
                        {
                            //Console.WriteLine("     " + myReaderChargeIDs["ChargeID"] + "   " + myReaderChargeIDs["Value"]);
                            ChargeIDTable.Add(myReaderChargeIDs["ChargeID"], myReaderChargeIDs["Value"]);
                        }
                    } // end using SqlDataReader
                } // end using SqlConnection
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Exception while loading Charge ID hash table.\r\n  " + ex);
            }

        } // End ReadChargeIDsFromDB

        // ------------------------------------------------------------------------------
        /// <summary>
        /// This method converts a comma separated string of Charge IDs to an integer by "ORing"
        /// the Charge ID associated bits found in the Fusion Admin Package Table.
        /// </summary>
        /// <param name="ChargeIDsString"></param>
        /// <returns></returns>
        public int ConvertChargeIDStringsToInt(string ChargeIDsString)
        {
            int Result = 0;

            string[] ChargeIDs = ChargeIDsString.Split(',');

            for (int i = 0; i < ChargeIDs.Length; i++ )
            {
                if (ChargeIDTable[ChargeIDs[i]] != null)
                    Result = Result | Convert.ToInt32(ChargeIDTable[ChargeIDs[i]]);
            }

            return Result;
        } // End ConvertChargeIDStringsToInt


    } // End class
} // End namespace


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace FUL
{
    public class StoreStartupTimeInDB
    {
        // ----------------------------------------------------------------
        /// <summary>
        /// Write the new startup time to the DB for clients to reset.
        /// (i.e., When DB is cleared of flights, when converting to "Live" mode or clearing AA Flights in DB) 
        /// </summary>
        /// <param name="StoreStartup"></param>
        /// <returns></returns>
        public void StoreStartupTime(string IngestorName)
        {
            string ExecutableName = IngestorName;
            if (ExecutableName.EndsWith("Class2"))
                ExecutableName = ExecutableName.Substring(0, IngestorName.Length - 6);
			else if (ExecutableName.EndsWith("Record"))
				ExecutableName = ExecutableName.Substring(0, IngestorName.Length - 6) + "MonitorControl";
            else if (ExecutableName.EndsWith("Archive"))
                ExecutableName = ExecutableName.Substring(0, IngestorName.Length - 7);
            FileInfo DSfile = new FileInfo(ExecutableName + ".exe");

			using (SqlConnection connection = new SqlConnection(FUL.Utils.Get_DB_ConnectionString(FUL.Utils.IngestHeartBeat_DB_Name)))
            {
                connection.Open();
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.Add_DS_startup";
                    cmd.Parameters.Clear();
                    FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Info, "MAIN", "--- Starting WSI Fusion " + IngestorName + " version " + DSfile.LastWriteTimeUtc + " UTC ---");
                    cmd.Parameters.Add(new SqlParameter("@build_time", DSfile.LastWriteTimeUtc));
                    cmd.Parameters.Add(new SqlParameter("@IngestorName", IngestorName));
                    using (SqlDataReader theReader = cmd.ExecuteReader())
                    {
                    } // end using SqlDataReader
                }
                catch (Exception ex)
                {
                    FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "MAIN", " Exception while Adding Ingestor start time to DataBase.\r\n  " + ex);
                }
            } // end using SqlConnection

        }// End StoreStartupTime

        // ----------------------------------------------------------------
        /// <summary>
        /// Write the new startup time to the DB for clients to reset.
        /// (i.e., When DB is cleared of flights, when converting to "Live" mode or clearing AA Flights in DB) 
        /// </summary>
        /// <param name="IngestorName"></param>
        /// <param name="build_time"></param>
        public void StoreStartupTime(string IngestorName, DateTime build_time)
        {
            using (SqlConnection connection = new SqlConnection(FUL.Utils.Get_DB_ConnectionString(FUL.Utils.IngestHeartBeat_DB_Name)))
            {
                connection.Open();
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.Add_DS_startup";
                    cmd.Parameters.Clear();
                    FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Info, "MAIN", "--- Starting WSI Fusion " + IngestorName + " version " + build_time + " UTC ---");
                    cmd.Parameters.Add(new SqlParameter("@build_time", build_time));
                    cmd.Parameters.Add(new SqlParameter("@IngestorName", IngestorName));
                    using (SqlDataReader theReader = cmd.ExecuteReader())
                    {
                    } // end using SqlDataReader
                }
                catch (Exception ex)
                {
                    FUL.FileWriter.WriteLine(true, FUL.FileWriter.EventType.Error, "MAIN", " Exception while Adding Ingestor start time to DataBase.\r\n  " + ex);
                    throw;
                }
            } // end using SqlConnection

        }// End StoreStartupTime

    } // End class
} // End namespace
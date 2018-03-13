using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Data.SqlServerCe;

namespace WSIMap
{
    public class FeatureCollectionSQLCECache : FeatureCollectionCache
    {
        #region Data Members
        protected List<DateTime> cachedTimes;
        protected string CacheDirectory;
        protected static BinaryFormatter formatter;
        protected static DateTime minTime;
        protected SqlCeCommand cmd = null;
        protected SqlCeConnection con = null;
        #endregion

        public FeatureCollectionSQLCECache(int MinutesToKeepData)
        {
            con = new SqlCeConnection(@"Data Source = FCCache.sdf;");
            this.cachedTimes = new List<DateTime>();
            KeepMinutes = MinutesToKeepData;
            formatter = new BinaryFormatter(); 
            ClearOldFiles();
        }

        public override FeatureCollection GetNear(DateTime t)
        {
            for (int x = cachedTimes.Count - 1; x >= 0; x--)
            {
                if (cachedTimes[x] <= t || x == 0)
                {
                    FeatureCollection fc = GetExact(cachedTimes[x]);
                    if (fc == null) { cachedTimes.RemoveAt(x); x--; continue; }
                    return fc;
                }
            }
            return null;
        }

        public override bool Put(FeatureCollection fc, DateTime sliceTime)
        {
            // if we already have that time, remove old one first
            if (cachedTimes.Contains(sliceTime))
                RemoveExact(sliceTime);

            MemoryStream ms = null;
            try
            {
                // put the feature collection into the DB
                ms = new MemoryStream();
                formatter.Serialize(ms, fc);
                ms.Seek(0, 0);
                string insert = "INSERT INTO Cache (TimeStamp, FeatureCollection) VALUES (@TimeStamp, @FeatureCollection)";
                con.Open();
                cmd = new SqlCeCommand(insert, con);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@TimeStamp", sliceTime);
                byte[] buffer = ms.ToArray();
                cmd.Parameters.AddWithValue("@FeatureCollection", buffer);
                cmd.ExecuteNonQuery();
                con.Close();
                ms.Close();
                cachedTimes.Add(sliceTime);

                ClearOldFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to DB: " + ex.ToString());
                if (ms != null)
                    ms.Close();
                con.Close();
                return false;
            }
            return true;
        }

        public override void ClearOldFiles()
        {
            minTime = DateTime.UtcNow.AddMinutes(-KeepMinutes);

            try
            {
                // Remove old entries from the DB
                string delete = "DELETE FROM Cache WHERE TimeStamp < '" + minTime.ToString() + "'";
                con.Open();
                cmd = new SqlCeCommand(delete, con);
                cmd.ExecuteNonQuery();
                con.Close();

                // Remove old entries from the cached times
                cachedTimes.RemoveAll(Expired);
            }
            catch
            {
                con.Close();
            }
        }

        private static bool Expired(DateTime t)
        {
            if (t < minTime)
                return true;
            else
                return false;
        }

        public override FeatureCollection GetExact(DateTime sliceTime)
        {
            // get the FeatureCollection at sliceTime out of the DB
            MemoryStream ms = null;
            try
            {
                string select = "SELECT FeatureCollection FROM Cache WHERE TimeStamp = '" + sliceTime.ToString() + "'";
                SqlCeCommand cmd = new SqlCeCommand(select, con);
                con.Open();
                SqlCeDataReader rdr = cmd.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                rdr.Read();
                long len = rdr.GetBytes(rdr.GetOrdinal("FeatureCollection"), 0, null, 0, 0);
                byte[] fcBytes = new byte[len];
                rdr.GetBytes(rdr.GetOrdinal("FeatureCollection"), 0, fcBytes, 0, (int)len);
                con.Close();
                ms = new MemoryStream(fcBytes);
                FeatureCollection fc = (FeatureCollection)formatter.Deserialize(ms);
                ms.Close();
                return fc;
            }
            catch
            {
                if (ms != null)
                    ms.Close();
                con.Close();
                return null;
            }
        }

        public override void RemoveExact(DateTime sliceTime)
        {
            try
            {
                // Remove from the DB
                string delete = "DELETE FROM Cache WHERE TimeStamp = '" + sliceTime.ToString() + "'";
                con.Open();
                cmd = new SqlCeCommand(delete, con);
                cmd.ExecuteNonQuery();
                con.Close();

                // Remove from the cached times
                cachedTimes.Remove(sliceTime);
            }
            catch
            {
                con.Close();
            }
        }

        public override void Clean()
        {
            try
            {
                string delete = "DELETE FROM Cache";
                con.Open();
                cmd = new SqlCeCommand(delete, con);
                cmd.ExecuteNonQuery();
                con.Close();
                cachedTimes.Clear();
            }
            catch
            {
                con.Close();
            }
        }

        public FeatureCollection GetNext(DateTime current)
        {
            for (int x = 0; x < cachedTimes.Count; x++)
            {
                if (current < cachedTimes[x]) return GetExact(cachedTimes[x]);
            }
            return GetExact(cachedTimes[0]);
        }

        public FeatureCollection GetNext(DateTime current, DateTime LowerLimit)
        {
            for (int x = 0; x < cachedTimes.Count; x++)
            {
                if (current < cachedTimes[x] && cachedTimes[x] >= LowerLimit) return GetExact(cachedTimes[x]);
            }
            // If not found, loop back around to the LowerLimit, not 0
            current = LowerLimit;
            for (int x = 0; x < cachedTimes.Count; x++)
            {
                if (current < cachedTimes[x]) return GetExact(cachedTimes[x]);
            }
            return GetExact(cachedTimes[0]);
        }

        public override DateTime EarliestTimestamp
        {
            get
            {
                if (cachedTimes.Count < 1) return DateTime.MaxValue;
                else return cachedTimes[0];
            }
        }

        public override DateTime LatestTimestamp
        {
            get
            {
                if (cachedTimes.Count < 1) return DateTime.MinValue;
                else return cachedTimes[0];
            }
        }

        public override FeatureCollection GetRange(DateTime start, DateTime end)
        {
            FeatureCollection col = new FeatureCollection();
            for (int x = 0; x < cachedTimes.Count; x++)
            {
                DateTime t = cachedTimes[x];
                if (t > start && t <= end)
                {
                    FeatureCollection temp = GetExact(t);
                    if (temp == null) continue;
                    foreach (Feature f in temp.GetAllFeatures())
                        col.Add(f);
                }
            }
            return col;
        }
    }
}

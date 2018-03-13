using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace WSIMap
{
    public class FeatureCollectionDiskCache : FeatureCollectionCache
    {
        #region Data Members
        protected List<DateTime> cachedTimes;
        public string FeatureType;
        protected string CacheDirectory;
        protected static BinaryFormatter formatter;
        #endregion

        public FeatureCollectionDiskCache(string FeatureType, int MinutesToKeepData)
        {
            formatter = new BinaryFormatter(); 
            this.cachedTimes = new List<DateTime>();
            this.FeatureType = FeatureType;
            CacheDirectory = "DiskCache\\" + FeatureType;
            if (!Directory.Exists("DiskCache")) Directory.CreateDirectory("DiskCache");
            if (!Directory.Exists(CacheDirectory)) Directory.CreateDirectory(CacheDirectory);
            KeepMinutes = MinutesToKeepData;
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

        #region file stuff
        public override bool Put(FeatureCollection fc, DateTime sliceTime)
        {
            // if we already have that time, remove old one first
            if (cachedTimes.Contains(sliceTime))
            {
                try { RemoveExact(sliceTime); }
                catch { }
            }

            FileStream fs = null;
            try
            {
                // put the feature collection to disk as GetFilename(sliceTime)
                if (!Directory.Exists(CacheDirectory))
                    Directory.CreateDirectory(CacheDirectory);
                fs = new FileStream(CacheDirectory + "\\" + GetFilename(sliceTime), FileMode.OpenOrCreate);
                formatter.Serialize(fs, fc);
                fs.Flush();
                fs.Close();

                ClearOldFiles();
            }
            catch //(Exception ex)
            {
                //Console.WriteLine("Error writing file: " + ex.ToString());
                if (fs != null)
                    fs.Close();
                return false;
            }
            return true;
        }

        public override void ClearOldFiles()
        {
            // check for old files and clean them out
            string[] s = Directory.GetFiles(CacheDirectory);
            cachedTimes.Clear();
            foreach (string file in s)
            {
                string realFN = file;
                DateTime t = GetDate(realFN);
                if (((TimeSpan)DateTime.UtcNow.Subtract(t)).TotalMinutes > KeepMinutes)
                {
                    RemoveExact(t);
                }
                cachedTimes.Add(t);
            }
            cachedTimes.Sort();
        }

        public override FeatureCollection GetExact(DateTime sliceTime)
        {
            // get the file with filename == GetFilename(sliceTime)
            if (!Directory.Exists(CacheDirectory) ||
                !File.Exists(CacheDirectory + "\\" + GetFilename(sliceTime)))
            {
                return null;
            }
            else
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(CacheDirectory + "\\" + GetFilename(sliceTime), FileMode.Open, FileAccess.Read);
                    FeatureCollection retVal = (FeatureCollection)formatter.Deserialize(fs);
                    fs.Flush();
                    fs.Close();
                    return retVal;
                }
                catch { if (fs != null)fs.Close(); return null; }
            }
        }

        public override void RemoveExact(DateTime sliceTime)
        {
            // clear file GetFileName(sliceTime)
            if (File.Exists(CacheDirectory + "\\" + GetFilename(sliceTime)))
                File.Delete(CacheDirectory + "\\" + GetFilename(sliceTime));
            if (cachedTimes.Contains(sliceTime))
                cachedTimes.Remove(sliceTime);
        }

        private string GetFilename(DateTime sliceTime)
        {
            return sliceTime.ToString().Replace("\\", "-").Replace("/","_").Replace(":", "-") + ".fls";
        }

        private DateTime GetDate(string filename)
        {
            if (filename.IndexOf("\\") >= 0)
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);
            if (filename.Length < 1) return DateTime.MinValue;
            return DateTime.Parse(filename.Replace(".fls", "").Replace("-", ":").Replace("_", "/"));
        }
        #endregion

        public override void Clean()
        {
            foreach (string s in Directory.GetFiles(CacheDirectory))
            {
                // TODO: test and see if the directory is on that string
                File.Delete(CacheDirectory + "\\" + s.Substring(s.LastIndexOf("\\")+1));
            }
            cachedTimes.Clear();
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace WSIMap
{
    public class FeatureCollectionMemoryCache : FeatureCollectionCache
    {
        #region Data Members
        protected Hashtable FeatureCollections;
        protected List<DateTime> cachedTimes;
        #endregion

        public FeatureCollectionMemoryCache(int MinutesToKeepData)
        {
            FeatureCollections = new Hashtable();
            cachedTimes = new List<DateTime>();
            KeepMinutes = MinutesToKeepData;
        }

        public override void Clean()
        {
            foreach (FeatureCollection fc in FeatureCollections.Values)
                fc.Clear(true, true);
            FeatureCollections.Clear();
            cachedTimes.Clear();
        }

        public override void ClearOldFiles()
        {
            List<DateTime> keysToRemove = new List<DateTime>();
            for (int x = 0; x < cachedTimes.Count; x++)
                if (((TimeSpan)DateTime.UtcNow.Subtract(cachedTimes[x])).TotalMinutes > KeepMinutes)
                {
                    keysToRemove.Add(cachedTimes[x]);
                    cachedTimes.RemoveAt(x);
                    x--;
                }
            foreach (DateTime t in keysToRemove)
                RemoveExact(t);
        }

        public override DateTime EarliestTimestamp
        {
            get
            {
                if (cachedTimes.Count > 0) return cachedTimes[0];
                return DateTime.MaxValue;
            }
        }

        public override FeatureCollection GetExact(DateTime sliceTime)
        {
            if (FeatureCollections.ContainsKey(sliceTime)) return (FeatureCollection)FeatureCollections[sliceTime];
            return null;
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
            if (!FeatureCollections.ContainsKey(sliceTime))
            {
                FeatureCollections.Add(sliceTime, fc);
                cachedTimes.Add(sliceTime);
                cachedTimes.Sort();
            }
            else
            {
                ((FeatureCollection)FeatureCollections[sliceTime]).Clear(true, true);
                FeatureCollections[sliceTime] = fc;
            }
            ClearOldFiles();
            return true;
        }

        public override void RemoveExact(DateTime sliceTime)
        {
            if (FeatureCollections.ContainsKey(sliceTime))
            {
                ((FeatureCollection)FeatureCollections[sliceTime]).Clear(true, true);
                FeatureCollections.Remove(sliceTime);
                if (cachedTimes.Contains(sliceTime))
                    cachedTimes.Remove(sliceTime);
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

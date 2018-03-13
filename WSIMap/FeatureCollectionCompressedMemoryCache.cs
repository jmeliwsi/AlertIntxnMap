using System;
using System.Collections.Generic;
using System.Text;
using ManagedLZO;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace WSIMap
{
    // -------------------------------------------------------------------------
    // *** WARNING ***
    // For performance reasons, this FeatureCollectionCache does not clean
    // features from FeatureCollections when it discards or replaces them.
    // This could result in a memory leak.
    // -------------------------------------------------------------------------
    public class FeatureCollectionCompressedMemoryCache : FeatureCollectionCache
    {
        #region Data Members
        protected Dictionary<DateTime, byte[]> featureCollections;
        protected List<DateTime> cachedTimes;
        protected Dictionary<DateTime, int> uncompressedSize;
        protected DateTime minTime;
        #endregion

        public FeatureCollectionCompressedMemoryCache(int MinutesToKeepData)
        {
            featureCollections = new Dictionary<DateTime, byte[]>();
            cachedTimes = new List<DateTime>();
            uncompressedSize = new Dictionary<DateTime, int>();
            KeepMinutes = MinutesToKeepData;
        }

        public override void Clean()
        {
            featureCollections.Clear();
            cachedTimes.Clear();
            uncompressedSize.Clear();
        }

        public override void ClearOldFiles()
        {
            minTime = DateTime.UtcNow.AddMinutes(-KeepMinutes);

            // Remove old entries
            for (int i = 0; i < cachedTimes.Count; i++)
            {
                if (cachedTimes[i] < minTime)
                {
                    featureCollections.Remove(cachedTimes[i]);
                    uncompressedSize.Remove(cachedTimes[i]);
                }
            }

            // Remove old entries from the cached times
            cachedTimes.RemoveAll(Expired);
        }

        private bool Expired(DateTime t)
        {
            if (t < minTime)
                return true;
            else
                return false;
        }

        public override DateTime EarliestTimestamp
        {
            get
            {
                if (cachedTimes.Count > 0)
                    return cachedTimes[0];
                return DateTime.MaxValue;
            }
        }

        public override DateTime LatestTimestamp
        {
            get
            {
                if (cachedTimes.Count > 0)
                    return cachedTimes[cachedTimes.Count - 1];
                return DateTime.MinValue;
            }
        }

        public override FeatureCollection GetExact(DateTime sliceTime)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            if (featureCollections.ContainsKey(sliceTime))
            {
                byte[] src = featureCollections[sliceTime];
                if (src == null) return null;
                byte[] dst = new byte[uncompressedSize[sliceTime]];
                MiniLZO.Decompress(src, dst);
                ms.Write(dst, 0, dst.Length);
                ms.Position = 0;
                FeatureCollection fc = (FeatureCollection)bf.Deserialize(ms);
                return fc;
            }
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
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            if (!featureCollections.ContainsKey(sliceTime))
            {
                bf.Serialize(ms, fc);
                ms.Position = 0;
                byte[] src = ms.ToArray();
                byte[] dst = null;
                MiniLZO.Compress(src, out dst);
                featureCollections.Add(sliceTime, dst);
                uncompressedSize.Add(sliceTime, src.Length);
                cachedTimes.Add(sliceTime);
                cachedTimes.Sort();
            }
            else
            {
                bf.Serialize(ms, fc);
                ms.Position = 0;
                byte[] src = ms.ToArray();
                byte[] dst = null;
                MiniLZO.Compress(src, out dst);
                featureCollections[sliceTime] = dst;
                uncompressedSize[sliceTime] = src.Length;
            }

            ms.Close();
            ms.Dispose();

            ClearOldFiles();
            return true;
        }

        public override void RemoveExact(DateTime sliceTime)
        {
            if (featureCollections.ContainsKey(sliceTime))
            {
                //((FeatureCollection)featureCollections[sliceTime]).Clear(true, true);
                featureCollections.Remove(sliceTime);
                uncompressedSize.Remove(sliceTime);
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

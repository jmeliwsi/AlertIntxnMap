using System;
using System.Collections.Generic;
using System.Text;

namespace WSIMap
{
    public abstract class FeatureCollectionCache
    {
        public int KeepMinutes;
        public abstract FeatureCollection GetNear(DateTime t);
        public abstract bool Put(FeatureCollection fc, DateTime sliceTime);
        public abstract void ClearOldFiles();
        public abstract FeatureCollection GetExact(DateTime sliceTime);
        public abstract void RemoveExact(DateTime sliceTime);
        public abstract void Clean();
        public virtual DateTime EarliestTimestamp
        {
            get { return DateTime.MaxValue; }
        }
        public virtual DateTime LatestTimestamp
        {
            get { return DateTime.MinValue; }
        }
        public abstract FeatureCollection GetRange(DateTime start, DateTime end);
    }
}

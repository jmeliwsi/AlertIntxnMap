using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WSIMap
{
    public class LoopLayer : Layer
    {
        #region Data Members
        protected Dictionary<DateTime, FeatureCollection> fcList;
        protected DateTime timeStamp;
        public FeatureCollectionCache diskCache;
        private int transparency;
        private Color threshold;
        private bool isApplyThreshold = false;
        #endregion

        public LoopLayer(FeatureCollectionCache dc)
        {
            this.fcList = new Dictionary<DateTime, FeatureCollection>();
            timeStamp = DateTime.MinValue;
            diskCache = dc;
        }

        public override void Refresh(MapProjections mapProjection, short centralLongitude)
        {
            base.Refresh(mapProjection, centralLongitude);
        }

        public void Goto(DateTime t)
        {
            //timeStamp = t;
            //if (!fcList.TryGetValue(t, out this.features))
            //{
            //    // t wasn't found; find the entry closest to, but not after, t
            //    TimeSpan ts, timeDiff = TimeSpan.MaxValue;
            //    foreach (KeyValuePair<DateTime, FeatureCollection> kv in fcList)
            //    {
            //        ts = t - kv.Key;
            //        if (ts.TotalSeconds >= 0 && ts < timeDiff)
            //        {
            //            timeDiff = ts;
            //            this.features = kv.Value;
            //            timeStamp = kv.Key;
            //        }
            //    }
            //}
            GotoNear(t);
        }

        public void GotoRange(DateTime start, DateTime end)
        {
            timeStamp = start;
            features = diskCache.GetRange(start, end);
        }

        public FeatureCollection GetAt(DateTime t)
        {
            //FeatureCollection fc = null;
            //try
            //{
            //    if (!fcList.TryGetValue(t, out fc))
            //    {
            //        fc = new FeatureCollection();
            //        fcList.Add(t, fc);
            //    }
            //}
            //catch { }
            //return fc;
            return diskCache.GetNear(t);
        }

        public void Clean()
        {
            //// Empty all the feature collections
            //foreach (KeyValuePair<DateTime, FeatureCollection> kv in fcList)
            //    kv.Value.Clear(true);

            //// Empty the feature collection list
            //fcList.Clear();
            diskCache.ClearOldFiles();
        }

        public void Clean(DateTime t)
        {
            //List<DateTime> removalList = new List<DateTime>();

            //// Find entries older than time t
            //foreach (KeyValuePair<DateTime, FeatureCollection> kv in fcList)
            //{
            //    if (kv.Key < t)
            //        removalList.Add(kv.Key);
            //}

            //// If there are any, clear them and remove them from the dictionary
            //if (removalList.Count > 0)
            //{
            //    foreach (DateTime dt in removalList)
            //    {
            //        fcList[dt].Clear(true);
            //        fcList.Remove(dt);
            //    }
            //    removalList.Clear();
            //}
            diskCache.RemoveExact(t);
        }

        public DateTime TimeStamp
        {
            get { return timeStamp; }
        }

        //public int FeatureCount
        //{
        //    get
        //    {
        //        int count = 0;
        //        foreach (KeyValuePair<DateTime, FeatureCollection> kv in fcList)
        //            count += kv.Value.Count;
        //        return count;
        //    }
        //}

        protected bool Add(DateTime key, FeatureCollection value)
        {
            //// Add the feature collection to the dictionary
            //try
            //{
            //    fcList.Add(key, value);
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
            diskCache.Put(value, key);
            return true;
        }

        internal override void Draw(MapGL parentMap)
        {
            if (this.features != null)
            {
                lock (features)
                {
                    base.Draw(parentMap);
                }
            }
        }

        public Color Threshold
        {
            set { threshold = value; }
        }

        public int Transparency
        {
            set { transparency = value; }
        }

        public bool IsApplyThreshold
        {
            set { isApplyThreshold = value; }
        }

        public void GotoNear(DateTime t)
        {
            timeStamp = t;
            features = diskCache.GetNear(t);

            if (features == null)
                return;

            lock (features)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    Feature f = features[i];

                    if (f.GetType() == typeof(WSIRaster))
                    {
                        WSIRaster image = f as WSIRaster;
                        if (isApplyThreshold && (image.Threshold != this.threshold))
                        {
                            if (image.Transparency != this.transparency)
                                image.SetAlpha(this.transparency);

                            image.Threshold = this.threshold;
                        }
                        else if (image.Transparency != this.transparency)
                        {
                            if (isApplyThreshold)
                            {
                                image.SetAlpha(this.transparency);
                                image.Threshold = this.threshold;
                            }
                            else
                                image.Transparency = this.transparency;
                        }
                    }
                }
            }
        }
    }
}

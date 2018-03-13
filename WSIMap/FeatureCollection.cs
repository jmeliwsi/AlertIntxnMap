using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WSIMap
{
    /**
     * \class FeatureCollection
     * \brief A collection class that holds a set of map features
     */
    [Serializable]
    public class FeatureCollection : ISerializable, IEnumerable
    {
        #region Data Members
        List<Feature> list = null;
        protected bool shared;
        #endregion

        public FeatureCollection()
        {
            shared = false;
            list = new List<Feature>();
        }

        public FeatureCollection(SerializationInfo i, StreamingContext s)
        {
            shared = false;
            list = new List<Feature>();
            i.SetType(typeof(FeatureCollection));
            try
            {
                object[] temp = (object[])i.GetValue("features", typeof(object[]));
                foreach (Feature f in temp)
                {
                    Add(f);
                }
            }
            catch { }
        }

        public bool Shared
        {
            get { return shared; }
            set { shared = value; }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public void Add(Feature feature)
        {
			lock (this.SyncRoot)
			{
				list.Add(feature);
			}
        }

		public void Insert(int index, Feature feature)
		{
			if (index < 0 || index > list.Count)
				return;

			lock (this.SyncRoot)
			{
				list.Insert(index, feature);
			}
		}

        public void Remove(Feature feature)
        {
            lock (this.SyncRoot)
            {
                list.Remove(feature);
            }
        }

		public void RemoveAll(Feature feature)
		{
			lock (this.SyncRoot)
			{
				list.RemoveAll(
					delegate(Feature f)
					{
						return f == feature;
					}
				);
			}
		}

        public void RemoveAt(int index)
        {
            lock (this.SyncRoot)
            {
				IDisposable l = list[index] as IDisposable;
				if (l != null)
					l.Dispose();
                list.RemoveAt(index);
            }
        }

        public void Clear(bool forceIfShared)
        {
            Clear(forceIfShared, true);
        }

        public void Clear(bool forceIfShared, bool disposeFeatures)
        {
            lock (this.SyncRoot)
            {
                // Don't clear if this collection is shared unless forced by the user
                if (!forceIfShared && shared) return;

                // Dispose the features if requested
                if (disposeFeatures)
                {
					for (int i = 0; i < list.Count; i++)
					{
						IDisposable l = list[i] as IDisposable;
						if (l != null)
							l.Dispose();
					}
                }

                // Clear the collection
                list.Clear();
            }
        }

		public Feature FindTypeOf(Type t)
		{
			foreach (Feature f in list)
				if (f.GetType() == t)
					return f;
			return null;
		}

		public string GetToolTipInfo()
        {
            StringBuilder toolTipText = new StringBuilder();

			for (int i = 0; i < list.Count; i++)
				toolTipText.Append(list[i].FeatureInfo).Append("\n");

            return toolTipText.ToString();
        }

        virtual public Feature this[string featureName]
        {
            get
            {
                return list.Find(delegate(Feature f) { return f.FeatureName == featureName; });
            }
        }

        virtual public Feature this[int index]
        {
            get { return list[index]; }
        }

        virtual public Feature[] GetAllFeatures()
        {
            return list.ToArray();
        }

        public object SyncRoot
        {
            get { return ((ICollection)list).SyncRoot; }
        }

		public IEnumerator GetEnumerator()
		{
			return this.list.GetEnumerator();
		}

        #region ISerializable Members

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(FeatureCollection));
            info.AddValue("features", this.GetAllFeatures(), typeof(object[]));
        }

        #endregion
    }
}

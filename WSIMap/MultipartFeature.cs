using System;
using System.Collections.Generic;
using System.Text;

namespace WSIMap
{
	/// <summary>
	/// A multipartFeature is a feature which contains multiple sub features.  The features (in general) are treated as a single
	/// feature for display. Some special code is needed to access and handle the sub features.
	/// </summary>
    public class MultipartFeature : Feature, IProjectable, IRefreshable
    {
        #region Data Members
        protected FeatureCollection features;
		protected MapProjections mapProjection;
		protected short centralLongitude;
		/// <summary>
		/// Boolean to indicate if on a search of the multipart feature whether all features which match should be returned or just
		/// one ... indicated that the multiple part feature as a single feature matches the search critea.  Example is searching for
		/// right mouse click context menu items, we want to show all the sub features of the multipart feature which match.
		/// </summary>
		protected bool searchReturnAllThatMatch;
		public bool SearchReturnAllThatMatch
		{
			get
			{
				return( this.searchReturnAllThatMatch);
			}
			set
			{
				this.searchReturnAllThatMatch = value;
			}
		}
        #endregion

        public MultipartFeature()
        {
            features = new FeatureCollection();
            features.Shared = false;
			this.SearchReturnAllThatMatch = false;
        }

		public MapProjections MapProjection
		{
			get { return mapProjection; }
		}

		public void Dispose()
        {
			for (int i = 0; i < this.Count; i++)
			{
				IDisposable f = features[i] as IDisposable;
				if (f != null)
					f.Dispose();
			}
        }

        public void Refresh(MapProjections mapProjection, short centralLongitude)
        {
			this.mapProjection = mapProjection;

            if (Tao.Platform.Windows.Wgl.wglGetCurrentContext() != IntPtr.Zero)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (features[i].visible && features[i] is IRefreshable)
                        ((IRefreshable)features[i]).Refresh(mapProjection, centralLongitude);
                }
            }
        }

		public int Count
        {
            get { return features.Count; }
        }

        public void Add(Feature feature)
        {
            features.Add(feature);
        }

        public void Remove(Feature feature)
        {
            features.Remove(feature);
        }

        public void RemoveAt(int index)
        {
            features.RemoveAt(index);
        }

        public void Clear(bool disposeFeatures)
        {
            features.Clear(true, disposeFeatures);
        }

        public Feature this[string featureName]
        {
            get { return features[featureName]; }
        }

        public Feature this[int index]
        {
            get { return features[index]; }
        }

        public Feature[] GetAllFeatures()
        {
            return features.GetAllFeatures();
        }

        public object SyncRoot
        {
            get { return features.SyncRoot; }
        }

        internal override void Draw(MapGL parentMap, Layer parentLayer)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (features[i].visible)
                    features[i].Draw(parentMap, parentLayer);
            }
        }
    }
}

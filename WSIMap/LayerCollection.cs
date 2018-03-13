using System;

namespace WSIMap
{
	/**
	 * \class LayerCollection
	 * \brief A collection class that holds all the map layers
	 */
	public class LayerCollection : System.Collections.CollectionBase
	{
		internal LayerCollection()
		{
		}

		public int Add(Layer layer)
		{
			int index;
			lock (this.List.SyncRoot)
			{
				index = this.List.Add(layer);
			}
			return index;
		}

		public int Insert(Layer layer, int index)
		{
			if (index > this.List.Count || index < 0) return -1;
			lock (this.List.SyncRoot)
			{
				this.List.Insert(index, layer);
			}
			return index;
		}

		public void Remove(Layer layer)
		{
			lock (this.List.SyncRoot)
			{
				this.List.Remove(layer);
			}
		}

		public Layer this[string layerName]
		{
			get
			{
				Layer layer = (Layer)null;
				bool found = false;
				lock (this.List.SyncRoot)
				{
					for (int i=0; i<this.List.Count; i++)
					{
						layer = (Layer)this.List[i];
						if (found = string.Equals(layer.LayerName, layerName))
							break;
					}
				}
				if (found)
					return layer;
				else
					return null;
			}
		}

		public Layer this[int index]
		{
			get
			{
				if (index < 0 || index >= this.List.Count)
					return null;
				else
					return (Layer)this.List[index];
			}
		}
	}
}

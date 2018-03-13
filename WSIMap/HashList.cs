using System;
using System.Collections;

namespace WSIMap
{
	public abstract class HashList : IDictionary, IEnumerable
	{
		#region Data Members
		protected ArrayList keys = new ArrayList();
		protected Hashtable values = new Hashtable();		
		#endregion

		#region ICollection implementation
		public int Count
		{
			get { return values.Count; }
		}

		public bool IsSynchronized
		{
			get { return values.IsSynchronized; }
		}

		public object SyncRoot
		{
			get { return values.SyncRoot; }
		}

		public void CopyTo(System.Array oArray, int iArrayIndex)
		{
			values.CopyTo(oArray, iArrayIndex);
		}
		#endregion

		#region IDictionary implementation
		public void Add(object oKey, object oValue)
		{
			keys.Add(oKey);
			values.Add(oKey, oValue);
		}

		public bool IsFixedSize
		{
			get { return keys.IsFixedSize; }
		}

		public bool IsReadOnly
		{
			get { return keys.IsReadOnly; }
		}

		public ICollection Keys
		{
			get { return values.Keys; }
		}

		public void Clear()
		{
			values.Clear();
			keys.Clear();
		}

		public bool Contains(object oKey)
		{
			return values.Contains(oKey);
		}

		public bool ContainsKey(object oKey)
		{
			return values.ContainsKey(oKey);
		}

		public IDictionaryEnumerator GetEnumerator()
		{
			return values.GetEnumerator();
		}	

		public void Remove(object oKey)
		{
			values.Remove(oKey);
			keys.Remove(oKey);
		}

		public object this[object oKey]
		{
			get { return values[oKey]; }
			set { values[oKey] = value; }
		}

		public ICollection Values
		{
			get { return values.Values; }
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator()
		{
			return values.GetEnumerator();
		}
		#endregion
		
		#region HashList specialized implementation
		public object this[string Key]
		{
			get { return values[Key]; }
		}

		public object this[int Index]
		{
			get { return values[keys[Index]]; }
		}
		#endregion
	}
}

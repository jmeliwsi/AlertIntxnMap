using System;

namespace FUL
{
	[Serializable]
	public class SDictionary : System.Collections.Generic.Dictionary<string, object>, System.Runtime.Serialization.ISerializable,
		System.Xml.Serialization.IXmlSerializable
	{
		[System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			System.Runtime.Serialization.SerializationInfoEnumerator enumerator = info.GetEnumerator();

			while (enumerator.MoveNext())
			{
				System.Runtime.Serialization.SerializationEntry entry = enumerator.Current;
				this.Add(entry.Name.ToString(), entry.Value);
			}

		}

		protected SDictionary(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			//For each value in the string dictionary, add a record in the info
			foreach (System.Collections.Generic.KeyValuePair<string, object> de in this)
				info.AddValue(de.Key.ToString(), de.Value);
		}

		public SDictionary() : base() { }

		private void Add(System.Object newObject)
		{
			//Do nothing.. only here to implement an unused interface
		}

		[System.Diagnostics.DebuggerStepThrough]
		void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter w)
		{
			System.Xml.Serialization.XmlSerializer keySer = new System.Xml.Serialization.XmlSerializer(typeof(string));
			System.Xml.Serialization.XmlSerializer valueSer = new System.Xml.Serialization.XmlSerializer(typeof(object));
			w.WriteStartElement("d");

			foreach (System.Collections.Generic.KeyValuePair<string, object> entry in this)
			{
				w.WriteStartElement("i");

				w.WriteStartElement("k");
				keySer.Serialize(w, entry.Key);
				w.WriteEndElement();

				w.WriteStartElement("v");
				valueSer.Serialize(w, entry.Value);
				w.WriteEndElement();

				w.WriteEndElement();
			}
			w.WriteEndElement();
		}

		[System.Diagnostics.DebuggerStepThrough]
		void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader r)
		{
			System.Xml.Serialization.XmlSerializer keySer = new System.Xml.Serialization.XmlSerializer(typeof(string));
			System.Xml.Serialization.XmlSerializer valueSer = new System.Xml.Serialization.XmlSerializer(typeof(object));

			r.Read();
			r.ReadStartElement("d");
			while (r.NodeType != System.Xml.XmlNodeType.EndElement)
			{
				r.ReadStartElement("i");

				r.ReadStartElement("k");
				string key = keySer.Deserialize(r).ToString();
				r.ReadEndElement();

				r.ReadStartElement("v");
				object value = valueSer.Deserialize(r);
				r.ReadEndElement();

				this.Add(key, value);

				r.ReadEndElement();
				r.MoveToContent();
			}
			r.ReadEndElement();
			r.Read();
		}

		System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
		{
			return null;
		}
	}
}

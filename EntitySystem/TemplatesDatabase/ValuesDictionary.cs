using Engine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using XmlUtilities;

namespace TemplatesDatabase
{
	public class ValuesDictionary : IEnumerable<KeyValuePair<string, object>>, IEnumerable
	{
		private Dictionary<string, object> m_dictionary = [];

		private DatabaseObject m_databaseObject;

		public int Count => m_dictionary.Count;

		public IEnumerable<string> Keys => m_dictionary.Keys;

		public IEnumerable<object> Values => m_dictionary.Values;

		public DatabaseObject DatabaseObject
		{
			get
			{
				return m_databaseObject;
			}
			set
			{
				m_databaseObject = value;
			}
		}

		public object this[string key]
		{
			get
			{
				return GetValue<object>(key);
			}
			set
			{
				SetValue(key, value);
			}
		}

		public bool ContainsKey(string key)
		{
			return m_dictionary.ContainsKey(key);
		}

		public bool ContainsValue(object value)
		{
			return m_dictionary.ContainsValue(value);
		}

		public void EnsureCapacity(int capacity)
		{
			m_dictionary.EnsureCapacity(capacity);
		}

		public T GetValue<T>(string key)
		{
			if (m_dictionary.TryGetValue(key, out object value))
			{
				return (T)value;
			}
			throw new InvalidOperationException($"Required value \"{key}\" not found in values dictionary");
		}

		public T GetValue<T>(string key, T defaultValue)
		{
			if (m_dictionary.TryGetValue(key, out object value))
			{
				return (T)value;
			}
			return defaultValue;
		}

		public void SetValue<T>(string key, T value)
		{
			m_dictionary[key] = value;
		}

		public void Add<T>(string key, T value)
		{
			m_dictionary.Add(key, value);
		}

		public void Clear()
		{
			m_dictionary.Clear();
		}
		public void Remove(string key)
		{
			m_dictionary.Remove(key);
		}

		public void Remove(string key, out object value)
		{
			m_dictionary.Remove(key, out value);
		}

		public void TrimExcess()
		{
			m_dictionary.TrimExcess();
		}

		public void TrimExcess(int capacity)
		{
			m_dictionary.TrimExcess(capacity);
		}

		public bool TryAdd<T>(string key, T value)
		{
			return m_dictionary.TryAdd(key, value);
		}

		public bool TryGetValue<T>(string key, out T value)
		{
			if (m_dictionary.TryGetValue(key, out object value2))
			{
				value = (T)value2;
				return true;
			}
			value = default;
			return false;
		}

		public void Save(XElement node)
		{
			foreach (KeyValuePair<string, object> item in m_dictionary)
			{
				ValuesDictionary valuesDictionary = item.Value as ValuesDictionary;
				if (valuesDictionary != null)
				{
					XElement node2 = XmlUtils.AddElement(node, "Values");
					XmlUtils.SetAttributeValue(node2, "Name", item.Key);
					valuesDictionary.Save(node2);
				}
				else
				{
					XElement node3 = XmlUtils.AddElement(node, "Value");
					XmlUtils.SetAttributeValue(node3, "Name", item.Key);
					XmlUtils.SetAttributeValue(node3, "Type", TypeCache.GetShortTypeName(item.Value.GetType().FullName));
					XmlUtils.SetAttributeValue(node3, "Value", item.Value);
				}
			}
		}

		public void PopulateFromDatabaseObject(DatabaseObject databaseObject)
		{
			m_databaseObject = databaseObject;
			foreach (DatabaseObject effectiveNestingChild in databaseObject.GetEffectiveNestingChildren(null, directChildrenOnly: true))
			{
				if (effectiveNestingChild.Type.SupportsValue)
				{
					if (effectiveNestingChild.Value is ProceduralValue)
					{
						object value = ((ProceduralValue)effectiveNestingChild.Value).Parse(databaseObject);
						SetValue(effectiveNestingChild.Name, value);
					}
					else
					{
						SetValue(effectiveNestingChild.Name, effectiveNestingChild.Value);
					}
				}
				else
				{
					ValuesDictionary valuesDictionary = [];
					valuesDictionary.PopulateFromDatabaseObject(effectiveNestingChild);
					SetValue(effectiveNestingChild.Name, valuesDictionary);
				}
			}
		}

		public void ApplyOverrides(ValuesDictionary overridesValuesDictionary)
		{
			foreach (KeyValuePair<string, object> item in overridesValuesDictionary)
			{
				ValuesDictionary valuesDictionary = item.Value as ValuesDictionary;
				if (valuesDictionary != null)
				{
					ValuesDictionary valuesDictionary2 = GetValue<object>(item.Key, null) as ValuesDictionary;
					if (valuesDictionary2 == null)
					{
						valuesDictionary2 = [];
						SetValue(item.Key, valuesDictionary2);
					}
					valuesDictionary2.ApplyOverrides(valuesDictionary);
				}
				else
				{
					SetValue(item.Key, item.Value);
				}
			}
		}

		public void ApplyOverrides(XElement overridesNode)
		{
			foreach (XElement item in overridesNode.Elements())
			{
				if (item.Name == "Value")
				{
					string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Name");
					string attributeValue2 = XmlUtils.GetAttributeValue<string>(item, "Type", null);
					Type type;
					if (attributeValue2 == null)
					{
						object value = GetValue<object>(attributeValue, null);
						if (value == null)
						{
							throw new InvalidOperationException($"Type of override \"{attributeValue}\" cannot be determined.");
						}
						type = value.GetType();
					}
					else
					{
						type = TypeCache.FindType(attributeValue2, skipSystemAssemblies: false, throwIfNotFound: true);
					}
					object attributeValue3 = XmlUtils.GetAttributeValue(item, "Value", type);
					SetValue(attributeValue, attributeValue3);
				}
				else
				{
					if (!(item.Name == "Values"))
					{
						throw new InvalidOperationException($"Unrecognized element \"{item.Name}\" in values dictionary overrides XML.");
					}
					string attributeValue4 = XmlUtils.GetAttributeValue<string>(item, "Name");
					ValuesDictionary valuesDictionary = GetValue<object>(attributeValue4, null) as ValuesDictionary;
					if (valuesDictionary == null)
					{
						valuesDictionary = [];
						SetValue(attributeValue4, valuesDictionary);
					}
					valuesDictionary.ApplyOverrides(item);
				}
			}
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return m_dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_dictionary.GetEnumerator();
		}
	}
}

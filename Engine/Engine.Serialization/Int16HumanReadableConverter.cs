using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(short))]
    public class Int16HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((short)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return short.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

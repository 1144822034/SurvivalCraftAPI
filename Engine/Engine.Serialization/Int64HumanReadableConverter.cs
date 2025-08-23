using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(long))]
    public class Int64HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((long)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return long.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

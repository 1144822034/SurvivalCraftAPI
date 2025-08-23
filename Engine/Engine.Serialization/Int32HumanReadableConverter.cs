using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(int))]
    public class Int32HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((int)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return int.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

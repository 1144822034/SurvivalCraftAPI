using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(uint))]
    public class UInt32HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((uint)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return uint.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

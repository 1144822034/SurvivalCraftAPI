using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(ulong))]
    public class UInt64HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((ulong)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return ulong.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

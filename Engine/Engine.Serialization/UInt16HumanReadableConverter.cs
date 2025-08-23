using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(ushort))]
    public class UInt16HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((ushort)value).ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return ushort.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

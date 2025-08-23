using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(decimal))]
    public class DecimalHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((decimal)value).ToString("R", CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return decimal.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

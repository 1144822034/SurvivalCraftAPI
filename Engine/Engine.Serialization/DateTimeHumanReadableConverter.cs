using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(DateTime))]
    public class DateTimeHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return new DateTime(long.Parse(data, CultureInfo.InvariantCulture));
		}
	}
}

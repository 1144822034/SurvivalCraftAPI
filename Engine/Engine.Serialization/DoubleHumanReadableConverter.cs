using System.Globalization;

namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(double))]
    public class DoubleHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((double)value).ToString("R", CultureInfo.InvariantCulture);
		}

		public object ConvertFromString(Type type, string data)
		{
			return double.Parse(data, CultureInfo.InvariantCulture);
		}
	}
}

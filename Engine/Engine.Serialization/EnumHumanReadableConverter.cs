namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(Enum))]
    public class EnumHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((Enum)value).ToString();
		}

		public object ConvertFromString(Type type, string data)
		{
			return Enum.Parse(type, data, ignoreCase: false);
		}
	}
}

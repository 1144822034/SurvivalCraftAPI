namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(char))]
    public class CharHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((char)value).ToString();
		}

		public object ConvertFromString(Type type, string data)
		{
			return data[0];
		}
	}
}

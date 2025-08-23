namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(Version))]
    public class VersionHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((Version)value).ToString();
		}

		public object ConvertFromString(Type type, string data)
		{
			return Version.Parse(data);
		}
	}
}

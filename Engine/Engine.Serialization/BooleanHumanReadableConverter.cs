namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(bool))]
    public class BooleanHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
            return !(bool)value ? "False" : "True";
        }

        public object ConvertFromString(Type type, string data)
		{
            return string.Equals(data, "True", StringComparison.OrdinalIgnoreCase)
                ? true
                : string.Equals(data, "False", StringComparison.OrdinalIgnoreCase) ? (object)false : throw new Exception();
        }
    }
}

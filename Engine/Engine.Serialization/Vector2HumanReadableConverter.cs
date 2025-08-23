namespace Engine.Serialization
{
	[HumanReadableConverter(typeof(Vector2))]
    public class Vector2HumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			var vector = (Vector2)value;
			return HumanReadableConverter.ValuesListToString(',', vector.X, vector.Y);
		}

		public object ConvertFromString(Type type, string data)
		{
			float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
            return array.Length == 2 ? (object)new Vector2(array[0], array[1]) : throw new Exception();
        }
    }
}

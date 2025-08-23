namespace Engine.Serialization
{
    public class NullableSerializer<T> : ISerializer<T?> where T : struct
	{
		public void Serialize(InputArchive archive, ref T? value)
		{
			bool value2 = false;
			archive.Serialize("HasValue", ref value2);
			if (value2)
			{
				T value3 = default(T);
				archive.Serialize("Value", ref value3);
				value = value3;
			}
		}

		public void Serialize(OutputArchive archive, T? value)
		{
			if (value.HasValue)
			{
				archive.Serialize("HasValue", value: true);
				archive.Serialize("Value", value.Value);
			}
			else
			{
				archive.Serialize("HasValue", value: false);
			}
		}
	}
}

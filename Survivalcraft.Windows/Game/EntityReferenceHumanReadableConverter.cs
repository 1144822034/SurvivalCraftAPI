using Engine.Serialization;

namespace Game
{
	[HumanReadableConverter(typeof(EntityReference))]
	public class EntityReferenceHumanReadableConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return ((EntityReference)value).ReferenceString;
		}

		public object ConvertFromString(Type type, string data)
		{
			return EntityReference.FromReferenceString(data);
		}
	}
}

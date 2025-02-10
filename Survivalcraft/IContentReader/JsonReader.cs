using System.IO;
using System.Text.Json;

namespace Game.IContentReader
{
	public class JsonArrayReader : IContentReader
	{
		public override string Type => "JsonArray";
		public override string[] DefaultSuffix => ["json"];
		public override object Get(ContentInfo[] contents)
		{
			JsonElement element = JsonDocument.Parse(new StreamReader(contents[0].Duplicate()).ReadToEnd()).RootElement;
			return element.ValueKind == JsonValueKind.Array ? element : throw new InvalidDataException(contents[0].Filename + "is not Json array");
		}
	}

	public class JsonModelReader : IContentReader
	{
		public override string Type => "Game.JsonModel";
		public override string[] DefaultSuffix => ["json"];
		public override object Get(ContentInfo[] contents)
		{
			return Game.JsonModelReader.Load(contents[0].Duplicate());
		}
	}

	public class JsonObjectReader : IContentReader
	{
		public override string Type => "JsonObject";
		public override string[] DefaultSuffix => ["json"];
		public override object Get(ContentInfo[] contents)
		{
			JsonElement element = JsonDocument.Parse(new StreamReader(contents[0].Duplicate()).ReadToEnd()).RootElement;
			return element.ValueKind == JsonValueKind.Object ? element : throw new InvalidDataException(contents[0].Filename + "is not Json object");
		}
	}
}

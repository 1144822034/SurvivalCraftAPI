using System.IO;
namespace Game.IContentReader
{
	public class StringReader : IContentReader
	{
		public override string Type => "System.String";
		public override string[] DefaultSuffix => new string[] { "txt" };
		public override object Get(ContentInfo[] contents)
		{
			return new StreamReader(contents[0].Duplicate()).ReadToEnd();
		}
	}
}

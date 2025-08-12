using Engine;
using Engine.Graphics;
using Engine.Media;
using System.Diagnostics;

namespace Game.IContentReader
{
	public class BitmapFontReader : IContentReader
	{
		public override string Type => "Engine.Media.BitmapFont";
		public override string[] DefaultSuffix => ["lst", "webp", "png"];
		public override object Get(ContentInfo[] contents)
		{
			if(contents.Length != 2)
			{
				throw new System.Exception("not matches content count");
			}
			ContentInfo contentInfo = contents[1];
			Texture2D texture2D = ContentManager.Get<Texture2D>(contentInfo.ContentPath, contentInfo.ContentSuffix);
			return BitmapFont.Initialize(texture2D, contents[0].Duplicate(), Vector2.Zero);
		}
	}
}

using Engine;
using Engine.Graphics;
namespace Game.IContentReader
{
	public class Texture2DReader : IContentReader
	{
		public override string Type => "Engine.Graphics.Texture2D";
		public override string[] DefaultSuffix => new string[] { "webp", "png", "jpg", "jpeg" };
		public override object Get(ContentInfo[] contents)
		{
			ContentInfo contentInfo = contents[0];
			if(contentInfo.ContentPath == "Fonts/Pericles")
			{
				return Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, contentInfo.ContentSuffix), 3);
			}
			return Texture2D.Load(ContentManager.Get<Image>(contentInfo.ContentPath, contentInfo.ContentSuffix));
		}
	}
}

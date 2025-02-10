using Engine;
using Engine.Graphics;
namespace Game
{
	public class Subtexture
	{
		public readonly Texture2D Texture;

		public readonly Vector2 TopLeft;

		public readonly Vector2 BottomRight;

		public Subtexture(Texture2D texture, Vector2 topLeft, Vector2 bottomRight)
		{
			Texture = texture;
			TopLeft = topLeft;
			BottomRight = bottomRight;
		}
	}
}

using Engine;

namespace Game
{
    public class MatrixUtils
	{
		public static Matrix CreateScaleTranslation(float sx, float sy, float tx, float ty)
		{
			return new Matrix(sx, 0f, 0f, 0f, 0f, sy, 0f, 0f, 0f, 0f, 1f, 0f, tx, ty, 0f, 1f);
		}
	}
}

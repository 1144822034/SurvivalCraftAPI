using Engine;

namespace Game
{
	public class WoodenFenceBlock : FenceBlock
	{
		public static int Index = 94;

		public WoodenFenceBlock()
			: base("Models/WoodenFence", doubleSidedPlanks: false, useAlphaTest: false, 23, Color.White, Color.White)
		{
		}
	}
}

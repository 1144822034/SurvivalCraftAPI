using Engine;

namespace Game
{
	public class MovingBlock
	{
		public static bool IsNullOrStopped(MovingBlock movingBlock)
		{
			if(movingBlock == null) return true;
			IMovingBlockSet movingBlockSet = movingBlock.MovingBlockSet;
			if(movingBlockSet == null) return true;
			if(movingBlockSet.Stopped) return true;
			return false;
		}

		public Vector3 Position
		{
			get
			{
				return MovingBlockSet.Position + new Vector3(Offset);
			}
		}

		public Point3 Offset;

		public int Value;

		public IMovingBlockSet MovingBlockSet;
	}
}

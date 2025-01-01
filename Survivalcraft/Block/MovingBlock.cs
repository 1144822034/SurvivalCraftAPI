using Engine;
using GameEntitySystem;
using Jint.Native;
using TemplatesDatabase;

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
		public static MovingBlock LoadFromValuesDictionary(Project project,ValuesDictionary valuesDictionary,bool throwOnError = true, bool throwIfNotFound = false)
		{
			SubsystemMovingBlocks subsystemMovingBlocks = project.FindSubsystem<SubsystemMovingBlocks>();
			Vector3? movingBlocksPosition = valuesDictionary.GetValue<Vector3?>("MovingBlockSetPosition",null);
			if(movingBlocksPosition != null && movingBlocksPosition.HasValue)
			{
				IMovingBlockSet movingBlockSet = subsystemMovingBlocks.MovingBlockSets.FirstOrDefault(set => set.Position.ToString() == movingBlocksPosition.ToString(),null);
				if(movingBlockSet != null)
				{
					Point3 point = valuesDictionary.GetValue<Point3>("MovingBlockOffset");
					MovingBlock movingBlock = movingBlockSet.Blocks.FirstOrDefault(block => block.Offset == point,null);
					if(!MovingBlock.IsNullOrStopped(movingBlock))
					{
						return movingBlock;
					}
					else
					{
						if(throwOnError) throw new Exception("Required moving block offset " + point.ToString() + " is not found in MovingBlockSet " + movingBlocksPosition.ToString());
						return null;
					}
				}
				else
				{
					if(throwOnError) throw new Exception("Required moving block set " + movingBlocksPosition.ToString() + " is not found.");
					return null;
				}
			}
			if(throwIfNotFound) throw new Exception("MovingBlockSetPosition is not contained in valuesDictionary.");
			return null;
		}

		public void SetValuesDicionary(ValuesDictionary valuesDictionary, bool saveWhenStopped = false)
		{
			if(!MovingBlock.IsNullOrStopped(this) || saveWhenStopped)
			{
				valuesDictionary.SetValue("MovingBlockSetPosition", MovingBlockSet.Position);
				valuesDictionary.SetValue("MovingBlockOffset", Offset);
			}
		}

		public Point3 Offset;

		public int Value;

		public IMovingBlockSet MovingBlockSet;
	}
}

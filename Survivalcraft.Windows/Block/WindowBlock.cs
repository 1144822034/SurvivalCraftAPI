namespace Game
{
	public class WindowBlock : AlphaTestCubeBlock
	{
		public static int Index = 60;

		public bool IsCollapseSupportBlock_ = false;
		public override bool IsNonAttachable(int value)
        {
            return false;
        }
		public override bool IsCollapseSupportBlock(SubsystemTerrain subsystemTerrain,int value)
		{
			return IsCollapseSupportBlock_;
		}
	}
}

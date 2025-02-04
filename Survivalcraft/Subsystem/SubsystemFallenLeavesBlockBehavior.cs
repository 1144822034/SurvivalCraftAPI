using TemplatesDatabase;

namespace Game;

public class SubsystemFallenLeavesBlockBehavior : SubsystemPollableBlockBehavior
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSeasons m_subsystemSeasons;

	private Random m_random = new Random();

	public override int[] HandledBlocks => new int[0];

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		if (!CanSupportFallenLeaves(base.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)))
		{
			base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		UpdateFallenLeaves(x, y, z);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_random.Bool(0.5f))
		{
			UpdateFallenLeaves(x, y, z);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSeasons = base.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
	}

	public static bool CanSupportFallenLeaves(int value)
	{
		int num = Terrain.ExtractContents(value);
		return !BlocksManager.Blocks[num].IsTransparent;
	}

	public static bool StopsFallenLeaves(int value)
	{
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (!(block is AirBlock))
		{
			return !(block is LeavesBlock);
		}
		return false;
	}

	public static bool CanBeReplacedByFallenLeaves(int value)
	{
		return Terrain.ExtractContents(value) == 0;
	}

	private void UpdateFallenLeaves(int x, int y, int z)
	{
		if (m_subsystemSeasons.Season == Season.Spring || m_subsystemSeasons.Season == Season.Summer)
		{
			m_subsystemTerrain.DestroyCell(0, x, y, z, Terrain.MakeBlockValue(0), noDrop: true, noParticleSystem: true);
		}
	}
}

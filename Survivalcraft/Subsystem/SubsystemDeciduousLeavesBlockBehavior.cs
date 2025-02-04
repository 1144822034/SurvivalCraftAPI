using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemDeciduousLeavesBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private struct LeafParticles
	{
		public double Time;

		public Point3 Position;
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSeasons m_subsystemSeasons;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public SubsystemGameWidgets m_subsystemGameWidgets;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemCellChangeQueue m_subsystemCellChangeQueue;

	private Random m_random = new Random();

	private DynamicArray<LeafParticles> m_leafParticles = new DynamicArray<LeafParticles>();

	private DynamicArray<LeafParticles> m_tmpLeafParticles = new DynamicArray<LeafParticles>();

	public override int[] HandledBlocks => new int[0];

	UpdateOrder IUpdateable.UpdateOrder => UpdateOrder.Default;

	public void CreateFallenLeaves(Point3 p, bool applyImmediately)
	{
		int? num = null;
		while (p.Y >= 1 && p.Y < 256)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
			if (num.HasValue)
			{
				if (SubsystemFallenLeavesBlockBehavior.CanSupportFallenLeaves(cellValue) && SubsystemFallenLeavesBlockBehavior.CanBeReplacedByFallenLeaves(num.Value))
				{
					m_subsystemCellChangeQueue.QueueCellChange(p.X, p.Y + 1, p.Z, Terrain.MakeBlockValue(261), applyImmediately);
					break;
				}
				if (SubsystemFallenLeavesBlockBehavior.StopsFallenLeaves(cellValue))
				{
					break;
				}
			}
			num = cellValue;
			p.Y--;
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		UpdateTimeOfYear(value, x, y, z, applyImmediately: true);
		QueueLeafParticles(value, x, y, z);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		UpdateTimeOfYear(value, x, y, z, applyImmediately: false);
		QueueLeafParticles(value, x, y, z);
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSeasons = base.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
		m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameWidgets = base.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemCellChangeQueue = base.Project.FindSubsystem<SubsystemCellChangeQueue>(throwOnError: true);
	}

	void IUpdateable.Update(float dt)
	{
		if (!m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
		{
			return;
		}
		foreach (LeafParticles leafParticle in m_leafParticles)
		{
			if (m_subsystemTime.GameTime >= leafParticle.Time)
			{
				if (m_subsystemGameWidgets.CalculateDistanceFromNearestView(new Vector3(leafParticle.Position)) < 32f)
				{
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(leafParticle.Position.X, leafParticle.Position.Y, leafParticle.Position.Z);
					int num = Terrain.ExtractContents(cellValue);
					if (BlocksManager.Blocks[num] is DeciduousLeavesBlock deciduousLeavesBlock && deciduousLeavesBlock.GetLeafDropProbability(cellValue) > 0f)
					{
						m_subsystemParticles.AddParticleSystem(new LeavesParticleSystem(m_subsystemTerrain, leafParticle.Position, m_random.Int(1, 2), fadeIn: true, createFallenLeaves: false, cellValue));
					}
				}
			}
			else
			{
				m_tmpLeafParticles.Add(leafParticle);
			}
		}
		Utilities.Swap(ref m_leafParticles, ref m_tmpLeafParticles);
		m_tmpLeafParticles.Clear();
	}

	private void UpdateTimeOfYear(int value, int x, int y, int z, bool applyImmediately)
	{
		float num = 0.03f * (float)MathUtils.Hash((uint)(x + y * 59 + z * 3319)) / 4.2949673E+09f;
		float timeOfYear = IntervalUtils.Normalize(m_subsystemGameInfo.WorldSettings.TimeOfYear + num);
		DeciduousLeavesBlock obj = (DeciduousLeavesBlock)BlocksManager.Blocks[Terrain.ExtractContents(value)];
		int num2 = Terrain.ExtractData(value);
		int num3 = obj.SetTimeOfYear(num2, timeOfYear);
		if (num3 != num2)
		{
			int value2 = Terrain.ReplaceData(value, num3);
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2, applyImmediately);
			Season season = DeciduousLeavesBlock.GetSeason(num2);
			if (DeciduousLeavesBlock.GetSeason(num3) == Season.Winter && season != Season.Winter)
			{
				CreateFallenLeaves(new Point3(x, y, z), applyImmediately);
			}
		}
	}

	private void QueueLeafParticles(int value, int x, int y, int z)
	{
		DeciduousLeavesBlock deciduousLeavesBlock = (DeciduousLeavesBlock)BlocksManager.Blocks[Terrain.ExtractContents(value)];
		if (m_leafParticles.Count < 30000 && m_random.Bool(deciduousLeavesBlock.GetLeafDropProbability(value) / 60f * 60f) && m_subsystemGameWidgets.CalculateDistanceFromNearestView(new Vector3(x, y, z)) < 128f)
		{
			m_leafParticles.Add(new LeafParticles
			{
				Position = new Point3(x, y, z),
				Time = m_subsystemTime.GameTime + (double)m_random.Float(0f, 60f)
			});
		}
	}
}

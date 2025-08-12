using TemplatesDatabase;

namespace Game
{
	public class SubsystemImpactExplosivesBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemExplosions m_subsystemExplosions;

		public override int[] HandledBlocks => new int[0];

		public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem)
		{
			return m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(worldItem.Position.X), Terrain.ToCell(worldItem.Position.Y), Terrain.ToCell(worldItem.Position.Z), worldItem.Value);
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		}
	}
}

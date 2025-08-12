using Engine;

namespace Game
{
	public class MagmaBlock : FluidBlock
	{
		public static int Index = 92;

		public new static int MaxLevel = 4;

		public MagmaBlock()
			: base(MaxLevel)
		{
			CanBeBuiltIntoFurniture = true;
		}

		public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
		{
			if (GetIsTop(Terrain.ExtractData(value)))
			{
				return face != 5;
			}
			return false;
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			GenerateFluidTerrainVertices(generator, value, x, y, z, Color.White, Color.White, geometry.OpaqueSubsetsByFace);
		}

		public override bool ShouldAvoid(int value)
		{
			return true;
		}

        public override bool ShouldAvoid(int value, ComponentPilot componentPilot)
        {
			ComponentHealth componentHealth = componentPilot.m_componentCreature.ComponentHealth;
			if (componentHealth.MagmaResilience == float.PositiveInfinity) return false;
            return base.ShouldAvoid(value, componentPilot);
        }
    }
}

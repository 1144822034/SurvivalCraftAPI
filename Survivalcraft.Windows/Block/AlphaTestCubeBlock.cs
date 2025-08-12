using Engine;

namespace Game
{
	public abstract class AlphaTestCubeBlock : CubeBlock
	{
		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateCubeVertices(this, value, x, y, z, Color.White, geometry.AlphaTestSubsetsByFace);
		}
	}
}

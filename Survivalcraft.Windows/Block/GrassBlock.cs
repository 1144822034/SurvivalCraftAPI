using Engine;
using Engine.Graphics;

namespace Game
{
	public class GrassBlock : CubeBlock
	{
		public static int Index = 8;

		public override int GetFaceTextureSlot(int face, int value)
		{
			switch (face)
			{
				case 4:
					return 0;
				case 5:
					return 2;
				default:
					if (Terrain.ExtractData(value) == 0)
					{
						return 3;
					}
					return 68;
			}
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			Color topColor = color * BlockColorsMap.Grass.Lookup(environmentData.Temperature, environmentData.Humidity);
			BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, topColor, environmentData);
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			Color topColor = BlockColorsMap.Grass.Lookup(generator.Terrain, x, y, z);
			Color topColor2 = BlockColorsMap.Grass.Lookup(generator.Terrain, x + 1, y, z);
			Color topColor3 = BlockColorsMap.Grass.Lookup(generator.Terrain, x + 1, y, z + 1);
			Color topColor4 = BlockColorsMap.Grass.Lookup(generator.Terrain, x, y, z + 1);
			generator.GenerateCubeVertices(this, value, x, y, z, 1f, 1f, 1f, 1f, Color.White, topColor, topColor2, topColor3, topColor4, -1, geometry.OpaqueSubsetsByFace);
		}

		public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, 2);
		}

        public override bool IsSuitableForPlants(int value, int plantValue)
        {
			return true;
        }
    }
}

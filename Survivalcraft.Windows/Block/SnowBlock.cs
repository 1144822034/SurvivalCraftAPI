using Engine;
using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
	public class SnowBlock : CubeBlock
	{
		public static int Index = 61;

		private const float m_height = 0.125f;

		public BoundingBox[] m_collisionBoxes = new BoundingBox[1]
		{
			new(new Vector3(0f, 0f, 0f), new Vector3(1f, m_height, 1f))
		};

		public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
		{
			return face != 5;
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateCubeVertices(this, value, x, y, z, m_height, m_height, m_height, m_height, Color.White, Color.White, Color.White, Color.White, Color.White, -1, geometry.OpaqueSubsetsByFace);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), m_height, ref matrix, color, color, environmentData);
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			showDebris = true;
			if (toolLevel >= RequiredToolLevel)
			{
				int num = Random.Int(1, 3);
				for (int i = 0; i < num; i++)
				{
					dropValues.Add(new BlockDropValue
					{
						Value = Terrain.MakeBlockValue(85),
						Count = 1
					});
				}
			}
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}
	}
}

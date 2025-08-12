using Engine;
using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
	public class BatteryBlock : Block, IElectricElementBlock
	{
		public static int Index = 138;

		public BlockMesh m_standaloneBlockMesh = new();

		public BlockMesh m_blockMesh = new();

		public BoundingBox[] m_collisionBoxes = new BoundingBox[1];

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Battery");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Battery").ParentBone);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Battery").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMesh.AppendModelMeshPart(model.FindMesh("Battery").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_collisionBoxes[0] = m_blockMesh.CalculateBoundingBox();
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			showDebris = true;
			if (toolLevel >= RequiredToolLevel)
			{
				int data = Terrain.ExtractData(oldValue);
				dropValues.Add(new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(138, 0, data),
					Count = 1
				});
			}
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMesh, Color.White, null, geometry.SubsetOpaque);
			generator.GenerateWireVertices(value, x, y, z, 4, 0.72f, Vector2.Zero, geometry.SubsetOpaque);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 1f * size, ref matrix, environmentData);
		}

		public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
		{
			return new BatteryElectricElement(subsystemElectricity, new CellFace(x, y, z, 4));
		}

		public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
		{
			if (face == 4 && SubsystemElectricity.GetConnectorDirection(4, 0, connectorFace).HasValue)
			{
				return ElectricConnectorType.Output;
			}
			return null;
		}

		public int GetConnectionMask(int value)
		{
			return int.MaxValue;
		}

		public static int GetVoltageLevel(int data)
		{
			return 15 - (data & 0xF);
		}

		public static int SetVoltageLevel(int data, int voltageLevel)
		{
			return (data & -16) | (15 - (voltageLevel & 0xF));
		}
	}
}

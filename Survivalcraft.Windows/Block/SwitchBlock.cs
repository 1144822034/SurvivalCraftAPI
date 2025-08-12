using Engine;
using Engine.Graphics;
using System;

namespace Game
{
	public class SwitchBlock : MountedElectricElementBlock
	{
		public static int Index = 141;

		public BlockMesh m_standaloneBlockMesh = new();

		public BlockMesh[] m_blockMeshesByIndex = new BlockMesh[12];

		public BoundingBox[][] m_collisionBoxesByIndex = new BoundingBox[12][];

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Switch");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Body").ParentBone);
			Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Lever").ParentBone);
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					int num = (i << 1) | j;
					Matrix matrix = (i >= 4) ? ((i != 4) ? (Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)) : Matrix.CreateTranslation(0.5f, 0f, 0.5f)) : (Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f));
					Matrix matrix2 = Matrix.CreateRotationX((j == 0) ? MathUtils.DegToRad(30f) : MathUtils.DegToRad(-30f));
					m_blockMeshesByIndex[num] = new BlockMesh();
					m_blockMeshesByIndex[num].AppendModelMeshPart(model.FindMesh("Body").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_blockMeshesByIndex[num].AppendModelMeshPart(model.FindMesh("Lever").MeshParts[0], boneAbsoluteTransform2 * matrix2 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_collisionBoxesByIndex[num] = new BoundingBox[1] { m_blockMeshesByIndex[num].CalculateBoundingBox() };
				}
			}
			Matrix matrix3 = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateRotationZ((float)Math.PI / 2f);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Body").MeshParts[0], boneAbsoluteTransform * matrix3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Lever").MeshParts[0], boneAbsoluteTransform2 * matrix3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}

		public static bool GetLeverState(int value)
		{
			return (Terrain.ExtractData(value) & 1) != 0;
		}

		public static int SetLeverState(int value, bool state)
		{
			return Terrain.ReplaceData(value, state ? (Terrain.ExtractData(value) | 1) : (Terrain.ExtractData(value) & -2));
		}

		public static int GetVoltageLevel(int data)
		{
			return 15 - ((data >> 4) & 0xF);
		}

		public static int SetVoltageLevel(int data, int voltageLevel)
		{
			return (data & -241) | ((15 - (voltageLevel & 0xF)) << 4);
		}

		public override int GetFace(int value)
		{
			return (Terrain.ExtractData(value) >> 1) & 7;
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default(BlockPlacementData);
			result.Value = Terrain.ReplaceData(value, raycastResult.CellFace.Face << 1);
			int data = SetVoltageLevel(Terrain.ExtractData(result.Value), GetVoltageLevel(Terrain.ExtractData(value)));
			result.Value = Terrain.ReplaceData(value, data);
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			int num = CalculateIndex(value);
			if (num >= m_collisionBoxesByIndex.Length)
			{
				return null;
			}
			return m_collisionBoxesByIndex[num];
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			int num = CalculateIndex(value);
			if (num < m_blockMeshesByIndex.Length)
			{
				generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByIndex[num], Color.White, null, geometry.SubsetOpaque);
				generator.GenerateWireVertices(value, x, y, z, GetFace(value), 0.25f, Vector2.Zero, geometry.SubsetOpaque);
			}
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
		}

		public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
		{
			return new SwitchElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)), value);
		}

		public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
		{
			int face2 = GetFace(value);
			if (face == face2 && SubsystemElectricity.GetConnectorDirection(face2, 0, connectorFace).HasValue)
			{
				return ElectricConnectorType.Output;
			}
			return null;
		}

		public int CalculateIndex(int value)
		{
			int face = GetFace(value);
			bool leverState = GetLeverState(value);
			return (face << 1) | (leverState ? 1 : 0);
		}
	}
}

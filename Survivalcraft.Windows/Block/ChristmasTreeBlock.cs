using Engine;
using Engine.Graphics;

namespace Game
{
	public class ChristmasTreeBlock : Block, IElectricElementBlock
	{
		public static int Index = 63;

		public BlockMesh m_standaloneBlockMesh = new();

		public BlockMesh m_leavesBlockMesh = new();

		public BlockMesh m_standTrunkBlockMesh = new();

		public BlockMesh m_decorationsBlockMesh = new();

		public BlockMesh m_litDecorationsBlockMesh = new();

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/ChristmasTree");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("StandTrunk").ParentBone);
			Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Leaves").ParentBone);
			Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Decorations").ParentBone);
			Color color = BlockColorsMap.SpruceLeaves.Lookup(4, 15);
			m_leavesBlockMesh.AppendModelMeshPart(model.FindMesh("Leaves").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
			m_standTrunkBlockMesh.AppendModelMeshPart(model.FindMesh("StandTrunk").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_decorationsBlockMesh.AppendModelMeshPart(model.FindMesh("Decorations").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_litDecorationsBlockMesh.AppendModelMeshPart(model.FindMesh("Decorations").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: true, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("StandTrunk").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -1f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Leaves").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -1f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, color);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Decorations").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -1f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			Color color = BlockColorsMap.SpruceLeaves.Lookup(generator.Terrain, x, y, z);
			if (GetLightState(Terrain.ExtractData(value)))
			{
				generator.GenerateMeshVertices(this, x, y, z, m_standTrunkBlockMesh, Color.White, null, geometry.SubsetOpaque);
				generator.GenerateMeshVertices(this, x, y, z, m_litDecorationsBlockMesh, Color.White, null, geometry.SubsetOpaque);
				generator.GenerateMeshVertices(this, x, y, z, m_leavesBlockMesh, color, null, geometry.SubsetAlphaTest);
			}
			else
			{
				generator.GenerateMeshVertices(this, x, y, z, m_standTrunkBlockMesh, Color.White, null, geometry.SubsetOpaque);
				generator.GenerateMeshVertices(this, x, y, z, m_decorationsBlockMesh, Color.White, null, geometry.SubsetOpaque);
				generator.GenerateMeshVertices(this, x, y, z, m_leavesBlockMesh, color, null, geometry.SubsetAlphaTest);
			}
			generator.GenerateWireVertices(value, x, y, z, 4, 0.01f, Vector2.Zero, geometry.SubsetOpaque);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		}

		public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			Color color = BlockColorsMap.SpruceLeaves.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, DefaultTextureSlot);
		}

		public override int GetEmittedLightAmount(int value)
		{
			if (!GetLightState(Terrain.ExtractData(value)))
			{
				return 0;
			}
			return DefaultEmittedLightAmount;
		}

		public override int GetShadowStrength(int value)
		{
			if (!GetLightState(Terrain.ExtractData(value)))
			{
				return DefaultShadowStrength;
			}
			return -99;
		}

		public static bool GetLightState(int data)
		{
			return (data & 1) != 0;
		}

		public static int SetLightState(int data, bool state)
		{
			if (!state)
			{
				return data & -2;
			}
			return data | 1;
		}

		public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
		{
			return new ChristmasTreeElectricElement(subsystemElectricity, new CellFace(x, y, z, 4), value);
		}

		public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
		{
			if (face == 4 && SubsystemElectricity.GetConnectorDirection(4, 0, connectorFace).HasValue)
			{
				return ElectricConnectorType.Input;
			}
			return null;
		}

		public int GetConnectionMask(int value)
		{
			return int.MaxValue;
		}
	}
}

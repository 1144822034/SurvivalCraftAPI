using Engine;
using Engine.Graphics;
using System;

namespace Game
{
	public abstract class LadderBlock : Block
	{
		public string m_modelName;

		public float m_offset;

		public BlockMesh m_standaloneBlockMesh = new();

		public BlockMesh[] m_blockMeshesByData = new BlockMesh[4];

		public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[4][];

		public LadderBlock(string modelName, float offset)
		{
			m_modelName = modelName;
			m_offset = offset;
		}

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>(m_modelName);
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Ladder").ParentBone);
			for (int i = 0; i < 4; i++)
			{
				m_blockMeshesByData[i] = new BlockMesh();
				Matrix m = Matrix.CreateTranslation(0f, 0f, 0f - (0.5f - m_offset)) * Matrix.CreateRotationY(i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				m_blockMeshesByData[i].AppendModelMeshPart(model.FindMesh("Ladder").MeshParts[0], boneAbsoluteTransform * m, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_blockMeshesByData[i].GenerateSidesData();
				m_collisionBoxesByData[i] = new BoundingBox[1]
				{
					m_blockMeshesByData[i].CalculateBoundingBox()
				};
			}
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Ladder").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_blockMeshesByData.Length)
			{
				generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByData[num], Color.White, null, geometry.SubsetOpaque);
			}
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default;
			result.Value = (raycastResult.CellFace.Face < 4) ? Terrain.MakeBlockValue(BlockIndex, 0, SetFace(0, raycastResult.CellFace.Face)) : 0;
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_collisionBoxesByData.Length)
			{
				return m_collisionBoxesByData[num];
			}
			return base.GetCustomCollisionBoxes(terrain, value);
		}

		public static int GetFace(int data)
		{
			return data & 3;
		}

		public static int SetFace(int data, int face)
		{
			return (data & -4) | (face & 3);
		}

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd)
        {
            isEnd = true;
			int data = Terrain.ExtractData(value);
            return pistonFace == GetFace(data);
        }
    }
}

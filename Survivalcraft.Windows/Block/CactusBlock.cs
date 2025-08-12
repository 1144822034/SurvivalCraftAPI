using Engine;
using Engine.Graphics;

namespace Game
{
	public class CactusBlock : Block
	{
		public static int Index = 127;

		public BlockMesh m_blockMesh = new();

		public BlockMesh m_standaloneBlockMesh = new();

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Cactus");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Cactus").ParentBone);
			m_blockMesh.AppendModelMeshPart(model.FindMesh("Cactus").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Cactus").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMesh, Color.White, null, geometry.SubsetAlphaTest);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		}

		public override bool ShouldAvoid(int value)
		{
			return true;
		}
        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd)
        {
            isEnd = false;
            return false;
        }
    }
}

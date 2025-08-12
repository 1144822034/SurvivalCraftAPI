using Engine;
using Engine.Graphics;

namespace Game
{
	public abstract class IngotBlock : Block
	{
		public string m_meshName;

		public BlockMesh m_standaloneBlockMesh = new();

		public IngotBlock(string meshName)
		{
			m_meshName = meshName;
		}

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Ingots");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_meshName).ParentBone);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh(m_meshName).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.1f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
		}
	}
}

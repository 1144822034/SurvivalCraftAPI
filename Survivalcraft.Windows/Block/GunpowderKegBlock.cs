using Engine;
using Engine.Graphics;

namespace Game
{
	public abstract class GunpowderKegBlock : Block, IElectricElementBlock
	{
		public BlockMesh m_standaloneBlockMesh = new();

		public BlockMesh m_blockMesh = new();

		public BoundingBox[] m_collisionBoxes;

		public string m_modelName;

		public bool m_isIncendiary;

		public Vector3 FuseOffset
		{
			get;
			set;
		}

		public GunpowderKegBlock(string modelName, bool isIncendiary)
		{
			m_modelName = modelName;
			m_isIncendiary = isIncendiary;
		}

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>(m_modelName);
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Keg").ParentBone);
			FuseOffset = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Fuse").ParentBone).Translation + new Vector3(0.5f, 0f, 0.5f);
			var blockMesh = new BlockMesh();
			blockMesh.AppendModelMeshPart(model.FindMesh("Keg").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMesh.AppendBlockMesh(blockMesh);
			if (m_isIncendiary)
			{
				m_blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(-0.25f, 0f, 0f));
			}
			m_collisionBoxes = new BoundingBox[1]
			{
				blockMesh.CalculateBoundingBox()
			};
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Keg").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			if (m_isIncendiary)
			{
				m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(-0.25f, 0f, 0f));
			}
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMesh, Color.White, null, geometry.SubsetOpaque);
			generator.GenerateWireVertices(value, x, y, z, 4, 0.25f, Vector2.Zero, geometry.SubsetOpaque);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}

		public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
		{
			return new GunpowderKegElectricElement(subsystemElectricity, new CellFace(x, y, z, 4));
		}

		public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
		{
			if (face == 4)
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

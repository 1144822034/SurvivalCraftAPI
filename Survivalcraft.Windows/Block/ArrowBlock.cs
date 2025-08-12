using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;

namespace Game
{
	public class ArrowBlock : Block
	{
		public enum ArrowType
		{
			WoodenArrow,
			StoneArrow,
			IronArrow,
			DiamondArrow,
			FireArrow,
			IronBolt,
			DiamondBolt,
			ExplosiveBolt,
			CopperArrow
		}

		public static int Index = 192;

		public List<BlockMesh> m_standaloneBlockMeshes = [];

		public static int[] m_order = new int[9]
		{
			0,
			1,
			8,
			2,
			3,
			4,
			5,
			6,
			7
		};

		public static string[] m_tipNames = new string[9]
		{
			"ArrowTip",
			"ArrowTip",
			"ArrowTip",
			"ArrowTip",
			"ArrowFireTip",
			"BoltTip",
			"BoltTip",
			"BoltExplosiveTip",
			"ArrowTip"
		};

		public static int[] m_tipTextureSlots = new int[9]
		{
			47,
			1,
			63,
			182,
			62,
			63,
			182,
			183,
			79
		};

		public static string[] m_shaftNames = new string[9]
		{
			"ArrowShaft",
			"ArrowShaft",
			"ArrowShaft",
			"ArrowShaft",
			"ArrowShaft",
			"BoltShaft",
			"BoltShaft",
			"BoltShaft",
			"ArrowShaft"
		};

		public static int[] m_shaftTextureSlots = new int[9]
		{
			4,
			4,
			4,
			4,
			4,
			63,
			63,
			63,
			4
		};

		public static string[] m_stabilizerNames = new string[9]
		{
			"ArrowStabilizer",
			"ArrowStabilizer",
			"ArrowStabilizer",
			"ArrowStabilizer",
			"ArrowStabilizer",
			"BoltStabilizer",
			"BoltStabilizer",
			"BoltStabilizer",
			"ArrowStabilizer"
		};

		public static int[] m_stabilizerTextureSlots = new int[9]
		{
			15,
			15,
			15,
			15,
			15,
			63,
			63,
			63,
			15
		};

		public static float[] m_offsets = new float[9]
		{
			-0.5f,
			-0.5f,
			-0.5f,
			-0.5f,
			-0.5f,
			-0.3f,
			-0.3f,
			-0.3f,
			-0.5f
		};

		public static float[] m_weaponPowers = new float[9]
		{
			5f,
			7f,
			14f,
			18f,
			4f,
			28f,
			36f,
			8f,
			10f
		};

		public static float[] m_iconViewScales = new float[9]
		{
			0.8f,
			0.8f,
			0.8f,
			0.8f,
			0.8f,
			1.1f,
			1.1f,
			1.1f,
			0.8f
		};

		public static float[] m_explosionPressures = new float[9]
		{
			0f,
			0f,
			0f,
			0f,
			0f,
			0f,
			0f,
			40f,
			0f
		};

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Arrows");
			foreach (int enumValue in EnumUtils.GetEnumValues(typeof(ArrowType)))
			{
				if (enumValue > 15)
				{
					throw new InvalidOperationException("Too many arrow types.");
				}
				Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_shaftNames[enumValue]).ParentBone);
				Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_stabilizerNames[enumValue]).ParentBone);
				Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(m_tipNames[enumValue]).ParentBone);
				var blockMesh = new BlockMesh();
				blockMesh.AppendModelMeshPart(model.FindMesh(m_tipNames[enumValue]).MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(m_tipTextureSlots[enumValue] % 16 / 16f, m_tipTextureSlots[enumValue] / 16 / 16f, 0f));
				var blockMesh2 = new BlockMesh();
				blockMesh2.AppendModelMeshPart(model.FindMesh(m_shaftNames[enumValue]).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				blockMesh2.TransformTextureCoordinates(Matrix.CreateTranslation(m_shaftTextureSlots[enumValue] % 16 / 16f, m_shaftTextureSlots[enumValue] / 16 / 16f, 0f));
				var blockMesh3 = new BlockMesh();
				blockMesh3.AppendModelMeshPart(model.FindMesh(m_stabilizerNames[enumValue]).MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, m_offsets[enumValue], 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
				blockMesh3.TransformTextureCoordinates(Matrix.CreateTranslation(m_stabilizerTextureSlots[enumValue] % 16 / 16f, m_stabilizerTextureSlots[enumValue] / 16 / 16f, 0f));
				var blockMesh4 = new BlockMesh();
				blockMesh4.AppendBlockMesh(blockMesh);
				blockMesh4.AppendBlockMesh(blockMesh2);
				blockMesh4.AppendBlockMesh(blockMesh3);
				m_standaloneBlockMeshes.Add(blockMesh4);
			}
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
			if (arrowType >= 0 && arrowType < m_standaloneBlockMeshes.Count)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[arrowType], color, 2f * size, ref matrix, environmentData);
			}
		}

		public override float GetProjectilePower(int value)
		{
			int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
			if (arrowType < 0 || arrowType >= m_weaponPowers.Length)
			{
				return 0f;
			}
			return m_weaponPowers[arrowType];
		}

		public override float GetExplosionPressure(int value)
		{
			int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
			if (arrowType < 0 || arrowType >= m_explosionPressures.Length)
			{
				return 0f;
			}
			return m_explosionPressures[arrowType];
		}

		public override float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData)
		{
			int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
			if (arrowType < 0 || arrowType >= m_iconViewScales.Length)
			{
				return 1f;
			}
			return m_iconViewScales[arrowType];
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			int i = 0;
			while (i < m_order.Length)
			{
				yield return Terrain.MakeBlockValue(BlockIndex, 0, SetArrowType(0, (ArrowType)m_order[i]));
				int num = i + 1;
				i = num;
			}
		}

		public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			int arrowType = (int)GetArrowType(Terrain.ExtractData(value));
			if (arrowType < 0 || arrowType >= Enum.GetValues(typeof(ArrowType)).Length)
			{
				return string.Empty;
			}
			return LanguageControl.Get(GetType().Name, arrowType);
		}

		public static ArrowType GetArrowType(int data)
		{
			return (ArrowType)(data & 0xF);
		}

		public static int SetArrowType(int data, ArrowType arrowType)
		{
			return (data & -16) | (int)(arrowType & (ArrowType)15);
		}
	}
}

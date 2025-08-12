using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game
{
	public class GravestoneBlock : Block
	{
		public static int Index = 189;

		public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[16];

		public BlockMesh[] m_blockMeshes = new BlockMesh[16];

		public BoundingBox[][] m_collisionBoxes = new BoundingBox[16][];

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Graves");
			for (int i = 0; i < 16; i++)
			{
				int variant = GetVariant(i);
				float radians = (GetRotation(i) == 0) ? 0f : ((float)Math.PI / 2f);
				string name = "Grave" + ((variant % 4) + 1).ToString(CultureInfo.InvariantCulture);
				bool num = variant >= 4;
				Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
				m_blockMeshes[i] = new BlockMesh();
				m_blockMeshes[i].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_standaloneBlockMeshes[i] = new BlockMesh();
				m_standaloneBlockMeshes[i].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (num)
				{
					Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Plinth").ParentBone);
					m_blockMeshes[i].AppendModelMeshPart(model.FindMesh("Plinth").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_standaloneBlockMeshes[i].AppendModelMeshPart(model.FindMesh("Plinth").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				}
				m_collisionBoxes[i] = new BoundingBox[1]
				{
					m_blockMeshes[i].CalculateBoundingBox()
				};
			}
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_blockMeshes.Length)
			{
				int num2 = Terrain.ExtractContents((y > 0) ? generator.Terrain.GetCellValueFast(x, y - 1, z) : 0);
				bool num3 = BlocksManager.Blocks[num2].DigMethod != BlockDigMethod.Shovel;
				bool flag = num2 == 7 || num2 == 4 || num2 == 52;
				int num4 = (int)(MathUtils.Hash((uint)(x + (172 * y) + (18271 * z))) & 0xFFFF);
				Matrix value2 = Matrix.Identity;
				if (!num3)
				{
					float radians = 0.2f * ((num4 % 16 / 7.5f) - 1f);
					float radians2 = 0.1f * (((num4 >> 4) % 16 / 7.5f) - 1f);
					value2 = (GetRotation(num) != 0) ? (Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationZ(radians) * Matrix.CreateRotationY(radians2) * Matrix.CreateTranslation(0.5f, 0f, 0.5f)) : (Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationX(radians) * Matrix.CreateRotationY(radians2) * Matrix.CreateTranslation(0.5f, 0f, 0.5f));
				}
				float f = num3 ? 0f : MathUtils.Sqr((num4 >> 8) % 16 / 15f);
				generator.GenerateMeshVertices(color: (!flag) ? Color.Lerp(Color.White, new Color(255, 233, 199), f) : Color.Lerp(new Color(217, 206, 123), new Color(229, 206, 123), f), block: this, x: x, y: y, z: z, blockMesh: m_blockMeshes[num], matrix: value2, subset: geometry.SubsetOpaque);
			}
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_blockMeshes.Length)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[num], color, size, ref matrix, environmentData);
			}
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_collisionBoxes.Length)
			{
				return m_collisionBoxes[num];
			}
			return base.GetCustomCollisionBoxes(terrain, value);
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			int data = Terrain.ExtractData(value);
			Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
			float num = MathF.Abs(Vector3.Dot(forward, Vector3.UnitX));
			BlockPlacementData result;
			if (MathF.Abs(Vector3.Dot(forward, Vector3.UnitZ)) > num)
			{
				result = default;
				result.Value = Terrain.MakeBlockValue(189, 0, SetRotation(data, 0));
				result.CellFace = raycastResult.CellFace;
				return result;
			}
			result = default;
			result.Value = Terrain.MakeBlockValue(189, 0, SetRotation(data, 1));
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			int i = 0;
			while (i < 8)
			{
				int data = SetVariant(0, i);
				yield return Terrain.MakeBlockValue(189, 0, data);
				int num = i + 1;
				i = num;
			}
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			showDebris = true;
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(189, 0, Terrain.ExtractData(oldValue)),
				Count = 1
			});
		}

		public static int GetRotation(int data)
		{
			return (data & 8) >> 3;
		}

		public static int SetRotation(int data, int rotation)
		{
			return (data & -9) | ((rotation << 3) & 8);
		}

		public static int GetVariant(int data)
		{
			return data & 7;
		}

		public static int SetVariant(int data, int variant)
		{
			return (data & -8) | (variant & 7);
		}
	}
}

using Engine;
using Engine.Graphics;
using System.Collections.Generic;
using System.Globalization;

namespace Game
{
	public class PumpkinSoupBucketBlock : BucketBlock
	{
		public static int Index = 251;

		public BlockMesh m_standaloneBlockMesh = new();

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/FullBucket");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bucket").ParentBone);
			Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Contents").ParentBone);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Contents").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(200, 130, 35));
			m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.0625f, 0.4375f, 0f));
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Bucket").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			base.Initialize();
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
		}

		public override int GetDamageDestructionValue(int value)
		{
			return 252;
		}

		public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
		{
			int isDead = 0;
			while (isDead <= 1)
			{
				int num;
				for (int rot = 0; rot <= 1; rot = num)
				{
					var craftingRecipe = new CraftingRecipe
					{
						ResultCount = 1,
						ResultValue = 251,
						RequiredHeatLevel = 1f,
						Description = "Åëâ¿ÄÏ¹ÏÖà"
					};
					int data = BasePumpkinBlock.SetIsDead(BasePumpkinBlock.SetSize(0, 7), isDead != 0);
					int value = SetDamage(Terrain.MakeBlockValue(131, 0, data), rot);
					craftingRecipe.Ingredients[0] = "pumpkin:" + Terrain.ExtractData(value).ToString(CultureInfo.InvariantCulture);
					craftingRecipe.Ingredients[1] = "waterbucket";
					yield return craftingRecipe;
					num = rot + 1;
				}
				num = isDead + 1;
				isDead = num;
			}
		}
	}
}

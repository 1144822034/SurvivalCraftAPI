using Engine;
using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
	public class SeedsBlock : FlatBlock
	{
		public enum SeedType
		{
			TallGrass,
			RedFlower,
			PurpleFlower,
			WhiteFlower,
			WildRye,
			Rye,
			Cotton,
			Pumpkin
		}

		public static int Index = 173;

		public override IEnumerable<int> GetCreativeValues()
		{
			var list = new List<int>();
			foreach (int enumValue in EnumUtils.GetEnumValues(typeof(SeedType)))
			{
				list.Add(Terrain.MakeBlockValue(173, 0, enumValue));
			}
			return list;
		}

		public override int GetFaceTextureSlot(int face, int value)
		{
			int num = Terrain.ExtractData(value);
			if (num == 5 || num == 4)
			{
				return 74;
			}
			return 75;
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default;
			result.CellFace = raycastResult.CellFace;
			if (raycastResult.CellFace.Face == 4)
			{
				switch (Terrain.ExtractData(value))
				{
					case 0:
						result.Value = Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, isSmall: true));
						break;
					case 1:
						result.Value = Terrain.MakeBlockValue(20, 0, FlowerBlock.SetIsSmall(0, isSmall: true));
						break;
					case 2:
						result.Value = Terrain.MakeBlockValue(24, 0, FlowerBlock.SetIsSmall(0, isSmall: true));
						break;
					case 3:
						result.Value = Terrain.MakeBlockValue(25, 0, FlowerBlock.SetIsSmall(0, isSmall: true));
						break;
					case 4:
						result.Value = Terrain.MakeBlockValue(174, 0, RyeBlock.SetSize(RyeBlock.SetIsWild(0, isWild: false), 0));
						break;
					case 5:
						result.Value = Terrain.MakeBlockValue(174, 0, RyeBlock.SetSize(RyeBlock.SetIsWild(0, isWild: false), 0));
						break;
					case 6:
						result.Value = Terrain.MakeBlockValue(204, 0, CottonBlock.SetSize(CottonBlock.SetIsWild(0, isWild: false), 0));
						break;
					case 7:
						result.Value = Terrain.MakeBlockValue(131, 0, BasePumpkinBlock.SetSize(BasePumpkinBlock.SetIsDead(0, isDead: false), 0));
						break;
				}
			}
			return result;
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			switch (Terrain.ExtractData(value))
			{
				case 0:
					color *= new Color(160, 150, 125);
					break;
				case 1:
					color *= new Color(192, 160, 160);
					break;
				case 2:
					color *= new Color(192, 160, 192);
					break;
				case 3:
					color *= new Color(192, 192, 192);
					break;
				case 4:
					color *= new Color(60, 138, 76);
					break;
				case 6:
					color *= new Color(255, 255, 255);
					break;
				case 7:
					color *= new Color(240, 225, 190);
					break;
			}
			BlocksManager.DrawFlatOrImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
		}
	}
}

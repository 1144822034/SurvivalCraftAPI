using Engine;
using Engine.Graphics;
using Engine.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public class ClothingBlock : Block
	{
		public static int Index = 203;

		public Dictionary<int, ClothingData> m_clothingData = [];

		public BlockMesh m_innerMesh;

		public int num = 0;

		public BlockMesh m_outerMesh;

		public static Matrix[] m_slotTransforms = new Matrix[4]
		{
			Matrix.CreateTranslation(0f, -1.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -1.1f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.1f, 0f) * Matrix.CreateScale(2.7f)
		};

		public virtual void LoadClothingData(XElement item)
		{
			if (item.Name.LocalName == "ClothingData")
			{
				int.TryParse(item.Attribute("Index").Value, out int ClothIndex);
				ClothIndex &= 0x3FF;
				ClothingData clothingData = new ClothingData(item);
				string className = item.Attribute("Class")?.Value ?? typeof(ClothingData).FullName;
				if(!string.IsNullOrEmpty(className))
				{
					try
					{
						Type type = TypeCache.FindType(className,false,true);
						clothingData = (ClothingData)Activator.CreateInstance(type: type,args: new object[] { item });
						if(clothingData == null) throw new Exception("ClothingData is not assignable to Game.ClothingData.");
					}
					catch(Exception ex)
					{
						Log.Error("ClothingData from class " + className + " create failed! " + ex);
					}
				}
				clothingData.DisplayIndex = num;
				m_clothingData[ClothIndex] = clothingData;
			}
			num++;
			foreach (XElement xElement1 in item.Elements())
			{
				LoadClothingData(xElement1);
			}
		}

		public override void Initialize()
		{
			num = 0;
			XElement xElement = null;
			ModsManager.ModListAllDo((modEntity) => { modEntity.LoadClo(this, ref xElement); });
			LoadClothingData(xElement);
			Model playerModel = CharacterSkinsManager.GetPlayerModel(PlayerClass.Male);
			var array = new Matrix[playerModel.Bones.Count];
			playerModel.CopyAbsoluteBoneTransformsTo(array);
			int index = playerModel.FindBone("Hand1").Index;
			int index2 = playerModel.FindBone("Hand2").Index;
			array[index] = Matrix.CreateRotationY(0.1f) * array[index];
			array[index2] = Matrix.CreateRotationY(-0.1f) * array[index2];
			m_innerMesh = new BlockMesh();
			foreach (ModelMesh mesh in playerModel.Meshes)
			{
				Matrix matrix = array[mesh.ParentBone.Index];
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					Color color = Color.White * 0.8f;
					color.A = byte.MaxValue;
					m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color);
				}
			}
			Model outerClothingModel = CharacterSkinsManager.GetOuterClothingModel(PlayerClass.Male);
			var array2 = new Matrix[outerClothingModel.Bones.Count];
			outerClothingModel.CopyAbsoluteBoneTransformsTo(array2);
			int index3 = outerClothingModel.FindBone("Leg1").Index;
			int index4 = outerClothingModel.FindBone("Leg2").Index;
			array2[index3] = Matrix.CreateTranslation(-0.02f, 0f, 0f) * array2[index3];
			array2[index4] = Matrix.CreateTranslation(0.02f, 0f, 0f) * array2[index4];
			m_outerMesh = new BlockMesh();
			foreach (ModelMesh mesh2 in outerClothingModel.Meshes)
			{
				Matrix matrix2 = array2[mesh2.ParentBone.Index];
				foreach (ModelMeshPart meshPart2 in mesh2.MeshParts)
				{
					Color color2 = Color.White * 0.8f;
					color2.A = byte.MaxValue;
					m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color2);
				}
			}
			base.Initialize();
		}

		public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			int data = Terrain.ExtractData(value);
			ClothingData clothingData = GetClothingData(value);
			if(clothingData == null)
			{
				return String.Empty;
			}
			int clothingColor = GetClothingColor(data);
			string displayName = clothingData.DisplayName;
			if (clothingColor != 0)
			{
				return SubsystemPalette.GetName(subsystemTerrain, clothingColor, displayName);
			}
			return displayName;
		}

		public override string GetDescription(int value)
		{
			int data = Terrain.ExtractData(value);
			ClothingData clothingData = GetClothingData(value);
			if (clothingData == null)
			{
				return String.Empty;
			}
			return clothingData.Description;
		}

		public override string GetCategory(int value)
		{
			if (GetClothingColor(Terrain.ExtractData(value)) == 0)
			{
				return base.GetCategory(value);
			}
			return "Dyed";
		}

		public override int GetDamage(int value)
		{
			return (Terrain.ExtractData(value) >> 8) & 0xF;
		}
		public override int GetDisplayOrder(int value)
		{
			return GetClothingData(value)?.DisplayIndex ?? int.MaxValue;
		}

		public override int SetDamage(int value, int damage)
		{
			int num = Terrain.ExtractData(value);
			num = (num & -3841) | ((damage & 0xF) << 8);
			return Terrain.ReplaceData(value, num);
		}
		public override bool CanWear(int value)
		{
			return true;
		}
		public override ClothingData GetClothingData(int value)
		{
			int data = Terrain.ExtractData(value);
			int num = GetClothingIndex(data);
			if(m_clothingData.TryGetValue(num,out ClothingData clothingData))
			{
				return clothingData;
			}
			return null;
		}
		public override IEnumerable<int> GetCreativeValues()
		{
            foreach (ClothingData clothingData in m_clothingData.Values.ToList().OrderBy((ClothingData cd) => cd.DisplayIndex))
			{
                if (clothingData == null) continue;
                int colorsCount = (!clothingData.CanBeDyed) ? 1 : 16;
                int color = 0;
                while (color < colorsCount)
                {
                    int data = SetClothingColor(SetClothingIndex(0, clothingData.Index), color);
                    yield return Terrain.MakeBlockValue(203, 0, data);
                    int num = color + 1;
                    color = num;
                }
            }
        }

		public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel)
		{
			if (heatLevel < 1f)
			{
				return null;
			}
			var list = ingredients.Where((string i) => !string.IsNullOrEmpty(i)).ToList();
			if (list.Count == 2)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				foreach (string item in list)
				{
					CraftingRecipesManager.DecodeIngredient(item, out string craftingId, out int? data);
					if (craftingId == BlocksManager.Blocks[203].CraftingId)
					{
						num3 = Terrain.MakeBlockValue(203, 0, data ?? 0);
					}
					else if (craftingId == BlocksManager.Blocks[129].CraftingId)
					{
						num = Terrain.MakeBlockValue(129, 0, data ?? 0);
					}
					else if (craftingId == BlocksManager.Blocks[128].CraftingId)
					{
						num2 = Terrain.MakeBlockValue(128, 0, data ?? 0);
					}
				}
				if (num != 0 && num3 != 0)
				{
					int data2 = Terrain.ExtractData(num3);
					int clothingColor = GetClothingColor(data2);
					int clothingIndex = GetClothingIndex(data2);
					bool canBeDyed = GetClothingData(data2)?.CanBeDyed ?? false;
					int damage = BlocksManager.Blocks[203].GetDamage(num3);
					int color = PaintBucketBlock.GetColor(Terrain.ExtractData(num));
					int damage2 = BlocksManager.Blocks[129].GetDamage(num);
					Block block = BlocksManager.Blocks[129];
					Block block2 = BlocksManager.Blocks[203];
					if (!canBeDyed)
					{
						return null;
					}
					int num4 = PaintBucketBlock.CombineColors(clothingColor, color);
					if (num4 != clothingColor)
					{
						return new CraftingRecipe
						{
							ResultCount = 1,
							ResultValue = block2.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, num4), clothingIndex)), damage),
							RemainsCount = 1,
							RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, color), damage2 + MathUtils.Max(block.Durability / 4, 1)),
							RequiredHeatLevel = 1f,
							Description = $"{LanguageControl.Get("BlocksManager", "Dyed")} {SubsystemPalette.GetName(terrain, color, null)}",
							Ingredients = (string[])ingredients.Clone()
						};
					}
				}
				if (num2 != 0 && num3 != 0)
				{
					int data3 = Terrain.ExtractData(num3);
					int clothingColor2 = GetClothingColor(data3);
					int clothingIndex2 = GetClothingIndex(data3);
					bool canBeDyed2 = GetClothingData(data3)?.CanBeDyed ?? false;
					int damage3 = BlocksManager.Blocks[203].GetDamage(num3);
					int damage4 = BlocksManager.Blocks[128].GetDamage(num2);
					Block block3 = BlocksManager.Blocks[128];
					Block block4 = BlocksManager.Blocks[203];
					if (!canBeDyed2)
					{
						return null;
					}
					if (clothingColor2 != 0)
					{
						return new CraftingRecipe
						{
							ResultCount = 1,
							ResultValue = block4.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, 0), clothingIndex2)), damage3),
							RemainsCount = 1,
							RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(128, 0, 0), damage4 + MathUtils.Max(block3.Durability / 4, 1)),
							RequiredHeatLevel = 1f,
							Description = LanguageControl.Get("BlocksManager", "Not Dyed") + " " + LanguageControl.Get("BlocksManager", "Clothes"),
							Ingredients = (string[])ingredients.Clone()
						};
					}
				}
			}
			return null;
		}

		//分成了1~4、17~18位储存
		public static int GetClothingIndex(int data)
		{
			return (data & 0xFF) | ((data >> 8) & 0x300);
		}

		public static int SetClothingIndex(int data, int clothingIndex)
		{
			clothingIndex &= 0x3FF;
            return (data & -196864) | (clothingIndex & 0xFF) | ((clothingIndex & 0x300) << 8);
		}

		public static int GetClothingColor(int data)
		{
			return (data >> 12) & 0xF;
		}

		public static int SetClothingColor(int data, int color)
		{
			return (data & -61441) | ((color & 0xF) << 12);
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int data = Terrain.ExtractData(value);
			int clothingColor = GetClothingColor(data);
			ClothingData clothingData = GetClothingData(value);
			if (clothingData == null)
			{
				return;
			}
			Matrix matrix2 = m_slotTransforms[(int)clothingData.Slot] * Matrix.CreateScale(size) * matrix;
			if (clothingData.IsOuter)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_outerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
			}
			else
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_innerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
			}
		}
	}
}

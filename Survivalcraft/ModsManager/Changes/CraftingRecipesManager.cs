using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Xml.Linq;
using XmlUtilities;
using Engine;
using Engine.Graphics;
using Engine.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class CraftingRecipesManager
	{
		public static List<CraftingRecipe> m_recipes = [];
		public static List<CraftingRecipe> Recipes => m_recipes;
		public static string fName = "CraftingRecipesManager";
		/// <summary>
		/// 启用等级限制
		/// Mod在初始化时，设置为false可以让物品合成不受玩家等级限制
		/// </summary>
		[Obsolete("建议在CraftingRecipesManagerInitialize时，对需要等级清空的配方，将其RequiredPlayerLevel设置为0")]
		public static bool EnableLevelRestrictions = true;
		public static void Initialize()
		{
			m_recipes.Clear();
			XElement source = null;
			foreach (ModEntity modEntity in ModsManager.ModList)
			{
				modEntity.LoadCr(ref source);
			}
			LoadData(source);
			Block[] blocks = BlocksManager.Blocks;
			foreach (Block block in blocks)
			{
				m_recipes.AddRange(block.GetProceduralCraftingRecipes());
			}
			bool sort = true;
			ModsManager.HookAction("CraftingRecipesManagerInitialize", loader =>
			{
				loader.CraftingRecipesManagerInitialize(m_recipes, ref sort);
				return false;
			});
			if(sort) m_recipes.Sort(delegate (CraftingRecipe r1, CraftingRecipe r2)
			{
				if(r1.DisplayOrder == r2.DisplayOrder)
				{
					int y = r1.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
					int x = r2.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
					return Comparer<int>.Default.Compare(x,y);
				}
				return Comparer<int>.Default.Compare(r1.DisplayOrder, r2.DisplayOrder);
			});
			ModsManager.HookAction("CraftingRecipesManagerInitialized", loader =>
			{
				loader.CraftingRecipesManagerInitialized();
				return false;
			});
		}
		public static void LoadData(XElement item)
		{
            try
            {
                if (ModsManager.HasAttribute(item, (name) => { return name == "Result"; }, out XAttribute xAttribute) == false)
				{
					foreach (XElement xElement in item.Elements())
					{
						LoadData(xElement);
					}
					return;
				}
			
				bool flag = false;
				ModsManager.HookAction("OnCraftingRecipeDecode", modLoader =>
				{
					modLoader.OnCraftingRecipeDecode(m_recipes, item, out flag);
					return flag;
				});
				if (flag == false)
				{
					CraftingRecipe craftingRecipe = DecodeElementToCraftingRecipe(item);
					m_recipes.Add(craftingRecipe);
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

		public static CraftingRecipe DecodeElementToCraftingRecipe(XElement item, int HorizontalLen = 3)
		{
			CraftingRecipe craftingRecipe = null;
			string className = item.Attribute("Class")?.Value ?? typeof(CraftingRecipe).FullName;
			if(!string.IsNullOrEmpty(className))
			{
				try
				{
					Type type = TypeCache.FindType(className,false,true);
					craftingRecipe = (CraftingRecipe)Activator.CreateInstance(type: type,args: new object[] { item });
					if(craftingRecipe == null) throw new Exception("Recipe is not assignable to Game.CraftingRecipe.");
				}
				catch(Exception ex)
				{
					Log.Error("CraftingRecipe from class " + className + " create failed! " + ex);
				}
			}
			return craftingRecipe;
		}
		public static CraftingRecipe FindMatchingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel)
		{
			CraftingContext context = new CraftingContext()
			{
				RowsCount = 3,
				ColumnsCount = 3,
				SubsystemTerrain = terrain,
				Ingredients = ingredients,
				HeatLevel = heatLevel,
				PlayerLevel = playerLevel
			};
			return context.FindMatchingRecipe(context.SearchPossibleRecipes());
		}
		public static int DecodeResult(string result)
		{
			bool flag2 = false;
			int result2 = 0;
			ModsManager.HookAction("DecodeResult", modLoader =>
			{
				result2 = modLoader.DecodeResult(result, out flag2);
				return flag2;
			});
			if (flag2) return result2;
			if (!string.IsNullOrEmpty(result))
			{
				string[] array = result.Split(new char[] { ':' }, StringSplitOptions.None);
				int blockIndex = BlocksManager.GetBlockIndex(array[0], throwIfNotFound: true);
				return Terrain.MakeBlockValue(blockIndex, 0, data: (array.Length == 2) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 0);
			}
			return 0;
		}
		public static void DecodeIngredient(string ingredient, out string craftingId, out int? data)
		{
			bool flag2 = false;
			string craftingId_R = string.Empty;
			int? data_R = null;
			ModsManager.HookAction("DecodeIngredient", modLoader =>
			{
				modLoader.DecodeIngredient(ingredient, out craftingId_R, out data_R, out flag2);
				return flag2;
			});
			if (flag2) { craftingId = craftingId_R; data = data_R; return; }
			string[] array = ingredient.Split(new char[] { ':' }, StringSplitOptions.None);
			craftingId = array[0];
			data = (array.Length >= 2) ? new int?(int.Parse(array[1], CultureInfo.InvariantCulture)) : null;
		}
		[Obsolete]
		public static bool MatchRecipe(string[] requiredIngredients, string[] actualIngredients)
		{
			bool flag2 = false;
			bool result = false;
			ModsManager.HookAction("MatchRecipe", modLoader =>
			{
				result = modLoader.MatchRecipe(requiredIngredients, actualIngredients, out flag2);
				return flag2;
			});
			if (flag2) return result;
			if (actualIngredients.Length > 9) return false;
			string[] array = new string[9];
			for (int i = 0; i < 2; i++)
			{
				bool flip = ((i != 0) ? true : false);
				for (int j = -4; j <= 2; j++)
				{
					for (int k = -4; k <= 2; k++)
					{
						if (!TransformRecipe(array, requiredIngredients, k, j, flip))
						{
							continue;
						}
						bool flag = true;
						for (int l = 0; l < 9; l++)
						{
							if (l == actualIngredients.Length || !CompareIngredients(array[l], actualIngredients[l]))
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		[Obsolete]
		public static bool TransformRecipe(string[] transformedIngredients, string[] ingredients, int shiftX, int shiftY, bool flip)
		{
			for (int i = 0; i < 9; i++)
			{
				transformedIngredients[i] = null;
			}
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					int num = (flip ? (3 - k - 1) : k) + shiftX;
					int num2 = j + shiftY;
					string text = ingredients[k + (j * 3)];
					if (num >= 0 && num2 >= 0 && num < 3 && num2 < 3)
					{
						transformedIngredients[num + (num2 * 3)] = text;
					}
					else if (!string.IsNullOrEmpty(text))
					{
						return false;
					}
				}
			}
			return true;
		}
		[Obsolete]
		public static bool CompareIngredients(string requiredIngredient, string actualIngredient)
		{
			if (requiredIngredient == null)
			{
				return actualIngredient == null;
			}
			if (actualIngredient == null)
			{
				return requiredIngredient == null;
			}
			DecodeIngredient(requiredIngredient, out string craftingId, out int? data);
			DecodeIngredient(actualIngredient, out string craftingId2, out int? data2);
			if (!data2.HasValue)
			{
				throw new InvalidOperationException("Actual ingredient data not specified.");
			}
			if (craftingId == craftingId2)
			{
				if (!data.HasValue)
				{
					return true;
				}
				return data.Value == data2.Value;
			}
			return false;
		}
	}
}

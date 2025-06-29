using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplatesDatabase;
namespace Game
{
	public class CraftingContext
	{
		public SubsystemTerrain SubsystemTerrain;
		public int RowsCount { get; set; } = 3;
		public int ColumnsCount { get; set; } = 3;
		/// <summary>
		/// 原版解析配方只能用这个长度为9的数组
		/// </summary>
		public virtual string[] Ingredients { get; set; }
		public virtual float HeatLevel { get; set; }
		public virtual float PlayerLevel { get; set; }
		public ValuesDictionary ValuesDictionary { get; set; } = new();
		public virtual string GetIngredient(int row, int col)
		{
			if(row < 0 || col < 0 || row >= RowsCount || col >= ColumnsCount) return null;
			return Ingredients[row * ColumnsCount + col];
		}
		public virtual List<CraftingRecipe> SearchPossibleRecipes()
		{
			List<CraftingRecipe> list = new List<CraftingRecipe>();
			if(Ingredients.All((string s) => string.IsNullOrEmpty(s)))
			{
				return list;
			}
			Block[] blocks = BlocksManager.Blocks;
			for(int i = 0; i < blocks.Length; i++)
			{
				CraftingRecipe adHocCraftingRecipe = blocks[i].GetAdHocCraftingRecipe(this);
				if(adHocCraftingRecipe != null)
				{
					list.Add(adHocCraftingRecipe);
				}
			}
			list.AddRange(CraftingRecipesManager.Recipes);
			return list;
		}
		public virtual CraftingRecipe FindMatchingRecipe(IEnumerable<CraftingRecipe> areaToMatch)
		{
			CraftingRecipe craftingRecipe = null;
			foreach(CraftingRecipe recipe in areaToMatch)
			{
				try
				{
					if(recipe.IsMatch(this,out CraftingRecipe suitableRecipeForContext))
					{
						craftingRecipe = suitableRecipeForContext;
						return craftingRecipe;
					}
				}
				catch(Exception e)
				{
					Log.Error(e);
				}
			}
			return null;
		}
	}
}

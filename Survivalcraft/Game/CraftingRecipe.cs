using System.Xml.Linq;
using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using TemplatesDatabase;
using XmlUtilities;

namespace Game
{
	public class CraftingRecipe
	{
		public int ResultValue;

		public int ResultCount;

		public int RemainsValue;

		public int RemainsCount;

		public float RequiredHeatLevel;

		public float RequiredPlayerLevel;

		public string[] Ingredients = new string[9];//会在CraftingRecipe(XElement xElement)中重新赋值

		public string Description;

		public string Message;
		/// <summary>
		/// 在配方表中的显示顺序，DisplayOrder越小，配方越靠前
		/// </summary>
		public int DisplayOrder = 0;
		public int RowsCount { get; set; } = 3;
		public int ColumnsCount { get; set; } = 3;
		public ValuesDictionary ValuesDictionary { get; set; } = new();
		public virtual string GetIngredient(int row,int col)
		{
			if(row < 0 || col < 0 || row >= RowsCount || col >= ColumnsCount) return null;
			return Ingredients[row * ColumnsCount + col];
		}
		public CraftingRecipe()
		{
		}
		public CraftingRecipe(XElement xElement)
		{
			string attributeValue = XmlUtils.GetAttributeValue<string>(xElement,"Result");
			string desc = XmlUtils.GetAttributeValue<string>(xElement,"Description");
			if(desc.StartsWith("[") && desc.EndsWith("]") && LanguageControl.TryGetBlock(attributeValue,"CRDescription:" + desc.Substring(1,desc.Length - 2),out var r)) desc = r;
			ResultValue = CraftingRecipesManager.DecodeResult(attributeValue);
			ResultCount = XmlUtils.GetAttributeValue<int>(xElement,"ResultCount");
			string attributeValue2 = XmlUtils.GetAttributeValue(xElement,"Remains",string.Empty);
			if(!string.IsNullOrEmpty(attributeValue2))
			{
				RemainsValue = CraftingRecipesManager.DecodeResult(attributeValue2);
				RemainsCount = XmlUtils.GetAttributeValue<int>(xElement,"RemainsCount");
			}
			RowsCount = XmlUtils.GetAttributeValue<int>(xElement,"RowsCount", 3);
			ColumnsCount = XmlUtils.GetAttributeValue<int>(xElement,"ColumnsCount", 3);
			Ingredients = new string[RowsCount * ColumnsCount];
			RequiredHeatLevel = XmlUtils.GetAttributeValue<float>(xElement,"RequiredHeatLevel");
			RequiredPlayerLevel = XmlUtils.GetAttributeValue(xElement,"RequiredPlayerLevel",1f);
			Description = desc;
			Message = XmlUtils.GetAttributeValue<string>(xElement,"Message",null);
			DisplayOrder = XmlUtils.GetAttributeValue<int>(xElement,"DisplayOrder",0);
			var dictionary = new Dictionary<char,string>();
			foreach(XAttribute item2 in from a in xElement.Attributes()
										where a.Name.LocalName.Length == 1 && char.IsLower(a.Name.LocalName[0])
										select a)
			{
				CraftingRecipesManager.DecodeIngredient(item2.Value,out string craftingId,out int? data);
				if(BlocksManager.FindBlocksByCraftingId(craftingId).Length == 0)
				{
					throw new InvalidOperationException($"Block with craftingId \"{item2.Value}\" not found.");
				}
				if(data.HasValue && (data.Value < 0 || data.Value > 262143))
				{
					throw new InvalidOperationException($"Data in recipe ingredient \"{item2.Value}\" must be between 0 and 0x3FFFF.");
				}
				dictionary.Add(item2.Name.LocalName[0],item2.Value);
			}

			//读取表格
			string[] array = xElement.Value.Trim().Split(new string[] { "\n" },StringSplitOptions.None);
			for(int i = 0; i < array.Length; i++)
			{
				int num = array[i].IndexOf('"');
				int num2 = array[i].LastIndexOf('"');
				if(num < 0 || num2 < 0 || num2 <= num)
				{
					throw new InvalidOperationException("Invalid recipe line.");
				}
				string text = array[i].Substring(num + 1,num2 - num - 1);
				for(int j = 0; j < text.Length; j++)
				{
					char c = text[j];
					if(char.IsLower(c))
					{
						string text2 = dictionary[c];
						Ingredients[(i * ColumnsCount) + j] = text2;
					}
				}
			}
		}
		public virtual bool IsMatch(CraftingContext context, out CraftingRecipe suitableRecipeForContext)
		{
			suitableRecipeForContext = this;
			bool craftingRecipeMatchingResult = MatchRecipeIngredients(context);
			if(!craftingRecipeMatchingResult)
				return false;
			if(context.HeatLevel < RequiredHeatLevel)
			{
				suitableRecipeForContext = (!(context.HeatLevel > 0f)) ? new CraftingRecipe
				{
					Message = LanguageControl.Get(CraftingRecipesManager.fName,0)
				} : new CraftingRecipe
				{
					Message = LanguageControl.Get(CraftingRecipesManager.fName,1)
				};
			}
			else if(context.PlayerLevel < RequiredPlayerLevel)
			{
				suitableRecipeForContext = (!(RequiredHeatLevel > 0f)) ? new CraftingRecipe
				{
					Message = String.Format(LanguageControl.Get(CraftingRecipesManager.fName,2),RequiredPlayerLevel)
				} : new CraftingRecipe
				{
					Message = String.Format(LanguageControl.Get(CraftingRecipesManager.fName,3),RequiredPlayerLevel)
				};
			}
			return true;
		}
		public virtual bool MatchRecipeIngredients(CraftingContext context)
		{
			bool skippedByModLoader = false;
			bool result = false;
			ModsManager.HookAction("MatchRecipe",modLoader => {
				result = modLoader.MatchRecipe(Ingredients,context.Ingredients,out skippedByModLoader);
				return skippedByModLoader;
			});
			if(skippedByModLoader) return result;

			int maxRowsCount = MathUtils.Max(RowsCount,context.RowsCount);
			int maxColumnsCount = MathUtils.Max(ColumnsCount,context.ColumnsCount);
			string[] actualIngredients = new string[maxRowsCount * maxColumnsCount];
			string[] requiredIngredients = new string[maxRowsCount * maxColumnsCount];
			for(int i = 0; i < maxRowsCount; i++)
				for(int j = 0; j < maxColumnsCount; j++)
				{
					requiredIngredients[i * maxColumnsCount + j] = string.Empty;
					actualIngredients[i * maxColumnsCount + j] = string.Empty;
					if(i < RowsCount && j < ColumnsCount)
						requiredIngredients[i * maxColumnsCount + j] = Ingredients[i * ColumnsCount + j];
					if(i < context.RowsCount && i < context.ColumnsCount)
						actualIngredients[i * maxColumnsCount + j] = context.Ingredients[i * context.ColumnsCount + j];
				}
			//为它们赋值
			for(int needToflip = 0; needToflip < 2; needToflip++)
			{
				bool flip = ((needToflip != 0) ? true : false);
				for(int shiftY = -RowsCount + 1; shiftY <= RowsCount - 1; shiftY++)
				{
					for(int shiftX = -ColumnsCount + 1; shiftX <= ColumnsCount - 1; shiftX++)
					{
						if(!TransformRecipe(actualIngredients,maxRowsCount, maxColumnsCount, shiftX, shiftY, flip, out string[] transformedIngredients))
						{
							continue;
						}
						bool flag = true;
						for(int l = 0; l < maxColumnsCount * maxRowsCount; l++)
						{
							if(!CompareIngredients(requiredIngredients[l], transformedIngredients[l]))
							{
								flag = false;
								break;
							}
						}
						if(flag)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="transformedIngredients"></param>
		/// <param name="ingredients"></param>
		/// <param name="shiftX"></param>
		/// <param name="shiftY"></param>
		/// <param name="flip"></param>
		/// <returns></returns>
		public virtual bool TransformRecipe(string[] ingredients, int rowsCount, int columnsCount, int shiftX, int shiftY, bool flip, out string[] transformedIngredients)
		{
			transformedIngredients = new string[rowsCount * columnsCount];
			for(int j = 0; j < rowsCount; j++)
			{
				for(int k = 0; k < columnsCount; k++)
				{
					int num = (flip ? (columnsCount - k - 1) : k) + shiftX;
					int num2 = j + shiftY;
					string text = ingredients[k + (j * columnsCount)];
					if(num >= 0 && num2 >= 0 && num < columnsCount && num2 < rowsCount)
					{
						transformedIngredients[num + (num2 * columnsCount)] = text;
					}
					else if(!string.IsNullOrEmpty(text))
					{
						return false;
					}
				}
			}
			return true;
		}
		public virtual bool CompareIngredients(string requiredIngredient,string actualIngredient)
		{
			if(requiredIngredient == null)
			{
				return actualIngredient == null;
			}
			if(actualIngredient == null)
			{
				return requiredIngredient == null;
			}
			CraftingRecipesManager.DecodeIngredient(requiredIngredient,out string craftingId,out int? data);
			CraftingRecipesManager.DecodeIngredient(actualIngredient,out string craftingId2,out int? data2);
			if(!data2.HasValue)
			{
				throw new InvalidOperationException("Actual ingredient data not specified.");
			}
			if(craftingId == craftingId2)
			{
				if(!data.HasValue)
				{
					return true;
				}
				return data.Value == data2.Value;
			}
			return false;
		}
	}
}

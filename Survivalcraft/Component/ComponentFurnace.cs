using Engine;
using GameEntitySystem;
using System;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentFurnace : ComponentInventoryBase, IUpdateable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemExplosions m_subsystemExplosions;

		public ComponentBlockEntity m_componentBlockEntity;

		public int m_furnaceSize;

		public string[] m_matchedIngredients = new string[9];

		public float m_fireTimeRemaining;

		public float m_heatLevel;

		public bool m_updateSmeltingRecipe;

		public CraftingRecipe m_smeltingRecipe;

		public float m_smeltingProgress;

		public int RemainsSlotIndex => SlotsCount - 1;

		public int ResultSlotIndex => SlotsCount - 2;

		public int FuelSlotIndex => SlotsCount - 3;

		public float HeatLevel => m_heatLevel;

		public float SmeltingProgress => m_smeltingProgress;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override int GetSlotCapacity(int slotIndex, int value)
		{
			if (slotIndex == FuelSlotIndex)
			{
				if (BlocksManager.Blocks[Terrain.ExtractContents(value)].GetFuelHeatLevel(value) > 0f)
				{
					return base.GetSlotCapacity(slotIndex, value);
				}
				return 0;
			}
			return base.GetSlotCapacity(slotIndex, value);
		}

		public override void AddSlotItems(int slotIndex, int value, int count)
		{
			m_updateSmeltingRecipe = true;
			base.AddSlotItems(slotIndex, value, count);
		}

		public override int RemoveSlotItems(int slotIndex, int count)
		{
			m_updateSmeltingRecipe = true;
			return base.RemoveSlotItems(slotIndex, count);
		}

		public void Update(float dt)
		{
			Point3 coordinates = m_componentBlockEntity.Coordinates;
			if (m_heatLevel > 0f)
			{
				m_fireTimeRemaining = MathUtils.Max(0f, m_fireTimeRemaining - dt);
				if (m_fireTimeRemaining == 0f)
				{
					m_heatLevel = 0f;
				}
			}
			if (m_updateSmeltingRecipe)
			{
				m_updateSmeltingRecipe = false;
				float heatLevel = 0f;
				if (m_heatLevel > 0f)
				{
					heatLevel = m_heatLevel;
				}
				else
				{
					Slot slot = m_slots[FuelSlotIndex];
					if (slot.Count > 0)
					{
						int num = Terrain.ExtractContents(slot.Value);
						heatLevel = BlocksManager.Blocks[num].GetFuelHeatLevel(slot.Value);
					}
				}
				CraftingRecipe craftingRecipe = FindSmeltingRecipe(heatLevel);
				if (craftingRecipe != m_smeltingRecipe)
				{
					m_smeltingRecipe = (craftingRecipe != null && craftingRecipe.ResultValue != 0) ? craftingRecipe : null;
					m_smeltingProgress = 0f;
				}
			}
			if (m_smeltingRecipe == null)
			{
				m_heatLevel = 0f;
				m_fireTimeRemaining = 0f;
			}
			if (m_smeltingRecipe != null && m_fireTimeRemaining <= 0f)
			{
				Slot slot2 = m_slots[FuelSlotIndex];
				if (slot2.Count > 0)
				{
					int num2 = Terrain.ExtractContents(slot2.Value);
					Block block = BlocksManager.Blocks[num2];
					if (block.GetExplosionPressure(slot2.Value) > 0f)
					{
						slot2.Count = 0;
						m_subsystemExplosions.TryExplodeBlock(coordinates.X, coordinates.Y, coordinates.Z, slot2.Value);
					}
					else if (block.GetFuelHeatLevel(slot2.Value) > 0f)
					{
						slot2.Count--;
						m_fireTimeRemaining = block.GetFuelFireDuration(slot2.Value);
						m_heatLevel = block.GetFuelHeatLevel(slot2.Value);
					}
				}
			}
			if (m_fireTimeRemaining <= 0f)
			{
				m_smeltingRecipe = null;
				m_smeltingProgress = 0f;
			}
			if (m_smeltingRecipe != null)
			{
				m_smeltingProgress = MathUtils.Min(m_smeltingProgress + (0.15f * dt), 1f);
				if (m_smeltingProgress >= 1f)
				{
					for (int i = 0; i < m_furnaceSize; i++)
					{
						if (m_slots[i].Count > 0)
						{
							m_slots[i].Count--;
						}
					}
					m_slots[ResultSlotIndex].Value = m_smeltingRecipe.ResultValue;
					m_slots[ResultSlotIndex].Count += m_smeltingRecipe.ResultCount;
					if (m_smeltingRecipe.RemainsValue != 0 && m_smeltingRecipe.RemainsCount > 0)
					{
						m_slots[RemainsSlotIndex].Value = m_smeltingRecipe.RemainsValue;
						m_slots[RemainsSlotIndex].Count += m_smeltingRecipe.RemainsCount;
					}
					m_smeltingRecipe = null;
					m_smeltingProgress = 0f;
					m_updateSmeltingRecipe = true;
				}
			}
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(coordinates.X, coordinates.Z);
			if (chunkAtCell != null && chunkAtCell.State == TerrainChunkState.Valid)
			{
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z);
				m_subsystemTerrain.ChangeCell(coordinates.X, coordinates.Y, coordinates.Z, Terrain.ReplaceContents(cellValue, (m_heatLevel > 0f) ? 65 : 64));
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
			m_furnaceSize = SlotsCount - 3;
			if (m_furnaceSize < 1 || m_furnaceSize > 3)
			{
				throw new InvalidOperationException("Invalid furnace size.");
			}
			m_fireTimeRemaining = valuesDictionary.GetValue<float>("FireTimeRemaining");
			m_heatLevel = valuesDictionary.GetValue<float>("HeatLevel");
			m_updateSmeltingRecipe = true;
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			base.Save(valuesDictionary, entityToIdMap);
			valuesDictionary.SetValue("FireTimeRemaining", m_fireTimeRemaining);
			valuesDictionary.SetValue("HeatLevel", m_heatLevel);
		}

		public virtual CraftingRecipe FindSmeltingRecipe(float heatLevel)
		{
			if (heatLevel > 0f)
			{
				for (int i = 0; i < m_furnaceSize; i++)
				{
					int slotValue = GetSlotValue(i);
					int num = Terrain.ExtractContents(slotValue);
					int num2 = Terrain.ExtractData(slotValue);
					if (GetSlotCount(i) > 0)
					{
						Block block = BlocksManager.Blocks[num];
						m_matchedIngredients[i] = block.GetCraftingId(slotValue) + ":" + num2.ToString(CultureInfo.InvariantCulture);
					}
					else
					{
						m_matchedIngredients[i] = null;
					}
				}
				ComponentPlayer componentPlayer = FindInteractingPlayer();
				float playerLevel = componentPlayer?.PlayerData.Level ?? 1f;
				CraftingRecipe craftingRecipe = null;
				craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(m_subsystemTerrain, m_matchedIngredients, heatLevel, playerLevel);
				if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
				{
					if (craftingRecipe.RequiredHeatLevel <= 0f)
					{
						craftingRecipe = null;
					}
					if (craftingRecipe != null)
					{
						Slot slot = m_slots[ResultSlotIndex];
						int num3 = Terrain.ExtractContents(craftingRecipe.ResultValue);
						if (slot.Count != 0 && (craftingRecipe.ResultValue != slot.Value || craftingRecipe.ResultCount + slot.Count > BlocksManager.Blocks[num3].GetMaxStacking(craftingRecipe.ResultValue)))
						{
							craftingRecipe = null;
						}
					}
					if (craftingRecipe != null && craftingRecipe.RemainsValue != 0 && craftingRecipe.RemainsCount > 0)
					{
						if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == craftingRecipe.RemainsValue)
						{
							if (BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.RemainsValue)].GetMaxStacking(craftingRecipe.RemainsValue) - m_slots[RemainsSlotIndex].Count < craftingRecipe.RemainsCount)
							{
								craftingRecipe = null;
							}
						}
						else
						{
							craftingRecipe = null;
						}
					}
				}
				if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
				{
					componentPlayer?.ComponentGui.DisplaySmallMessage(craftingRecipe.Message, Color.White, blinking: true, playNotificationSound: true);
				}
				return craftingRecipe;
			}
			return null;
		}
	}
}

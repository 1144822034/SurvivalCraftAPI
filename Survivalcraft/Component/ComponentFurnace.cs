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

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTime m_subsystemTime;

		public FireParticleSystem m_fireParticleSystem;

		public SubsystemParticles m_subsystemParticles;

		public bool StopFuelWhenNoRecipeIsActive = true;

		public float SmeltSpeed = 0.15f;

		public float SmeltProgressReductionSpeed = float.PositiveInfinity;

		public float FuelTimeEfficiency = 1f;

		public int m_furnaceSize;

		public string[] m_matchedIngredients = new string[9];

		public float m_fuelEndTime;

		public float m_heatLevel;

		public bool m_updateSmeltingRecipe;

		public CraftingRecipe m_smeltingRecipe;

		public float m_smeltingProgress;
		private float epsilon => Math.Min(m_subsystemTime.GameTimeDelta, 0.1f);
		public int RemainsSlotIndex => SlotsCount - 1;
		public int ResultSlotIndex => SlotsCount - 2;
		public int FuelSlotIndex => SlotsCount - 3;
		public float HeatLevel => m_heatLevel;

		public float SmeltingProgress => m_smeltingProgress;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public float FireTimeRemaining
		{
			get
			{
				float ans = m_fuelEndTime - (float)m_subsystemGameInfo.TotalElapsedGameTime;
				return ans > -epsilon ? ans + epsilon : 0f;
			}
		}

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

		public virtual bool UseFuel()
        {
            Point3 coordinates = m_componentBlockEntity.Coordinates;
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
					if (m_heatLevel == 0f) m_fuelEndTime = (float)m_subsystemGameInfo.TotalElapsedGameTime;
                    m_fuelEndTime = m_fuelEndTime + block.GetFuelFireDuration(slot2.Value) * FuelTimeEfficiency;
                    m_heatLevel = block.GetFuelHeatLevel(slot2.Value);
					return true;
                }
            }
			return false;
        }

		public virtual void UpdateSmeltingRecipe()
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
                if (FireTimeRemaining <= 0 && m_smeltingRecipe != null) UseFuel();
            }
        }

		public virtual void StopSmelting(bool resetProgress)
		{
            m_heatLevel = 0f;
			m_fuelEndTime = 0f;
            m_smeltingRecipe = null;
            if(resetProgress) m_smeltingProgress = 0f;
        }

		public void Update(float dt)
		{
			if (m_heatLevel > 0f)
			{
				int fuelAdded = 0;
				while (m_fuelEndTime + epsilon < (float)m_subsystemGameInfo.TotalElapsedGameTime)
				{
					if (m_smeltingRecipe != null && UseFuel()){
						fuelAdded++;
						if (fuelAdded == 100) break;
					}
					else
					{
                        StopSmelting(false);
						break;
                    }
				}
			}
			if (m_updateSmeltingRecipe)
				UpdateSmeltingRecipe();
			if (m_smeltingRecipe == null)
			{
				if (StopFuelWhenNoRecipeIsActive)
					StopSmelting(true);
			}
			if(FireTimeRemaining <= 0)
			{
                m_smeltingProgress = MathUtils.Max(m_smeltingProgress - (SmeltProgressReductionSpeed * dt), 0f);
            }
			if (m_smeltingRecipe != null && FireTimeRemaining > 0)
			{
				m_smeltingProgress = MathUtils.Min(m_smeltingProgress + (SmeltSpeed * dt), 1f);
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

			int cellValue = m_componentBlockEntity.BlockValue;
			if(m_heatLevel > 0f)
			{
				m_fireParticleSystem.m_position = m_componentBlockEntity.Position + new Vector3(0.5f,0.2f,0.5f);
				if(Terrain.ExtractContents(cellValue) == 64)
					m_subsystemParticles.AddParticleSystem(m_fireParticleSystem);
				m_componentBlockEntity.BlockValue = Terrain.ReplaceContents(cellValue,65);
			}
			else
			{
				if(Terrain.ExtractContents(cellValue) == 65)
					m_subsystemParticles.RemoveParticleSystem(m_fireParticleSystem);
				m_componentBlockEntity.BlockValue = Terrain.ReplaceContents(cellValue,64);
			}
		}

		public override void OnEntityRemoved()
		{
			m_subsystemParticles.RemoveParticleSystem(m_fireParticleSystem);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
			m_furnaceSize = SlotsCount - 3;
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_fireParticleSystem = new FireParticleSystem(m_componentBlockEntity.Position + new Vector3(0.5f,0.2f,0.5f), 0.15f, 16f);
			if (m_furnaceSize < 1 || m_furnaceSize > 3)
			{
				throw new InvalidOperationException("Invalid furnace size.");
			}
			float fireTimeRemaining = valuesDictionary.GetValue<float>("FireTimeRemaining");
			m_fuelEndTime = (float)m_subsystemGameInfo.TotalElapsedGameTime + fireTimeRemaining;
			m_heatLevel = valuesDictionary.GetValue<float>("HeatLevel");
			m_updateSmeltingRecipe = true;
			if(m_heatLevel > 0f)
				m_subsystemParticles.AddParticleSystem(m_fireParticleSystem);
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			base.Save(valuesDictionary, entityToIdMap);
			float fireTimeRemaining = m_fuelEndTime - (float)m_subsystemGameInfo.TotalElapsedGameTime;
			if(fireTimeRemaining < 0) fireTimeRemaining = 0 ;
            valuesDictionary.SetValue("FireTimeRemaining", fireTimeRemaining);
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

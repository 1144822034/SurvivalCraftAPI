using Engine;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ComponentClothing : Component, IUpdateable, IInventory
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemTime m_subsystemTime;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemPickables m_subsystemPickables;

		public ComponentGui m_componentGui;

		public ComponentHumanModel m_componentHumanModel;

		public ComponentBody m_componentBody;

		public ComponentOuterClothingModel m_componentOuterClothingModel;

		public ComponentVitalStats m_componentVitalStats;

		public ComponentLocomotion m_componentLocomotion;

		public ComponentPlayer m_componentPlayer;

		public Texture2D m_skinTexture;

		public string m_skinTextureName;

		public RenderTarget2D m_innerClothedTexture;

		public RenderTarget2D m_outerClothedTexture;

		public PrimitivesRenderer2D m_primitivesRenderer = new();

		public Random m_random = new();

		public float m_densityModifierApplied;

		public double? m_lastTotalElapsedGameTime;

		public bool m_clothedTexturesValid;

		public static string fName = "ComponentClothing";

		public List<int> m_clothesList = [];

		public Dictionary<ClothingSlot, List<int>> m_clothes = [];

		public static ClothingSlot[] m_innerSlotsOrder = new ClothingSlot[4]
		{
			ClothingSlot.Head,
			ClothingSlot.Torso,
			ClothingSlot.Feet,
			ClothingSlot.Legs
		};

		public static ClothingSlot[] m_outerSlotsOrder = new ClothingSlot[4]
		{
			ClothingSlot.Head,
			ClothingSlot.Torso,
			ClothingSlot.Legs,
			ClothingSlot.Feet
		};

		public static bool ShowClothedTexture = false;

		public static bool DrawClothedTexture = true;

		public Texture2D InnerClothedTexture => m_innerClothedTexture;

		public Texture2D OuterClothedTexture => m_outerClothedTexture;

		public float Insulation
		{
			get;
			set;
		}

		public ClothingSlot LeastInsulatedSlot
		{
			get;
			set;
		}

		public float SteedMovementSpeedFactor
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		Project IInventory.Project => Project;

		public int SlotsCount => 4;

		public int VisibleSlotsCount
		{
			get
			{
				return SlotsCount;
			}
			set
			{
			}
		}

		public int ActiveSlotIndex
		{
			get
			{
				return -1;
			}
			set
			{
			}
		}

		public virtual ReadOnlyList<int> GetClothes(ClothingSlot slot)
		{
			return new ReadOnlyList<int>(m_clothes[slot]);
		}

		public virtual void SetClothes(ClothingSlot slot, IEnumerable<int> clothes)
		{
			if (!m_clothes[slot].SequenceEqual(clothes))
			{
				m_clothes[slot].Clear();
				m_clothes[slot].AddRange(clothes);
				m_clothedTexturesValid = false;
				float num = 0f;
				foreach (KeyValuePair<ClothingSlot, List<int>> clothe in m_clothes)
				{
					foreach (int item in clothe.Value)
					{
						Block block = BlocksManager.Blocks[Terrain.ExtractContents(item)];
						ClothingData clothingData = block.GetClothingData(item);
						num += clothingData?.DensityModifier ?? 0f;
					}
				}
				float num2 = num - m_densityModifierApplied;
				m_densityModifierApplied += num2;
				m_componentBody.Density += num2;
				SteedMovementSpeedFactor = 1f;
				float num3 = 2f;
				float num4 = 0.2f;
				float num5 = 0.4f;
				float num6 = 2f;
				foreach (int clothe2 in GetClothes(ClothingSlot.Head))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe2)];
					ClothingData clothingData2 = block.GetClothingData(clothe2);
					if (clothingData2 != null)
					{
						num3 += clothingData2.Insulation;
						SteedMovementSpeedFactor *= clothingData2.SteedMovementSpeedFactor;
					}
				}
				foreach (int clothe3 in GetClothes(ClothingSlot.Torso))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe3)];
					ClothingData clothingData3 = block.GetClothingData(clothe3);
					if (clothingData3 != null)
					{
						num4 += clothingData3.Insulation;
						SteedMovementSpeedFactor *= clothingData3.SteedMovementSpeedFactor;
					}
				}
				foreach (int clothe4 in GetClothes(ClothingSlot.Legs))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe4)];
					ClothingData clothingData4 = block.GetClothingData(clothe4);
					if (clothingData4 != null)
					{
						num5 += clothingData4.Insulation;
						SteedMovementSpeedFactor *= clothingData4.SteedMovementSpeedFactor;
					}
				}
				foreach (int clothe5 in GetClothes(ClothingSlot.Feet))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe5)];
					ClothingData clothingData5 = block.GetClothingData(clothe5);
					if (clothingData5 != null)
					{
						num6 += clothingData5.Insulation;
						SteedMovementSpeedFactor *= clothingData5.SteedMovementSpeedFactor;
					}
				}
				Insulation = 1f / ((1f / num3) + (1f / num4) + (1f / num5) + (1f / num6));
				float num7 = MathUtils.Min(num3, num4, num5, num6);
				if (num3 == num7)
				{
					LeastInsulatedSlot = ClothingSlot.Head;
				}
				else if (num4 == num7)
				{
					LeastInsulatedSlot = ClothingSlot.Torso;
				}
				else if (num5 == num7)
				{
					LeastInsulatedSlot = ClothingSlot.Legs;
				}
				else if (num6 == num7)
				{
					LeastInsulatedSlot = ClothingSlot.Feet;
				}
			}
            ModsManager.HookAction("SetClothes", loader =>
            {
                loader.SetClothes(this, slot, clothes);
                return false;
            });
        }

		public float ApplyArmorProtection(float attackPower)
		{
			bool Applied = false;
			ModsManager.HookAction("ApplyArmorProtection", modLoader =>
			{
				attackPower = modLoader.ApplyArmorProtection(this, attackPower, Applied, out bool flag2);
				Applied |= flag2;
				return false;
			});
			if (Applied == false)
			{
				float num = m_random.Float(0f, 1f);
				ClothingSlot slot = (num < 0.1f) ? ClothingSlot.Feet : ((num < 0.3f) ? ClothingSlot.Legs : ((num < 0.9f) ? ClothingSlot.Torso : ClothingSlot.Head));
				List<int> list = new(GetClothes(slot));
				for (int i = 0; i < list.Count; i++)
				{
					int value = list[i];
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
					float num2 = block.GetDurability(value) + 1;
					ClothingData clothingData = block.GetClothingData(value);
					if(clothingData == null)
					{
						continue;
					}
					float x = (num2 - block.GetDamage(value)) / num2 * clothingData.Sturdiness;
					float num3 = MathF.Min(attackPower * MathUtils.Saturate(clothingData.ArmorProtection), x);
					if (num3 > 0f)
					{
						attackPower -= num3;
						if (m_subsystemGameInfo.WorldSettings.GameMode != 0)
						{
							float x2 = (num3 / clothingData.Sturdiness * num2) + 0.001f;
							int damageCount = (int)(MathF.Floor(x2) + (m_random.Bool(MathUtils.Remainder(x2, 1f)) ? 1 : 0));
							list[i] = BlocksManager.DamageItem(value, damageCount, Entity);
						}
						if (!string.IsNullOrEmpty(clothingData.ImpactSoundsFolder))
						{
							m_subsystemAudio.PlayRandomSound(clothingData.ImpactSoundsFolder, 1f, m_random.Float(-0.3f, 0.3f), m_componentBody.Position, 4f, 0.15f);
						}
					}
				}
				int num4 = 0;
				while (num4 < list.Count)
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(list[num4])];
					if (!block.CanWear(list[num4]))
					{
						list.RemoveAt(num4);
						m_subsystemParticles.AddParticleSystem(new BlockDebrisParticleSystem(m_subsystemTerrain, m_componentBody.Position + (m_componentBody.StanceBoxSize / 2f), 1f, 1f, Color.White, 0));
					}
					else
					{
						num4++;
					}
				}
				SetClothes(slot, list);

			}
			return MathF.Max(attackPower, 0f);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_componentGui = Entity.FindComponent<ComponentGui>(throwOnError: true);
			m_componentHumanModel = Entity.FindComponent<ComponentHumanModel>(throwOnError: true);
			m_componentBody = Entity.FindComponent<ComponentBody>(throwOnError: true);
			m_componentOuterClothingModel = Entity.FindComponent<ComponentOuterClothingModel>(throwOnError: true);
			m_componentVitalStats = Entity.FindComponent<ComponentVitalStats>(throwOnError: true);
			m_componentLocomotion = Entity.FindComponent<ComponentLocomotion>(throwOnError: true);
			m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			SteedMovementSpeedFactor = 1f;
			Insulation = 0f;
			LeastInsulatedSlot = ClothingSlot.Feet;
			m_clothes[ClothingSlot.Head] = [];
			m_clothes[ClothingSlot.Torso] = [];
			m_clothes[ClothingSlot.Legs] = [];
			m_clothes[ClothingSlot.Feet] = [];
			ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Clothes");
			SetClothes(ClothingSlot.Head, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>("Head")));
			SetClothes(ClothingSlot.Torso, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>("Torso")));
			SetClothes(ClothingSlot.Legs, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>("Legs")));
			SetClothes(ClothingSlot.Feet, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>("Feet")));
			Display.DeviceReset += Display_DeviceReset;
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Clothes", valuesDictionary2);
			valuesDictionary2.SetValue("Head", HumanReadableConverter.ValuesListToString(';', m_clothes[ClothingSlot.Head].ToArray()));
			valuesDictionary2.SetValue("Torso", HumanReadableConverter.ValuesListToString(';', m_clothes[ClothingSlot.Torso].ToArray()));
			valuesDictionary2.SetValue("Legs", HumanReadableConverter.ValuesListToString(';', m_clothes[ClothingSlot.Legs].ToArray()));
			valuesDictionary2.SetValue("Feet", HumanReadableConverter.ValuesListToString(';', m_clothes[ClothingSlot.Feet].ToArray()));
		}

		public override void Dispose()
		{
			base.Dispose();
			if (m_skinTexture != null && !ContentManager.IsContent(m_skinTexture))
			{
				m_skinTexture.Dispose();
				m_skinTexture = null;
			}
			if (m_innerClothedTexture != null)
			{
				m_innerClothedTexture.Dispose();
				m_innerClothedTexture = null;
			}
			if (m_outerClothedTexture != null)
			{
				m_outerClothedTexture.Dispose();
				m_outerClothedTexture = null;
			}
			Display.DeviceReset -= Display_DeviceReset;
		}

		public void Update(float dt)
		{
			foreach (ClothingSlot slot in m_innerSlotsOrder)
			{
				foreach (int clothe in GetClothes(slot))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
					ClothingData clothingData = block.GetClothingData(clothe);
					clothingData?.Update?.Invoke(clothe, this);
				}
			}
			foreach (ClothingSlot slot in m_outerSlotsOrder)
			{
				foreach (int clothe in GetClothes(slot))
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
					ClothingData clothingData = block.GetClothingData(clothe);
					clothingData?.Update?.Invoke(clothe, this);
				}
			}

			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled && m_subsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
			{
				foreach (int enumValue in EnumUtils.GetEnumValues(typeof(ClothingSlot)))
				{
					bool flag = false;
					m_clothesList.Clear();
					m_clothesList.AddRange(GetClothes((ClothingSlot)enumValue));
					int num = 0;
					while (num < m_clothesList.Count)
					{
						int value = m_clothesList[num];
						Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
						ClothingData clothingData = block.GetClothingData(value);
						if (clothingData == null)
						{
							m_clothesList.RemoveAt(num);
							flag = true;
						}
						if (clothingData.PlayerLevelRequired > m_componentPlayer.PlayerData.Level)
						{

							m_componentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), clothingData.PlayerLevelRequired, clothingData.DisplayName), Color.White, blinking: true, playNotificationSound: true);
							m_subsystemPickables.AddPickable(value, 1, m_componentBody.Position, null, null, Entity);
							m_clothesList.RemoveAt(num);
							flag = true;
						}
						else
						{
							num++;
						}
					}
					if (flag)
					{
						SetClothes((ClothingSlot)enumValue, m_clothesList);
					}
				}
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled && m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0) && ((m_componentLocomotion.LastWalkOrder.HasValue && m_componentLocomotion.LastWalkOrder.Value != Vector2.Zero) || (m_componentLocomotion.LastSwimOrder.HasValue && m_componentLocomotion.LastSwimOrder.Value != Vector3.Zero) || m_componentLocomotion.LastJumpOrder != 0f))
			{
				if (m_lastTotalElapsedGameTime.HasValue)
				{
					foreach (int enumValue2 in EnumUtils.GetEnumValues(typeof(ClothingSlot)))
					{
						bool flag2 = false;
						m_clothesList.Clear();
						m_clothesList.AddRange(GetClothes((ClothingSlot)enumValue2));
						for (int i = 0; i < m_clothesList.Count; i++)
						{
							int value2 = m_clothesList[i];
							Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
							ClothingData clothingData2 = block2.GetClothingData(value2);
							if (clothingData2 == null)
							{
								continue;
							}
							float num2 = (m_componentVitalStats.Wetness > 0f) ? (10f * clothingData2.Sturdiness) : (20f * clothingData2.Sturdiness);
							double num3 = Math.Floor(m_lastTotalElapsedGameTime.Value / num2);
							if (Math.Floor(m_subsystemGameInfo.TotalElapsedGameTime / num2) > num3 && m_random.Float(0f, 1f) < 0.75f)
							{
								m_clothesList[i] = BlocksManager.DamageItem(value2, 1, Entity);
								flag2 = true;
							}
						}
						int num4 = 0;
						while (num4 < m_clothesList.Count)
						{
							Block block = BlocksManager.Blocks[Terrain.ExtractContents(m_clothesList[num4])];
							if (!block.CanWear(m_clothesList[num4]))
							{
								m_clothesList.RemoveAt(num4);
								m_subsystemParticles.AddParticleSystem(new BlockDebrisParticleSystem(m_subsystemTerrain, m_componentBody.Position + (m_componentBody.StanceBoxSize / 2f), 1f, 1f, Color.White, 0));
								m_componentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, blinking: true, playNotificationSound: true);
							}
							else
							{
								num4++;
							}
						}
						if (flag2)
						{
							SetClothes((ClothingSlot)enumValue2, m_clothesList);
						}
					}
				}
				m_lastTotalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			}
			UpdateRenderTargets();
		}

		public virtual int GetSlotValue(int slotIndex)
		{
			return GetClothes((ClothingSlot)slotIndex).LastOrDefault();
		}

		public virtual int GetSlotCount(int slotIndex)
		{
			if (GetClothes((ClothingSlot)slotIndex).Count <= 0)
			{
				return 0;
			}
			return 1;
		}

		public virtual int GetSlotCapacity(int slotIndex, int value)
		{
			return 0;
		}

		public virtual int GetSlotProcessCapacity(int slotIndex, int value)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			if (block.GetNutritionalValue(value) > 0f)
			{
				return 1;
			}
			if (block.CanWear(value) && CanWearClothing(value))
			{
				return 1;
			}
			return 0;
		}

		public virtual void AddSlotItems(int slotIndex, int value, int count)
		{
		}

		public virtual void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
		{
			processedCount = 0;
			processedValue = 0;
			if (processCount != 1)
			{
				return;
			}
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			ModsManager.HookAction("ClothingProcessSlotItems", modLoader => { return modLoader.ClothingProcessSlotItems(m_componentPlayer, block, slotIndex, value, count); });
			if (block.GetNutritionalValue(value) > 0f)
			{
				if (block is BucketBlock)
				{
					processedValue = EmptyBucketBlock.Index;
					processedCount = 1;
				}
				if (count > 1 && processedCount > 0 && processedValue != value)
				{
					processedValue = value;
					processedCount = processCount;
				}
				else if (block.Eat(m_componentVitalStats, value) || !m_componentVitalStats.Eat(value))
				{
					processedValue = value;
					processedCount = processCount;
				}

			}
			if (block.CanWear(value))
			{
				ClothingData clothingData = block.GetClothingData(value);
				if (clothingData == null)
				{
					return;
				}
				clothingData.Mount?.Invoke(value, this);
				var list = new List<int>(GetClothes(clothingData.Slot))
				{
					value
				};
				SetClothes(clothingData.Slot, list);
			}
		}

		public virtual int RemoveSlotItems(int slotIndex, int count)
		{
			if (count == 1)
			{
				var list = new List<int>(GetClothes((ClothingSlot)slotIndex));
				if (list.Count > 0)
				{
					int value = list[^1];
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
					ClothingData clothingData = block.GetClothingData(value);
					clothingData?.Dismount?.Invoke(value, this);
					list.RemoveAt(list.Count - 1);
					SetClothes((ClothingSlot)slotIndex, list);
					return 1;
				}
			}
			return 0;
		}

		public virtual void DropAllItems(Vector3 position)
		{
			var random = new Random();
			SubsystemPickables subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			for (int i = 0; i < SlotsCount; i++)
			{
				int slotCount = GetSlotCount(i);
				if (slotCount > 0)
				{
					int slotValue = GetSlotValue(i);
					int count = RemoveSlotItems(i, slotCount);
					Vector3 value = random.Float(5f, 10f) * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(1f, 2f), random.Float(-1f, 1f)));
					subsystemPickables.AddPickable(slotValue, count, position, value, null, Entity);
				}
			}
		}

		public virtual void Display_DeviceReset()
		{
			m_clothedTexturesValid = false;
		}

		public virtual bool CanWearClothing(int value)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			ClothingData clothingData = block.GetClothingData(value);
			if (clothingData == null)
			{
				return false;
			}
			IList<int> list = GetClothes(clothingData.Slot);
			if (list.Count == 0)
			{
				return true;
			}
			int value2 = list[list.Count - 1];
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
			ClothingData clothingData2 = block2.GetClothingData(value2);
			if (clothingData2 == null)
			{
				return false;
			}
			return clothingData.Layer > clothingData2.Layer;
		}

		public virtual void UpdateRenderTargets()
		{
			if (m_skinTexture == null || m_componentPlayer.PlayerData.CharacterSkinName != m_skinTextureName)
			{
				m_skinTexture = CharacterSkinsManager.LoadTexture(m_componentPlayer.PlayerData.CharacterSkinName);
				m_skinTextureName = m_componentPlayer.PlayerData.CharacterSkinName;
				Utilities.Dispose(ref m_innerClothedTexture);
				Utilities.Dispose(ref m_outerClothedTexture);
			}
			if (m_innerClothedTexture == null || m_innerClothedTexture.Width != m_skinTexture.Width || m_innerClothedTexture.Height != m_skinTexture.Height)
			{
				m_innerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
				m_componentHumanModel.TextureOverride = m_innerClothedTexture;
				m_clothedTexturesValid = false;
			}
			if (m_outerClothedTexture == null || m_outerClothedTexture.Width != m_skinTexture.Width || m_outerClothedTexture.Height != m_skinTexture.Height)
			{
				m_outerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
				m_componentOuterClothingModel.TextureOverride = m_outerClothedTexture;
				m_clothedTexturesValid = false;
			}
			if (DrawClothedTexture && !m_clothedTexturesValid)
			{
				m_clothedTexturesValid = true;
				Rectangle scissorRectangle = Display.ScissorRectangle;
				RenderTarget2D renderTarget = Display.RenderTarget;
				try
				{
					Display.RenderTarget = m_innerClothedTexture;
					Display.Clear(new Vector4(Color.Transparent));
					int num = 0;
					TexturedBatch2D texturedBatch2D = m_primitivesRenderer.TexturedBatch(m_skinTexture, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
					texturedBatch2D.QueueQuad(Vector2.Zero, new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, Color.White);
					ClothingSlot[] innerSlotsOrder = m_innerSlotsOrder;
					foreach (ClothingSlot slot in innerSlotsOrder)
					{
						foreach (int clothe in GetClothes(slot))
						{
							int data = Terrain.ExtractData(clothe);
							Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
							ClothingData clothingData = block.GetClothingData(clothe);
							if(clothingData == null)
							{
								continue;
							}
							Color fabricColor = SubsystemPalette.GetFabricColor(m_subsystemTerrain, ClothingBlock.GetClothingColor(data));
							texturedBatch2D = m_primitivesRenderer.TexturedBatch(clothingData.Texture, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
							if (!clothingData.IsOuter)
							{
								texturedBatch2D.QueueQuad(new Vector2(0f, 0f), new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, fabricColor);
							}
						}
					}
					m_primitivesRenderer.Flush();
					Display.RenderTarget = m_outerClothedTexture;
					Display.Clear(new Vector4(Color.Transparent));
					num = 0;
					innerSlotsOrder = m_outerSlotsOrder;
					foreach (ClothingSlot slot2 in innerSlotsOrder)
					{
						foreach (int clothe2 in GetClothes(slot2))
						{
							int data2 = Terrain.ExtractData(clothe2);
							Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(clothe2)];
							ClothingData clothingData2 = block2.GetClothingData(clothe2);
							if(clothingData2 == null)
							{
								continue;
							}
							Color fabricColor2 = SubsystemPalette.GetFabricColor(m_subsystemTerrain, ClothingBlock.GetClothingColor(data2));
							texturedBatch2D = m_primitivesRenderer.TexturedBatch(clothingData2.Texture, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
							if (clothingData2.IsOuter)
							{
								texturedBatch2D.QueueQuad(new Vector2(0f, 0f), new Vector2(m_outerClothedTexture.Width, m_outerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, fabricColor2);
							}
						}
					}
					m_primitivesRenderer.Flush();
				}
				finally
				{
					Display.RenderTarget = renderTarget;
					Display.ScissorRectangle = scissorRectangle;
				}
			}
		}
	}
}

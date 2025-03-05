using Engine;
using GameEntitySystem;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentMiner : Component, IUpdateable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemMovingBlocks m_subsystemMovingBlocks;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTime m_subsystemTime;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemSoundMaterials m_subsystemSoundMaterials;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		private ComponentHealth m_componentHealth;

		public Random m_random = new();

		public static Random s_random = new();

		public double m_digStartTime;

		public float m_digProgress;

		public double m_lastHitTime;

		public static string fName = "ComponentMiner";

		public int m_lastDigFrameIndex;

		public float m_lastPokingPhase;

		private double m_lastToolHintTime;
		/// <summary>
		/// 伤害间隔(原版为0.66f)
		/// </summary>
		public virtual double HitInterval 
		{
			get
			{
				return m_basicHitInterval / ComponentFactors.OtherFactorsResults["AttackSpeed"];
			}
			[Obsolete("Do not set the added hit interval, set m_basicHitInterval instead.")]
			set
			{
				m_basicHitInterval = value;
			}
		}
		public double m_basicHitInterval;

		public ComponentCreature ComponentCreature
		{
			get;
			set;
		}

		public ComponentPlayer ComponentPlayer
		{
			get;
			set;
		}

		public ComponentFactors ComponentFactors { get; set; }

		public IInventory Inventory
		{
			get;
			set;
		}

		public int ActiveBlockValue
		{
			get
			{
				if (Inventory == null)
				{
					return 0;
				}
				return Inventory.GetSlotValue(Inventory.ActiveSlotIndex);
			}
		}

		public float AttackPower
		{
			get;
			set;
		}

		public float AutoInteractRate
		{
			get;
			set;
		}

		public float StrengthFactor
		{
			get
			{
				return ComponentFactors?.StrengthFactor ?? 1;
			}
		}
		/// <summary>
		/// 挖掘速度是否受玩家力量属性加成
		/// </summary>
		public bool m_digSpeedBasedOnStrengthFactor = true;
		public float DigSpeedFactor
		{
			get
			{
				float ans = 1f;
				if(m_digSpeedBasedOnStrengthFactor) ans *= StrengthFactor;
				if(ComponentFactors?.OtherFactorsResults.TryGetValue("DigSpeed", out var result) ?? false)
				{
					ans *= result;
				}
				return ans;
			}
		}
		public float PokingPhase
		{
			get;
			set;
		}

		public CellFace? DigCellFace
		{
			get;
			set;
		}

		public float DigTime
		{
			get
			{
				if (!DigCellFace.HasValue)
				{
					return 0f;
				}
				return (float)(m_subsystemTime.GameTime - m_digStartTime);
			}
		}

		public float DigProgress
		{
			get
			{
				if (!DigCellFace.HasValue)
				{
					return 0f;
				}
				return m_digProgress;
			}
		}

		public bool m_canSqueezeBlock = true;

		public bool m_canJumpToPlace = false;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public virtual void Poke(bool forceRestart)
		{
			PokingPhase = forceRestart ? 0.0001f : MathUtils.Max(0.0001f, PokingPhase);
		}

		public bool Dig(TerrainRaycastResult raycastResult)
		{
			bool result = false;
			m_lastDigFrameIndex = Time.FrameIndex;
			CellFace cellFace = raycastResult.CellFace;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
			int cellContents = Terrain.ExtractContents(cellValue);
			Block cellBlock = BlocksManager.Blocks[cellContents];
			int activeBlockValue = ActiveBlockValue;
			int activeBlockContents = Terrain.ExtractContents(activeBlockValue);
			Block activeBlock = BlocksManager.Blocks[activeBlockContents];
			if (!DigCellFace.HasValue || DigCellFace.Value.X != cellFace.X || DigCellFace.Value.Y != cellFace.Y || DigCellFace.Value.Z != cellFace.Z)
			{
				m_digStartTime = m_subsystemTime.GameTime;
				DigCellFace = cellFace;
			}
			float digTimeWithActiveTool = CalculateDigTime(cellValue, activeBlockValue);
			m_digProgress = (digTimeWithActiveTool > 0f) ? MathUtils.Saturate((float)(m_subsystemTime.GameTime - m_digStartTime) / digTimeWithActiveTool) : 1f;
			if (!IsLevelSufficientForTool(activeBlockValue))
			{
				m_digProgress = 0f;
				if (m_subsystemTime.PeriodicGameTimeEvent(5.0, m_digStartTime + 1.0))
				{
					ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), activeBlock.PlayerLevelRequired, activeBlock.GetDisplayName(m_subsystemTerrain, activeBlockValue)), Color.White, blinking: true, playNotificationSound: true);
				}
			}
			bool flag2 = ComponentPlayer != null && !ComponentPlayer.ComponentInput.IsControlledByTouch && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
			ModsManager.HookAction("OnMinerDig", modLoader =>
			{
				modLoader.OnMinerDig(this, raycastResult, ref m_digProgress, out bool flag3);
					flag2 |= flag3;
				return false;
			});
			if ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Survival || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless) && ComponentPlayer != null && digTimeWithActiveTool >= 3f && m_digProgress > 0.5f && (m_lastToolHintTime == 0.0 || Time.FrameStartTime - m_lastToolHintTime > 300.0))
			{
				bool flag = digTimeWithActiveTool == CalculateDigTime(cellValue, 0);//flag:该物品挖掘时间和空手相同
				int bestInventoryToolValue = FindBestInventoryToolForDigging(cellValue);
				if (bestInventoryToolValue == 0)
				{
					if (activeBlockContents != 23 && flag)
					{
						ComponentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "11"), Color.White, blinking: true, playNotificationSound: true);
						m_lastToolHintTime = Time.FrameStartTime;
					}
				}
				else if (CalculateDigTime(cellValue, bestInventoryToolValue) < 0.5f * digTimeWithActiveTool || flag)
				{
					string displayName = BlocksManager.Blocks[Terrain.ExtractContents(bestInventoryToolValue)].GetDisplayName(m_subsystemTerrain, bestInventoryToolValue);
					ComponentPlayer.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, "12"), displayName), Color.White, blinking: true, playNotificationSound: true);
					m_lastToolHintTime = Time.FrameStartTime;
				}
			}
			if (flag2 || (m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f))
			{
				if (m_digProgress >= 1f)
				{
					DigCellFace = null;
                    if (flag2)
                    {
                        Poke(forceRestart: true);
                    }
                    BlockPlacementData digValue = cellBlock.GetDigValue(m_subsystemTerrain, this, cellValue, activeBlockValue, raycastResult);
                    m_subsystemTerrain.DestroyCell(activeBlock.ToolLevel, digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z, digValue.Value, noDrop: false, noParticleSystem: false);
					int durabilityReduction = 1;
					int playerDataAdd = 1;
					bool mute_ = false;
                    ModsManager.HookAction("OnBlockDug", modLoader =>
					{
						bool mute = false;
						modLoader.OnBlockDug(this, digValue, cellValue, ref durabilityReduction, ref mute, ref playerDataAdd);
						mute_ |= mute;
						return false;
					});
					if(!mute_) m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f);
					DamageActiveTool(durabilityReduction);
					if (ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksDug += playerDataAdd;
					}
					result = true;
				}
				else
				{
					m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 1f);
					BlockDebrisParticleSystem particleSystem = cellBlock.CreateDebrisParticleSystem(m_subsystemTerrain, raycastResult.HitPoint(0.1f), cellValue, 0.35f);
					base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
				}
			}
			return result;
		}

		public bool Place(TerrainRaycastResult raycastResult)
		{
			if (Place(raycastResult, ActiveBlockValue))
			{
				if (Inventory != null)
				{
					Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
				}
				return true;
			}
			return false;
		}

		public bool Place(TerrainRaycastResult raycastResult, int value)
		{
			int num = Terrain.ExtractContents(value);
			if (BlocksManager.Blocks[num].IsPlaceable_(value))
			{
				Block block = BlocksManager.Blocks[num];
				BlockPlacementData placementData = block.GetPlacementValue(m_subsystemTerrain, this, value, raycastResult);
				if (placementData.Value != 0)
				{
					Point3 point = CellFace.FaceToPoint3(placementData.CellFace.Face);
					int num2 = placementData.CellFace.X + point.X;
					int num3 = placementData.CellFace.Y + point.Y;
					int num4 = placementData.CellFace.Z + point.Z;
					bool placed = false;
					bool placementNotAllowed_ = false;
					ModsManager.HookAction("BeforeMinerPlace",loader => {
						loader.BeforeMinerPlace(this, raycastResult, num2, num3, num4, placementData, out bool placementNotAllowed);
						placementNotAllowed_ |= placementNotAllowed;
						return false;
					});
					if(placementNotAllowed_) return false;
					ModsManager.HookAction("OnMinerPlace", modLoader =>
					{
						modLoader.OnMinerPlace(this, raycastResult, num2, num3, num4, value, out bool Placed);
						placed |= Placed;
						return false;
					});
					if (placed) return true;
					if (!m_canSqueezeBlock)
					{
						if (m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4) != 0) return false;
					}
					if (num3 > 0 && num3 < 255 && (m_canJumpToPlace || IsBlockPlacingAllowed(ComponentCreature.ComponentBody) || m_subsystemGameInfo.WorldSettings.GameMode <= GameMode.Survival))
					{
						bool flag = false;
						if (block.IsCollidable_(value))
						{
							BoundingBox boundingBox = ComponentCreature.ComponentBody.BoundingBox;
							boundingBox.Min += new Vector3(0.2f);
							boundingBox.Max -= new Vector3(0.2f);
							BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, placementData.Value);
							for (int i = 0; i < customCollisionBoxes.Length; i++)
							{
								BoundingBox box = customCollisionBoxes[i];
								box.Min += new Vector3(num2, num3, num4);
								box.Max += new Vector3(num2, num3, num4);
								if (boundingBox.Intersection(box))
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(placementData.Value));
							for (int i = 0; i < blockBehaviors.Length; i++)
							{
								blockBehaviors[i].OnItemPlaced(num2, num3, num4, ref placementData, value);
							}
							m_subsystemTerrain.DestroyCell(0, num2, num3, num4, placementData.Value, noDrop: false, noParticleSystem: false);
							m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(placementData.CellFace.X, placementData.CellFace.Y, placementData.CellFace.Z), 5f, autoDelay: false);
							Poke(forceRestart: false);
							if (ComponentCreature.PlayerStats != null)
							{
								ComponentCreature.PlayerStats.BlocksPlaced++;
							}
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool Use(Ray3 ray)
		{
			int num = Terrain.ExtractContents(ActiveBlockValue);
			Block block = BlocksManager.Blocks[num];
			if (!IsLevelSufficientForTool(ActiveBlockValue))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block.PlayerLevelRequired, block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				return false;
			}
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(ActiveBlockValue));
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i].OnUse(ray, this))
				{
					Poke(forceRestart: false);
					return true;
				}
			}
			return false;
		}

		public bool Interact(TerrainRaycastResult raycastResult)
		{
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(raycastResult.Value));
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i].OnInteract(raycastResult, this))
				{
					if (ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksInteracted++;
					}
					Poke(forceRestart: false);
					return true;
				}
			}
			return false;
		}
		public bool Interact(MovingBlocksRaycastResult raycastResult)
		{
			if(raycastResult.MovingBlock == null) return false;
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(raycastResult.MovingBlock.Value));
			for(int i = 0; i < blockBehaviors.Length; i++)
			{
				if(blockBehaviors[i].OnInteract(raycastResult,this))
				{
					if(ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksInteracted++;
					}
					Poke(forceRestart: false);
					return true;
				}
			}
			return false;
		}

		public void Hit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection)
		{
			var hitInterval = HitInterval;
			ModsManager.HookAction("SetHitInterval",modLoader => {
				modLoader.SetHitInterval(this,ref hitInterval);
				return false;
			});
			if (!(m_subsystemTime.GameTime - m_lastHitTime > hitInterval))
			{
				return;
			}
			m_lastHitTime = m_subsystemTime.GameTime;
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(ActiveBlockValue)];
			if (!IsLevelSufficientForTool(ActiveBlockValue))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block.PlayerLevelRequired, block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				return;
			}
			float num = 0f;//伤害
			float num2 = 1f;//玩家命中率
			float num3 = 1f;//生物命中率
			if (ActiveBlockValue != 0)
			{
				num = block.GetMeleePower(ActiveBlockValue) * AttackPower * m_random.Float(0.8f, 1.2f);
				num2 = block.GetMeleeHitProbability(ActiveBlockValue);
			}
			else
			{
				num = AttackPower * m_random.Float(0.8f, 1.2f);
				num2 = 0.66f;
			}
			num2 *= ((componentBody.Velocity.Length() < 0.05f) ? 2f : 1f);
			bool flag;

            ModsManager.HookAction("OnMinerHit", modLoader =>
			{
				modLoader.OnMinerHit(this, componentBody, hitPoint, hitDirection, ref num, ref num2, ref num3, out bool Hitted);
				return false;
			});

			if (ComponentPlayer != null)
			{
				m_subsystemAudio.PlaySound("Audio/Swoosh", 1f, m_random.Float(-0.2f, 0.2f), componentBody.Position, 3f, autoDelay: false);
				flag = m_random.Bool(num2);
			}
			else
			{
				flag = m_random.Bool(num3);
            }
            num *= StrengthFactor;
            if (flag)
            {
                int durabilityReduction = 1;
                Attackment attackment = new MeleeAttackment(componentBody, Entity, hitPoint, hitDirection, num);
                ModsManager.HookAction("OnMinerHit2", loader =>
                {
                    loader.OnMinerHit2(this, componentBody, hitPoint, hitDirection, ref durabilityReduction, ref attackment);
                    return false;
                });
                AttackBody(attackment);
				DamageActiveTool(durabilityReduction);
			}
			else if (ComponentCreature is ComponentPlayer)
			{
				HitValueParticleSystem particleSystem = new(hitPoint + (0.75f * hitDirection), (1f * hitDirection) + ComponentCreature.ComponentBody.Velocity, Color.White, LanguageControl.Get(fName, 2));
				ModsManager.HookAction("SetHitValueParticleSystem", modLoader =>
				{
					modLoader.SetHitValueParticleSystem(particleSystem, null);
					return false;
				});
				Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
			}
			if (ComponentCreature.PlayerStats != null)
			{
				ComponentCreature.PlayerStats.MeleeAttacks++;
				if (flag)
				{
					ComponentCreature.PlayerStats.MeleeHits++;
				}
			}

			Poke(forceRestart: false);
		}

		public bool Aim(Ray3 aim, AimState state)
		{
			int num = Terrain.ExtractContents(ActiveBlockValue);
			Block block = BlocksManager.Blocks[num];
			if (block.IsAimable_(ActiveBlockValue))
			{
				if (!IsLevelSufficientForTool(ActiveBlockValue))
				{
					ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block.GetPlayerLevelRequired(ActiveBlockValue), block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
					Poke(forceRestart: false);
					return true;
				}
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(ActiveBlockValue));
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					if (blockBehaviors[i].OnAim(aim, this, state))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// 发出射线检测，检测玩家点击到的目标
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="mode">发出射线的意图</param>
		/// <param name="raycastTerrain">该射线是否和地形交互，为false时则忽略地形</param>
		/// <param name="raycastBodies">该射线是否和生物等实体交互，为false时则忽略实体</param>
		/// <param name="raycastMovingBlocks">该射线是否和移动方块交互，为false时则忽略移动方块</param>
		/// <param name="Reach">进行Raycast的距离</param>
		/// <returns></returns>
		public virtual object Raycast(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true, float? Reach = null)
		{
			float reach = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? SettingsManager.CreativeReach : 5f;
			if(Reach.HasValue) reach = Reach.Value;
			reach = Math.Min(reach, SettingsManager.VisibilityRange);
			Vector3 creaturePosition = ComponentCreature.ComponentCreatureModel.EyePosition;
			Vector3 start = ray.Position;
			var direction = Vector3.Normalize(ray.Direction);
			Vector3 end = ray.Position + (direction * (reach + 1f));
			Point3 startCell = Terrain.ToCell(start);
			BodyRaycastResult? bodyRaycastResult = null;
			if(raycastBodies) bodyRaycastResult = m_subsystemBodies.Raycast(start, end, 0.35f, (ComponentBody body, float distance) => Vector3.DistanceSquared(start + (distance * direction), creaturePosition) <= reach * reach && body.Entity != Entity && !body.IsChildOfBody(ComponentCreature.ComponentBody) && !ComponentCreature.ComponentBody.IsChildOfBody(body) && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f);
			MovingBlocksRaycastResult? movingBlocksRaycastResult = null;
			if(raycastMovingBlocks) movingBlocksRaycastResult = m_subsystemMovingBlocks.Raycast(start, end, extendToFillCells: true);
			TerrainRaycastResult? terrainRaycastResult = null;
			if(raycastTerrain) terrainRaycastResult = m_subsystemTerrain.Raycast(start, end, useInteractionBoxes: true, skipAirBlocks: true, delegate (int value, float distance)
			{
				if (Vector3.DistanceSquared(start + (distance * direction), creaturePosition) <= reach * reach)
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
					if (distance == 0f && block is CrossBlock && Vector3.Dot(direction, new Vector3(startCell) + new Vector3(0.5f) - start) < 0f)
					{
						return false;
					}
					if (mode == RaycastMode.Digging)
					{
						return !block.IsDiggingTransparent;
					}
					if (mode == RaycastMode.Interaction)
					{
						if (block.IsPlacementTransparent_(value))
						{
							return block.IsInteractive(m_subsystemTerrain, value);
						}
						return true;
					}
					if (mode == RaycastMode.Gathering)
					{
						return block.IsGatherable_(value);
					}
				}
				return false;
			});

            if (!raycastBodies) bodyRaycastResult = null;
            if (!raycastTerrain) terrainRaycastResult = null;
            if (!raycastMovingBlocks) movingBlocksRaycastResult = null;

            float num = bodyRaycastResult.HasValue ? bodyRaycastResult.Value.Distance : float.PositiveInfinity;
			float num2 = movingBlocksRaycastResult.HasValue ? movingBlocksRaycastResult.Value.Distance : float.PositiveInfinity;
			float num3 = terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : float.PositiveInfinity;
			if (num < num2 && num < num3)
			{
				return bodyRaycastResult.Value;
			}
			if (num2 < num && num2 < num3)
			{
				return movingBlocksRaycastResult.Value;
			}
			if (num3 < num && num3 < num2)
			{
				return terrainRaycastResult.Value;
			}
			return new Ray3(start, direction);
		}

		public T? Raycast<T>(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true, float? reach = null) where T : struct
		{
			object obj = Raycast(ray, mode, raycastTerrain, raycastBodies, raycastMovingBlocks, reach);
			if (!(obj is T))
			{
				return null;
			}
			return (T)obj;
		}

		public virtual void RemoveActiveTool(int removeCount)
		{
			if (Inventory != null)
			{
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, removeCount);
			}
		}

		public virtual void DamageActiveTool(int damageCount)
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || Inventory == null)
			{
				return;
			}
			int num = BlocksManager.DamageItem(ActiveBlockValue, damageCount, Entity);
			if (num != 0)
			{
				int slotCount = Inventory.GetSlotCount(Inventory.ActiveSlotIndex);
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, slotCount);
				if (Inventory.GetSlotCount(Inventory.ActiveSlotIndex) == 0)
				{
					Inventory.AddSlotItems(Inventory.ActiveSlotIndex, num, slotCount);
				}
			}
			else
			{
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
			}
		}

		public static void AttackBody(Attackment attackment)
		{
			try
			{
				attackment.ProcessAttackment();
			}
			catch (Exception e)
			{
				Log.Error("Attack execute error: " + e);
			}
		}
        public static void AttackBody(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower, bool isMeleeAttack)
        {
			if (isMeleeAttack) AttackBody(new MeleeAttackment(target?.Entity, attacker?.Entity, hitPoint, hitDirection, attackPower));
			else AttackBody(new ProjectileAttackment(target?.Entity, attacker?.Entity, hitPoint, hitDirection, attackPower, null));
        }

		[Obsolete("Use AddHitValueParticleSystem() in Attackment instead.", true)]
		public static void AddHitValueParticleSystem(float damage, Entity attacker, Entity attacked, Vector3 hitPoint, Vector3 hitDirection)
		{
			ComponentBody attackerBody = attacker?.FindComponent<ComponentBody>();
			ComponentPlayer attackerComponentPlayer = attacker?.FindComponent<ComponentPlayer>();
			ComponentHealth attackedComponentHealth = attacked?.FindComponent<ComponentHealth>();
            string text2 = (0f - damage).ToString("0", CultureInfo.InvariantCulture);
            Vector3 hitValueParticleVelocity = Vector3.Zero;
            if (attackerBody != null) hitValueParticleVelocity = attackerBody.Velocity;
            Color color = (attackerComponentPlayer != null && damage > 0f && attackedComponentHealth != null) ? Color.White : Color.Transparent;
            HitValueParticleSystem particleSystem = new(hitPoint + (0.75f * hitDirection), (1f * hitDirection) + hitValueParticleVelocity, color, text2);
            ModsManager.HookAction("SetHitValueParticleSystem", modLoader =>
            {
                modLoader.SetHitValueParticleSystem(particleSystem, null);
                return false;
            });
            attacked?.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
        }

        public void Update(float dt)
		{
			float num = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? (1f / SettingsManager.CreativeDigTime) : 4f;
			m_lastPokingPhase = PokingPhase;
			if (DigCellFace.HasValue || PokingPhase > 0f)
			{
				PokingPhase += num * m_subsystemTime.GameTimeDelta;
				if (PokingPhase > 1f)
				{
					PokingPhase = DigCellFace.HasValue ? MathUtils.Remainder(PokingPhase, 1f) : 0f;
				}
			}
			if (DigCellFace.HasValue && Time.FrameIndex - m_lastDigFrameIndex > 1)
			{
				DigCellFace = null;
			}
			if ((m_componentHealth != null && !(m_componentHealth.Health > 0f)) || !(AutoInteractRate > 0f) || !m_random.Bool(AutoInteractRate) || !m_subsystemTime.PeriodicGameTimeEvent(1.0, (float)(GetHashCode() % 100) / 100f))
			{
				return;
			}
			ComponentCreatureModel componentCreatureModel = ComponentCreature.ComponentCreatureModel;
			Vector3 eyePosition = componentCreatureModel.EyePosition;
			Vector3 forwardVector = componentCreatureModel.EyeRotation.GetForwardVector();
			for (int i = 0; i < 10; i++)
			{
				TerrainRaycastResult? terrainRaycastResult = Raycast<TerrainRaycastResult>(new Ray3(eyePosition, forwardVector + m_random.Vector3(0.75f)), RaycastMode.Interaction);
				if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.Distance < 1.5f && Terrain.ExtractContents(terrainRaycastResult.Value.Value) != 57 && Interact(terrainRaycastResult.Value))
				{
					break;
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
			m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			ComponentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
			ComponentPlayer = Entity.FindComponent<ComponentPlayer>();
			m_componentHealth = base.Entity.FindComponent<ComponentHealth>();
			ComponentFactors = Entity.FindComponent<ComponentFactors>(throwOnError: true);
			Inventory = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && ComponentPlayer != null
				? Entity.FindComponent<ComponentCreativeInventory>()
				: (IInventory)Entity.FindComponent<ComponentInventory>();
			AttackPower = valuesDictionary.GetValue<float>("AttackPower");
			HitInterval = valuesDictionary.GetValue<float>("HitInterval");
			AutoInteractRate = valuesDictionary.GetValue<float>("AutoInteractRate");
			if (string.CompareOrdinal(m_subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.4") < 0 || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Survival)
			{
				AutoInteractRate = 0f;
			}
		}
		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("AttackPower", AttackPower);
		}
		public static bool IsBlockPlacingAllowed(ComponentBody componentBody)
		{
			if (componentBody.StandingOnBody != null || componentBody.StandingOnValue.HasValue)
			{
				return true;
			}
			if (componentBody.ImmersionFactor > 0.01f)
			{
				return true;
			}
			if (componentBody.ParentBody != null && IsBlockPlacingAllowed(componentBody.ParentBody))
			{
				return true;
			}
			ComponentLocomotion componentLocomotion = componentBody.Entity.FindComponent<ComponentLocomotion>();
			if (componentLocomotion != null && componentLocomotion.LadderValue.HasValue)
			{
				return true;
			}
			return false;
		}

		public virtual float CalculateDigTime(int digValue, int toolValue)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(digValue)];
			float digResilience = block2.GetDigResilience(digValue);
			BlockDigMethod digBlockMethod = block2.GetBlockDigMethod(digValue);
			float ShovelPower = block.GetShovelPower(toolValue);
			float QuarryPower = block.GetQuarryPower(toolValue);
			float HackPower = block.GetHackPower(toolValue);

			if (ComponentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				if (digResilience < float.PositiveInfinity)
				{
					return 0f;
				}
				return float.PositiveInfinity;
			}
			if (ComponentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
			{
				float num = 0f;
				if (digBlockMethod == BlockDigMethod.Shovel && ShovelPower >= 2f)
				{
					num = ShovelPower;
				}
				else if (digBlockMethod == BlockDigMethod.Quarry && QuarryPower >= 2f)
				{
					num = QuarryPower;
				}
				else if (digBlockMethod == BlockDigMethod.Hack && HackPower >= 2f)
				{
					num = HackPower;
				}
				num *= StrengthFactor;
				if (!(num > 0f))
				{
					return float.PositiveInfinity;
				}
				return MathUtils.Max(digResilience / num, 0f);
			}
			float num2 = 1f;
			if (digBlockMethod == BlockDigMethod.Shovel)
			{
				num2 = ShovelPower;
			}
			else if (digBlockMethod == BlockDigMethod.Quarry)
			{
				num2 = QuarryPower;
			}
			else if (digBlockMethod == BlockDigMethod.Hack)
			{
				num2 = HackPower;
			}
			num2 *= DigSpeedFactor;
			if (!(num2 > 0f))
			{
				return float.PositiveInfinity;
			}
			return MathUtils.Max(digResilience / num2, 0f);
		}

		public virtual bool IsLevelSufficientForTool(int toolValue)
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
				if (ComponentPlayer != null && ComponentPlayer.PlayerData.Level < block.GetPlayerLevelRequired(toolValue))
				{
					return false;
				}
			}
			return true;
		}

		public virtual int FindBestInventoryToolForDigging(int digValue)
		{
			int result = 0;
			float num = CalculateDigTime(digValue, 0);
			foreach (IInventory item in base.Entity.FindComponents<IInventory>())
			{
				if (item is ComponentCreativeInventory)
				{
					continue;
				}
				for (int i = 0; i < item.SlotsCount; i++)
				{
					int slotValue = item.GetSlotValue(i);
					if (IsLevelSufficientForTool(slotValue))
					{
						float num2 = CalculateDigTime(digValue, slotValue);
						if (num2 < num)
						{
							num = num2;
							result = slotValue;
						}
					}
				}
			}
			return result;
		}
	}
}
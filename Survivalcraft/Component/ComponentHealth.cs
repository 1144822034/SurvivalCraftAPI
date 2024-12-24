using Engine;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
    public class ComponentHealth : Component, IUpdateable
    {
        public SubsystemTime m_subsystemTime;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemPickables m_subsystemPickables;

        public ComponentCreature m_componentCreature;

        public ComponentPlayer m_componentPlayer;

        public ComponentOnFire m_componentOnFire;

        public ComponentFactors m_componentFactors;

        //public Block ExperienceOrbBlock;

        public int ExperienceOrbBlockIndex = 8;

        public float m_lastHealth;

        public bool m_wasStanding;

        public float m_redScreenFactor;

        public Random m_random = new();

        public bool m_regenerateLifeEnabled = true;//生命再生

		public float? RedScreenFactorInCrush = 1f;
		public virtual float VoidDamageFactor { get; set; }//y轴过高或者过低造成的伤害系数
        public virtual float AirLackResilience { get; set; }//溺水伤害抗性
        public virtual float MagmaResilience { get; set; }//熔岩伤害抗性
        public virtual float CrushResilience { get; set; }//挤压伤害抗性
        public virtual float SpikeResilience { get; set; }//尖刺伤害抗性
        public virtual float ExplosionResilience { get; set; }//爆炸伤害抗性

        public virtual void OnSpiked(SubsystemBlockBehavior spikeBlockBehavior, float damage , CellFace cellFace, float velocity, ComponentBody componentBody, string causeOfDeath)
        {
            Injury blockInjury = new BlockInjury(damage, cellFace, causeOfDeath, componentBody.m_subsystemTerrain);
            ModsManager.HookAction("OnCreatureSpiked", loader =>
            {
                loader.OnCreatureSpiked(this, spikeBlockBehavior, cellFace, velocity, ref blockInjury);
                return false;
            });
            Injure(blockInjury);
        }

        public virtual float CalculateFallDamage()
        {
            float velocityChange = MathF.Abs(m_componentCreature.ComponentBody.CollisionVelocityChange.Y);
            if (!m_wasStanding && velocityChange > FallResilience && !m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled)
            {
                float num4 = MathUtils.Sqr(MathUtils.Max(velocityChange - FallResilience, 0f)) / 15f;
                num4 /= m_componentFactors?.ResilienceFactor ?? 1;
                return num4;
            }
            return 0f;
        }

        public virtual bool StackExperienceOnKill { get; set; }

        public virtual string CauseOfDeath
        {
            get;
            set;
        }

        public virtual bool IsInvulnerable
        {
            get;
            set;
        }

        public virtual float Health
        {
            get;
            set;
        }

        public virtual float HealthChange
        {
            get;
            set;
        }

        public virtual BreathingMode BreathingMode
        {
            get;
            set;
        }

        public virtual float Air
        {
            get;
            set;
        }

        public virtual float AirCapacity
        {
            get;
            set;
        }

        public virtual bool CanStrand
        {
            get;
            set;
        }
        public float m_attackResilience;
        public float m_fallResilience;
        public float m_fireResilience;

        /// <summary>
        /// 攻击抗性
        /// </summary>
        public virtual float AttackResilience
        {
            get;
            set;
        }
        /// <summary>
        /// 掉落抗性
        /// </summary>
        public virtual float FallResilience
        {
            get;
            set;
        }

        public virtual float FireResilience
        {
            get;
            set;
        }

        public virtual double? DeathTime
        {
            get;
            set;
        }

        public virtual float CorpseDuration
        {
            get;
            set;
        }

        /// <summary>
        /// 攻击抗性加成系数
        /// </summary>
        public virtual float AttackResilienceFactor { get; set; }
        /// <summary>
        /// 掉落抗性加成系数
        /// </summary>
        public virtual float FallResilienceFactor { get; set; }
        /// <summary>
        /// 火焰伤害抗性系数
        /// </summary>
        public virtual float FireResilienceFactor { get; set; }
        /// <summary>
        /// 生命恢复速度系数
        /// </summary>
        public virtual float HealFactor { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        [Obsolete("Use ComponentHealth.Injured instead of attacked.")]
        public virtual Action<ComponentCreature> Attacked {  get; set; }
        public virtual Action<Injury> Injured { get; set; }

        public virtual void Heal(float amount)
        {
            lock (this)
            {
                if (amount > 0f && Health < 1f)
                {
                    Health = MathUtils.Saturate(Health + (amount * HealFactor));
                }
            }
        }

        public virtual void Injure(float amount, ComponentCreature attacker, bool ignoreInvulnerability, string cause)
        {
            Injury injury = new Injury(amount, attacker, ignoreInvulnerability, cause);
            Injure(injury);
        }
        public virtual void Injure(Injury injury)
        {
            if (injury == null) return;
            if (injury.ComponentHealth == null) injury.ComponentHealth = this;
            if (Health > 0f)
            {
                lock(this) {
                    injury.Process();
                    if (Health <= 0f)
                    {
                        ModsManager.HookAction("OnCreatureDying", loader =>
                        {
                            loader.OnCreatureDying(this, injury);
                            return false;
                        });
                    }
                    if (Health <= 0f)
                    {
                        Die(injury);
                    }
                }
            }
        }
        public virtual void Die(Injury injury)
        {
            Health = 0f;
            ComponentCreature attacker = injury?.Attacker;
            int experienceOrbDropCount = (int)MathF.Ceiling(m_componentCreature.ComponentHealth.AttackResilience / 12f);
            bool calculateInKill = true;
            ModsManager.HookAction("OnCreatureDied", loader =>
            {
                loader.OnCreatureDied(this, injury, ref experienceOrbDropCount, ref calculateInKill);
                return false;
            });
            CauseOfDeath = injury?.Cause;
            if (m_componentCreature.PlayerStats != null && calculateInKill)
            {
                m_componentCreature.PlayerStats.AddDeathRecord(new PlayerStats.DeathRecord
                {
                    Day = m_subsystemTimeOfDay.Day,
                    Location = m_componentCreature.ComponentBody.Position,
                    Cause = CauseOfDeath
                });
            }
            if (attacker != null)
            {
                ComponentPlayer componentPlayer = attacker?.Entity.FindComponent<ComponentPlayer>();
                if (componentPlayer != null)
                {
                    if (calculateInKill)
                    {
                        if (m_componentPlayer != null)
                        {
                            componentPlayer.PlayerStats.PlayerKills++;
                        }
                        else if (m_componentCreature.Category == CreatureCategory.LandPredator || m_componentCreature.Category == CreatureCategory.LandOther)
                        {
                            componentPlayer.PlayerStats.LandCreatureKills++;
                        }
                        else if (m_componentCreature.Category == CreatureCategory.WaterPredator || m_componentCreature.Category == CreatureCategory.WaterOther)
                        {
                            componentPlayer.PlayerStats.WaterCreatureKills++;
                        }
                        else
                        {
                            componentPlayer.PlayerStats.AirCreatureKills++;
                        }
                    }

                    if (StackExperienceOnKill)
                    {
                        for (int i = 0; i < Math.Min(100, experienceOrbDropCount); i++) //调整经验球的掉落逻辑，多于100个时则成组掉落防止卡顿
                        {
                            Vector2 vector = m_random.Vector2(2.5f, 3.5f);
                            int dropInWave = experienceOrbDropCount / 100;
                            if (i < experienceOrbDropCount % 100) dropInWave++;
                            m_subsystemPickables.AddPickable(ExperienceOrbBlockIndex, dropInWave, m_componentCreature.ComponentBody.Position, new Vector3(vector.X, 6f, vector.Y), null, Entity);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < experienceOrbDropCount; i++)
                        {
                            Vector2 vector = m_random.Vector2(2.5f, 3.5f);
                            m_subsystemPickables.AddPickable(ExperienceOrbBlockIndex, 1, m_componentCreature.ComponentBody.Position, new Vector3(vector.X, 6f, vector.Y), null, Entity);
                        }
                    }
                }
            }
        }
        public void Update(float dt)
        {
            lock (this)
            {
                //更新属性加成
                AttackResilience = m_attackResilience * AttackResilienceFactor;
                FallResilience = m_fallResilience * FallResilienceFactor;
                FireResilienceFactor = m_fireResilience * FireResilienceFactor;
                Vector3 position = m_componentCreature.ComponentBody.Position;
                if (m_regenerateLifeEnabled && Health > 0f && Health < 1f)
                {
                    float num = 0f;
                    if (m_componentPlayer != null)
                    {
                        if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
                        {
                            num = 0.0166666675f;
                        }
                        else if (m_componentPlayer.ComponentSleep.SleepFactor == 1f && m_componentPlayer.ComponentVitalStats.Food > 0f)
                        {
                            num = 0.00166666671f;
                        }
                        else if (m_componentPlayer.ComponentVitalStats.Food > 0.5f)
                        {
                            num = 0.00111111114f;
                        }
                    }
                    else
                    {
                        num = 0.00111111114f;
                    }
                    Heal(m_subsystemGameInfo.TotalElapsedGameTimeDelta * num);
                }
                //溺水空气值
                if (BreathingMode == BreathingMode.Air)
                {
                    int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position.X), Terrain.ToCell(m_componentCreature.ComponentCreatureModel.EyePosition.Y), Terrain.ToCell(position.Z));
                    Air = BlocksManager.Blocks[cellContents] is FluidBlock || position.Y > 259f ? MathUtils.Saturate(Air - (dt / AirCapacity)) : 1f;
                }
                else if (BreathingMode == BreathingMode.Water)
                {
                    Air = (m_componentCreature.ComponentBody.ImmersionFactor > 0.25f || m_componentCreature.ComponentBody.IsEmbeddedInIce) ? 1f : MathUtils.Saturate(Air - (dt / AirCapacity));
                }
                //岩浆伤害
                if (m_componentCreature.ComponentBody.ImmersionFactor > 0f && m_componentCreature.ComponentBody.ImmersionFluidBlock is MagmaBlock)
                {
                    Injure(1f / MagmaResilience * m_componentCreature.ComponentBody.ImmersionFactor * dt, null, ignoreInvulnerability: false, LanguageControl.Get(GetType().Name, 1));
                    float num2 = 1.1f + (0.1f * (float)Math.Sin(12.0 * m_subsystemTime.GameTime));
                    m_redScreenFactor = MathUtils.Max(m_redScreenFactor, num2 * 0.75f * m_componentCreature.ComponentBody.ImmersionFactor / MagmaResilience);
                }
                //跌落伤害
                float fallDamage = CalculateFallDamage();
                ModsManager.HookAction("CalculateFallDamage", loader =>
                {
                    loader.CalculateFallDamage(this, ref fallDamage);
                    return false;
                });
                if (fallDamage > 0f) Injure(fallDamage, null, ignoreInvulnerability: false, LanguageControl.Get(GetType().Name, 2));
                m_wasStanding = m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.StandingOnBody != null;
                //虚空伤害
                if (VoidDamageFactor > 0f && (position.Y < 0f || position.Y > 296f) && m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0))
                {
                    Injure(VoidDamageFactor * 0.1f, null, ignoreInvulnerability: true, LanguageControl.Get(GetType().Name, 3));
                    m_componentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(GetType().Name, 4), Color.White, blinking: true, playNotificationSound: false);
                }
                //溺水伤害
                bool num5 = m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0);
                if (num5 && Air == 0f)
                {
                    float num6 = 1f / AirLackResilience;
                    num6 /= m_componentFactors?.ResilienceFactor ?? 1;
                    Injure(num6, null, ignoreInvulnerability: false, LanguageControl.Get(GetType().Name, 7));
                }
                //火焰伤害
                if (num5 && (m_componentOnFire.IsOnFire || m_componentOnFire.TouchesFire))
                {
                    float num7 = 1f / FireResilience;
                    num7 /= m_componentFactors?.ResilienceFactor ?? 1;
                    Injure(new FireInjury(num7, m_componentOnFire.Attacker));
                }
                //挤压伤害
                if (m_componentCreature.ComponentBody.CrushedTime > 0f)
                {
	                if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0) && m_componentCreature.ComponentBody.CrushedTime > 0.2f)
	                {
		                Injure(1 / CrushResilience, null, ignoreInvulnerability: true, LanguageControl.Get("ComponentMiner", "crushed"));
	                }
	                if(RedScreenFactorInCrush.HasValue) m_redScreenFactor = RedScreenFactorInCrush.Value;
                }
                //鱼类搁浅伤害
                if (num5 && CanStrand && m_componentCreature.ComponentBody.ImmersionFactor < 0.25f && (m_componentCreature.ComponentBody.StandingOnValue != 0 || m_componentCreature.ComponentBody.StandingOnBody != null))
                {
                    Injure(1f / AirLackResilience, null, ignoreInvulnerability: false, LanguageControl.Get(GetType().Name, 6));
                }
                //伤害结算
                float lastHealth = m_lastHealth;
                HealthChange = Health - m_lastHealth;
                m_lastHealth = Health;
                float redScreenFactorCalculated = m_redScreenFactor;
				float creatureModelRedFactorCalculated = MathUtils.Saturate(m_componentCreature.ComponentCreatureModel.m_injuryColorFactor - (3f * dt)); ;
                bool playPainSound = true;
                int healthBarFlashCount = Math.Clamp((int)((0f - HealthChange) * 30f), 0, 10);
                if (redScreenFactorCalculated > 0.01f)
                {
                    redScreenFactorCalculated *= MathF.Pow(0.2f, dt);
                }
                else
                {
                    redScreenFactorCalculated = 0f;
                }
                if (HealthChange < 0f)
                {
                    redScreenFactorCalculated += -4f * HealthChange;
					creatureModelRedFactorCalculated = 1f;
                }
                ModsManager.HookAction("ChangeVisualEffectOnInjury", loader =>
                {
                    loader.ChangeVisualEffectOnInjury(this, lastHealth, ref redScreenFactorCalculated, ref playPainSound, ref healthBarFlashCount, ref creatureModelRedFactorCalculated);
                    return false;
                });
                if (HealthChange < 0f)
                {
                    if (playPainSound) m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    m_componentPlayer?.ComponentGui.HealthBarWidget.Flash(healthBarFlashCount);
                }
                m_redScreenFactor = redScreenFactorCalculated;
				m_componentCreature.ComponentCreatureModel.m_injuryColorFactor = creatureModelRedFactorCalculated;
				if (m_componentPlayer != null)
                {
                    m_componentPlayer.ComponentScreenOverlays.RedoutFactor = MathUtils.Max(m_componentPlayer.ComponentScreenOverlays.RedoutFactor, m_redScreenFactor);
                }
                if (m_componentPlayer != null)
                {
                    m_componentPlayer.ComponentGui.HealthBarWidget.Value = Health;
                }
                if (Health == 0f && HealthChange < 0f)
				{
					DeathTime = m_subsystemGameInfo.TotalElapsedGameTime;
					Vector3 position2 = m_componentCreature.ComponentBody.Position + new Vector3(0f,m_componentCreature.ComponentBody.StanceBoxSize.Y / 2f,0f);
					float x = m_componentCreature.ComponentBody.StanceBoxSize.X;
					KillParticleSystem killParticleSystem = new KillParticleSystem(m_subsystemTerrain,position2,x);
					bool dropAllItems = true;
					ModsManager.HookAction("DeadBeforeDrops", loader =>
                    {
                        loader.DeadBeforeDrops(this, ref killParticleSystem, ref dropAllItems);
                        return false;
                    });
					if(killParticleSystem != null) m_subsystemParticles.AddParticleSystem(killParticleSystem);
					if (dropAllItems)
                    {
                        Vector3 position3 = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
                        foreach (IInventory item in Entity.FindComponents<IInventory>())
                        {
                            item.DropAllItems(position3);
                        }
                    }
                }
                if (Health <= 0f && CorpseDuration > 0f && m_subsystemGameInfo.TotalElapsedGameTime - DeathTime > CorpseDuration)
                {
                    m_componentCreature.ComponentSpawn.Despawn();
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
        {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
            m_componentOnFire = Entity.FindComponent<ComponentOnFire>(throwOnError: true);
            m_componentFactors = Entity.FindComponent<ComponentFactors>(throwOnError: true);
            AttackResilience = valuesDictionary.GetValue<float>("AttackResilience");
            FallResilience = valuesDictionary.GetValue<float>("FallResilience");
            FireResilience = valuesDictionary.GetValue<float>("FireResilience");
            m_attackResilience = AttackResilience;
            m_fallResilience = FallResilience;
            m_fireResilience = FireResilience;
            CorpseDuration = valuesDictionary.GetValue<float>("CorpseDuration");
            BreathingMode = valuesDictionary.GetValue<BreathingMode>("BreathingMode");
            CanStrand = valuesDictionary.GetValue<bool>("CanStrand");
            Health = valuesDictionary.GetValue<float>("Health");
            Air = valuesDictionary.GetValue<float>("Air");
            AirCapacity = valuesDictionary.GetValue<float>("AirCapacity");
            double value = valuesDictionary.GetValue<double>("DeathTime");
            AttackResilienceFactor = 1f;
            FallResilienceFactor = 1f;
            FireResilienceFactor = 1f;
            HealFactor = valuesDictionary.GetValue<float>("HealFactor");
            VoidDamageFactor = valuesDictionary.GetValue<float>("VoidDamageFactor");
            AirLackResilience = valuesDictionary.GetValue<float>("AirLackResilience");
            MagmaResilience = valuesDictionary.GetValue<float>("MagmaResilience");
            CrushResilience = valuesDictionary.GetValue<float>("CrushResilience");
            SpikeResilience = valuesDictionary.GetValue<float>("SpikeResilience");
            ExplosionResilience = valuesDictionary.GetValue<float>("ExplosionResilience");
            StackExperienceOnKill = true;
            DeathTime = (value >= 0.0) ? new double?(value) : null;
            CauseOfDeath = valuesDictionary.GetValue<string>("CauseOfDeath");
            ExperienceOrbBlockIndex = BlocksManager.GetBlockIndex<ExperienceBlock>();
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && Entity.FindComponent<ComponentPlayer>() != null)
            {
                IsInvulnerable = true;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
        {
            valuesDictionary.SetValue("Health", Health);
            valuesDictionary.SetValue("Air", Air);
            if (DeathTime.HasValue)
            {
                valuesDictionary.SetValue("DeathTime", DeathTime);
            }
            if (!string.IsNullOrEmpty(CauseOfDeath))
            {
                valuesDictionary.SetValue("CauseOfDeath", CauseOfDeath);
            }
        }


    }
}

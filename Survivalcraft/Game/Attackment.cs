using Engine;
using GameEntitySystem;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	/// <summary>
	/// The spell "Attackment" is wrong,
	/// But it is not recommended to change it because many mods rely on this class.
	/// Change of its name will make a lot of mods unavailable.
	/// </summary>
    public class Attackment
    {
        public Entity Target;
        public Entity Attacker;
        protected Random m_random = new Random();
        public Attackment(Entity target, Entity attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower)
        {
            Target = target;
            Attacker = attacker;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            AttackPower = attackPower;
            ComponentCreature attackerCreature = attacker?.FindComponent<ComponentCreature>();
            if (attackerCreature != null)
            {
                string str = attackerCreature.KillVerbs[m_random.Int(0, attackerCreature.KillVerbs.Count - 1)];
                string attackerName = attackerCreature.DisplayName;
                CauseOfDeath = string.Format(LanguageControl.Get("ComponentMiner", 4), attackerName, LanguageControl.Get("ComponentMiner", str));
            }
            else
            {
                CauseOfDeath = LanguageControl.Get("ComponentMiner", m_random.Int(0, 5) + 5);
            }
            ComponentDamage componentDamage = Target.FindComponent<ComponentDamage>();
            if (componentDamage != null) AttackSoundName = componentDamage.DamageSoundName;
            AttackSoundPitch = m_random.Float(-0.3f, 0.3f);
        }
        public Attackment(ComponentBody target, Entity attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower)
            : this(target.Entity, attacker, hitPoint, hitDirection, attackPower) 
        {
            
        }
        public virtual ComponentBody TargetBody
        {
            get
            {
                return Target.FindComponent<ComponentBody>();
            }
            set
            {
                Target = value.Entity;
            }
        }
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public float AttackPower;
        public float? StunTimeSet;
        public float StunTimeAdd = 0.2f;
        public float ImpulseFactor = 2f;
        public string CauseOfDeath = String.Empty;
        public bool EnableArmorProtection = true;
        public bool EnableResilienceFactor = true;
        public bool EnableHitValueParticleSystem = true;
        public string AttackSoundName = "Audio/Impacts/Body";
        public float AttackSoundVolume = 1f;
        public float AttackSoundPitch = 0f;

		public bool AllowImpulseAndStunWhenDamageIsZero = true;

		public float? m_injuryAmount = null;
		/// <summary>
		/// 模组可以向Dictionary里面添加内容，另一个模组可以从Dictionary读取内容，以实现模组联动效果
		/// </summary>
		public ValuesDictionary DictionaryForOtherMods = new ValuesDictionary();
		public virtual float CalculateInjuryAmount()
        {
			if(m_injuryAmount != null) return m_injuryAmount.Value;
            if (AttackPower <= 0f) return AttackPower;
            float attackPower = AttackPower;
            if (EnableArmorProtection)
            {
                ComponentClothing componentClothing = Target.FindComponent<ComponentClothing>();
                if (componentClothing != null)
                {
                    attackPower = componentClothing.ApplyArmorProtection(this);
                }
            }
            if (EnableResilienceFactor)
            {
                ComponentFactors componentFactors = Target.FindComponent<ComponentFactors>();
                if (componentFactors != null)
                {
                    attackPower /= componentFactors.ResilienceFactor;
                }
            }
            ComponentHealth componentHealth = Target.FindComponent<ComponentHealth>();
            if (componentHealth != null) return attackPower / componentHealth.AttackResilience;
            ComponentDamage componentDamage = Target.FindComponent<ComponentDamage>();
            if (componentDamage != null) return attackPower / componentDamage.AttackResilience;
            return -1f;
        }

        public virtual void AddHitValueParticleSystem(float damage)
        {
			if(!EnableHitValueParticleSystem) return;
            ComponentBody attackerBody = Attacker?.FindComponent<ComponentBody>();
            ComponentPlayer attackerComponentPlayer = Attacker?.FindComponent<ComponentPlayer>();
            ComponentHealth attackedComponentHealth = Target?.FindComponent<ComponentHealth>();
            string text2 = (0f - damage).ToString("0", CultureInfo.InvariantCulture);
            Vector3 hitValueParticleVelocity = Vector3.Zero;
            if (attackerBody != null) hitValueParticleVelocity = attackerBody.Velocity;
            Color color = (attackerComponentPlayer != null && damage > 0f && attackedComponentHealth != null) ? Color.White : Color.Transparent;
            HitValueParticleSystem particleSystem = new(HitPoint + (0.75f * HitDirection), (1f * HitDirection) + hitValueParticleVelocity, color, text2);
            ModsManager.HookAction("SetHitValueParticleSystem", modLoader =>
            {
                modLoader.SetHitValueParticleSystem(particleSystem, this);
                return false;
            });
            Target.Project.FindSubsystem<SubsystemParticles>()?.AddParticleSystem(particleSystem);
        }
        public virtual void ProcessAttackmentToCreature(out float injuryAmount)
        {
            ComponentHealth componentHealth = Target.FindComponent<ComponentHealth>();
            ComponentBody componentBody = Target.FindComponent<ComponentBody>();
            ComponentCreature attackerCreature = Attacker?.FindComponent<ComponentCreature>();
			if(componentHealth == null || componentBody == null) { injuryAmount = 0f; return; }
            injuryAmount = CalculateInjuryAmount();
            float healthBeforeAttack = componentHealth.Health;
            componentHealth.Injure(new AttackInjury(injuryAmount, this));
            if (injuryAmount > 0f)
            {
				if(AttackSoundName != "")
					Target.Project.FindSubsystem<SubsystemAudio>()?.PlayRandomSound(AttackSoundName, AttackSoundVolume, AttackSoundPitch, componentBody.Position, 4f, autoDelay: false);
                //显示粒子效果的攻击，不需要一定是玩家攻击
                float num2 = (healthBeforeAttack - componentHealth.Health) * componentHealth.AttackResilience;
                AddHitValueParticleSystem(num2);
            }
        }
        public virtual void ProcessAttackmentToNonCreature(out float injuryAmount)
        {
            ComponentDamage componentDamage = Target.FindComponent<ComponentDamage>();
            ComponentBody componentBody = Target.FindComponent<ComponentBody>();
            ComponentCreature attackerCreature = Attacker?.FindComponent<ComponentCreature>();
			if(componentDamage == null || componentBody == null) { injuryAmount = 0f; return; }
            injuryAmount = CalculateInjuryAmount();
			m_injuryAmount = injuryAmount;
            float hitPointsBeforeAttack = componentDamage.Hitpoints;
            componentDamage.Damage(injuryAmount);
            float damage = (hitPointsBeforeAttack - componentDamage.Hitpoints) * componentDamage.AttackResilience;
            AddHitValueParticleSystem(damage);
            if (injuryAmount > 0f&& AttackSoundName!="")
            {
                Target.Project.FindSubsystem<SubsystemAudio>()?.PlayRandomSound(AttackSoundName, AttackSoundVolume, AttackSoundPitch, componentBody.Position, 4f, autoDelay: false);
            }
        }

        public virtual bool DisableFriendlyFire()
        {
            if (Attacker?.FindComponent<ComponentPlayer>() != null && Target?.FindComponent<ComponentPlayer>() != null
                && !Target.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.IsFriendlyFireEnabled)
            {
                Attacker?.FindComponent<ComponentGui>()?.DisplaySmallMessage(LanguageControl.Get("ComponentMiner", 3), Color.White, blinking: true, playNotificationSound: true);
                return true;
            }
            return false;
        }

        public virtual void ImpulseTarget()
        {
            if (ImpulseFactor <= 0f) return;
            Target.FindComponent<ComponentBody>().ApplyImpulse(ImpulseFactor * Vector3.Normalize(HitDirection + m_random.Vector3(0.1f) + (0.2f * Vector3.UnitY)));
        }

        public virtual void StunTarget()
        {
            if (ImpulseFactor <= 0f) return;
            ComponentLocomotion componentLocomotion = Target.FindComponent<ComponentLocomotion>();
            if (componentLocomotion != null)
            {
                componentLocomotion.StunTime = StunTimeSet ?? componentLocomotion.StunTime + StunTimeAdd;
            }
        }
        public virtual void ProcessAttackment()
        {
            if (DisableFriendlyFire()) return;
            ModsManager.HookAction("ProcessAttackment", loader =>
            {
                loader.ProcessAttackment(this);
                return false;
            });
			float injuryAmount = 0f;
            if (AttackPower > 0f)
			{
				ComponentBody componentBody = Target.FindComponent<ComponentBody>();
				componentBody?.Attacked?.Invoke(this);
				ComponentHealth componentHealth = Target.FindComponent<ComponentHealth>();
                if (componentHealth != null)
                {
                    ProcessAttackmentToCreature(out injuryAmount);
                }
                ComponentDamage componentDamage = Target.FindComponent<ComponentDamage>();
                if (componentDamage != null)
                {
                    ProcessAttackmentToNonCreature(out injuryAmount);
                }
            }
            ModsManager.HookAction("AttackPowerParameter", modloader =>
            {
                bool reclalculate = false;
                float stunTimeSet = StunTimeSet ?? -1f;
                modloader.AttackPowerParameter(Target.FindComponent<ComponentBody>(), Attacker?.FindComponent<ComponentCreature>(), HitPoint, HitDirection, ref ImpulseFactor, ref stunTimeSet, ref reclalculate);
                if(stunTimeSet >= 0f) StunTimeSet = stunTimeSet;
                return false;
            });
			if(AllowImpulseAndStunWhenDamageIsZero || injuryAmount > 0f)
			{
				ImpulseTarget();
				StunTarget();
			}
        }
    }
}

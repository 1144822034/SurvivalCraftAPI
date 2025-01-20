using GameEntitySystem;
using Engine;
using TemplatesDatabase;
using System.Reflection;
using static Game.ComponentLevel;

namespace Game
{
    public class ComponentFactors : Component, IUpdateable
    {
        public Random m_random = new();

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTime m_subsystemTime;

		/// <summary>
		/// 模组如果有自定义的Factors，可以使用这个OtherFactors。例如使用OtherFactors["AttackRate"]来定义攻击频率。
		/// </summary>
        public Dictionary<string, List<Factor>> OtherFactors = new Dictionary<string, List<Factor>>();

		/// <summary>
		/// 这四个Factors是可以调整的影响因素
		/// </summary>
		public List<Factor> m_strengthFactors = [];
		public List<Factor> m_speedFactors = [];
		public List<Factor> m_hungerFactors = [];
		public List<Factor> m_resilienceFactors = [];
        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public static string fName = "ComponentFactors";

        public float StrengthFactor
        {
            get;
            set;
        }

        public float ResilienceFactor
        {
            get;
            set;
        }

        public float SpeedFactor
        {
            get;
            set;
        }

        public float HungerFactor
        {
            get;
            set;
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
        {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
            StrengthFactor = 1f;
            SpeedFactor = 1f;
            HungerFactor = 1f;
            ResilienceFactor = 1f;
        }

		public float CalculateFactorCount(ICollection<Factor> factors)
		{
			float ans = 1f;
			foreach(var factor in factors)
			{
				ans *= factor.Value;
			}
			return ans;
		}

		public virtual void GenerateStrengthFactors()
		{
			m_strengthFactors.Clear();
		}
		public virtual void GenerateResilienceFactors()
		{
			m_resilienceFactors.Clear();
		}
		public virtual void GenerateSpeedFactors()
		{
			m_speedFactors.Clear();
		}
		public virtual void GenerateHungerFactors()
		{
			m_hungerFactors.Clear();
		}
        public virtual float CalculateStrengthFactor(ICollection<Factor> factors) {
			if(factors is List<Factor> factorsList) factorsList.AddRange(m_strengthFactors);
            return CalculateFactorCount(m_strengthFactors);
        }
        public virtual float CalculateResilienceFactor(ICollection<Factor> factors)
        {
			if(factors is List<Factor> factorsList) factorsList.AddRange(m_resilienceFactors);
			return CalculateFactorCount(m_resilienceFactors);
        }
        public virtual float CalculateSpeedFactor(ICollection<Factor> factors)
        {
			if(factors is List<Factor> factorsList) factorsList.AddRange(m_speedFactors);
			return CalculateFactorCount(m_speedFactors);
        }
        public virtual float CalculateHungerFactor(ICollection<Factor> factors)
        {
			if(factors is List<Factor> factorsList) factorsList.AddRange(m_hungerFactors);
			return CalculateFactorCount(m_hungerFactors);
        }

		/// <summary>
		/// 对等级系统的更新进行了调整。
		/// 第一步是GenerateFactors对四个属性进行生成，此时四个m_xxxFactors会拥有初始值
		/// 第二步在ModLoader接口控制四个m_xxxFactors的值。模组此时可以对这些Factors进行增删改
		/// 第三步是计算这些Factors的最终结果，并进行赋值
		/// </summary>
		/// <param name="dt"></param>
        public virtual void Update(float dt)
		{
			GenerateStrengthFactors();
			GenerateResilienceFactors();
			GenerateSpeedFactors();
			GenerateHungerFactors();
			ModsManager.HookAction("OnFactorsGenerate",loader => {
				loader.OnFactorsGenerate(this);
				return false;
			});
			StrengthFactor = CalculateStrengthFactor(null);
            SpeedFactor = CalculateSpeedFactor(null);
            HungerFactor = CalculateHungerFactor(null);
            ResilienceFactor = CalculateResilienceFactor(null);
			ModsManager.HookAction("OnFactorsUpdate",Loader => {
				Loader.OnFactorsUpdate(this,dt);
				return false;
			});
		}
    }
}

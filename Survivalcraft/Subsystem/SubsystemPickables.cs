using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;
namespace Game
{
	public class SubsystemPickables : Subsystem, IDrawable, IUpdateable
	{
		public SubsystemAudio m_subsystemAudio;

		public SubsystemPlayers m_subsystemPlayers;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemExplosions m_subsystemExplosions;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

		[Obsolete("该字段已弃用，掉落物被玩家的拾取逻辑被转移到ComponentPickableGathererPlayer中")]
		public List<ComponentPlayer> m_tmpPlayers = [];

		public List<Pickable> m_pickables = [];

		public List<Pickable> m_pickablesToRemove = [];

		public PrimitivesRenderer3D m_primitivesRenderer = new();

		public Random m_random = new();

		public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new();

		public static int[] m_drawOrders = new int[]
		{
			10
		};

		public ReadOnlyList<Pickable> Pickables => new(m_pickables);

		public int[] DrawOrders => m_drawOrders;

		public virtual Action<Pickable> PickableAdded { get; set; }
		public virtual Action<Pickable> PickableRemoved { get; set; }
		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public virtual Pickable AddPickable(Pickable pickable)
        {
            pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
            ModsManager.HookAction("OnPickableAdded", loader =>
            {
                loader.OnPickableAdded(this, ref pickable, null);
                return false;
            });
            lock (m_pickables)
            {
                m_pickables.Add(pickable);
            }
            PickableAdded?.Invoke(pickable);
            return pickable;
        }
        public virtual Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
		{
			return AddPickable(value, count, position, velocity, stuckMatrix, null);
        }
        public virtual Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner)
        {
            return AddPickable<Pickable>(value, count, position, velocity, stuckMatrix, owner);
        }
        public virtual Pickable CreatePickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner)
		{
			return CreatePickable<Pickable>(value, count, position, velocity, stuckMatrix, owner);
		}
        public virtual T CreatePickable<T>(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner) where T : Pickable, new()
		{
            var pickable = new T();
            pickable.Initialize(value, count, position, velocity, stuckMatrix, owner);
			return pickable;
        }

        public virtual T AddPickable<T>(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner) where T : Pickable, new()
		{
			T pickable = CreatePickable<T>(value, count, position, velocity, stuckMatrix, owner);
            Pickable pickable2 = AddPickable(pickable);
			return pickable2 as T;
		}

		public void Draw(Camera camera, int drawOrder)
		{
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			var matrix = Matrix.CreateRotationY((float)MathUtils.Remainder(totalElapsedGameTime, 6.2831854820251465));
			foreach (Pickable pickable in m_pickables)
			{
				pickable.Draw(camera, drawOrder, totalElapsedGameTime, matrix);
			}
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}

		public void Update(float dt)
		{
			for(int i = 0; i < m_pickables.Count; i++)
			{ 
				Pickable pickable = m_pickables[i];
				lock (pickable)
				{
                    if (pickable.ToRemove)
                    {
                        m_pickablesToRemove.Add(pickable);
                    }
                    else
                    {
						try
						{
							pickable.SubsystemTerrain = m_subsystemTerrain;
							pickable.SubsystemPickables = this;
							pickable.Update(dt);
						}
						catch (Exception e)
						{
							Log.Error("Pickable update error: " + e);
							pickable.ToRemove = true;
						}
                    }
                }
			}
			foreach (Pickable item in m_pickablesToRemove)
			{
				lock (m_pickables)
				{
                    m_pickables.Remove(item);
                }
				PickableRemoved?.Invoke(item);
			}
			m_pickablesToRemove.Clear();
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			foreach (ValuesDictionary item in valuesDictionary.GetValue<ValuesDictionary>("Pickables").Values.Where((object v) => v is ValuesDictionary))
			{
				var pickable = new Pickable();
				pickable.Value = item.GetValue<int>("Value");
				pickable.Count = item.GetValue<int>("Count");
				pickable.Position = item.GetValue<Vector3>("Position");
				pickable.Velocity = item.GetValue<Vector3>("Velocity");
				pickable.CreationTime = item.GetValue("CreationTime", 0.0);
				if (item.ContainsKey("StuckMatrix"))
				{
					pickable.StuckMatrix = item.GetValue<Matrix>("StuckMatrix");
				}
                int ownerEntityID = item.GetValue("OwnerID", 0);
                if (ownerEntityID != 0)
                {
                    pickable.OwnerEntity = Project.FindEntity(ownerEntityID);
                }
                ModsManager.HookAction("OnPickableAdded", loader =>
                {
                    loader.OnPickableAdded(this, ref pickable, item);
                    return false;
                });
				lock (m_pickables)
				{
                    m_pickables.Add(pickable);
                }
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Pickables", valuesDictionary2);
			int num = 0;
			foreach (Pickable pickable in m_pickables)
			{
				var valuesDictionary3 = new ValuesDictionary();
				pickable.Save(valuesDictionary3);
				ModsManager.HookAction("SavePickable", loader =>
				{
					loader.SavePickable(this, pickable, ref valuesDictionary3);
					return false;
				});
                valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
                num++;
			}
		}
	}
}

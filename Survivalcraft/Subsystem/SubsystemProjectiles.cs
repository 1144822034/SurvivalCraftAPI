using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TemplatesDatabase;
using Engine.Serialization;

namespace Game
{
	public class SubsystemProjectiles : Subsystem, IUpdateable, IDrawable
	{
		public SubsystemAudio m_subsystemAudio;

		public SubsystemSoundMaterials m_subsystemSoundMaterials;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public SubsystemTime m_subsystemTime;

		public SubsystemNoise m_subsystemNoise;

		public SubsystemExplosions m_subsystemExplosions;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public List<Projectile> m_projectiles = [];

		public List<Projectile> m_projectilesToRemove = [];

		public PrimitivesRenderer3D m_primitivesRenderer = new();

		public Random m_random = new();

		public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new();

		public const float BodyInflateAmount = 0.2f;

		public static int[] m_drawOrders = new int[1]
		{
			10
		};

		public ReadOnlyList<Projectile> Projectiles => new(m_projectiles);

		public int[] DrawOrders => m_drawOrders;

		public virtual Action<Projectile> ProjectileAdded { get; set; }

		public virtual Action<Projectile> ProjectileRemoved { get; set; }

		public UpdateOrder UpdateOrder => UpdateOrder.Default;
        public virtual Projectile AddProjectile(Projectile projectile)
        {
			if(projectile == null) return null;
            projectile.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
            projectile.IsInFluid = IsWater(projectile.Position);

            ModsManager.HookAction("OnProjectileAdded", loader =>
            {
                loader.OnProjectileAdded(this, ref projectile, null);
                return false;
            });

            lock (m_projectiles)
            {
                m_projectiles.Add(projectile);
            }

            if (projectile.Owner != null && projectile.Owner.PlayerStats != null)
            {
                projectile.Owner.PlayerStats.RangedAttacks++;
            }
            ProjectileAdded?.Invoke(projectile);
            return projectile;
        }
        public virtual Projectile AddProjectile(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
		{
			return AddProjectile<Projectile>(value, position, velocity, angularVelocity, owner);
		}
        public virtual Projectile CreateProjectile(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
        {
			return CreateProjectile<Projectile>(value, position, velocity, angularVelocity, owner);
        }
        public virtual T CreateProjectile<T>(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner) where T : Projectile, new()
		{
			try
			{
				var projectile = new T();
				projectile.Initialize(value,position,velocity,angularVelocity,owner);
				return projectile;
			}
			catch(Exception ex)
			{
				Log.Error("Projectile create error: " + ex);
				return null;
			}
            
        }

        public virtual T AddProjectile<T>(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner) where T : Projectile, new()
		{
			try
			{
				T projectile = CreateProjectile<T>(value,position,velocity,angularVelocity,owner);
				Projectile projectile2 = AddProjectile(projectile);
				return projectile2 as T;
			}
			catch(Exception ex)
			{
				Log.Error("Projectile add error: " + ex);
				return null;
			}
        }

        public virtual Projectile FireProjectile(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner)
		{
			return FireProjectile<Projectile>(value, position, velocity, angularVelocity, owner);
        }

		public virtual bool CanFireProjectile(int value, Vector3 position, Vector3 velocity, ComponentCreature owner, out Vector3 firePosition)
		{
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            var v = Vector3.Normalize(velocity);
            firePosition = position;
            if (owner != null)
            {
                var ray = new Ray3(position + (v * 5f), -v);
                BoundingBox boundingBox = owner.ComponentBody.BoundingBox;
                boundingBox.Min -= new Vector3(0.4f);
                boundingBox.Max += new Vector3(0.4f);
                float? num2 = ray.Intersection(boundingBox);
                if (num2.HasValue)
                {
                    if (num2.Value == 0f)
                    {
						firePosition = Vector3.Zero;
                        return false;
                    }
                    firePosition = position + (v * (5f - num2.Value + 0.1f));
                }
            }
            Vector3 end = firePosition + (v * block.ProjectileTipOffset);
			return !m_subsystemTerrain.Raycast(position, end, useInteractionBoxes: false, skipAirBlocks: true, (int testValue, float distance) =>
			BlocksManager.Blocks[Terrain.ExtractContents(testValue)].IsCollidable_(testValue)).HasValue;
        }
        public virtual T FireProjectile<T>(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner) where T : Projectile, new()
		{
			if (CanFireProjectile(value, position, velocity, owner, out Vector3 firePosition))
			{
				T projectile = CreateProjectile<T>(value, firePosition, velocity, angularVelocity, owner);
				FireProjectileFast(projectile);
				return projectile;
			}
			return null;
		}

        public virtual void FireProjectileFast(Projectile projectile)
		{
            projectile = AddProjectile(projectile);
			if(projectile == null) return;
            SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(projectile.Value));
            for (int i = 0; i < blockBehaviors.Length; i++)
            {
                blockBehaviors[i].OnFiredAsProjectile(projectile);
            }
        }


        public virtual void AddTrail(Projectile projectile, Vector3 offset, ITrailParticleSystem particleSystem)
		{
			RemoveTrail(projectile);
			projectile.TrailParticleSystem = particleSystem;
			projectile.TrailOffset = offset;
		}

		public virtual void RemoveTrail(Projectile projectile)
		{
			if (projectile.TrailParticleSystem != null)
			{
				if (m_subsystemParticles.ContainsParticleSystem((ParticleSystemBase)projectile.TrailParticleSystem))
				{
					m_subsystemParticles.RemoveParticleSystem((ParticleSystemBase)projectile.TrailParticleSystem);
				}
				projectile.TrailParticleSystem = null;
			}
		}

		public void Draw(Camera camera, int drawOrder)
		{
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			m_drawBlockEnvironmentData.InWorldMatrix = Matrix.Identity;
			for(int i = 0; i < m_projectiles.Count; i++)
			{
				Projectile projectile = m_projectiles[i];
				try
				{
					projectile.SubsystemProjectiles = this;
					projectile.SubsystemTerrain = m_subsystemTerrain;
					projectile.Draw(camera,drawOrder);
				}
				catch(Exception e)
				{
					if(projectile.LogDrawError)
					{
						Log.Error("Projectile draw error: " + e);
						projectile.LogDrawError = false;
					}
				}
			}
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}

		public void Update(float dt)
		{
			for(int i = 0; i < m_projectiles.Count; i++)
			{
				Projectile projectile = m_projectiles[i];
				if(projectile != null)
				{
					lock (projectile)
					{
                        if (projectile.ToRemove)
                        {
                            m_projectilesToRemove.Add(projectile);
                        }
                        else
                        {
							try
							{

								projectile.SubsystemProjectiles = this;
								projectile.SubsystemTerrain = m_subsystemTerrain;
								projectile.Update(dt);
							}
							catch (Exception ex)
							{
								Log.Error("Projectile update error: ");
								Log.Error(ex);
								projectile.ToRemove = true;
							}
                        }
                    }
                }
			}
			foreach (Projectile item in m_projectilesToRemove)
			{
				if (item.TrailParticleSystem != null)
				{
					item.TrailParticleSystem.IsStopped = true;
				}
				item.OnRemove?.Invoke();
				lock (m_projectiles)
				{
                    m_projectiles.Remove(item);
                }
				ProjectileRemoved?.Invoke(item);
			}
			m_projectilesToRemove.Clear();
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
			m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			foreach (ValuesDictionary item in valuesDictionary.GetValue<ValuesDictionary>("Projectiles").Values.Where((object v) => v is ValuesDictionary))
			{
				try
				{
					string className = item.GetValue("Class",typeof(Projectile).FullName);
					Type type = TypeCache.FindType(className,false,true);
					var projectile = (Projectile)Activator.CreateInstance(type);
					projectile.SubsystemProjectiles = this;
					projectile.SubsystemTerrain = m_subsystemTerrain;
					projectile.Load(item);
					ModsManager.HookAction("OnProjectileAdded",loader => {
						loader.OnProjectileAdded(this,ref projectile,item);
						return false;
					});
					m_projectiles.Add(projectile);
				}
				catch(Exception ex)
				{
					Log.Error("Projectile Loaded Error");
					Log.Error(ex);
				}
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Projectiles", valuesDictionary2);
			int num = 0;
			foreach (Projectile projectile in m_projectiles)
			{
				try
				{
                    var valuesDictionary3 = new ValuesDictionary();
                    projectile.Save(this, valuesDictionary3);
                    valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
                    num++;
				}
				catch (Exception ex) 
				{
					Log.Error("Projectile Save Error: " + ex);
				}
            }
		}

        public virtual bool IsWater(Vector3 position)
        {
            int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
            return BlocksManager.Blocks[cellContents] is FluidBlock;
        }
		[Obsolete("SubsystemProjectiles不再提供射弹更新，请转移到Projectile.Update()中")]
        public virtual bool IsMagma(Vector3 position)
        {
            int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
            return BlocksManager.Blocks[cellContents] is MagmaBlock;
        }
		[Obsolete("SubsystemProjectiles不再提供射弹更新，请转移到Projectile.Update()中")]
        public virtual void MakeProjectileNoise(Projectile projectile)
        {
            if (m_subsystemTime.GameTime - projectile.LastNoiseTime > 0.5)
            {
                m_subsystemNoise.MakeNoise(projectile.Position, 0.25f, 6f);
                projectile.LastNoiseTime = m_subsystemTime.GameTime;
            }
        }

        public static void CalculateVelocityAlignMatrix(Block projectileBlock, Vector3 position, Vector3 velocity, out Matrix matrix)
		{
			matrix = Matrix.Identity;
			matrix.Up = Vector3.Normalize(velocity);
			matrix.Right = Vector3.Normalize(Vector3.Cross(matrix.Up, Vector3.UnitY));
			matrix.Forward = Vector3.Normalize(Vector3.Cross(matrix.Up, matrix.Right));
			matrix.Translation = position;
		}
	}
}

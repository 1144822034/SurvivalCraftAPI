using Engine;
using Engine.Audio;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemExplosivesBlockBehavior : SubsystemBlockBehavior, IUpdateable
	{
		public class ExplosiveData
		{
			public Point3 Point;

			public float TimeToExplosion;

			public FuseParticleSystem FuseParticleSystem;
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemExplosions m_subsystemExplosions;

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public SubsystemAudio m_subsystemAudio;

		public Random m_random = new();

		public Dictionary<Point3, ExplosiveData> m_explosiveDataByPoint = [];

		public Sound m_fuseSound;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override int[] HandledBlocks => new int[0];

		public bool IgniteFuse(int x, int y, int z)
		{
			int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
			if (BlocksManager.Blocks[cellContents] is GunpowderKegBlock)
			{
				AddExplosive(new Point3(x, y, z), m_random.Float(6f, 7f));
				return true;
			}
			if (BlocksManager.Blocks[cellContents] is DetonatorBlock)
			{
				AddExplosive(new Point3(x, y, z), m_random.Float(0.8f, 1.2f));
				return true;
			}
			return false;
		}

		public void Update(float dt)
		{
			float num = float.MaxValue;
			if (m_explosiveDataByPoint.Count > 0)
			{
				ExplosiveData[] array = m_explosiveDataByPoint.Values.ToArray();
				foreach (ExplosiveData explosiveData in array)
				{
					Point3 point = explosiveData.Point;
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
					int num2 = Terrain.ExtractContents(cellValue);
					Block block = BlocksManager.Blocks[num2];
					if (explosiveData.FuseParticleSystem == null)
					{
						var gunpowderKegBlock = block as GunpowderKegBlock;
						if (gunpowderKegBlock != null)
						{
							explosiveData.FuseParticleSystem = new FuseParticleSystem(new Vector3(point.X, point.Y, point.Z) + gunpowderKegBlock.FuseOffset);
							m_subsystemParticles.AddParticleSystem(explosiveData.FuseParticleSystem);
						}
					}
					explosiveData.TimeToExplosion -= dt;
					if (explosiveData.TimeToExplosion <= 0f)
					{
						m_subsystemExplosions.TryExplodeBlock(explosiveData.Point.X, explosiveData.Point.Y, explosiveData.Point.Z, cellValue);
					}
					float x = m_subsystemAudio.CalculateListenerDistance(new Vector3(point.X, point.Y, point.Z) + new Vector3(0.5f));
					num = MathUtils.Min(num, x);
				}
			}
			if (m_fuseSound != null)
			{
				m_fuseSound.Volume = SettingsManager.SoundsVolume * m_subsystemAudio.CalculateVolume(num, 2f);
				if (m_fuseSound.Volume > AudioManager.MinAudibleVolume)
				{
					m_fuseSound.Play();
				}
				else
				{
					m_fuseSound.Pause();
				}
			}
		}

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			if (m_subsystemFireBlockBehavior.IsCellOnFire(x, y, z))
			{
				IgniteFuse(x, y, z);
			}
		}

		public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
		{
			var point = new Point3(x, y, z);
			RemoveExplosive(point);
		}

		public override void OnChunkDiscarding(TerrainChunk chunk)
		{
			var list = new List<Point3>();
			foreach (Point3 key in m_explosiveDataByPoint.Keys)
			{
				if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
				{
					list.Add(key);
				}
			}
			foreach (Point3 item in list)
			{
				RemoveExplosive(item);
			}
		}

		public override void OnExplosion(int value, int x, int y, int z, float damage)
		{
			int num = Terrain.ExtractContents(value);
			Block block = BlocksManager.Blocks[num];
			if (block.GetExplosionPressure(value) > 0f && MathUtils.Saturate(damage / block.ExplosionResilience) > 0.01f && m_random.Float(0f, 1f) < 0.5f)
			{
				IgniteFuse(x, y, z);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_fuseSound = m_subsystemAudio.CreateSound("Audio/Fuse");
			m_fuseSound.IsLooped = true;
			foreach (ValuesDictionary value3 in valuesDictionary.GetValue<ValuesDictionary>("Explosives").Values)
			{
				Point3 value = value3.GetValue<Point3>("Point");
				float value2 = value3.GetValue<float>("TimeToExplosion");
				AddExplosive(value, value2);
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			base.Save(valuesDictionary);
			int num = 0;
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Explosives", valuesDictionary2);
			foreach (ExplosiveData value in m_explosiveDataByPoint.Values)
			{
				var valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				valuesDictionary3.SetValue("Point", value.Point);
				valuesDictionary3.SetValue("TimeToExplosion", value.TimeToExplosion);
			}
		}

		public override void Dispose()
		{
			Utilities.Dispose(ref m_fuseSound);
		}

		public void AddExplosive(Point3 point, float timeToExplosion)
		{
			if (!m_explosiveDataByPoint.ContainsKey(point))
			{
				var explosiveData = new ExplosiveData();
				explosiveData.Point = point;
				explosiveData.TimeToExplosion = timeToExplosion;
				m_explosiveDataByPoint.Add(point, explosiveData);
			}
		}

		public void RemoveExplosive(Point3 point)
		{
			if (m_explosiveDataByPoint.TryGetValue(point, out ExplosiveData value))
			{
				m_explosiveDataByPoint.Remove(point);
				if (value.FuseParticleSystem != null)
				{
					m_subsystemParticles.RemoveParticleSystem(value.FuseParticleSystem);
				}
			}
		}
	}
}

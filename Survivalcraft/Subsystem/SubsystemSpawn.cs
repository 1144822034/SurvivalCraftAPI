using Engine;
using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TemplatesDatabase;
using System.Linq;

namespace Game
{
	public class SubsystemSpawn : Subsystem, IUpdateable
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemPlayers m_subsystemPlayers;

		public SubsystemGameWidgets m_subsystemViews;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemTime m_subsystemTime;

		public Random m_random = new();

		public double m_nextDiscardOldChunksTime = 1.0;

		public double m_nextVisitedTime = 1.0;

		public double m_nextChunkSpawnTime = 1.0;

		public double m_nextDespawnTime = 1.0;

		public Dictionary<Point2, SpawnChunk> m_chunks = [];

		public Dictionary<ComponentSpawn, bool> m_spawns = [];

		public Dictionary<int, SpawnEntityData> m_spawnEntityDatas = new Dictionary<int, SpawnEntityData>();

		public float MaxChunkAge = 76800f;

		public float VisitedRadius = 8f;

		public float SpawnRadius = 48f;

		public float DespawnRadius = 60f;

		public Dictionary<ComponentSpawn, bool>.KeyCollection Spawns => m_spawns.Keys;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public virtual Action<SpawnChunk> SpawningChunk { get; set; }

		public virtual SpawnChunk GetSpawnChunk(Point2 point)
		{
			m_chunks.TryGetValue(point, out SpawnChunk value);
			return value;
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.GameTime >= m_nextDiscardOldChunksTime)
			{
				m_nextDiscardOldChunksTime = m_subsystemTime.GameTime + 60.0;
				DiscardOldChunks();
			}
			if (m_subsystemTime.GameTime >= m_nextVisitedTime)
			{
				m_nextVisitedTime = m_subsystemTime.GameTime + 5.0;
				UpdateLastVisitedTime();
			}
			if (m_subsystemTime.GameTime >= m_nextChunkSpawnTime)
			{
				m_nextChunkSpawnTime = m_subsystemTime.GameTime + 4.0;
				SpawnChunks();
			}
			if (m_subsystemTime.GameTime >= m_nextDespawnTime)
			{
				m_nextDespawnTime = m_subsystemTime.GameTime + 2.0;
				DespawnChunks();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
			m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Chunks"))
			{
				var valuesDictionary2 = (ValuesDictionary)item.Value;
				var spawnChunk = new SpawnChunk();
				spawnChunk.Point = HumanReadableConverter.ConvertFromString<Point2>(item.Key);
				spawnChunk.IsSpawned = valuesDictionary2.GetValue<bool>("IsSpawned");
				spawnChunk.LastVisitedTime = valuesDictionary2.GetValue<double>("LastVisitedTime");
				object obj = valuesDictionary2.GetValue("SpawnsData", new object());
				if (obj is string str)
				{
					LoadSpawnsData(str, spawnChunk.SpawnsData);
				}
				else if (obj is ValuesDictionary data) {
#pragma warning disable CS0612 // 类型或成员已过时
					LoadSpawnsData(data, spawnChunk.SpawnsData);
#pragma warning restore CS0612 // 类型或成员已过时
				}
				m_chunks[spawnChunk.Point] = spawnChunk;
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Chunks", valuesDictionary2);
			foreach (SpawnChunk value2 in m_chunks.Values)
			{
				if (value2.LastVisitedTime.HasValue)
				{
					var valuesDictionary3 = new ValuesDictionary();
					valuesDictionary2.SetValue(HumanReadableConverter.ConvertToString(value2.Point), valuesDictionary3);
					valuesDictionary3.SetValue("IsSpawned", value2.IsSpawned);
					valuesDictionary3.SetValue("LastVisitedTime", value2.LastVisitedTime.Value);
					/*ValuesDictionary v = [];
					SaveSpawnsData(v, value2.SpawnsData);
					valuesDictionary3.SetValue("SpawnsData", v);*/
                    string text = this.SaveSpawnsData(value2.SpawnsData);
                    if (!string.IsNullOrEmpty(text))
                    {
                        valuesDictionary3.SetValue<string>("SpawnsData", text);
                    }
                }
			}
		}

		public override void OnEntityAdded(Entity entity)
		{
			foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>())
			{
				m_spawns.Add(item, value: true);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>())
			{
				m_spawns.Remove(item);
			}
		}

		public virtual SpawnChunk GetOrCreateSpawnChunk(Point2 point)
		{
			SpawnChunk spawnChunk = GetSpawnChunk(point);
			if (spawnChunk == null)
			{
				spawnChunk = new SpawnChunk
				{
					Point = point
				};
				m_chunks.Add(point, spawnChunk);
			}
			return spawnChunk;
		}

		public virtual void DiscardOldChunks()
		{
			var list = new List<Point2>();
			foreach (SpawnChunk value in m_chunks.Values)
			{
				if (!value.LastVisitedTime.HasValue || m_subsystemGameInfo.TotalElapsedGameTime - value.LastVisitedTime.Value > MaxChunkAge)
				{
					list.Add(value.Point);
				}
			}
			foreach (Point2 item in list)
			{
				m_chunks.Remove(item);
			}
		}

		public virtual void UpdateLastVisitedTime()
		{
			foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
			{
				var v = new Vector2(componentPlayer.ComponentBody.Position.X, componentPlayer.ComponentBody.Position.Z);
				Vector2 p = v - new Vector2(VisitedRadius);
				Vector2 p2 = v + new Vector2(VisitedRadius);
				Point2 point = Terrain.ToChunk(p);
				Point2 point2 = Terrain.ToChunk(p2);
				for (int i = point.X; i <= point2.X; i++)
				{
					for (int j = point.Y; j <= point2.Y; j++)
					{
						SpawnChunk spawnChunk = GetSpawnChunk(new Point2(i, j));
						if (spawnChunk != null)
						{
							spawnChunk.LastVisitedTime = m_subsystemGameInfo.TotalElapsedGameTime;
						}
					}
				}
			}
		}

		public virtual void SpawnChunks()
		{
			var list = new List<SpawnChunk>();
			foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
			{
				var v = new Vector2(gameWidget.ActiveCamera.ViewPosition.X, gameWidget.ActiveCamera.ViewPosition.Z);
				Vector2 p = v - new Vector2(SpawnRadius);
				Vector2 p2 = v + new Vector2(SpawnRadius);
				Point2 point = Terrain.ToChunk(p);
				Point2 point2 = Terrain.ToChunk(p2);
				for (int i = point.X; i <= point2.X; i++)
				{
					for (int j = point.Y; j <= point2.Y; j++)
					{
						var v2 = new Vector2((i + 0.5f) * 16f, (j + 0.5f) * 16f);
						if (Vector2.DistanceSquared(v, v2) < SpawnRadius * SpawnRadius)
						{
							TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(v2.X), Terrain.ToCell(v2.Y));
							if (chunkAtCell != null && chunkAtCell.State > TerrainChunkState.InvalidPropagatedLight)
							{
								var point3 = new Point2(i, j);
								SpawnChunk orCreateSpawnChunk = GetOrCreateSpawnChunk(point3);
								foreach (SpawnEntityData spawnsDatum in orCreateSpawnChunk.SpawnsData)
								{
									SpawnEntity(spawnsDatum);
								}
								orCreateSpawnChunk.SpawnsData.Clear();
								SpawningChunk?.Invoke(orCreateSpawnChunk);
								orCreateSpawnChunk.IsSpawned = true;
							}
						}
					}
				}
			}
			foreach (SpawnChunk item in list)
			{
				foreach (SpawnEntityData spawnsDatum2 in item.SpawnsData)
				{
					SpawnEntity(spawnsDatum2);
				}
				item.SpawnsData.Clear();
			}
		}

		public virtual void DespawnChunks()
		{
			var list = new List<ComponentSpawn>(0);
			foreach (ComponentSpawn key in m_spawns.Keys)
			{
				if (key.AutoDespawn && !key.IsDespawning)
				{
					bool flag = true;
					Vector3 position = key.ComponentFrame.Position;
					var v = new Vector2(position.X, position.Z);
					foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
					{
						Vector3 viewPosition = gameWidget.ActiveCamera.ViewPosition;
						var v2 = new Vector2(viewPosition.X, viewPosition.Z);
						if (Vector2.DistanceSquared(v, v2) <= DespawnRadius * DespawnRadius)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						list.Add(key);
					}
				}
			}
			foreach (ComponentSpawn item in list)
			{
				Point2 point = Terrain.ToChunk(item.ComponentFrame.Position.XZ);
				var data = new SpawnEntityData
				{
					TemplateName = item.Entity.ValuesDictionary.DatabaseObject.Name,
					Position = item.ComponentFrame.Position,
					ConstantSpawn = item.ComponentCreature?.ConstantSpawn ?? false,
					Data = string.Empty,
					EntityId = item.Entity.Id
				};
				ModsManager.HookAction("OnSaveSpawnData", (ModLoader loader) => { loader.OnSaveSpawnData(item, data); return true; });
				GetOrCreateSpawnChunk(point).SpawnsData.Add(data);
				m_spawnEntityDatas[data.EntityId] = data;
				item.Despawn();
			}
		}

		public virtual Entity SpawnEntity(SpawnEntityData data)
		{
			try
			{
				Entity entity = DatabaseManager.CreateEntity(Project, data, throwIfNotFound: true);
				ModsManager.HookAction("OnReadSpawnData", (ModLoader loader) => { loader.OnReadSpawnData(entity, data); return true; });
				entity.FindComponent<ComponentBody>(throwOnError: true).Position = data.Position;
				entity.FindComponent<ComponentBody>(throwOnError: true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
				ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					componentCreature.ConstantSpawn = data.ConstantSpawn;
				}
				Project.AddEntity(entity);
				return entity;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to spawn entity with template \"{data.TemplateName}\". Reason: {ex.ToString()}");
				return null;
			}
		}
        [Obsolete]
        public virtual void LoadSpawnsData(ValuesDictionary loadData, List<SpawnEntityData> creaturesData)
		{
			foreach(var (item, data) in from ValuesDictionary item in loadData.Values
										let data = new SpawnEntityData()
										select (item, data))
			{
				data.ConstantSpawn = item.GetValue<bool>("c");
				data.Position = item.GetValue<Vector3>("p");
				data.TemplateName = item.GetValue<string>("n");
				object obj = item.GetValue("d",new object());
				data.Data = obj is string str && str != null ? str : string.Empty;
				creaturesData.Add(data);
			}
		}
        public virtual void LoadSpawnsData(string data, List<SpawnEntityData> creaturesData)
        {
            string[] array = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = array[i].Split(new char[] { ',' });
                if (array2.Length < 4)
                {
                    throw new InvalidOperationException("Invalid spawn data string.");
                }
                SpawnEntityData spawnEntityData = new SpawnEntityData
                {
                    TemplateName = array2[0],
                    Position = new Vector3
                    {
                        X = float.Parse(array2[1], CultureInfo.InvariantCulture),
                        Y = float.Parse(array2[2], CultureInfo.InvariantCulture),
                        Z = float.Parse(array2[3], CultureInfo.InvariantCulture)
                    }
                };
                if (array2.Length >= 5)
                {
                    spawnEntityData.ConstantSpawn = bool.Parse(array2[4]);
                }
                if (array2.Length >= 6)
                {
                    spawnEntityData.Data = array2[5];
				}
				else
				{
                    spawnEntityData.Data = string.Empty;
                }
				if(array2.Length >= 7)
				{
					spawnEntityData.EntityId = int.Parse(array2[6]);
				}
                creaturesData.Add(spawnEntityData);
				m_spawnEntityDatas[spawnEntityData.EntityId] = spawnEntityData;
            }
        }
        [Obsolete]
		public virtual void SaveSpawnsData(ValuesDictionary saveData, List<SpawnEntityData> spawnsData)
		{
			int i = 0;
			foreach (var d in spawnsData)
			{
				ValuesDictionary v2 = [];
				v2.SetValue("c", d.ConstantSpawn);
				v2.SetValue("p", d.Position);
				v2.SetValue("n", d.TemplateName);
				v2.SetValue("d", d.Data);
				saveData.SetValue($"{i++}", v2);
			}
		}
        public virtual string SaveSpawnsData(List<SpawnEntityData> spawnsData)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (SpawnEntityData spawnEntityData in spawnsData)
            {
                stringBuilder.Append(spawnEntityData.TemplateName);
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.X * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.Y * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.Z * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(spawnEntityData.ConstantSpawn.ToString());
                stringBuilder.Append(',');
                if (spawnEntityData.Data?.Length > 0)
				{
                    stringBuilder.Append(spawnEntityData.Data);
                }
                stringBuilder.Append(',');
                stringBuilder.Append(spawnEntityData.EntityId.ToString());
                stringBuilder.Append(';');
            }
            return stringBuilder.ToString();
        }
    }
}

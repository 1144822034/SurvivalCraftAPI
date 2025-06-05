using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemUpdate : Subsystem
	{
		public class UpdateableInfo
		{
			public float FloatUpdateOrder;
		}

		public class Comparer : IComparer<IUpdateable>
		{
			public static Comparer Instance = new();

			public int Compare(IUpdateable u1, IUpdateable u2)
			{
				float num = u1.FloatUpdateOrder - u2.FloatUpdateOrder;
				if (num != 0)
				{
					return Math.Sign(num);
				}
				return u1.GetHashCode() - u2.GetHashCode();
			}
		}

		public SubsystemTime m_subsystemTime;

		public float DefaultFixedTimeStep => m_subsystemTime.DefaultFixedTimeStep;

		public int DefaultFixedUpdateStep => m_subsystemTime.DefaultFixedUpdateStep;

        public Dictionary<IUpdateable, UpdateableInfo> m_updateables = [];

		public Dictionary<IUpdateable, bool> m_toAddOrRemove = [];

		public List<IUpdateable> m_sortedUpdateables = [];
#if DEBUG
		public Dictionary<Type, DebugInfo> m_debugInfos = [];
		public Stopwatch m_debugStopwatch = new ();
		public bool UpdateTimeDebug = false;
#endif

		public int UpdateablesCount => m_updateables.Count;

		public int UpdatesPerFrame
		{
			get;
			set;
		}

		public virtual void Update()
		{
			for (int i = 0; i < UpdatesPerFrame; i++)
			{
				m_subsystemTime.NextFrame();
				bool flag = false;
				lock (m_toAddOrRemove)
				{
                    foreach (KeyValuePair<IUpdateable, bool> item in m_toAddOrRemove)
                    {
                        bool skipVanilla = false;
                        ModsManager.HookAction("OnIUpdateableAddOrRemove", loader =>
                        {
                            loader.OnIUpdateableAddOrRemove(this, item.Key, item.Value, skipVanilla, out bool skip);
                            skipVanilla |= skip;
                            return false;
                        });
                        if (!skipVanilla)
                        {
                            if (item.Value)
                            {
                                m_updateables.Add(item.Key, new UpdateableInfo
                                {
                                    FloatUpdateOrder = item.Key.FloatUpdateOrder
                                });
                                flag = true;
                            }
                            else
                            {
                                m_updateables.Remove(item.Key);
                                flag = true;
                            }
                        }
                    }
                    m_toAddOrRemove.Clear();
                }
				
				foreach (KeyValuePair<IUpdateable, UpdateableInfo> updateable in m_updateables)
				{
					float updateOrder = updateable.Key.FloatUpdateOrder;
					if (updateOrder != updateable.Value.FloatUpdateOrder)
					{
						flag = true;
						updateable.Value.FloatUpdateOrder = updateOrder;
					}
				}
				if (flag)
				{
					m_sortedUpdateables.Clear();
					foreach (IUpdateable key in m_updateables.Keys)
					{
						m_sortedUpdateables.Add(key);
					}
					m_sortedUpdateables.Sort(Comparer.Instance);
				}
				float dt = m_subsystemTime.GameTimeDelta;
#if DEBUG
				m_debugStopwatch.Start();
#endif
				foreach (IUpdateable sortedUpdateable in m_sortedUpdateables) {
#if DEBUG
					long ticks = m_debugStopwatch.ElapsedTicks;
#endif
					try
					{
						lock (sortedUpdateable)
						{
							sortedUpdateable.Update(dt);
						}
					}
					catch(Exception)
					{
						// ignored
					}
#if DEBUG
					finally
					{
						if(UpdateTimeDebug)
						{
							Type type = sortedUpdateable.GetType();
							long ticksCosted = m_debugStopwatch.ElapsedTicks - ticks;
							if(!m_debugInfos.TryGetValue(type,out DebugInfo info))
							{
								info = new DebugInfo();
								m_debugInfos.Add(type, info);
							}
							info.Counter++;
							info.TotalTicksCosted += ticksCosted;
							if(ticksCosted > info.MaxTicksCosted1)
							{
								info.MaxTicksCosted1 = ticksCosted;
							}
							else if(ticksCosted > info.MaxTicksCosted2)
							{
								info.MaxTicksCosted2 = ticksCosted;
							}
						}
					}
#endif
				}
#if DEBUG
				m_debugStopwatch.Reset();
#endif
				ModsManager.HookAction("SubsystemUpdate", loader => { loader.SubsystemUpdate(dt); return false; });
			}
		}

		public void AddUpdateable(IUpdateable updateable)
		{
			lock(m_toAddOrRemove)
			{
                m_toAddOrRemove[updateable] = true;
            }
		}

		public void RemoveUpdateable(IUpdateable updateable)
		{
			lock(m_toAddOrRemove)
			{
                m_toAddOrRemove[updateable] = false;
            }
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			foreach (IUpdateable item in Project.FindSubsystems<IUpdateable>())
			{
				AddUpdateable(item);
			}
			UpdatesPerFrame = 1;
		}

#if DEBUG
        public override void Save(ValuesDictionary valuesDictionary)
        {
            if(UpdateTimeDebug)
			{
				int maxTypeNameLength = m_debugInfos.Keys.Max(type => type.FullName?.Length ?? 0) + 1;
				StringBuilder stringBuilder = new ();
				stringBuilder.AppendLine("====== SubsystemUpdate Performance Analyze ======");
				stringBuilder.Append("TypeName".PadRight(maxTypeNameLength));
				stringBuilder.Append("    Counter   TotalTime AverageTime    MaxTime1    MaxTime2");
				foreach((Type type, DebugInfo info) in m_debugInfos.OrderByDescending(pair => pair.Value.TotalTicksCosted))
				{
					stringBuilder.AppendLine();
					stringBuilder.Append(type.FullName?.PadRight(maxTypeNameLength));
					stringBuilder.Append(info.Counter.ToString().PadLeft(11));
					stringBuilder.Append($"{((float)info.TotalTicksCosted / Stopwatch.Frequency * 1000):F}ms".PadLeft(12));
					stringBuilder.Append($"{((float)info.TotalTicksCosted / info.Counter / Stopwatch.Frequency * 1000000f):F}μs".PadLeft(12));
					stringBuilder.Append($"{((float)info.MaxTicksCosted1 / Stopwatch.Frequency * 1000000f):F}μs".PadLeft(12));
					stringBuilder.Append($"{((float)info.MaxTicksCosted2 / Stopwatch.Frequency * 1000000f):F}μs".PadLeft(12));
				}
				Log.Information(stringBuilder.ToString());
				m_debugInfos.Clear();
            }
        }
#endif

        public override void OnEntityAdded(Entity entity)
		{
			foreach (IUpdateable item in entity.FindComponents<IUpdateable>())
			{
				AddUpdateable(item);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			foreach (IUpdateable item in entity.FindComponents<IUpdateable>())
			{
				RemoveUpdateable(item);
			}
		}

	}
	public class SubsystemPostprocessor : Subsystem
	{
	}
}

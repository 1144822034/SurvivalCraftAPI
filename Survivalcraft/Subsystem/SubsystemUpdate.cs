using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemUpdate : Subsystem
	{
		public class UpdateableInfo
		{
			public UpdateOrder UpdateOrder;
		}

		public class Comparer : IComparer<IUpdateable>
		{
			public static Comparer Instance = new();

			public int Compare(IUpdateable u1, IUpdateable u2)
			{
				int num = u1.UpdateOrder - u2.UpdateOrder;
				if (num != 0)
				{
					return num;
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

		public Dictionary<Type, int> m_updateTicksCount = new Dictionary<Type, int>();

		public Dictionary<Type, int> m_updateTimesCount = new Dictionary<Type, int>();

		public bool UpdateTimeDebug = false;

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
                                    UpdateOrder = item.Key.UpdateOrder
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
					UpdateOrder updateOrder = updateable.Key.UpdateOrder;
					if (updateOrder != updateable.Value.UpdateOrder)
					{
						flag = true;
						updateable.Value.UpdateOrder = updateOrder;
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
				for(int j = 0; j < m_sortedUpdateables.Count; j++)
				{
					var sortedUpdateable = m_sortedUpdateables[j];
                    Type type = sortedUpdateable.GetType();
					int tick1 = Environment.TickCount;
                    try
					{
						lock (sortedUpdateable)
						{
							sortedUpdateable.Update(dt);
						}
					}
					catch (Exception)
					{
					}
					finally
					{
						int tick2 = Environment.TickCount;
						bool updateTicksHasValue = m_updateTicksCount.TryGetValue(type, out int updateTicksCount);
						m_updateTicksCount[type] = (updateTicksHasValue ? updateTicksCount : 0) + (tick2 - tick1);
						bool updateTimesHasValue = m_updateTimesCount.TryGetValue(type, out int updateTimesCount);
                        m_updateTimesCount[type] = (updateTimesHasValue ? updateTimesCount : 0) + 1;
					}
				}
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

        public override void Save(ValuesDictionary valuesDictionary)
        {
            if(UpdateTimeDebug)
			{
				Engine.Log.Information("======SubsystemUpdate性能分析======");
				var list = m_updateTicksCount.Keys.ToList();
				int maxLength = 0;
				for (int i = 0; i < list.Count; i++)
				{
					int lengthStr = list[i].FullName.Length;
					if(maxLength < lengthStr) maxLength = lengthStr;
				}
				for (int i = 0; i < list.Count; i++)
				{
					var item = list[i];
					string updateName = String.Format("{0, -" + (maxLength + 5).ToString() + "}", item.FullName);
					bool updateTimeExists = m_updateTimesCount.TryGetValue(item, out int updateTime);
					bool updateTickExists = m_updateTicksCount.TryGetValue(item, out int updateTick);
                    string updateTimeInfo = "TimesOfUpdate: " + String.Format("{0, -8}", updateTimeExists ? updateTime : "Error");
					string updateTimeInfo2 = "TimeOfUpdate: " + String.Format("{0, -10}", (updateTickExists ? updateTick : "Error") + "ms");
                    Engine.Log.Information(updateName + updateTimeInfo + updateTimeInfo2);
				}
                Engine.Log.Information("======SubsystemUpdate性能分析======");
				m_updateTicksCount.Clear();
				m_updateTimesCount.Clear();
            }
        }

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

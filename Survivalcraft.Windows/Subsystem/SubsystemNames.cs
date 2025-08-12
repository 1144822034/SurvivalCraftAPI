using GameEntitySystem;
using System;
using System.Collections.Generic;

namespace Game
{
	public class SubsystemNames : Subsystem
	{
		public Dictionary<string, ComponentName> m_componentsByName = [];

		public Component FindComponentByName(string name, Type componentType, string componentName)
		{
			return FindEntityByName(name)?.FindComponent(componentType, componentName, throwOnError: false);
		}

		public T FindComponentByName<T>(string name, string componentName) where T : Component
		{
			Entity entity = FindEntityByName(name);
			if (entity == null)
			{
				return null;
			}
			return entity.FindComponent<T>(componentName, throwOnError: false);
		}

		public Entity FindEntityByName(string name)
		{
			m_componentsByName.TryGetValue(name, out ComponentName value);
			return value?.Entity;
		}

		public static string GetEntityName(Entity entity)
		{
			ComponentName componentName = entity.FindComponent<ComponentName>();
			if (componentName != null)
			{
				return componentName.Name;
			}
			return string.Empty;
		}

		public override void OnEntityAdded(Entity entity)
		{
			foreach (ComponentName item in entity.FindComponents<ComponentName>())
			{
				m_componentsByName.Add(item.Name, item);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			foreach (ComponentName item in entity.FindComponents<ComponentName>())
			{
				m_componentsByName.Remove(item.Name);
			}
		}
	}
}

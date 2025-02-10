using System;
using System.Collections.Generic;

namespace GameEntitySystem
{
	public class EntityToIdMap
	{
		private Dictionary<Entity, int> m_map;

		internal EntityToIdMap(Dictionary<Entity, int> map)
		{
			m_map = map;
		}

		[Obsolete("Use Entity.Id instead.", true)]
		public int FindId(Entity entity)
		{
			if (entity != null && m_map.TryGetValue(entity, out int value))
			{
				return value;
			}
			return 0;
		}

		[Obsolete("Use Component.Entity.Id instead.", true)]
		public int FindId(Component component)
		{
			if (component == null)
			{
				return 0;
			}
			return FindId(component.Entity);
		}
	}
}

using GameEntitySystem;
using System.Collections.Generic;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemPlayerStats : Subsystem
	{
		public Dictionary<int, PlayerStats> m_playerStats = [];

		public PlayerStats GetPlayerStats(int playerIndex)
		{
			if (!m_playerStats.TryGetValue(playerIndex, out PlayerStats value))
			{
				value = new PlayerStats();
				m_playerStats.Add(playerIndex, value);
			}
			return value;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Stats"))
			{
				var playerStats = new PlayerStats();
				playerStats.Load((ValuesDictionary)item.Value);
				m_playerStats.Add(int.Parse(item.Key, CultureInfo.InvariantCulture), playerStats);
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			var valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Stats", valuesDictionary2);
			foreach (KeyValuePair<int, PlayerStats> playerStat in m_playerStats)
			{
				var valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue(playerStat.Key.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				playerStat.Value.Save(valuesDictionary3);
			}
		}
	}
}

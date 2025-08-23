using GameEntitySystem;
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
				PlayerStats playerStats = new();
				playerStats.Load((ValuesDictionary)item.Value);
				m_playerStats.Add(int.Parse(item.Key, CultureInfo.InvariantCulture), playerStats);
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			ValuesDictionary valuesDictionary2 = new();
			valuesDictionary.SetValue("Stats", valuesDictionary2);
			foreach (KeyValuePair<int, PlayerStats> playerStat in m_playerStats)
			{
				ValuesDictionary valuesDictionary3 = new();
				valuesDictionary2.SetValue(playerStat.Key.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				playerStat.Value.Save(valuesDictionary3);
			}
		}
	}
}

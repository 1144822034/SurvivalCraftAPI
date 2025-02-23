using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using TemplatesDatabase;

namespace Game
{
	public class PlayerStats
	{
		public class StatAttribute : Attribute
		{
		}

		public struct DeathRecord
		{
			public DeathRecord() { }
			public DeathRecord(double day, Vector3 location, string cause)
			{
				Day = day;
				Location = location;
				Cause = cause;
			}
			public double Day;

			public Vector3 Location;

			public string Cause;
			public void Load(string s)
			{
				string[] array = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
				if (array.Length != 5)
				{
					throw new InvalidOperationException("Invalid death record.");
				}
				Day = double.Parse(array[0], CultureInfo.InvariantCulture);
				Location.X = float.Parse(array[1], CultureInfo.InvariantCulture);
				Location.Y = float.Parse(array[2], CultureInfo.InvariantCulture);
				Location.Z = float.Parse(array[3], CultureInfo.InvariantCulture);
				Cause = array[4];
			}

			public string Save()
			{
				var stringBuilder = new StringBuilder();
				stringBuilder.Append(Day.ToString("R", CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append(Location.X.ToString("R", CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append(Location.Y.ToString("R", CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append(Location.Z.ToString("R", CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append(Cause);
				return stringBuilder.ToString();
			}

			/// <summary>
			/// 模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
			/// 目前API暂不支持直接保存到存档死亡信息中，建议模组自行保存额外死亡信息到自己的Subsystem中
			/// </summary>
			public ValuesDictionary ValuesDictionaryForMods = new ValuesDictionary();
		}

		public List<DeathRecord> m_deathRecords = [];

		[Stat]
		public double DistanceTravelled;

		[Stat]
		public double DistanceWalked;

		[Stat]
		public double DistanceFallen;

		[Stat]
		public double DistanceClimbed;

		[Stat]
		public double DistanceFlown;

		[Stat]
		public double DistanceSwam;

		[Stat]
		public double DistanceRidden;

		[Stat]
		public double LowestAltitude = 1.0 / 0.0;

		[Stat]
		public double HighestAltitude = -1.0 / 0.0;

		[Stat]
		public double DeepestDive;

		[Stat]
		public long Jumps;

		[Stat]
		public long BlocksDug;

		[Stat]
		public long BlocksPlaced;

		[Stat]
		public long BlocksInteracted;

		[Stat]
		public long PlayerKills;

		[Stat]
		public long LandCreatureKills;

		[Stat]
		public long WaterCreatureKills;

		[Stat]
		public long AirCreatureKills;

		[Stat]
		public long MeleeAttacks;

		[Stat]
		public long MeleeHits;

		[Stat]
		public long RangedAttacks;

		[Stat]
		public long RangedHits;

		[Stat]
		public long HitsReceived;

		[Stat]
		public long StruckByLightning;

		[Stat]
		public double TotalHealthLost;

		[Stat]
		public long FoodItemsEaten;

		[Stat]
		public long TimesWasSick;

		[Stat]
		public long TimesHadFlu;

		[Stat]
		public long TimesPuked;

		[Stat]
		public long TimesWentToSleep;

		[Stat]
		public double TimeSlept;

		[Stat]
		public long ItemsCrafted;

		[Stat]
		public long FurnitureItemsMade;

		[Stat]
		public GameMode EasiestModeUsed = (GameMode)2147483647;

		[Stat]
		public float HighestLevel;

		[Stat]
		public string DeathRecordsString;

		public IEnumerable<FieldInfo> Stats
		{
			get
			{
				foreach (FieldInfo item in from f in typeof(PlayerStats).GetRuntimeFields()
										   where f.GetCustomAttribute<StatAttribute>() != null
										   select f)
				{
					yield return item;
				}
			}
		}

		public ReadOnlyList<DeathRecord> DeathRecords => new(m_deathRecords);
		/// <summary>
		/// 模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
		/// 目前API暂不支持直接保存到存档中，建议模组自行保存额外信息到自己的Subsystem中
		/// </summary>
		public ValuesDictionary ValuesDictionaryForMods = new ValuesDictionary();

		public void AddDeathRecord(DeathRecord deathRecord)
		{
			m_deathRecords.Add(deathRecord);
		}

		public void Load(ValuesDictionary valuesDictionary)
		{
			foreach (FieldInfo stat in Stats)
			{
				if (valuesDictionary.ContainsKey(stat.Name))
				{
					object value = valuesDictionary.GetValue<object>(stat.Name);
					stat.SetValue(this, value);
				}
			}
			if (!string.IsNullOrEmpty(DeathRecordsString))
			{
				string[] array = DeathRecordsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
				foreach (string s in array)
				{
					DeathRecord item = default;
					item.Load(s);
					m_deathRecords.Add(item);
				}
			}
		}

		public void Save(ValuesDictionary valuesDictionary)
		{
			var stringBuilder = new StringBuilder();
			foreach (DeathRecord deathRecord in m_deathRecords)
			{
				stringBuilder.Append(deathRecord.Save());
				stringBuilder.Append(';');
			}
			DeathRecordsString = stringBuilder.ToString();
			foreach (FieldInfo stat in Stats)
			{
				object value = stat.GetValue(this);
				valuesDictionary.SetValue(stat.Name, value);
			}
		}
	}
}

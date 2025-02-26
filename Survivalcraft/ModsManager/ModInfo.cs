using System.Collections.Generic;

namespace Game
{
	public enum LoadOrder
	{
		Survivalcraft = -2147483648,
		ThemeMod = -16384,
		Default = 0,
		HelpfulMod = 16384
	}
	public class ModInfo
	{
		public string Name,
					  Version,
					  ApiVersion,
					  Description,
					  ScVersion,
					  Link,
					  Author,
					  PackageName;
		public int LoadOrder = 0;
		public List<string> Dependencies = [];

		/// <summary>
		/// 该项为true表示：在存档中不记录该模组的modInfo，当玩家在未装载该模组，并运行之前带有该模组的存档时，不报错
		/// 适用于不在存档中存储特殊信息的辅助模组
		/// </summary>
		public bool NonPersistentMod = false;//“注意这玩意写开发文档里”，git push的时候把这句话提交上去
		public override int GetHashCode()
		{
			return HashCode.Combine(Name, PackageName, Version);
		}

		public override bool Equals(object obj) => obj is ModInfo && obj.GetHashCode() == GetHashCode();
	}
}

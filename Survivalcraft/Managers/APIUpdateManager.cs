using System.Text.Json;

namespace Game
{
	public static class APIUpdateManager
	{
		/// <summary>
		/// API是否需要更新？ture：需要；false：不需要；null：正在获取
		/// </summary>
		public static bool? IsNeedUpdate
		{
			get;
			private set;
		}

		/// <summary>
		/// 当前API版本
		/// </summary>
		public static string CurrentVersion => ModsManager.APIVersionString;

		/// <summary>
		/// 网络上最新的API的版本
		/// </summary>
		public static string LatestVersion
		{
			get;
			private set;
		} = null;

		public static void Initialize()
		{
			Task.Run(
				async () => {
					LatestVersion = await GetLatestVersion(true);
					IsNeedUpdate = await GetIsNeedUpdate();
				}
			);
		}

		/// <summary>
		/// 对比API版本，判断是否需要更新
		/// </summary>
		/// <returns>API统一链接发布的最新版本</returns>
		public static async Task<bool> GetIsNeedUpdate()
		{
			string latestVersion = await GetLatestVersion(true);
			string currentVersion = ModsManager.APIVersionString;
			return ParseVersionFromString(latestVersion) > ParseVersionFromString(currentVersion);
		}

		/// <summary>
		/// 获取 Gitee release最后一个版本的Json文件数据
		/// </summary>
		/// <returns></returns>
		public static async Task<JsonDocument> GetLatestAPIJsonDocument() => await OnlineJsonReader.GetJsonFromUrlAsync(ModsManager.APIReleaseLink);

		/// <summary>
		/// 将API版本字符串转为浮点数
		/// </summary>
		/// <param name="version"></param>
		/// <returns>浮点数的版本</returns>
		/// <exception cref="FormatException">字符串格式不正确</exception>
		public static float ParseVersionFromString(string version)
		{
			if(version.StartsWith("API",StringComparison.OrdinalIgnoreCase))
			{
				string versionPart = version.Substring(3);
				// 4. 尝试转换为浮点数
				if(float.TryParse(versionPart,out float versionFloat))
				{
					return versionFloat;
				}
			}
			else
			{
				if(float.TryParse(version,out float versionFloat))
				{
					return versionFloat;
				}
			}
			throw new FormatException($"The API version string format is incorrect: {version}. The correct format should look like this: API1.80 or 1.80.");
		}

		/// <summary>
		/// 获取 Gitee release最后一个版本的版本号
		/// </summary>
		/// <param name="url">平台Json文件的API链接</param>
		/// <returns>最新版本号</returns>
		//当 direct 为真，将获取的 json 直接当做数组中的一个元素
		//常用于链接自带 latest 标识的情况
		public static async Task<string> GetLatestVersion(bool direct)
		{
			using(JsonDocument remoteDoc = await GetLatestAPIJsonDocument())
			{
				JsonElement root = remoteDoc.RootElement;
				// 假设 API 返回的版本信息在第一个 release 的 tag_name 字段
				string input = direct
					? root.GetProperty("tag_name").GetString()
					: root[root.GetArrayLength() - 1].GetProperty("tag_name").GetString();
				return input;
			}
		}
	}
}
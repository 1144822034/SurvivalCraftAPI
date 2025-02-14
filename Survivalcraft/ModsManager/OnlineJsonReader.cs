//通常用于下载并解析 git 平台在线接口的 json 文件
using System.Text.Json;

namespace Game
{
    class OnlineJsonReader
    {
		public static readonly HttpClient client = new();
		/// <summary>
		/// 对比API版本，判断是否需要更新
		/// </summary>
		/// <returns>API统一链接发布的最新版本</returns>
		public static async Task<bool> GetLatestAPIVersion()
		{
			string url = "https://gitee.com/api/v5/repos/THPRC/survivalcraft-api/releases/latest";
			return await GetLatestVersion(url,true)>float.Parse(ModsManager.ApiVersionString);
		}
		/*public static async Task<bool> GetLatestAPIVersion()
		{
			string url = "https://gitee.com/api/v5/repos/THPRC/survivalcraft-api/releases";
			return await GetLatestVersion(url,false) > float.Parse(ModsManager.ApiVersionString);
		}*/
		/// <summary>
		/// 获取 Gitee release最后一个版本
		/// </summary>
		/// <param name="url">平台Json文件的API链接</param>
		/// <returns>最新版本号</returns>
		//当 direct 为真，将获取的 json 直接当做数组中的一个元素
		//常用于链接自带 latest 标识的情况
		public static async Task<float> GetLatestVersion(string url,bool direct)
		{
			using(JsonDocument remoteDoc = await GetJsonFromUrlAsync(url))
			{
				JsonElement root = remoteDoc.RootElement;
				// 假设 API 返回的版本信息在第一个 release 的 tag_name 字段
				string input = direct
					? root.GetProperty("tag_name").GetString() 
					: root[root.GetArrayLength() - 1].GetProperty("tag_name").GetString();
				if(input.StartsWith("API",StringComparison.OrdinalIgnoreCase))
				{
					string versionPart = input.Substring(3);
					// 4. 尝试转换为浮点数
					if(float.TryParse(versionPart,out float version))
					{
						return version;
					}
				}
				return float.Parse(ModsManager.ApiVersionString);
			}
		}
		/// <summary>
		/// 从链接获取 Json 文档
		/// </summary>
		/// <param name="url">Json文件链接</param>
		/// <returns>Json文档</returns>
		static async Task<JsonDocument> GetJsonFromUrlAsync(string url)
		{
			HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			string jsonString = await response.Content.ReadAsStringAsync();
			return JsonDocument.Parse(jsonString);
		}
    }
}

//通常用于下载并解析git平台在线接口的json文件
namespace Game
{/*
    class OnlineJsonReader
    {
		public static readonly HttpClient client = new();
		public static bool DetectionUpdate()
		{

			return false;
		}
		public static JsonModel GetLatestVersion(string url)
		{
			
		}
		static async Task Save()
		{
			string url = "https://gitee.com/api/v5/repos/THPRC/survivalcraft-api/releases";
			try
			{
				string jsonContent = await GetJsonFronUrlAsync(url);
				//new JsonModel = JsonSerializer.Deserialize(jsonContent);
			}
			catch(HttpRequestException e)
			{

			}
		}
		static async Task<string> GetJsonFronUrlAsync(string url)
		{
			HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}
    }*/
}

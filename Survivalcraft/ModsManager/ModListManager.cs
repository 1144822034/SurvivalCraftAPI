using Engine;
using Tomlyn;
using Tomlyn.Model;
namespace Game
{
	internal class ModListManager
	{
		private Dictionary<string,string> RelativelySafeUrl = new()
		{
			{"gitee","https://gitee.com/"},
			{"github","https://github.com"},
			{"schub","https://schub.top"}
		};
		/// <summary>
		///检查文件夹中各个整合包（更新和补充） 
		/// </summary>
		/// <param name="folderPath">按照系统格式的文件夹路径</param>
		/// <returns></returns>
		public static bool InspectionModLists(string folderPath)
		{
			FileInfo[] files = new DirectoryInfo(folderPath).GetFiles();
			foreach(FileInfo file in files)
			{

			}
			return true;
		}
		public struct AggregationPackageInfo
		{
			public string PackageName;
			public double Version;
			public double ApiVersion;
			public string Author;
			public string Link;
			public string Description;
		}
		/// <summary>
		///解析整合包文件 
		///可以用整合包的模组路径代替全局模组路径
		/// </summary>
		/// <param name="filePath">整合包的路径</param>
		/// <returns>整合包的模组路径</returns>
		public static string AnalysisModList(string filePath)
		{
			var model = Toml.ToModel(File.ReadAllText(filePath));
			AggregationPackageInfo packageInfo = new()
			{
				PackageName = (string)((TomlTable)model["packageinfo"]!)["PackageName"],
				Version = (double)((TomlTable)model["packageinfo"]!)["Version"],
				ApiVersion = (double)((TomlTable)model["packageinfo"]!)["ApiVersion"],
				Author = (string)((TomlTable)model["packageinfo"]!)["Author"],
				Link = (string)((TomlTable)model["packageinfo"]!)["Link"],
				Description = (string)((TomlTable)model["packageinfo"]!)["Description"]
			};
			if(((TomlTable)model["requisite"]).Count!=0){
				string packagePath = ModsManager.ProcessModListPath + '/' + packageInfo.PackageName;
				Storage.CreateDirectory(packagePath);
				foreach(var item in ((TomlTable)model["requisite"]!))
				{
					Log.Information(item.Key);
					Log.Information(item.Value);
				}
				return packagePath;
			}
			else
			{
				return ModsManager.ModsPath;
			}
		}
	}
}

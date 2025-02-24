using Engine;
using System.Text;
using System.Xml.Linq;
using TemplatesDatabase;
using XmlUtilities;

namespace Game
{
	public static class ModSettingsManager
	{
		/// <summary>
		/// 储存每一个没有使用到的Mod设置的键值对，键：Mod的包名，值：Mod的设置信息XElement
		/// </summary>
		public static Dictionary<string, XElement> ModSettingsCache { get; private set; } = new();

		public static void LoadModSettings()
		{
			if (!Storage.FileExists(ModsManager.ModsSettingsPath)) return;

			using Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Read);
			//读取设置并且加入到ModSettings表内
			try
			{
				XElement element = XElement.Load(stream);
				foreach(var modXElement in element.Elements("Mod"))
				{
					string packageName = XmlUtils.GetAttributeValue<string>(modXElement, "PackageName");
					ModSettingsCache[packageName] = modXElement;
				}
			}
			catch (Exception e)
			{
				Log.Warning(e.ToString());
			}

			//遍历每个模组，加载设置项，如果设置项已加载，就从ModSettingsCache中删除
			try
			{
				foreach(var modEntity in ModsManager.ModList)
				{
					string packageName = modEntity.modInfo.PackageName;
					if(ModSettingsCache.TryGetValue(packageName, out XElement setting))
					{
						modEntity.LoadSettings(setting);
					}
				}
			}
			catch(Exception e)
			{
				Log.Warning(e.ToString());
			}

			Log.Information("Loaded mod settings");
		}

		public static void SaveModSettings()
		{
			foreach(var modEntity in ModsManager.ModList)
			{
				string packageName = modEntity.modInfo.PackageName;
				XElement settingsElement = new XElement("Mod");
				XmlUtils.SetAttributeValue(settingsElement, "PackageName", packageName);
				modEntity.SaveSettings(settingsElement);
				//模组保存了设置
				if (settingsElement.Elements().Any() || settingsElement.Attributes().Count() > 1) ModSettingsCache[packageName] = settingsElement;
			}

			XElement xElement = new("ModSettings");
			foreach(var settingElement in ModSettingsCache)
			{
				xElement.Add(settingElement.Value);
			}

			using (Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Create))
			{
				XmlUtils.SaveXmlToStream(xElement,stream,Encoding.UTF8,throwOnError: true);
			}
			Log.Information("Saved mod settings");
		}
	}
}
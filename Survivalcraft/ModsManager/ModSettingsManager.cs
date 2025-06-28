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

		/// <summary>
		/// 储存每个模组的键位映射设置，键：模组包名，值：模组的键位映射设置
		/// </summary>
		public static Dictionary<string, ValuesDictionary> ModKeyboardMapSettings { get; private set; } = new();
		/// <summary>
		/// 储存每个模组的相机设置，键：模组包名，值：模组的相机设置
		/// </summary>
		public static Dictionary<string, ValuesDictionary> ModCameraManageSettings { get; private set; } = new();

		public static Dictionary<string,object> CombinedKeyboardMappingSettings
		{
			get
			{//合并模组设置和原版设置
				Dictionary<string,object> dictionary = new Dictionary<string,object>();
				foreach(var item in SettingsManager.KeyboardMappingSettings)
					dictionary.TryAdd(item.Key,item.Value);
				foreach(var item in ModKeyboardMapSettings.Values)
				{
					foreach(var item2 in item)
						dictionary.TryAdd(item2.Key,item2.Value);
				}
				return dictionary;
			}
		}
		public static Dictionary<string,int> CombinedCameraManageSettings
		{
			get
			{
				Dictionary<string,int> dictionary = new Dictionary<string,int>();
				foreach(var item in SettingsManager.CameraManageSettings)
					dictionary.TryAdd(item.Key,Convert.ToInt32(item.Value));
				foreach(var item in ModCameraManageSettings.Values)
				{
					foreach(var item2 in item)
						dictionary.TryAdd(item2.Key,Convert.ToInt32(item2.Value));
				}
				return dictionary;
			}
		}

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

					ValuesDictionary modKeyboardSettings = [];
					ValuesDictionary modCameraSettings = [];
					var keysToAdd = modEntity.Loader?.GetKeyboardMappings() ?? [];//初始化模组默认键位设置
					var camerasToAdd = modEntity.Loader?.GetCameraList() ?? [];//初始化模组默认相机设置
					foreach(var item1 in keysToAdd)
						modKeyboardSettings.Add(item1.Key, item1.Value);
					foreach(var item2 in camerasToAdd)
						modCameraSettings.Add(item2.Key, item2.Value);

					if(ModSettingsCache.TryGetValue(packageName, out XElement setting))
					{
						modEntity.LoadSettings(setting);
						if(setting != null)
						{//加载模组保存的键位映射、相机设置
							XElement keyboardMapping = setting.Element("KeyboardMapping");
							if(keyboardMapping != null)
								modKeyboardSettings.ApplyOverrides(keyboardMapping,overrideExistOnly: true);
							XElement cameraList = setting.Element("CameraList");
							if(cameraList != null)
								modCameraSettings.ApplyOverrides(cameraList,overrideExistOnly: true);
						}
					}
					if(!ModKeyboardMapSettings.TryAdd(packageName,modKeyboardSettings))
						ModKeyboardMapSettings[packageName] = modKeyboardSettings;
					if(!ModCameraManageSettings.TryAdd(packageName,modCameraSettings))
						ModCameraManageSettings[packageName] = modCameraSettings;
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
				//保存模组的键位映射设置
				XElement keyboardMapping = new XElement("KeyboardMapping");
				if(ModKeyboardMapSettings.TryGetValue(packageName,out ValuesDictionary modKeyboardSettings) && modKeyboardSettings.Count > 0)
				{
					modKeyboardSettings.Save(keyboardMapping);
					settingsElement.Add(keyboardMapping);
				}
				//保存模组的相机设置
				XElement cameraList = new XElement("CameraList");
				if(ModCameraManageSettings.TryGetValue(packageName,out ValuesDictionary modCameraSettings) && modCameraSettings.Count > 0)
				{
					modCameraSettings.Save(cameraList);
					settingsElement.Add(cameraList);
				}
				//模组保存了设置
				if(settingsElement.Elements().Any() || settingsElement.Attributes().Count() > 1)
					ModSettingsCache[packageName] = settingsElement;
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

		public static void ResetModsKeyboardMappingSettings()
		{
			foreach(var modEntity in ModsManager.ModList)
			{
				string packageName = modEntity.modInfo.PackageName;
				if(ModKeyboardMapSettings.TryGetValue(packageName,out ValuesDictionary keyboardSettings))
				{
					keyboardSettings.Clear();
					var keysToAdd = modEntity.Loader?.GetKeyboardMappings() ?? [];
					foreach(var item1 in keysToAdd)
						keyboardSettings.Add(item1.Key,item1.Value);
				}
			}
			Log.Information("Reset mod keyboard mapping settings");
		}

		public static void ResetModsCameraManageSettings()
		{
			foreach(var modEntity in ModsManager.ModList)
			{
				string packageName = modEntity.modInfo.PackageName;
				if(ModCameraManageSettings.TryGetValue(packageName,out ValuesDictionary cameraSettings))
				{
					cameraSettings.Clear();
					var camerasToAdd = modEntity.Loader?.GetCameraList() ?? [];
					foreach(var item1 in camerasToAdd)
						cameraSettings.Add(item1.Key,item1.Value);
				}
			}
			Log.Information("Reset mod camera manage settings");
		}
	}
}
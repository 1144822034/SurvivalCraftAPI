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

		public const string fName = "ModSettingsManager";

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

			//读取设置并且加入到ModSettings表内
			try
			{
				using(Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath,OpenFileMode.Read))
				{
					XElement element = XElement.Load(stream);
					foreach(var modXElement in element.Elements("Mod"))
					{
						string packageName = XmlUtils.GetAttributeValue<string>(modXElement, "PackageName");
						ModSettingsCache[packageName] = modXElement;
					}
				}
			}
			catch (Exception e)
			{
				if(!LanguageControl.TryGet(out string str,fName,"1"))
				{
					str = "Error serializing mod settings file:";
				}
				Log.Warning($"{str} {e.Message}");
				return;
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
				if(!LanguageControl.TryGet(out string info,fName,"2"))
				{
					info = "Loaded mod settings";
				}
				Log.Information(info);
			}
			catch(Exception e)
			{
				if(!LanguageControl.TryGet(out string str,fName,"3"))
				{
					str = "Error loading mod settings:";
				}
				Log.Warning($"{str} {e}");
			}
		}

		public static void SaveModSettings()
		{
			foreach(var modEntity in ModsManager.ModList)
			{
				string packageName = modEntity.modInfo.PackageName;
				XElement settingsElement = new XElement("Mod");
				XmlUtils.SetAttributeValue(settingsElement, "PackageName", packageName);
				try
				{
					modEntity.SaveSettings(settingsElement);
				}
				catch(Exception e)
				{
					if(!LanguageControl.TryGet(out string str,fName,"4"))
					{
						str = "Error saving the mod settings of [{0}]:";
					}
					Log.Warning($"{string.Format(str,packageName)} {e}");
				}
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

			try
			{
				using(Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath,OpenFileMode.Create))
				{
					XmlUtils.SaveXmlToStream(xElement,stream,Encoding.UTF8,throwOnError: true);
				}
			}
			catch(Exception e)
			{
				if(!LanguageControl.TryGet(out string str,fName,"5"))
				{
					str = "Error saving mod settings file:";
				}
				Log.Warning($"{str} {e.Message}");
			}

			if(!LanguageControl.TryGet(out string info,fName,"6"))
			{
				info = "Saved mod settings";
			}
			Log.Information(info);
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
			Log.Information(LanguageControl.Get(fName, "7"));
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
			Log.Information(LanguageControl.Get(fName, "8"));
		}
	}
}
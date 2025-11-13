using System.Text;
using System.Xml.Linq;
using Engine;
using TemplatesDatabase;
using XmlUtilities;

namespace Game {
    public static class ModSettingsManager {
        /// <summary>
        ///     储存每一个没有使用到的Mod设置的键值对，键：Mod的包名，值：Mod的设置信息XElement
        /// </summary>
        public static Dictionary<string, XElement> ModSettingsCache { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的键盘鼠标键位映射设置，键：模组包名，值：模组的键位映射设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModKeyboardMapSettings { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的手柄键位映射设置，键：模组包名，值：模组的键位映射设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModGamepadMapSettings { get; private set; } = new();

        /// <summary>
        ///     储存每个模组的相机设置，键：模组包名，值：模组的相机设置
        /// </summary>
        public static Dictionary<string, ValuesDictionary> ModCameraManageSettings { get; private set; } = new();

        public const string fName = "ModSettingsManager";

        public static Dictionary<string, object> CombinedKeyboardMappingSettings {
            get { //合并模组设置和原版设置
                Dictionary<string, object> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.KeyboardMappingSettings) {
                    dictionary.TryAdd(item.Key, item.Value);
                }
                foreach (ValuesDictionary item in ModKeyboardMapSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, item2.Value);
                    }
                }
                return dictionary;
            }
        }
        public static Dictionary<string, object> CombinedGamepadMappingSettings {
            get { //合并模组设置和原版设置
                Dictionary<string, object> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.GamepadMappingSettings) {
                    dictionary.TryAdd(item.Key, item.Value);
                }
                foreach (ValuesDictionary item in ModGamepadMapSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, item2.Value);
                    }
                }
                return dictionary;
            }
        }

        public static Dictionary<string, int> CombinedCameraManageSettings {
            get {
                Dictionary<string, int> dictionary = new();
                foreach (KeyValuePair<string, object> item in SettingsManager.CameraManageSettings) {
                    dictionary.TryAdd(item.Key, Convert.ToInt32(item.Value));
                }
                foreach (ValuesDictionary item in ModCameraManageSettings.Values) {
                    foreach (KeyValuePair<string, object> item2 in item) {
                        dictionary.TryAdd(item2.Key, Convert.ToInt32(item2.Value));
                    }
                }
                return dictionary;
            }
        }

        public static void LoadModSettings() {
            if (!Storage.FileExists(ModsManager.ModsSettingsPath)) {
                return;
            }

            //读取设置并且加入到ModSettings表内
            try {
                using (Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Read)) {
                    XElement element = XElement.Load(stream);
                    foreach (XElement modXElement in element.Elements("Mod")) {
                        string packageName = XmlUtils.GetAttributeValue<string>(modXElement, "PackageName");
                        ModSettingsCache[packageName] = modXElement;
                    }
                }
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "1")) {
                    str = "Error serializing mod settings file:";
                }
                Log.Warning($"{str} {e.Message}");
                return;
            }

            //遍历每个模组，加载设置项，如果设置项已加载，就从ModSettingsCache中删除
            try {
                foreach (ModEntity modEntity in ModsManager.ModList) {
                    string packageName = modEntity.modInfo.PackageName;
                    ValuesDictionary modKeyboardSettings = [];
                    ValuesDictionary modGamepadSettings = [];
                    ValuesDictionary modCameraSettings = [];
                    IEnumerable<KeyValuePair<string, object>> keysToAdd = modEntity.Loader?.GetKeyboardMappings() ?? []; //初始化模组默认键位设置
                    IEnumerable<KeyValuePair<string, object>> gamepadKeysToAdd = modEntity.Loader?.GetGamepadMappings() ?? []; //初始化模组默认键位设置
                    IEnumerable<KeyValuePair<string, int>> camerasToAdd = modEntity.Loader?.GetCameraList() ?? []; //初始化模组默认相机设置
                    foreach (KeyValuePair<string, object> item1 in keysToAdd) {
                        modKeyboardSettings.Add(item1.Key, item1.Value);
                    }
                    foreach (KeyValuePair<string, object> item1 in gamepadKeysToAdd) {
                        modGamepadSettings.Add(item1.Key, item1.Value);
                    }
                    foreach (KeyValuePair<string, int> item2 in camerasToAdd) {
                        modCameraSettings.Add(item2.Key, item2.Value);
                    }
                    if (ModSettingsCache.TryGetValue(packageName, out XElement setting)) {
                        modEntity.LoadSettings(setting);
                        if (setting != null) { //加载模组保存的键位映射、相机设置
                            XElement keyboardMapping = setting.Element("KeyboardMapping");
                            if (keyboardMapping != null) {
                                modKeyboardSettings.ApplyOverrides(keyboardMapping, true);
                            }
                            XElement gamepadMapping = setting.Element("GamepadMapping");
                            if (gamepadMapping != null) {
                                modGamepadSettings.ApplyOverrides(gamepadMapping);
                            }
                            XElement cameraList = setting.Element("CameraList");
                            if (cameraList != null) {
                                modCameraSettings.ApplyOverrides(cameraList, true);
                            }
                        }
                    }
                    ModKeyboardMapSettings[packageName] = modKeyboardSettings;
                    ModGamepadMapSettings[packageName] = modGamepadSettings;
                    ModCameraManageSettings[packageName] = modCameraSettings;
                }
                if (!LanguageControl.TryGet(out string info, fName, "2")) {
                    info = "Loaded mod settings";
                }
                Log.Information(info);
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "3")) {
                    str = "Error loading mod settings:";
                }
                Log.Warning($"{str} {e}");
            }
        }

        public static void SaveModSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                XElement settingsElement = new("Mod");
                XmlUtils.SetAttributeValue(settingsElement, "PackageName", packageName);
                try {
                    modEntity.SaveSettings(settingsElement);
                }
                catch (Exception e) {
                    if (!LanguageControl.TryGet(out string str, fName, "4")) {
                        str = "Error saving the mod settings of [{0}]:";
                    }
                    Log.Warning($"{string.Format(str, packageName)} {e}");
                }
                //保存模组的键盘鼠标键位映射设置
                XElement keyboardMapping = new("KeyboardMapping");
                if (ModKeyboardMapSettings.TryGetValue(packageName, out ValuesDictionary modKeyboardSettings)
                    && modKeyboardSettings.Count > 0) {
                    modKeyboardSettings.Save(keyboardMapping);
                    settingsElement.Add(keyboardMapping);
                }
                //保存模组的手柄键位映射设置
                XElement gamepadMapping = new("GamepadMapping");
                if (ModGamepadMapSettings.TryGetValue(packageName, out ValuesDictionary modGamepadSettings)
                    && modGamepadSettings.Count > 0) {
                    modGamepadSettings.Save(gamepadMapping);
                    settingsElement.Add(gamepadMapping);
                }
                //保存模组的相机设置
                XElement cameraList = new("CameraList");
                if (ModCameraManageSettings.TryGetValue(packageName, out ValuesDictionary modCameraSettings)
                    && modCameraSettings.Count > 0) {
                    modCameraSettings.Save(cameraList);
                    settingsElement.Add(cameraList);
                }
                //模组保存了设置
                if (settingsElement.Elements().Any()
                    || settingsElement.Attributes().Count() > 1) {
                    ModSettingsCache[packageName] = settingsElement;
                }
            }
            XElement xElement = new("ModSettings");
            foreach (KeyValuePair<string, XElement> settingElement in ModSettingsCache) {
                xElement.Add(settingElement.Value);
            }
            try {
                using (Stream stream = Storage.OpenFile(ModsManager.ModsSettingsPath, OpenFileMode.Create)) {
                    XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, true);
                }
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "5")) {
                    str = "Error saving mod settings file:";
                }
                Log.Warning($"{str} {e.Message}");
            }
            if (!LanguageControl.TryGet(out string info, fName, "6")) {
                info = "Saved mod settings";
            }
            Log.Information(info);
        }

        public static void ResetModsKeyboardMappingSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModKeyboardMapSettings.TryGetValue(packageName, out ValuesDictionary keyboardSettings)) {
                    keyboardSettings.Clear();
                    IEnumerable<KeyValuePair<string, object>> keysToAdd = modEntity.Loader?.GetKeyboardMappings() ?? [];
                    foreach (KeyValuePair<string, object> item1 in keysToAdd) {
                        keyboardSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "7"));
        }

        public static void ResetModsGamepadMappingSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModGamepadMapSettings.TryGetValue(packageName, out ValuesDictionary gamepadSettings)) {
                    gamepadSettings.Clear();
                    IEnumerable<KeyValuePair<string, object>> keysToAdd = modEntity.Loader?.GetGamepadMappings() ?? [];
                    foreach (KeyValuePair<string, object> item1 in keysToAdd) {
                        gamepadSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "9"));
        }

        public static void ResetModsCameraManageSettings() {
            foreach (ModEntity modEntity in ModsManager.ModList) {
                string packageName = modEntity.modInfo.PackageName;
                if (ModCameraManageSettings.TryGetValue(packageName, out ValuesDictionary cameraSettings)) {
                    cameraSettings.Clear();
                    IEnumerable<KeyValuePair<string, int>> camerasToAdd = modEntity.Loader?.GetCameraList() ?? [];
                    foreach (KeyValuePair<string, int> item1 in camerasToAdd) {
                        cameraSettings.Add(item1.Key, item1.Value);
                    }
                }
            }
            Log.Information(LanguageControl.Get(fName, "8"));
        }
    }
}
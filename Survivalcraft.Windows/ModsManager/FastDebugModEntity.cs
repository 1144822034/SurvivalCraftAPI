using System.Reflection;
using System.Xml.Linq;
using Engine;
using NuGet.Versioning;

namespace Game {
    public class FastDebugModEntity : ModEntity {
        public Dictionary<string, FileInfo> FModFiles = [];

        public FastDebugModEntity() {
            modInfo = new ModInfo { Name = "[Debug]", PackageName = "debug" };
            InitResources();
            modInfo.LoadOrder = int.MinValue + 1;
        }

        public override void InitResources() {
            ReadDirResouces(ModsManager.ModsPath, "");
            if (!GetFile(
                    "modinfo.json",
                    stream => {
                        modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(stream));
                        modInfo.Name = $"[FastDebug]{modInfo.Name}";
                    }
                )) {
                modInfo = new ModInfo {
                    Name = "FastDebug",
                    Version = "1.0.0",
                    NuGetVersion = new NuGetVersion(1, 0, 0),
                    ApiVersion = ModsManager.APIVersionString,
                    ApiVersionRange = new VersionRange(ModsManager.APINuGetVersion),
                    Link = "https://gitee.com/SC-SPM/SurvivalcraftApi",
                    Author = "SC-SPM",
                    Description = "Debug uncompressed mod. 调试未压缩模组",
                    ScVersion = "2.4.0.0",
                    PackageName = "fastdebug"
                };
            }
            if(!GetFile("icon.webp", LoadIcon)) {
                GetFile("icon.png", LoadIcon);
            }
        }

        public void ReadDirResouces(string basepath, string path) {
            if (string.IsNullOrEmpty(path)) {
                path = basepath;
            }
            foreach (string d in Storage.ListDirectoryNames(path)) {
                ReadDirResouces(basepath, $"{path}/{d}");
            }
            foreach (string f in Storage.ListFileNames(path)) {
                if (f.EndsWith(".scmod")) {
                    continue;
                }
                string abpath = $"{path}/{f}";
                string FilenameInZip = abpath.Substring(basepath.Length + 1);
                if (FilenameInZip.StartsWith("Assets/")) {
                    string name = FilenameInZip.Substring(7);
                    ContentInfo contentInfo = new(name);
                    MemoryStream memoryStream = new();
                    using (Stream stream = Storage.OpenFile(abpath, OpenFileMode.Read)) {
                        stream.CopyTo(memoryStream);
                        contentInfo.SetContentStream(memoryStream);
                        ContentManager.Add(contentInfo);
                    }
                }
                FModFiles.Add(FilenameInZip, new FileInfo(Storage.GetSystemPath(abpath)));
            }
        }

        /// <summary>
        ///     获取指定后缀文件列表，带.
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override void GetFiles(string extension, Action<string, Stream> action) {
            bool skip = false;
            Loader?.GetModFiles(extension, action, out skip);
            if (skip) {
                return;
            }
            foreach (KeyValuePair<string, FileInfo> item in FModFiles) {
                if (item.Key.EndsWith(extension)) {
                    using (Stream fs = item.Value.OpenRead()) {
                        try {
                            action?.Invoke(item.Key, fs);
                        }
                        catch (Exception e) {
                            Log.Error($"GetFile {item.Key} Error:{e}");
                        }
                    }
                }
            }
        }

        public override bool GetFile(string filename, Action<Stream> stream) {
            bool skip = false;
            bool loaderReturns = false;
            Loader?.GetModFile(filename, stream, out skip, out loaderReturns);
            if (skip) {
                return loaderReturns;
            }
            if (FModFiles.TryGetValue(filename, out FileInfo fileInfo)) {
                using (Stream fs = fileInfo.OpenRead()) {
                    try {
                        stream?.Invoke(fs);
                    }
                    catch (Exception e) {
                        Log.Error($"GetFile {filename} Error:{e}");
                    }
                }
                return true;
            }
            return false;
        }

        public override bool GetAssetsFile(string filename, Action<Stream> stream) => GetFile($"Assets/{filename}", stream);
    }
}
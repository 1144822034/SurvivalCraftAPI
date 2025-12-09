using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using NuGet.Versioning;

namespace Game {
    public class ModEntity {
        public ModInfo modInfo;
        public Texture2D Icon;
        public ZipArchive ModArchive;
        public Dictionary<string, ZipArchiveEntry> ModFiles = [];
        public List<Type> BlockTypes = [];
        public string ModFilePath;
        public bool IsDisabled;
        public ModDisableReason DisableReason = ModDisableReason.Unknown;
        public long Size;
        public bool IsDependencyChecked;
        public static HashSet<string> InvalidDllNames = ["Survivalcraft.dll", "Engine.dll", "EntitySystem.dll"];
        public const string fName = "ModEntity";

        public ModLoader Loader {
            get => ModLoader_;
            set => ModLoader_ = value;
        }

        ModLoader ModLoader_;

        public ModEntity() { }

        public ModEntity(ZipArchive zipArchive) {
            ModFilePath = ModsManager.ModsPath;
            ModArchive = zipArchive;
            InitResources();
        }

        public ModEntity(string FileName, ZipArchive zipArchive) {
            ModFilePath = FileName;
            Size = Storage.GetFileSize(FileName);
            ModArchive = zipArchive;
            InitResources();
        }

        public virtual void LoadIcon(Stream stream) {
            Icon = Texture2D.Load(stream);
            stream.Close();
        }

        /// <summary>
        ///     获取模组的文件时调用。
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <param name="action">参数1文件名参数，2打开的文件流</param>
        public virtual void GetFiles(string extension, Action<string, Stream> action) {
            //将每个zip里面的文件读进内存中
            bool skip = false;
            Loader?.GetModFiles(extension, action, out skip);
            if (skip) {
                return;
            }
            foreach (ZipArchiveEntry zipArchiveEntry in ModArchive.ReadCentralDir()) {
                if (Storage.GetExtension(zipArchiveEntry.FilenameInZip) == extension) {
                    MemoryStream stream = new();
                    ModArchive.ExtractFile(zipArchiveEntry, stream);
                    stream.Position = 0L;
                    try {
                        action.Invoke(zipArchiveEntry.FilenameInZip, stream);
                    }
                    catch (Exception e) {
                        Log.Error($"Get file [{zipArchiveEntry.FilenameInZip}] failed: {e}");
                    }
                    finally {
                        stream.Dispose();
                    }
                }
            }
        }

        /// <param name="extension">文件扩展名</param>
        /// <param name="action">参数1文件名参数，2打开的文件流</param>
        /// <return>列表是否为空</return>
        public virtual bool GetFilesAndExist(string extension, Action<string, Stream> action) {
            if (ModArchive.ReadCentralDir().Count != 0) {
                GetFiles(extension, action);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     获取指定文件
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="stream">参数1打开的文件流</param>
        /// <returns></returns>
        public virtual bool GetFile(string filename, Action<Stream> stream) {
            bool skip = false;
            bool loaderReturns = false;
            Loader?.GetModFile(filename, stream, out skip, out loaderReturns);
            if (skip) {
                return loaderReturns;
            }
            if (ModFiles.TryGetValue(filename, out ZipArchiveEntry entry)) {
                using MemoryStream ms = new();
                ModArchive.ExtractFile(entry, ms);
                ms.Position = 0L;
                try {
                    stream?.Invoke(ms);
                }
                catch (Exception e) {
                    LoadingScreen.Error($"[{modInfo.Name}] Get file [{filename}] failed: {e}");
                }
                return false;
            }
            return true;
        }

        public virtual bool GetAssetsFile(string filename, Action<Stream> stream) => GetFile($"Assets/{filename}", stream);

        /// <summary>
        ///     初始化语言包
        /// </summary>
        public virtual void LoadLauguage() {
            GetAssetsFile(
                "Lang/en-US.json",
                stream => {
                    LoadingScreen.Info($"[{modInfo.Name}] Loading English Language file");
                    LanguageControl.LoadEnglishJson(stream);
                }
            );
            string language = ModsManager.Configs["Language"];
            if (language == "en-US") {
                return;
            }
            GetAssetsFile(
                $"Lang/{language}.json",
                stream => {
                    LoadingScreen.Info($"[{modInfo.Name}] Loading Current Language file");
                    LanguageControl.loadJson(stream);
                }
            );
        }

        /// <summary>
        ///     Mod初始化
        /// </summary>
        public virtual void ModInitialize() {
            LoadingScreen.Info($"[{modInfo.Name}] Executing __ModInitialize mission");
            ModLoader_?.__ModInitialize();
        }

        /// <summary>
        ///     初始化Content资源
        /// </summary>
        public virtual void InitResources() {
            ModFiles.Clear();
            if (ModArchive == null) {
                return;
            }
            List<ZipArchiveEntry> entries = ModArchive.ReadCentralDir();
            foreach (ZipArchiveEntry zipArchiveEntry in entries) {
                if (zipArchiveEntry.FileSize > 0) {
                    ModFiles.Add(zipArchiveEntry.FilenameInZip, zipArchiveEntry);
                }
            }
            if (GetFile("icon.webp", LoadIcon)) {
                GetFile("icon.png", LoadIcon);
            }
            GetFile("modinfo.json", stream => { modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(stream)); });
            if (modInfo == null) {
                IsDisabled = true;
                DisableReason = ModDisableReason.NoModInfo;
                return;
            }
            if (modInfo.PackageName.Contains(';')) {
                IsDisabled = true;
                DisableReason = ModDisableReason.InvalidPackageName;
                return;
            }
            if (ModsManager.DisabledMods.TryGetValue(modInfo.PackageName, out HashSet<string> disabledVersions)
                && disabledVersions.Contains(modInfo.Version)) {
                IsDisabled = true;
                DisableReason = ModDisableReason.Manually;
                return;
            }
            foreach (KeyValuePair<string, ZipArchiveEntry> c in ModFiles) {
                ZipArchiveEntry zipArchiveEntry = c.Value;
                string filename = zipArchiveEntry.FilenameInZip;
                if (!zipArchiveEntry.IsFilenameUtf8) {
                    ModsManager.AddException(
                        new Exception(
                            $"[{modInfo.Name}] The file name [{zipArchiveEntry.FilenameInZip}] is not encoded in UTF-8, need to be corrected."
                        )
                    );
                }
                if (filename.StartsWith("Assets/")) {
                    MemoryStream memoryStream = new();
                    ContentInfo contentInfo = new(filename.Substring(7));
                    ModArchive.ExtractFile(zipArchiveEntry, memoryStream);
                    contentInfo.SetContentStream(memoryStream);
                    ContentManager.Add(contentInfo);
                }
            }
            LoadingScreen.Info($"[{modInfo.Name}] Loaded {ModFiles.Count} resource files.");
        }

        /// <summary>
        ///     初始化BlocksData资源
        /// </summary>
        public virtual void LoadBlocksData() {
            bool flag = true;
            GetFiles(
                ".csv",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "1")}");
                        flag = false;
                    }
                    BlocksManager.LoadBlocksData(ModsManager.StreamToString(stream));
                }
            );
        }

        /// <summary>
        ///     初始化Database数据
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadXdb(ref XElement xElement) {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".xdb",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "2")}");
                        flag = false;
                    }
                    ModsManager.CombineDataBase(element, stream, modInfo.PackageName);
                }
            );
            Loader?.OnXdbLoad(xElement);
        }

        /// <summary>
        ///     初始化Clothing数据
        /// </summary>
        /// <param name="block"></param>
        /// <param name="xElement"></param>
        // ReSharper disable UnusedParameter.Global
        public virtual void LoadClo(ClothingBlock block, ref XElement xElement)
            // ReSharper restore UnusedParameter.Global
        {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".clo",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "3")}");
                        flag = false;
                    }
                    ModsManager.CombineClo(element, stream);
                }
            );
        }

        /// <summary>
        ///     初始化CraftingRecipe
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadCr(ref XElement xElement) {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".cr",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "4")}");
                        flag = false;
                    }
                    ModsManager.CombineCr(element, stream);
                }
            );
        }

        /// <summary>
        ///     加载mod程序集
        /// </summary>
        public virtual Assembly[] GetAssemblies() {
            bool flag = true;
            List<Assembly> assemblies = new();
            GetFiles(
                ".dll",
                (filename, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] Loading .dll assembly files.");
                        flag = false;
                    }
                    if (!filename.StartsWith("Assets/")) {
                        string fileNameWithoutDirectory = Storage.GetFileName(filename);
                        if (!InvalidDllNames.Contains(fileNameWithoutDirectory)) {
                            assemblies.Add(Assembly.Load(ModsManager.StreamToBytes(stream)));
                        }
                    }
                }
            ); //获取mod文件内的dll文件（不包括Assets目录内的dll）
            return [.. assemblies];
        }

        public virtual void HandleAssembly(Assembly assembly) {
            List<Type> blockTypes = new();
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++) {
                Type type = types[i];
                if (type.IsSubclassOf(typeof(ModLoader))
                    && !type.IsAbstract) {
                    if (Activator.CreateInstance(types[i]) is ModLoader modLoader) {
                        modLoader.Entity = this;
                        Loader = modLoader;
                        modLoader.__ModInitialize();
                        ModsManager.ModLoaders.Add(modLoader);
                    }
                }
                if (type.IsSubclassOf(typeof(IContentReader.IContentReader))
                    && !type.IsAbstract
                    && Activator.CreateInstance(type) is IContentReader.IContentReader reader) {
                    ContentManager.ReaderList.TryAdd(reader.Type, reader);
                }
                if (type.IsSubclassOf(typeof(Block))
                    && !type.IsAbstract) {
                    blockTypes.Add(type);
                }
                /*if (type.Namespace == "Game")
                {
                    Log.Warning("\"Game\" is not recommended as a namespace for mod class. It is only for Survivalcraft itself. " + type.AssemblyQualifiedName);
                }*/
            }
            BlockTypes.AddRange(blockTypes);
        }

        public virtual void LoadJs() {
            bool flag = true;
            GetFiles(
                ".js",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "5")}");
                        flag = false;
                    }
                    JsInterface.Execute(new StreamReader(stream).ReadToEnd());
                }
            );
        }

        /// <summary>
        ///     检查依赖项
        /// </summary>
        public virtual void CheckDependencies(List<ModEntity> modEntities) {
            if (IsDisabled || modInfo == null) {
                return;
            }
            if (modInfo.DependencyRanges.Count == 0) {
                IsDependencyChecked = true;
                modEntities.Add(this);
                return;
            }
            LoadingScreen.Info($"[{modInfo.Name}] Checking dependencies.");
            foreach ((string name, VersionRange range) in modInfo.DependencyRanges) {
                ModEntity entity = ModsManager.ModListAll.Find(px => !px.IsDisabled
                    && px.modInfo != null
                    && px.modInfo.PackageName == name
                    && (range.Satisfies(px.modInfo.NuGetVersion) || px.modInfo.Version == range.OriginalString)
                );
                if (entity != null) {
                    if (!entity.IsDependencyChecked) {
                        if (entity.modInfo.DependencyRanges.ContainsKey(modInfo.PackageName)) {
                            throw new Exception($"[{modInfo.Name}] Dependency {name} is dependent on {modInfo.Name}, which is not allowed.");
                        }
                        entity.CheckDependencies(modEntities);
                    }
                }
                else {
                    IsDisabled = true;
                    DisableReason = ModDisableReason.DependencyError;
                    throw new Exception($"[{modInfo.Name}] Failed to find dependency {name}");
                }
            }
            IsDependencyChecked = true;
            modEntities.Add(this);
        }

        /// <summary>
        ///     保存设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void SaveSettings(XElement xElement) {
            Loader?.SaveSettings(xElement);
        }

        /// <summary>
        ///     加载设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadSettings(XElement xElement) {
            Loader?.LoadSettings(xElement);
        }

        /// <summary>
        ///     BlocksManager初始化完毕
        /// </summary>
        // <param name="categories"></param>
        public virtual void OnBlocksInitalized() {
            Loader?.BlocksInitalized();
        }

        //释放资源
        public virtual void Dispose() {
            try {
                Loader?.ModDispose();
            }
            catch {
                // ignored
            }
            ModArchive?.ZipFileStream.Close();
        }

        public override bool Equals(object obj) {
            if (obj is ModEntity px) {
                return px.modInfo.PackageName == modInfo.PackageName && px.modInfo.NuGetVersion.Equals(modInfo.NuGetVersion);
            }
            return false;
        }

        public override int GetHashCode() =>
            // ReSharper disable NonReadonlyMemberInGetHashCode
            modInfo.GetHashCode();
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
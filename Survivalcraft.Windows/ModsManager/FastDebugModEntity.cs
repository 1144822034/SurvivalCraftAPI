using System;
using System.IO;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

using Engine;

namespace Game
{
	public class FastDebugModEntity : ModEntity
	{
		public Dictionary<string, FileInfo> FModFiles = [];

		public FastDebugModEntity()
		{
			modInfo = new ModInfo() { Name = "[Debug]", PackageName = "debug" };
			InitResources();
            modInfo.LoadOrder = int.MinValue + 1;
        }

		public override void InitResources()
		{
			ReadDirResouces(ModsManager.ModsPath, "");
			if (!GetFile("modinfo.json", (stream) =>
			{
				modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(stream));
				modInfo.Name = $"[Debug]{modInfo.Name}";
			}))
			{
				modInfo = new ModInfo() { Name = "FastDebug", Version = "1.0.0", ApiVersion = ModsManager.APIVersionString, Author = "Mod", Description = "调试Mod插件", ScVersion = "2.4.0.0", PackageName = "com.fastdebug" };
			}
			GetFile("icon.png", (stream) => { LoadIcon(stream); });
		}

		public void ReadDirResouces(string basepath, string path)
		{
			if (string.IsNullOrEmpty(path)) path = basepath;
			foreach (string d in Storage.ListDirectoryNames(path)) ReadDirResouces(basepath, path + "/" + d);
			foreach (string f in Storage.ListFileNames(path))
			{
				string abpath = path + "/" + f;
				string FilenameInZip = abpath.Substring(basepath.Length + 1);
				if (FilenameInZip.StartsWith("Assets/"))
				{
					string name = FilenameInZip.Substring(7);
					ContentInfo contentInfo = new(name);
					MemoryStream memoryStream = new();
					using (Stream stream = Storage.OpenFile(abpath, OpenFileMode.Read))
					{
						stream.CopyTo(memoryStream);
						contentInfo.SetContentStream(memoryStream);
						ContentManager.Add(contentInfo);
					}
				}
				FModFiles.Add(FilenameInZip, new FileInfo(Storage.GetSystemPath(abpath)));
			}
		}

		public override Assembly[] GetAssemblies()
		{
		    var assemblies = new List<Assembly>();
			foreach (string c in Storage.ListFileNames(ModsManager.ModsPath))
			{
				if (c.EndsWith(".dll") && !(c.StartsWith("EntitySystem") || c.StartsWith("Engine") || c.StartsWith("Survivalcraft") || c.StartsWith("OpenTK")))
				{
					var assemblyStream = Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, c), OpenFileMode.Read);
					
					assemblies.Add(Assembly.Load(ModsManager.StreamToBytes(assemblyStream)));
				}
			}
			return [.. assemblies];
		}

		public override void LoadClo(ClothingBlock block, ref XElement xElement)
		{
			foreach (string c in Storage.ListFileNames(ModsManager.ModsPath))
			{
				if (c.EndsWith(".clo")) ModsManager.CombineClo(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, c), OpenFileMode.Read));
			}
		}

		public override void LoadCr(ref XElement xElement)
		{
			foreach (string c in Storage.ListFileNames(ModsManager.ModsPath))
			{
				if (c.EndsWith(".cr"))
					ModsManager.CombineCr(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, c), OpenFileMode.Read));
			}
		}

		public override void LoadLauguage()
		{
			string path = Storage.CombinePaths(ModsManager.ModsPath, "Assets/Lang");
			if (Storage.DirectoryExists(path))
			{
				foreach (string c in Storage.ListFileNames(path))
				{
					string fn = ModsManager.Configs["Language"] + ".json";
					string fpn = Storage.CombinePaths(path, c);
					if (c == fn && Storage.FileExists(fpn))
					{
						LanguageControl.loadJson(Storage.OpenFile(fpn, OpenFileMode.Read));
					}
				}
			}
		}

		public override void LoadBlocksData()
		{
			foreach (string c in Storage.ListFileNames(ModsManager.ModsPath))
			{
				if (c.EndsWith(".csv"))
					BlocksManager.LoadBlocksData(ModsManager.StreamToString(Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, c), OpenFileMode.Read)));
			}
		}

		public override void LoadXdb(ref XElement xElement)
		{
			foreach (string c in Storage.ListFileNames(ModsManager.ModsPath))
			{
				if (c.EndsWith(".xdb"))
				{
					ModsManager.CombineDataBase(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, c), OpenFileMode.Read));
				}
			}
			Loader?.OnXdbLoad(xElement);
		}

		/// <summary>
		/// 获取指定后缀文件列表，带.
		/// </summary>
		/// <param name="extension"></param>
		/// <returns></returns>
		public override void GetFiles(string extension, Action<string, Stream> action)
		{
			bool skip = false;
			Loader?.GetModFiles(extension,action,out skip);
			if(skip) return;
			foreach (var item in FModFiles)
			{
				if (item.Key.EndsWith(extension))
				{
					using (Stream fs = item.Value.OpenRead())
					{
						try
						{
							action?.Invoke(item.Key, fs);
						}
						catch (Exception e)
						{
							Log.Error(string.Format("GetFile {0} Error:{1}", item.Key, e));
						}
					}
				}
			}
		}
		public override bool GetFile(string filename, Action<Stream> stream)
		{
			bool skip = false;
			bool loaderReturns = false;
			Loader?.GetModFile(filename,stream,out skip,out loaderReturns);
			if(skip) return loaderReturns;
			if (FModFiles.TryGetValue(filename, out FileInfo fileInfo))
			{
				using (Stream fs = fileInfo.OpenRead())
				{
					try
					{
						stream?.Invoke(fs);
					}
					catch (Exception e)
					{
						Log.Error(string.Format("GetFile {0} Error:{1}", filename, e));
					}
				}
				return true;
			}
			return false;
		}
		public override bool GetAssetsFile(string filename, Action<Stream> stream)
		{
			return GetFile("Assets/" + filename, stream);
		}
	}
}

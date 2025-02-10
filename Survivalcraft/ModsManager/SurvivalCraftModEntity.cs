using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

using Engine;
using Game.IContentReader;
using System.Linq.Expressions;

namespace Game
{
	public class SurvivalCraftModEntity : ModEntity
	{
		public const string fName = "SurvivalCraftModEntity";
		public SurvivalCraftModEntity()
		{
			var readers = new List<IContentReader.IContentReader>();
			readers.AddRange(new IContentReader.IContentReader[]
			{
				new AssemblyReader(),
				new BitmapFontReader(),
				new DaeModelReader(),
				new ImageReader(),
				new JsonArrayReader(),
				new JsonObjectReader(),
				new IContentReader.JsonModelReader(),
				new MtllibStructReader(),
				new IContentReader.ObjModelReader(),
				new ShaderReader(),
				new SoundBufferReader(),
				new StreamingSourceReader(),
				new IContentReader.StringReader(),
				new SubtextureReader(),
				new Texture2DReader(),
				new XmlReader(),
				new ContentStreamReader()
			});
			for (int i = 0; i < readers.Count; i++)
			{
				ContentManager.ReaderList.Add(readers[i].Type, readers[i]);
			}
			MemoryStream memoryStream = new();
			string ContentPath = "app:/Content.zip";
			if(Storage.FileExists(ContentPath))//检测外置资源是否存在，如果不存在就使用内置资源
			{
				Storage.OpenFile(ContentPath,OpenFileMode.Read).CopyTo(memoryStream);
			}
			else
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				assembly.GetManifestResourceStream("Game.Content.zip").CopyTo(memoryStream);
			}
			if(memoryStream == null)
			{
				throw new Exception("Unable to load Content.zip file.");
			}
			memoryStream.Position = 0L;
			ModArchive = ZipArchive.Open(memoryStream, false);
			InitResources();
			if(modInfo != null)
			{
				modInfo.LoadOrder = int.MinValue;
			}
		}
		public override void LoadBlocksData()
		{
			LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "1")}");
			BlocksManager.LoadBlocksData(ContentManager.Get<string>("BlocksData"));
			ContentManager.Dispose("BlocksData");
		}
		public override Assembly[] GetAssemblies()
		{
			return [typeof(BlocksManager).Assembly];
		}
		public override void HandleAssembly(Assembly assembly)
		{
			var types = assembly.GetTypes();
			foreach (var type in types)
			{
				if (type.IsSubclassOf(typeof(ModLoader)) && !type.IsAbstract)
				{
					if(Activator.CreateInstance(type) is not ModLoader modLoader) continue;
					modLoader.Entity = this;
					modLoader.__ModInitialize();
					Loader = modLoader;
					ModsManager.ModLoaders.Add(modLoader);
				}
				else if (type.IsSubclassOf(typeof(Block)) && !type.IsAbstract)
				{
					var fieldInfo = type.GetRuntimeFields().FirstOrDefault(p => p.Name == "Index" && p.IsPublic && p.IsStatic);
					if (fieldInfo == null || fieldInfo.FieldType != typeof(int))
					{
						ModsManager.AddException(new InvalidOperationException($"Block type \"{type.FullName}\" does not have static field Index of type int."));
					}
					else
					{
						/*var index = (int)fieldInfo.GetValue(null);
						var block = (Block)Activator.CreateInstance(type.GetTypeInfo().AsType());
						block.BlockIndex = index;*/
						BlockTypes.Add(type);
					}
				}
			}
		}
		public override void LoadXdb(ref XElement xElement)
		{
			LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "2")}");
			xElement = ContentManager.Get<XElement>("Database");
			ContentManager.Dispose("Database");
		}
		public override void LoadCr(ref XElement xElement)
		{
			LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "3")}");
			xElement = ContentManager.Get<XElement>("CraftingRecipes");
			ContentManager.Dispose("CraftingRecipes");
		}
		public override void LoadClo(ClothingBlock block, ref XElement xElement)
		{
			LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "4")}");
			xElement = ContentManager.Get<XElement>("Clothes");
			ContentManager.Dispose("Clothes");
		}
		public override void SaveSettings(XElement xElement)
		{


		}
		public override void LoadSettings(XElement xElement)
		{



		}
		public override void OnBlocksInitalized()
		{
			BlocksManager.AddCategory("Terrain");
			BlocksManager.AddCategory("Minerals");
			BlocksManager.AddCategory("Plants");
			BlocksManager.AddCategory("Construction");
			BlocksManager.AddCategory("Items");
			BlocksManager.AddCategory("Tools");
			BlocksManager.AddCategory("Weapons");
			BlocksManager.AddCategory("Clothes");
			BlocksManager.AddCategory("Electrics");
			BlocksManager.AddCategory("Food");
			BlocksManager.AddCategory("Spawner Eggs");
			BlocksManager.AddCategory("Painted");
			BlocksManager.AddCategory("Dyed");
			BlocksManager.AddCategory("Fireworks");
		}
	}
}

using Engine;
using Engine.Graphics;

namespace Game
{
	public static class BlocksTexturesManager
	{
		public const string fName = "BlocksTexturesManager";
		public static List<string> m_blockTextureNames = [];

		public static Texture2D DefaultBlocksTexture
		{
			get;
			set;
		}

		public static ReadOnlyList<string> BlockTexturesNames => new(m_blockTextureNames);

		public static string BlockTexturesDirectoryName => ModsManager.BlockTexturesDirectoryName;

		public static event Action<string> BlocksTextureDeleted;

		public static void Initialize()
		{
			Storage.CreateDirectory(BlockTexturesDirectoryName);
			DefaultBlocksTexture = ContentManager.Get<Texture2D>("Textures/Blocks");
		}

		public static bool IsBuiltIn(string name)
		{
			return string.IsNullOrEmpty(name);
		}

		public static string GetFileName(string name)
		{
			if (IsBuiltIn(name))
			{
				return null;
			}
			return Storage.CombinePaths(BlockTexturesDirectoryName, name);
		}

		public static string GetDisplayName(string name)
		{
			if (IsBuiltIn(name))
			{
				return "Survivalcraft";
			}
			return Storage.GetFileNameWithoutExtension(name);
		}

		public static DateTime GetCreationDate(string name)
		{
			try
			{
				if (!IsBuiltIn(name))
				{
					return Storage.GetFileLastWriteTime(GetFileName(name));
				}
			}
			catch
			{
				// ignored
			}
			return new DateTime(2000, 1, 1);
		}

		public static Texture2D LoadTexture(string name)
		{
			Texture2D texture2D = null;
			if (!IsBuiltIn(name))
			{
				try
				{
					string fileName = GetFileName(name);
					if(Storage.FileExists(fileName))
					{
						Image image = Image.Load(fileName);
						ValidateBlocksTexture(image);
						texture2D = Texture2D.Load(image);
						texture2D.Tag = image;
					}
					else
					{
						Log.Warning(string.Format(LanguageControl.Get(fName,"1"),name));
					}
				}
				catch (Exception ex)
				{
					Log.Warning(string.Format(LanguageControl.Get(fName,"2"),name, ex.Message));
				}
			}
			texture2D ??= DefaultBlocksTexture;
			return texture2D;
		}

		public static string ImportBlocksTexture(string name, Stream stream)
		{
			Exception ex = ExternalContentManager.VerifyExternalContentName(name);
			if (ex != null)
			{
				throw ex;
			}
			if (Storage.GetExtension(name) != ".scbtex")
			{
				name += ".scbtex";
			}
			ValidateBlocksTexture(stream);
			stream.Position = 0L;
			using (Stream destination = Storage.OpenFile(GetFileName(name), OpenFileMode.Create))
			{
				stream.CopyTo(destination);
				return name;
			}
		}

		public static void DeleteBlocksTexture(string name)
		{
			try
			{
				string fileName = GetFileName(name);
				if (!string.IsNullOrEmpty(fileName))
				{
					Storage.DeleteFile(fileName);
					BlocksTextureDeleted?.Invoke(name);
				}
			}
			catch (Exception e)
			{
				ExceptionManager.ReportExceptionToUser(string.Format(LanguageControl.Get(fName,"3"),name), e);
			}
		}

		public static void UpdateBlocksTexturesList()
		{
			m_blockTextureNames.Clear();
			m_blockTextureNames.Add(string.Empty);
			foreach (string item in Storage.ListFileNames(BlockTexturesDirectoryName))
			{
				m_blockTextureNames.Add(item);
			}
		}

		public static void ValidateBlocksTexture(Stream stream)
		{
			Image image = Image.Load(stream);
			if (image.Width > 65536 || image.Height > 65536)
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get(fName,"4"),image.Width,image.Height));
			}
			if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get(fName,"5"),image.Width,image.Height));
			}
			image.Dispose();
		}

		public static void ValidateBlocksTexture(Image image)
		{
			if (image.Width > 65536 || image.Height > 65536)
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get(fName,"4"),image.Width,image.Height));
			}
			if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
			{
				throw new InvalidOperationException(string.Format(LanguageControl.Get(fName,"5"),image.Width,image.Height));
			}
		}
	}
}

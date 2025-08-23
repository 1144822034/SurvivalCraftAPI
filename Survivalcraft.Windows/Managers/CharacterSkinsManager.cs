using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game
{
	public static class CharacterSkinsManager
	{
		public static List<string> m_characterSkinNames = [];

		public static Dictionary<PlayerClass, Model> m_playerModels = [];

		public static Dictionary<PlayerClass, Model> m_outerClothingModels = [];

		public static ReadOnlyList<string> CharacterSkinsNames => new(m_characterSkinNames);

		public static string CharacterSkinsDirectoryName => ModsManager.CharacterSkinsDirectoryName;

		public static event Action<string> CharacterSkinDeleted;

		public static void Initialize()
		{
			Storage.CreateDirectory(CharacterSkinsDirectoryName);
		}

		public static bool IsBuiltIn(string name)
		{
			return name.StartsWith('$');
		}

		public static PlayerClass? GetPlayerClass(string name)
		{
			name = name.ToLower();
			if (name.Contains("female") || name.Contains("girl") || name.Contains("woman"))
			{
				return PlayerClass.Female;
			}
			if (name.Contains("male") || name.Contains("boy") || name.Contains("man"))
			{
				return PlayerClass.Male;
			}
			return null;
		}

		public static string GetFileName(string name)
		{
			if (IsBuiltIn(name))
			{
				return null;
			}
			return Storage.CombinePaths(CharacterSkinsDirectoryName, name);
		}

		public static string GetDisplayName(string name)
		{
			if (IsBuiltIn(name))
			{
				if (name.Contains("Female"))
				{
					if (name.Contains("1"))
					{
						return "Doris";
					}
					if (name.Contains("2"))
					{
						return "Mabel";
					}
					if (name.Contains("3"))
					{
						return "Ada";
					}
					return "Shirley";
				}
				if (name.Contains("1"))
				{
					return "Walter";
				}
				if (name.Contains("2"))
				{
					return "Basil";
				}
				if (name.Contains("3"))
				{
					return "Geoffrey";
				}
				return "Zachary";
			}
			return Storage.GetFileNameWithoutExtension(name);
		}

		public static DateTime GetCreationDate(string name)
		{
			try
			{
				string fileName = GetFileName(name);
				if (!string.IsNullOrEmpty(fileName))
				{
					return Storage.GetFileLastWriteTime(fileName);
				}
			}
			catch
			{
			}
			return new DateTime(2000, 1, 1);
		}

		public static Texture2D LoadTexture(string name)
		{
			Texture2D texture2D = null;
			try
			{
				string fileName = GetFileName(name);
				if (!string.IsNullOrEmpty(fileName))
				{
					using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read))
					{
						ValidateCharacterSkin(stream);
						stream.Position = 0L;
						texture2D = Texture2D.Load(stream);
					}
				}
				else
				{
					texture2D = ContentManager.Get<Texture2D>("Textures/Creatures/Human" + name.Substring(1).Replace(" ", ""));
				}
			}
			catch (Exception ex)
			{
				Log.Warning($"Could not load character skin \"{name}\". Reason: {ex.Message}.");
			}
			if (texture2D == null)
			{
				texture2D = ContentManager.Get<Texture2D>("Textures/Creatures/HumanMale1");
			}
			return texture2D;
		}

		public static string ImportCharacterSkin(string name, Stream stream)
		{
			Exception ex = ExternalContentManager.VerifyExternalContentName(name);
			if (ex != null)
			{
				throw ex;
			}
			if (Storage.GetExtension(name) != ".scskin")
			{
				name += ".scskin";
			}
			ValidateCharacterSkin(stream);
			stream.Position = 0L;
			using (Stream destination = Storage.OpenFile(GetFileName(name), OpenFileMode.Create))
			{
				stream.CopyTo(destination);
				return name;
			}
		}

		public static void DeleteCharacterSkin(string name)
		{
			try
			{
				string fileName = GetFileName(name);
				if (!string.IsNullOrEmpty(fileName))
				{
					Storage.DeleteFile(fileName);
					CharacterSkinDeleted?.Invoke(name);
				}
			}
			catch (Exception e)
			{
				ExceptionManager.ReportExceptionToUser($"Unable to delete character skin \"{name}\"", e);
			}
		}

		public static void UpdateCharacterSkinsList()
		{
			m_characterSkinNames.Clear();
			m_characterSkinNames.Add("$Male1");
			m_characterSkinNames.Add("$Male2");
			m_characterSkinNames.Add("$Male3");
			m_characterSkinNames.Add("$Male4");
			m_characterSkinNames.Add("$Female1");
			m_characterSkinNames.Add("$Female2");
			m_characterSkinNames.Add("$Female3");
			m_characterSkinNames.Add("$Female4");
			foreach (string item in Storage.ListFileNames(CharacterSkinsDirectoryName))
			{
				if (Storage.GetExtension(item).ToLower() == ".scskin")
				{
					m_characterSkinNames.Add(item);
				}
			}
		}

		public static Model GetPlayerModel(PlayerClass playerClass)
		{
			if (!m_playerModels.TryGetValue(playerClass, out Model value))
			{
				ValuesDictionary valuesDictionary;
				switch (playerClass)
				{
					case PlayerClass.Male:
						valuesDictionary = DatabaseManager.FindEntityValuesDictionary("MalePlayer", throwIfNotFound: true);
						break;
					case PlayerClass.Female:
						valuesDictionary = DatabaseManager.FindEntityValuesDictionary("FemalePlayer", throwIfNotFound: true);
						break;
					default:
						throw new InvalidOperationException("Unknown player class.");
				}
				value = ContentManager.Get<Model>(valuesDictionary.GetValue<ValuesDictionary>("HumanModel").GetValue<string>("ModelName"));
				m_playerModels.Add(playerClass, value);
			}
			return value;
		}

		public static Model GetOuterClothingModel(PlayerClass playerClass)
		{
			if (!m_outerClothingModels.TryGetValue(playerClass, out Model value))
			{
				ValuesDictionary valuesDictionary;
				switch (playerClass)
				{
					case PlayerClass.Male:
						valuesDictionary = DatabaseManager.FindEntityValuesDictionary("MalePlayer", throwIfNotFound: true);
						break;
					case PlayerClass.Female:
						valuesDictionary = DatabaseManager.FindEntityValuesDictionary("FemalePlayer", throwIfNotFound: true);
						break;
					default:
						throw new InvalidOperationException("Unknown player class.");
				}
				value = ContentManager.Get<Model>(valuesDictionary.GetValue<ValuesDictionary>("OuterClothingModel").GetValue<string>("ModelName"));
				m_outerClothingModels.Add(playerClass, value);
			}
			return value;
		}

		public static void ValidateCharacterSkin(Stream stream)
		{
			var image = Image.Load(stream);
			if (image.Width > 65536 || image.Height > 65536)
			{
				throw new InvalidOperationException($"Character skin is larger than 65536x65536 pixels (size={image.Width}x{image.Height})");
			}
			if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
			{
				throw new InvalidOperationException($"Character skin does not have power-of-two size (size={image.Width}x{image.Height})");
			}
		}
	}
}

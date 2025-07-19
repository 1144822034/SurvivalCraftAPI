using Engine;
using System.Globalization;
using System.Text.Json;
using System.IO;
using System.Text.Json.Nodes;
namespace Game
{
	public static class LanguageControl
	{
		public static JsonNode jsonNode = null;
		public static JsonNode englishJsonNode = null;
		public static string Ok = default;
		public static string Cancel = default;
		public static string None = default;
		public static string Nothing = default;
		public static string Error = default;
		public static string On = default;
		public static string Off = default;
		public static string Disable = default;
		public static string Enable = default;
		public static string Warning = default;
		public static string Back = default;
		public static string Allowed = default;
		public static string NAllowed = default;
		public static string Unknown = default;
		public static string Yes = default;
		public static string No = default;
		public static string Unavailable = default;
		public static string Exists = default;
		public static string Success = default;
		public static string Delete = default;
		/// <summary>
		/// 语言标识符、与相应的CultureInfo
		/// </summary>
		public static Dictionary<string,CultureInfo> LanguageTypes = [];

		public static void Initialize(string languageType)
		{
			Ok = default;
			Cancel = default;
			None = default;
			Nothing = default;
			Error = default;
			On = default;
			Off = default;
			Disable = default;
			Enable = default;
			Warning = default;
			Back = default;
			Allowed = default;
			NAllowed = default;
			Unknown = default;
			Yes = default;
			No = default;
			Unavailable = default;
			Exists = default;
			Success = default;
			Delete = default;
            jsonNode = null;
			ModsManager.SetConfig("Language", languageType);
		}
		public static void loadJson(Stream stream)
		{
			string txt = new StreamReader(stream).ReadToEnd();
			if (txt.Length > 0)
			{//加载原版语言包
				JsonNode newJsonNode = null;
				try
				{
					newJsonNode = JsonNode.Parse(txt);
				}
				catch(Exception e)
				{
					Log.Error($"Invalid json file, reason: {e}");
					return;
				}
                if (jsonNode == null)
				{
					jsonNode = newJsonNode;
				}
				else
				{
                    MergeJsonNode(jsonNode, newJsonNode);
                }
            }
			SetUsual();
		}

		public static void LoadEnglishJson(Stream stream)
		{
			string txt = new StreamReader(stream).ReadToEnd();
			if (txt.Length > 0)
			{
				JsonNode newJsonNode = null;
				try
				{
					newJsonNode = JsonNode.Parse(txt);
				}
				catch(Exception e)
				{
					Log.Error($"Invalid json file, reason: {e}");
					return;
				}
				if (englishJsonNode == null)
				{
					englishJsonNode = newJsonNode;
				}
				else
				{
					MergeJsonNode(englishJsonNode, newJsonNode);
				}
			}
			SetUsual();
		}

		public static void SetUsual()
		{
			Ok ??= Get("Usual", "ok");
			Cancel ??= Get("Usual", "cancel");
			None ??= Get("Usual", "none");
			Nothing ??= Get("Usual", "nothing");
			Error ??= Get("Usual", "error");
			On ??= Get("Usual", "on");
			Off ??= Get("Usual", "off");
			Disable ??= Get("Usual", "disable");
			Enable ??= Get("Usual", "enable");
			Warning ??= Get("Usual", "warning");
			Back ??= Get("Usual", "back");
			Allowed ??= Get("Usual", "allowed");
			NAllowed ??= Get("Usual", "not allowed");
			Unknown ??= Get("Usual", "unknown");
			Yes ??= Get("Usual", "yes");
			No ??= Get("Usual", "no");
			Unavailable ??= Get("Usual", "Unavailable");
			Exists ??= Get("Usual", "exist");
			Success ??= Get("Usual", "success");
			Delete ??= Get("Usual", "delete");
		}
		public static void MergeJsonNode(JsonNode oldNode, JsonNode newNode)
		{
			if (oldNode == null || newNode == null)
			{
				return;
			}
			switch (newNode.GetValueKind())
			{
				case JsonValueKind.Object:
					{
						foreach (var newChild in newNode.AsObject())
						{
							JsonNode oldChild = oldNode[newChild.Key];
							if (oldChild == null)
							{
								oldNode.AsObject().Add(newChild.Key, newChild.Value.DeepClone());
							}
							else
							{
								MergeJsonNode(oldChild, newChild.Value);
							}
						}

						break;
					}
				case JsonValueKind.Array:
					{
						if (oldNode.GetValueKind() == JsonValueKind.Array)
						{
							JsonArray oldArray = oldNode.AsArray();
							JsonArray newArray = newNode.AsArray();
							if (newArray.Count >= oldArray.Count)
							{
								oldNode.ReplaceWith(newNode.DeepClone());
							}
							else
							{
								for (int i = 0; i < newArray.Count; i++)
								{
									oldNode[i] = newArray[i];
								}
							}
						}
						else
						{
							oldNode.ReplaceWith(newNode.DeepClone());
						}
						break;
					}
				case JsonValueKind.String:
				case JsonValueKind.Number:
				case JsonValueKind.True:
				case JsonValueKind.False:
					{
                        oldNode.ReplaceWith(newNode.DeepClone());
						break;
                    }
			}
        }
		/// <returns>当前语言的标识符</returns>
		public static string LName()
		{
			return ModsManager.Configs["Language"];
		}
		/// <summary>
		/// 获取在当前语言类名键对应的字符串
		/// </summary>
		/// <param name="className">类名</param>
		/// <param name="key">键</param>
		/// <returns>本地化字符串</returns>
		public static string Get(string className, int key)
		{//获得键值
			return Get(className, key.ToString());
		}
		public static string GetWorldPalette(int index)
		{
			return Get("WorldPalette", "Colors", index.ToString());
		}
		public static string Get(params string[] key)
		{
			return Get(out bool _, key);
		}
		public static string Get(out bool r, params string[] keys)
		{//获得键值
			if(jsonNode == null)
			{
				if(englishJsonNode == null)
				{
					r = false;
					return String.Join(':', keys);
				}
				jsonNode = englishJsonNode;
			}
			string result = Get(out r,jsonNode,keys);
			if(r)
			{
				return result;
			}
			if(ModsManager.Configs["Language"] != "en-US")
			{
				result = Get(out r,englishJsonNode,keys);
			}
			return result;
		}

		public static string Get(out bool r,JsonNode node,params string[] keys)
		{
			r = false;
			JsonNode nowNode = node;
			bool flag = false;
			foreach (string key in keys)
			{
				if(string.IsNullOrEmpty(key) || nowNode == null)
				{
					break;
				}
				if(nowNode.GetValueKind() == JsonValueKind.Object)
				{
					nowNode = nowNode[key];
					if (nowNode == null)
					{
						break;
					}
					else
					{
						flag = true;
					}
				}
				else if(nowNode.GetValueKind() == JsonValueKind.Array && int.TryParse(key, out int num) && num >= 0)
				{
					JsonArray array = nowNode.AsArray();
					if(num < array.Count)
					{
						nowNode = array[num];
						flag = true;
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}
			if (nowNode != null)
			{
				switch (nowNode.GetValueKind())
				{
					case JsonValueKind.String:
						r = true;
						return nowNode.GetValue<string>();
					case JsonValueKind.Number:
						r = true;
						return nowNode.GetValue<decimal>().ToString();
				}
			}
			return flag? keys.Last() : String.Join(':', keys);
		}

		public static string GetWithoutFallback(out bool r,params string[] keys)
		{
			return Get(out r,jsonNode,keys);
		}
		public static string GetBlock(string blockName, string prop)
		{
			return TryGetBlock(blockName, prop, out var result) ? result : result;
		}
		public static bool TryGetBlock(string blockName, string prop, out string result)
		{
			if(blockName.Length == 0)
			{
				result = string.Empty;
				return false;
			}
			string[] nm = blockName.Split(':');//这里不要改成集合表达式，在不同的编译器上会导致bug或者编译失败
			result = Get(out bool r, "Blocks", nm.Length < 2 ? (blockName + ":0") : blockName, prop);
			if (!r) result = Get(out r, "Blocks", nm[0] + ":0", prop);
			return r;
		}
		public static string GetContentWidgets(string name, string prop)
		{
			return Get("ContentWidgets", name, prop);
		}
		public static string GetContentWidgets(string name, int pos)
		{
			return Get("ContentWidgets", name, pos.ToString());
		}
		public static string GetDatabase(string name, string prop)
		{
			return Get("Database", name, prop);
		}
		public static string GetFireworks(string name, string prop)
		{
			return Get("FireworksBlock", name, prop);
		}

		public static void ChangeLanguage(string languageType)
		{
			Initialize(languageType);
			if(languageType == "en-US" && englishJsonNode != null)
			{
				jsonNode = englishJsonNode;
				SetUsual();
			}
			else
			{
				foreach(var c in ModsManager.ModList)
				{
					c.LoadLauguage();
				}
			}
#if WINDOWS
			string title = $"{Get("Usual", "gameName")} {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
#if DEBUG
			title = $"[{Get("Usual","debug")}]{title}";
#endif
			Window.TitlePrefix = title;
#endif
			Dictionary<string, object> objs = [];
			foreach (var c in ScreensManager.m_screens)
			{
				Type type = c.Value.GetType();
				object obj = Activator.CreateInstance(type);
				objs.Add(c.Key, obj);
			}
			foreach (var c in objs)
			{
				ScreensManager.m_screens[c.Key] = c.Value as Screen;
			}
			CraftingRecipesManager.Initialize();
			BlocksManager.Blocks[ClothingBlock.Index].Initialize();
			BlocksManager.Blocks[EggBlock.Index].Initialize();
			ScreensManager.SwitchScreen("MainMenu");
		}
	}
}

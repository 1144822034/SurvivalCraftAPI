using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Engine;
using Game;
using XmlUtilities;

public static class OriginalCommunityContentManager
{
	private const string m_scResDirAddress = "https://scresdir.appspot.com/resource";

	private static Dictionary<string, string> m_idToAddressMap = new Dictionary<string, string>();

	private static Dictionary<string, bool> m_feedbackCache = new Dictionary<string, bool>();

	public const string fName1 = "CommunityContentManager";

	public static void Initialize()
	{
		Load();
		WorldsManager.WorldDeleted += delegate(string path)
		{
			m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.World, path));
		};
		BlocksTexturesManager.BlocksTextureDeleted += delegate(string path)
		{
			m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.BlocksTexture, path));
		};
		CharacterSkinsManager.CharacterSkinDeleted += delegate(string path)
		{
			m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.CharacterSkin, path));
		};
		FurniturePacksManager.FurniturePackDeleted += delegate(string path)
		{
			m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.FurniturePack, path));
		};
		Window.Deactivated += delegate
		{
			Save();
		};
	}

	public static string GetDownloadedContentAddress(ExternalContentType type, string name)
	{
		m_idToAddressMap.TryGetValue(MakeContentIdString(type, name), out var value);
		return value;
	}

	public static bool IsContentRated(string address, string userId)
	{
		string key = MakeFeedbackCacheKey(address, "Rating", userId);
		return m_feedbackCache.ContainsKey(key);
	}

	public static void List(string cursor, string userId, string userUrl, string type, string moderation, string search, string sortOrder, CancellableProgress progress, Action<List<OriginalCommunityContentEntry>, string> success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		if (!WebManager.IsInternetConnectionAvailable())
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "1")));
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("Action", "list");
		dictionary.Add("Cursor", cursor ?? string.Empty);
		dictionary.Add("UserId", userId ?? string.Empty);
		dictionary.Add("UserUrl", userUrl ?? string.Empty);
		dictionary.Add("Type", type ?? string.Empty);
		dictionary.Add("Moderation", moderation ?? string.Empty);
		dictionary.Add("Search", search ?? string.Empty);
		dictionary.Add("SortOrder", sortOrder ?? string.Empty);
		dictionary.Add("Platform", VersionsManager.PlatformString);
		dictionary.Add("Version", VersionsManager.Version);
		WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(dictionary), progress, delegate(byte[] result)
		{
			try
			{
				XElement xElement = XmlUtils.LoadXmlFromString(Encoding.UTF8.GetString(result, 0, result.Length), throwOnError: true);
				string attributeValue = XmlUtils.GetAttributeValue<string>(xElement, "NextCursor");
				List<OriginalCommunityContentEntry> list = new List<OriginalCommunityContentEntry>();
				string downloadString = LanguageControl.Get("OriginalCommunityContentScreen","10");
				foreach (XElement item in xElement.Elements())
				{
					try
					{
						list.Add(new OriginalCommunityContentEntry
						{
							Type = XmlUtils.GetAttributeValue(item, "Type", ExternalContentType.Unknown),
							Name = XmlUtils.GetAttributeValue<string>(item, "Name"),
							Url = XmlUtils.GetAttributeValue<string>(item, "Url"),
							Version = XmlUtils.GetAttributeValue(item, "Version", new Version(0, 0)),
							Size = XmlUtils.GetAttributeValue<long>(item, "Size"),
							ExtraText = XmlUtils.GetAttributeValue(item, "ExtraText", string.Empty).Replace("downloads", downloadString),
							RatingsAverage = XmlUtils.GetAttributeValue(item, "RatingsAverage", 0f)
						});
					}
					catch (Exception)
					{
					}
				}
				success(list, attributeValue);
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}, delegate(Exception error)
		{
			failure(error);
		});
	}

	public static void Download(string address, string name, ExternalContentType type, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		if (!WebManager.IsInternetConnectionAvailable())
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "1")));
			return;
		}
		WebManager.Get(address, null, null, progress, delegate(byte[] data)
		{
			string hash = CalculateContentHashString(data);
			ExternalContentManager.ImportExternalContent(new MemoryStream(data), type, name, delegate(string downloadedName)
			{
				m_idToAddressMap[MakeContentIdString(type, downloadedName)] = address;
				Feedback(address, "Success", null, hash, data.Length, userId, progress, delegate
				{
				}, delegate
				{
				});
				//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Download Success", new AnalyticsParameter("Name", name));
				success();
			}, delegate(Exception error)
			{
				Feedback(address, "ImportFailure", null, hash, data.Length, userId, null, delegate
				{
				}, delegate
				{
				});
				//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Import Failure", new AnalyticsParameter("Name", name), new AnalyticsParameter("Error", error.Message.ToString()));
				failure(error);
			});
		}, delegate(Exception error)
		{
			Feedback(address, "DownloadFailure", null, null, 0L, userId, null, delegate
			{
			}, delegate
			{
			});
			//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Download Failure", new AnalyticsParameter("Name", name), new AnalyticsParameter("Error", error.Message.ToString()));
			failure(error);
		});
	}

	public static void Publish(string address, string name, ExternalContentType type, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		if (MarketplaceManager.IsTrialMode)
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "2")));
			return;
		}
		if (!WebManager.IsInternetConnectionAvailable())
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "1")));
			return;
		}
		VerifyLinkContent(address, name, type, progress, delegate(byte[] data)
		{
			string value = CalculateContentHashString(data);
			WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(new Dictionary<string, string>
			{
				{ "Action", "publish" },
				{ "UserId", userId },
				{ "Name", name },
				{ "Url", address },
				{
					"Type",
					type.ToString()
				},
				{ "Hash", value },
				{
					"Size",
					data.Length.ToString(CultureInfo.InvariantCulture)
				},
				{
					"Platform",
					VersionsManager.PlatformString
				},
				{
					"Version",
					VersionsManager.Version
				}
			}), progress, delegate
			{
				success();
				//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Publish Success", new AnalyticsParameter("Name", name), new AnalyticsParameter("Type", type.ToString()), new AnalyticsParameter("Size", data.Length.ToString()), new AnalyticsParameter("User", userId));
			}, delegate(Exception error)
			{
				failure(error);
				//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Publish Failure", new AnalyticsParameter("Name", name), new AnalyticsParameter("Type", type.ToString()), new AnalyticsParameter("Size", data.Length.ToString()), new AnalyticsParameter("User", userId), new AnalyticsParameter("Error", error.Message.ToString()));
			});
		}, failure);
	}

	public static void Delete(string address, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		if (!WebManager.IsInternetConnectionAvailable())
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "1")));
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("Action", "delete");
		dictionary.Add("UserId", userId);
		dictionary.Add("Url", address);
		dictionary.Add("Platform", VersionsManager.PlatformString);
		dictionary.Add("Version", VersionsManager.Version);
		WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(dictionary), progress, delegate
		{
			success();
			//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Delete Success", new AnalyticsParameter("Name", address), new AnalyticsParameter("User", userId));
		}, delegate(Exception error)
		{
			failure(error);
			//AnalyticsManager.LogEvent("[OriginalCommunityContentManager] Delete Failure", new AnalyticsParameter("Name", address), new AnalyticsParameter("User", userId), new AnalyticsParameter("Error", error.Message.ToString()));
		});
	}

	public static void Rate(string address, string userId, int rating, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		rating = Math.Clamp(rating, 1, 5);
		Feedback(address, "Rating", rating.ToString(CultureInfo.InvariantCulture), null, 0L, userId, progress, success, failure);
	}

	public static void Report(string address, string userId, string report, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		Feedback(address, "Report", report, null, 0L, userId, progress, success, failure);
	}

	public static void SendPlayTime(string address, string userId, double time, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		Feedback(address, "PlayTime", Math.Round(time).ToString(CultureInfo.InvariantCulture), null, 0L, userId, progress, success, failure);
	}

	private static void VerifyLinkContent(string address, string name, ExternalContentType type, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		WebManager.Get(address, null, null, progress, delegate(byte[] data)
		{
			ExternalContentManager.ImportExternalContent(new MemoryStream(data), type, "__Temp", delegate(string downloadedName)
			{
				ExternalContentManager.DeleteExternalContent(type, downloadedName);
				success(data);
			}, failure);
		}, failure);
	}

	private static void Feedback(string address, string feedback, string feedbackParameter, string hash, long size, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
	{
		progress = progress ?? new CancellableProgress();
		if (!WebManager.IsInternetConnectionAvailable())
		{
			failure(new InvalidOperationException(LanguageControl.Get(fName1, "1")));
			return;
		}
		string key = MakeFeedbackCacheKey(address, feedback, userId);
		if (m_feedbackCache.ContainsKey(key))
		{
			Task.Run(delegate
			{
				Task.Delay(1500).Wait();
				failure(new InvalidOperationException(LanguageControl.Get(fName1, "3")));
			});
			return;
		}
		m_feedbackCache[key] = true;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("Action", "feedback");
		dictionary.Add("Feedback", feedback);
		if (feedbackParameter != null)
		{
			dictionary.Add("FeedbackParameter", feedbackParameter);
		}
		dictionary.Add("UserId", userId);
		if (address != null)
		{
			dictionary.Add("Url", address);
		}
		if (hash != null)
		{
			dictionary.Add("Hash", hash);
		}
		if (size > 0)
		{
			dictionary.Add("Size", size.ToString(CultureInfo.InvariantCulture));
		}
		dictionary.Add("Platform", VersionsManager.PlatformString);
		dictionary.Add("Version", VersionsManager.Version);
		WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(dictionary), progress, delegate
		{
			success();
		}, delegate(Exception error)
		{
			failure(error);
		});
	}

	private static string CalculateContentHashString(byte[] data)
	{
		return Convert.ToBase64String(SHA1.HashData(data));
	}

	private static string MakeFeedbackCacheKey(string address, string feedback, string userId)
	{
		return address + "\n" + feedback + "\n" + userId;
	}

	private static string MakeContentIdString(ExternalContentType type, string name)
	{
		return type.ToString() + ":" + name;
	}

	private static void Load()
	{
		try
		{
			if (!Storage.FileExists(ModsManager.OriginalCommunityContentCachePath))
			{
				return;
			}
			using Stream stream = Storage.OpenFile(ModsManager.OriginalCommunityContentCachePath, OpenFileMode.Read);
			XElement xElement = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
			foreach (XElement item in xElement.Element("Feedback").Elements())
			{
				string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Key");
				m_feedbackCache[attributeValue] = true;
			}
			foreach (XElement item2 in xElement.Element("Content").Elements())
			{
				string attributeValue2 = XmlUtils.GetAttributeValue<string>(item2, "Path");
				string attributeValue3 = XmlUtils.GetAttributeValue<string>(item2, "Address");
				m_idToAddressMap[attributeValue2] = attributeValue3;
			}
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser(LanguageControl.Get(fName1, "4"), e);
		}
	}

	private static void Save()
	{
		try
		{
			XElement xElement = new XElement("Cache");
			XElement xElement2 = new XElement("Feedback");
			xElement.Add(xElement2);
			foreach (string key in m_feedbackCache.Keys)
			{
				XElement xElement3 = new XElement("Item");
				XmlUtils.SetAttributeValue(xElement3, "Key", key);
				xElement2.Add(xElement3);
			}
			XElement xElement4 = new XElement("Content");
			xElement.Add(xElement4);
			foreach (KeyValuePair<string, string> item in m_idToAddressMap)
			{
				XElement xElement5 = new XElement("Item");
				XmlUtils.SetAttributeValue(xElement5, "Path", item.Key);
				XmlUtils.SetAttributeValue(xElement5, "Address", item.Value);
				xElement4.Add(xElement5);
			}
			using Stream stream = Storage.OpenFile(ModsManager.OriginalCommunityContentCachePath, OpenFileMode.Create);
			XmlUtils.SaveXmlToStream(xElement, stream, null, throwOnError: true);
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser(LanguageControl.Get(fName1, "5"), e);
		}
	}
}

using Engine;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using XmlUtilities;
using static ManageUserScreen;

namespace Game
{
	public static class CommunityContentManager
	{
		public const string m_scResDirAddress = "https://m.schub.top/com/list";

		public static Dictionary<string, string> m_idToAddressMap = [];
		public static Dictionary<string, bool> m_feedbackCache = [];

		public const string fName = "CommunityContentManager";

		public static void Initialize()
		{
			Load();
			WorldsManager.WorldDeleted += delegate (string path)
			{
				m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.World, path));
			};
			BlocksTexturesManager.BlocksTextureDeleted += delegate (string path)
			{
				m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.BlocksTexture, path));
			};
			CharacterSkinsManager.CharacterSkinDeleted += delegate (string path)
			{
				m_idToAddressMap.Remove(MakeContentIdString(ExternalContentType.CharacterSkin, path));
			};
			FurniturePacksManager.FurniturePackDeleted += delegate (string path)
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
			m_idToAddressMap.TryGetValue(MakeContentIdString(type, name), out string value);
			return value;
		}

		public static bool IsContentRated(string address, string userId)
		{
			string key = MakeFeedbackCacheKey(address, "Rating", userId);
			return m_feedbackCache.ContainsKey(key);
		}

		public static void List(string cursor, string userFilter, string typeFilter, string moderationFilter, string sortOrder, string keySearch, string searchType, CancellableProgress progress, Action<List<CommunityContentEntry>, string> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var dictionary = new Dictionary<string, string>();
			var Header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			dictionary.Add("Action", "list");
			dictionary.Add("Cursor", cursor ?? string.Empty);
			dictionary.Add("UserId", userFilter ?? string.Empty);
			dictionary.Add("Type", typeFilter ?? string.Empty);
			dictionary.Add("Moderation", moderationFilter ?? string.Empty);
			dictionary.Add("SortOrder", sortOrder ?? string.Empty);
			dictionary.Add("Platform", VersionsManager.PlatformString);
			dictionary.Add("Version", VersionsManager.Version);
			dictionary.Add("APIVersion", ModsManager.APIVersionString);
			dictionary.Add("key", keySearch);
			dictionary.Add("SearchType", searchType);
			WebManager.Post(m_scResDirAddress, null, Header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] result)
			{
				try
				{
					string data = Encoding.UTF8.GetString(result,0,result.Length);
					XElement xElement = XmlUtils.LoadXmlFromString(data, throwOnError: true);
					string attributeValue = XmlUtils.GetAttributeValue<string>(xElement, "NextCursor");
					var list = new List<CommunityContentEntry>();
					foreach (XElement item in xElement.Elements())
					{
						try
						{
							list.Add(new CommunityContentEntry
							{
								Type = XmlUtils.GetAttributeValue(item, "Type", ExternalContentType.Unknown),
								Name = XmlUtils.GetAttributeValue<string>(item, "Name"),
								Address = XmlUtils.GetAttributeValue<string>(item, "Url"),
								UserId = XmlUtils.GetAttributeValue<string>(item, "UserId"),
								UserName = XmlUtils.GetAttributeValue<string>(item, "UName"),
								Boutique = XmlUtils.GetAttributeValue<int>(item, "Boutique"),
								IsShow = XmlUtils.GetAttributeValue<int>(item, "IsShow"),
								Size = XmlUtils.GetAttributeValue<long>(item, "Size"),
								ExtraText = XmlUtils.GetAttributeValue(item, "ExtraText", string.Empty),
								RatingsAverage = XmlUtils.GetAttributeValue(item, "RatingsAverage", 0f),
								IconSrc = XmlUtils.GetAttributeValue(item, "Icon", ""),
								CollectionID = XmlUtils.GetAttributeValue<int>(item, "CollectionID"),
								CollectionName = XmlUtils.GetAttributeValue<string>(item, "CollectionName"),
								CollectionDetails = XmlUtils.GetAttributeValue<string>(item, "CollectionDetails"),
								Index = XmlUtils.GetAttributeValue<int>(item, "Id")
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
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void Download(string address, string name, ExternalContentType type, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
			}
			else
			{
				WebManager.Get(address, null, null, progress, delegate (byte[] data)
				{
					string hash = CalculateContentHashString(data);
					ExternalContentManager.ImportExternalContent(new MemoryStream(data), type, name, delegate (string downloadedName)
					{
						m_idToAddressMap[MakeContentIdString(type, downloadedName)] = address;
						Feedback(address, "Success", null, hash, data.Length, userId, progress, delegate
						{
						}, delegate
						{
						});
						success();
					}, delegate (Exception error)
					{
						Feedback(address, "ImportFailure", null, hash, data.Length, userId, null, delegate
						{
						}, delegate
						{
						});
						failure(error);
					});
				}, delegate (Exception error)
				{
					Feedback(address, "DownloadFailure", null, null, 0L, userId, null, delegate
					{
					}, delegate
					{
					});
					failure(error);
				});
			}
		}

		public static void Publish(string address, string name, ExternalContentType type, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (MarketplaceManager.IsTrialMode)
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "2")));
			}
			else if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
			}
			else
			{
				VerifyLinkContent(address, name, type, progress, delegate (byte[] data)
				{
					string value = CalculateContentHashString(data);
					WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(new Dictionary<string, string>
					{
						{
							"Action",
							"publish"
						},
						{
							"UserId",
							userId
						},
						{
							"Name",
							name
						},
						{
							"Url",
							address
						},
						{
							"Type",
							type.ToString()
						},
						{
							"Hash",
							value
						},
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
					}, delegate (Exception error)
					{
						failure(error);
					});
				}, failure);
			}
		}

		public static void Delete(string address, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var dictionary = new Dictionary<string, string>
			{
				{ "Action", "delete" },
				{ "UserId", userId },
				{ "Url", address },
				{ "Platform", VersionsManager.PlatformString},
				{ "Version", VersionsManager.Version }
			};
			WebManager.Post(m_scResDirAddress, null, null, WebManager.UrlParametersToStream(dictionary), progress, delegate
			{
				success();

			}, delegate (Exception error)
			{
				failure(error);

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

		public static void VerifyLinkContent(string address, string name, ExternalContentType type, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			WebManager.Get(address, null, null, progress, delegate (byte[] data)
			{
				ExternalContentManager.ImportExternalContent(new MemoryStream(data), type, "__Temp", delegate (string downloadedName)
				{
					ExternalContentManager.DeleteExternalContent(type, downloadedName);
					success(data);
				}, failure);
			}, failure);
		}

		public static void Feedback(string address, string feedback, string feedbackParameter, string hash, long size, string userId, CancellableProgress progress, Action success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}

			var dictionary = new Dictionary<string, string>
			{
				{ "Action", "feedback" },
				{ "Feedback", feedback }
			};
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
				string key = MakeFeedbackCacheKey(address, feedback, userId);
				if (m_feedbackCache.ContainsKey(key))
				{
					Task.Run(delegate
					{
						Task.Delay(1500).Wait();
						failure(new InvalidOperationException(LanguageControl.Get(fName, "3")));
					});
					return;
				}
				m_feedbackCache[key] = true;
				success();
			}, delegate (Exception error)
			{
				failure(error);
			});
		}


		public static void UserList(string cursor, string searchKey, string searchType, string filter, int order, CancellableProgress progress, Action<List<ComUserInfo>, string> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var Header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Cursor", cursor ?? string.Empty },
				{ "Action", "GetUserList" },
				{ "Operater", SettingsManager.ScpboxAccessToken },
				{ "SearchKey", searchKey },
				{ "SearchType", searchType },
				{ "Filter", filter },
				{ "Order", order.ToString() }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/userList", null, Header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] result)
			{
				try
				{
					//if (result != null)
					//{
					//    using (FileStream fileStream = new FileStream(Storage.GetSystemPath(ModsManager.ModCachePath) + "/123����.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
					//    {
					//        fileStream.Write(result, 0, result.Length);
					//        fileStream.Flush();
					//    }
					//}
					//var json = (JsonObject)WebManager.JsonFromBytes(result);
					XElement xElement = XmlUtils.LoadXmlFromString(Encoding.UTF8.GetString(result, 0, result.Length), throwOnError: true);
					string attributeValue = XmlUtils.GetAttributeValue<string>(xElement, "NextCursor");
					var list = new List<ComUserInfo>();
					foreach (XElement item in xElement.Elements())
					{
						try
						{
							list.Add(new ComUserInfo
							{
								Id = XmlUtils.GetAttributeValue<int>(item, "Id"),
								UserNo = XmlUtils.GetAttributeValue<string>(item, "User"),
								Name = XmlUtils.GetAttributeValue<string>(item, "Nickname"),
								Token = XmlUtils.GetAttributeValue<string>(item, "Token"),
								LastLoginTime = XmlUtils.GetAttributeValue<string>(item, "LastLoginTime"),
								ErrCount = XmlUtils.GetAttributeValue<int>(item, "ErrorTimes", 0),
								IsLock = XmlUtils.GetAttributeValue<int>(item, "IsLock", 0),
								LockTime = XmlUtils.GetAttributeValue<string>(item, "LockTime"),
								UnlockTime = XmlUtils.GetAttributeValue<string>(item, "UnlockTime"),
								LockDuration = XmlUtils.GetAttributeValue<int>(item, "LockDuration", 0),
								Money = XmlUtils.GetAttributeValue<int>(item, "Money", 0),
								Authority = XmlUtils.GetAttributeValue<string>(item, "Authority"),
								HeadImg = XmlUtils.GetAttributeValue<string>(item, "HeadImg"),
								IsAdmin = XmlUtils.GetAttributeValue<int>(item, "IsAdmin", 0),
								RegTime = XmlUtils.GetAttributeValue<string>(item, "RegTime"),
								LoginIP = XmlUtils.GetAttributeValue<string>(item, "LoginIP"),
								MGroup = XmlUtils.GetAttributeValue<string>(item, "MGroup"),
								PawToken = XmlUtils.GetAttributeValue<string>(item, "PassToken"),
								Email = XmlUtils.GetAttributeValue<string>(item, "Email"),
								Status = XmlUtils.GetAttributeValue<int>(item, "Status", 1),
								LockReason = XmlUtils.GetAttributeValue<string>(item, "LockReason"),
								EmailCount = XmlUtils.GetAttributeValue<int>(item, "EmailCount", 0),
								EmailTime = XmlUtils.GetAttributeValue<string>(item, "EmailTime"),
								Die = XmlUtils.GetAttributeValue<int>(item, "Die", 0),
								Moblie = XmlUtils.GetAttributeValue<string>(item, "Moblie"),
								AreaCode = XmlUtils.GetAttributeValue<string>(item, "AreaCode")
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
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void UpdateLockState(int id, int lockState, string reason, int duration, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Action", "UpdateLockState" },
				{ "Id", id.ToString() },
				{ "Operater", SettingsManager.ScpboxAccessToken },
				{ "LockState", lockState.ToString() },
				{ "Duration", duration.ToString() },
				{ "Reason", reason }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/userList", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void ResetPassword(int id, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Action", "ResetPassword" },
				{ "Id", id.ToString() },
				{ "Operater", SettingsManager.ScpboxAccessToken }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/userList", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void UpdateBoutique(string type, int id, int boutique, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Type", type },
				{ "Id", id.ToString() },
				{ "Operater", SettingsManager.ScpboxAccessToken },
				{ "Boutique", boutique.ToString() }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/boutique", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void UpdateHidePara(int id, int isShow, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Id", id.ToString() },
				{ "Operater", SettingsManager.ScpboxAccessToken },
				{ "IsShow", isShow.ToString() }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/hide", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void DeleteFile(int id, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Id", id.ToString() },
				{ "Operater", SettingsManager.ScpboxAccessToken }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/deleteFile", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void IsAdmin(CancellableProgress progress, Action<bool> success, Action<Exception> failure)
		{
			progress ??= new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException(LanguageControl.Get(fName, "1")));
				return;
			}
			var header = new Dictionary<string, string>
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			var dictionary = new Dictionary<string, string>
			{
				{ "Operater", SettingsManager.ScpboxAccessToken }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/isadmin", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				int i = 0;
                foreach(JsonProperty property in JsonDocument.Parse(data).RootElement.EnumerateObject())
				{
					if (i == 2)
					{
                        success(property.Value.GetString() == "Y");
						break;
                    }
					i++;
				}
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static string CalculateContentHashString(byte[] data)
		{
			return Convert.ToBase64String(SHA384.HashData(data));
		}

		public static string MakeFeedbackCacheKey(string address, string feedback, string userId)
		{
			return address + "\n" + feedback + "\n" + userId;
		}

		public static string MakeContentIdString(ExternalContentType type, string name)
		{
			return type.ToString() + ":" + name;
		}

		public static void Load()
		{
			try
			{
				if (Storage.FileExists(ModsManager.CommunityContentCachePath))
				{
					using Stream stream = Storage.OpenFile(ModsManager.CommunityContentCachePath,OpenFileMode.Read);
					XElement xElement = XmlUtils.LoadXmlFromStream(stream,null,throwOnError: true);
					foreach(XElement item in xElement.Element("Feedback").Elements())
					{
						string attributeValue = XmlUtils.GetAttributeValue<string>(item,"Key");
						m_feedbackCache[attributeValue] = true;
					}
					foreach(XElement item2 in xElement.Element("Content").Elements())
					{
						string attributeValue2 = XmlUtils.GetAttributeValue<string>(item2,"Path");
						string attributeValue3 = XmlUtils.GetAttributeValue<string>(item2,"Address");
						m_idToAddressMap[attributeValue2] = attributeValue3;
					}
				}
			}
			catch (Exception e)
			{
				ExceptionManager.ReportExceptionToUser(LanguageControl.Get(fName, "4"), e);
			}
		}

		public static void Save()
		{
			try
			{
				var xElement = new XElement("Cache");
				var xElement2 = new XElement("Feedback");
				xElement.Add(xElement2);
				foreach (string key in m_feedbackCache.Keys)
				{
					var xElement3 = new XElement("Item");
					XmlUtils.SetAttributeValue(xElement3, "Key", key);
					xElement2.Add(xElement3);
				}
				var xElement4 = new XElement("Content");
				xElement.Add(xElement4);
				foreach (KeyValuePair<string, string> item in m_idToAddressMap)
				{
					var xElement5 = new XElement("Item");
					XmlUtils.SetAttributeValue(xElement5, "Path", item.Key);
					XmlUtils.SetAttributeValue(xElement5, "Address", item.Value);
					xElement4.Add(xElement5);
				}
				using Stream stream = Storage.OpenFile(ModsManager.CommunityContentCachePath,OpenFileMode.Create);
				XmlUtils.SaveXmlToStream(xElement,stream,null,throwOnError: true);
			}
			catch (Exception e)
			{
				ExceptionManager.ReportExceptionToUser(LanguageControl.Get(fName, "5"), e);
			}
		}
	}
}

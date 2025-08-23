using Engine;
using System.Text.Json;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class MotdManager
	{
		public class Message
		{
			public List<Line> Lines = [];
		}

		public class Line
		{
			public float Time;

			public XElement Node;

			public string Text;
		}

		public class Bulletin
		{
			public string Title = string.Empty;

			public string EnTitle = string.Empty;

			public string Time = string.Empty;

			public string Content = string.Empty;

			public string EnContent = string.Empty;
		}

		public class FilterMod
		{
			public string Name = string.Empty;

			public string PackageName = string.Empty;

			public string Version = string.Empty;

			public string FilterAPIVersion = string.Empty;

			public string Explanation = string.Empty;
		}

		public static Bulletin m_bulletin;

		public static bool CanShowBulletin;

		public static bool CanDownloadMotd = true;

		public static List<FilterMod> FilterModAll = [];

		public static Message m_message;

		public static JsonDocument UpdateResult;

		public static bool m_isAdmin;

		public static Message MessageOfTheDay
		{
			get => m_message;
			set
			{
				m_message = value;
				MessageOfTheDayUpdated?.Invoke();
			}
		}

		public static event Action MessageOfTheDayUpdated;

		public static void ForceRedownload()
		{
			SettingsManager.MotdLastUpdateTime = DateTime.MinValue;
		}

		public static void Initialize()
		{
			if (VersionsManager.Version != VersionsManager.LastLaunchedVersion)
			{
				ForceRedownload();
			}
		}

		public static void UpdateVersion()
		{
			string url = string.Format(SettingsManager.MotdUpdateCheckUrl, VersionsManager.SerializationVersion, VersionsManager.PlatformString, ModsManager.APIVersionString, LanguageControl.LName());
			WebManager.Get(url, null, null, new CancellableProgress(), data =>
			{
				UpdateResult = JsonDocument.Parse(data);
			}, ex =>
			{
				Log.Warning("Failed processing Update check. Reason: " + ex.Message);
			});
		}

		public static void DownloadMotd()
		{
			string url = GetMotdUrl();
			WebManager.Get(url, null, null, null, delegate (byte[] result)
			{
				try
				{
					string motdLastDownloadedData = UnpackMotd(result);
					MessageOfTheDay = null;
					SettingsManager.MotdLastDownloadedData = motdLastDownloadedData;
					Log.Information("Downloaded MOTD");
				}
				catch (Exception ex)
				{
					Log.Warning("Failed processing MOTD string. Reason: " + ex.Message);
				}
			}, delegate (Exception error)
			{
				Log.Warning("Failed downloading MOTD. Reason: " + error.Message);
			});

		}

		public static void Update()
		{
			//if (Time.PeriodicEvent(1.0, 0.0) && ModsManager.ConfigLoaded)
			//{
			//var t = TimeSpan.FromHours(SettingsManager.MotdUpdatePeriodHours);
			//DateTime now = DateTime.Now;
			//if (now >= SettingsManager.MotdLastUpdateTime + t)
			//{
			//    SettingsManager.MotdLastUpdateTime = now;
			//    DownloadMotd();
			//    UpdateVersion();
			//}
			//}
#if DEBUG
			CanDownloadMotd = false;
#endif
            if (CanDownloadMotd)
			{
				DownloadMotd();
				CommunityContentManager.IsAdmin(new CancellableProgress(), delegate (bool isAdmin)
				{
					m_isAdmin = isAdmin;
				}, delegate {
				});
				CanDownloadMotd = false;
			}
			if (MessageOfTheDay == null && !string.IsNullOrEmpty(SettingsManager.MotdLastDownloadedData))
			{
				MessageOfTheDay = ParseMotd(SettingsManager.MotdLastDownloadedData);
				if (MessageOfTheDay == null)
				{
					SettingsManager.MotdLastDownloadedData = string.Empty;
				}
				if (m_bulletin != null && SettingsManager.BulletinTime != m_bulletin.Time)
				{
					if (IsCNLanguageType() && m_bulletin.Title.ToLower() != "null")
					{
						CanShowBulletin = true;
					}
					else if (!IsCNLanguageType() && m_bulletin.EnTitle.ToLower() != "null")
					{
						CanShowBulletin = true;
					}
				}
			}
		}

		public static string UnpackMotd(byte[] data)
		{
			using (MemoryStream stream = new(data))
				return new StreamReader(stream).ReadToEnd();
			throw new InvalidOperationException("\"motd.xml\" file not found in Motd zip archive.");
		}

		public static Message ParseMotd(string dataString)
		{
			try
			{
				int num = dataString.IndexOf("<Motd");
				if (num < 0)
				{
					throw new InvalidOperationException("Invalid MOTD data string.");
				}
				int num2 = dataString.IndexOf("</Motd>");
				if (num2 >= 0 && num2 > num)
				{
					num2 += 7;
				}
				XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
				SettingsManager.MotdUpdatePeriodHours = XmlUtils.GetAttributeValue(xElement, "UpdatePeriodHours", 24);
				SettingsManager.MotdUpdateUrl = XmlUtils.GetAttributeValue(xElement, "UpdateUrl", SettingsManager.MotdUpdateUrl);
				Message message = new();
				foreach (XElement item2 in xElement.Elements())
				{
					if (Widget.IsNodeIncludedOnCurrentPlatform(item2))
					{
						Line item = new()
						{
							Time = XmlUtils.GetAttributeValue<float>(item2, "Time"),
							Node = item2.Elements().FirstOrDefault(),
							Text = item2.Value
						};
						message.Lines.Add(item);
					}
				}
				LoadBulletin(dataString);
				LoadFilterMods(dataString);
				return message;
			}
			catch (Exception ex)
			{
				Log.Warning("Failed extracting MOTD string. Reason: " + ex.Message);
			}
			return null;
		}

		public static void LoadBulletin(string dataString)
		{
			int num = dataString.IndexOf("<Motd2");
			if (num < 0)
			{
				throw new InvalidOperationException("Invalid MOTD2 data string.");
			}
			int num2 = dataString.IndexOf("</Motd2>");
			if (num2 >= 0 && num2 > num)
			{
				num2 += 8;
			}
			XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
			string languageType = (!ModsManager.Configs.TryGetValue("Language", out string config)) ? "zh-CN" : config;
			foreach (XElement item in xElement.Elements())
			{
				if (item.Name.LocalName == "Bulletin")
				{
					m_bulletin = new Bulletin { Title = item.Attribute("Title").Value,EnTitle = item.Attribute("EnTitle").Value,Time = languageType + "$" + item.Attribute("Time").Value,Content = item.Element("Content").Value,EnContent = item.Element("EnContent").Value };
					break;
				}
			}
		}

		public static void SaveBulletin(string dataString, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			progress = progress ?? new CancellableProgress();
			if (!WebManager.IsInternetConnectionAvailable())
			{
				failure(new InvalidOperationException("Internet connection is unavailable."));
				return;
			}
			Dictionary<string,string> header = new()
			{
				{ "Content-Type", "application/x-www-form-urlencoded" }
			};
			Dictionary<string,string> dictionary = new()
			{
				{ "Operater", SettingsManager.ScpboxAccessToken },
				{ "Content", dataString }
			};
			WebManager.Post("https://m.schub.top/com/api/zh/setnotice", null, header, WebManager.UrlParametersToStream(dictionary), progress, delegate (byte[] data)
			{
				success(data);
			}, delegate (Exception error)
			{
				failure(error);
			});
		}

		public static void LoadFilterMods(string dataString)
		{
			int num = dataString.IndexOf("<Motd3");
			if (num < 0)
			{
				throw new InvalidOperationException("Invalid MOTD3 data string.");
			}
			int num2 = dataString.IndexOf("</Motd3>");
			if (num2 >= 0 && num2 > num)
			{
				num2 += 8;
			}
			XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
			FilterModAll.Clear();
			foreach (XElement item in xElement.Elements())
			{
				if (item.Name.LocalName == "FilterMod")
				{
					FilterMod filterMod = new() { Name = item.Attribute("Name").Value,PackageName = item.Attribute("PackageName").Value,Version = item.Attribute("Version").Value,FilterAPIVersion = item.Attribute("FilterAPIVersion").Value,Explanation = item.Value };
					FilterModAll.Add(filterMod);
				}
			}
		}

		public static void ShowBulletin()
		{
			try
			{
				string time = m_bulletin.Time.Contains('$') ? m_bulletin.Time.Split('$', StringSplitOptions.RemoveEmptyEntries)[1] : string.Empty;
				if (!string.IsNullOrEmpty(time)) time = (IsCNLanguageType() ? "公告发布时间: " : "Time: ") + time;
				string title = IsCNLanguageType() ? m_bulletin.Title : m_bulletin.EnTitle;
				string content = IsCNLanguageType() ? m_bulletin.Content : m_bulletin.EnContent;
				BulletinDialog bulletinDialog = new(title, content, time, delegate
				{
					SettingsManager.BulletinTime = m_bulletin.Time;
				}, delegate (LabelWidget titleLabel, LabelWidget contentLabel)
				{
					DialogsManager.ShowDialog(null, new TextBoxDialog("请输入标题", titleLabel.Text, 1024, delegate (string inputTitle)
					{
						DialogsManager.ShowDialog(null, new TextBoxDialog("请输入内容", contentLabel.Text.Replace("\n", "[n]"), 8192, delegate (string inputContent)
						{
							if (!string.IsNullOrEmpty(inputTitle) && !string.IsNullOrEmpty(inputContent))
							{
								titleLabel.Text = inputTitle;
								contentLabel.Text = inputContent.Replace("[n]", "\n");
								if (IsCNLanguageType())
								{
									m_bulletin.Title = titleLabel.Text;
									m_bulletin.Content = contentLabel.Text;
								}
								else
								{
									m_bulletin.EnTitle = titleLabel.Text;
									m_bulletin.EnContent = contentLabel.Text;
								}
								string languageType = (!ModsManager.Configs.TryGetValue("Language",out string value)) ? "zh-CN" : value;
								m_bulletin.Time = languageType + "$" + DateTime.Now;
							}
						}, delegate (TextBoxWidget textBox)
						{
							textBox.Text = textBox.Text.Replace("\n", "[n]");
						}));
					}));
				}, delegate (LabelWidget titleLabel, LabelWidget contentLabel)
				{
					int num = SettingsManager.MotdLastDownloadedData.IndexOf("<Motd2");
					int num2 = SettingsManager.MotdLastDownloadedData.IndexOf("</Motd2>") + 8;
					XElement xElement = XmlUtils.LoadXmlFromString(SettingsManager.MotdLastDownloadedData.Substring(num, num2 - num), throwOnError: true);
					string languageType = (!ModsManager.Configs.TryGetValue("Language", out string config)) ? "zh-CN" : config;
					foreach (XElement item in xElement.Elements())
					{
						if (item.Name.LocalName == "Bulletin")
						{
							if (IsCNLanguageType())
							{
								item.Attribute("Title").Value = titleLabel.m_text;
								item.Element("Content").Value = contentLabel.m_text;
							}
							else
							{
								item.Attribute("EnTitle").Value = titleLabel.m_text;
								item.Element("EnContent").Value = contentLabel.m_text;
							}
							item.Attribute("Time").Value = DateTime.Now.ToString();
							break;
						}
					}
					string newDownloadedData = SettingsManager.MotdLastDownloadedData.Substring(0, num);
					newDownloadedData += xElement.ToString();
					newDownloadedData += SettingsManager.MotdLastDownloadedData.Substring(num2);
					CancellableBusyDialog busyDialog = new("操作等待中", autoHideOnCancel: false);
					DialogsManager.ShowDialog(null, busyDialog);
					SaveBulletin(newDownloadedData, busyDialog.Progress, delegate (byte[] data)
					{
						DialogsManager.HideDialog(busyDialog);
						JsonElement result = JsonDocument.Parse(data).RootElement;
						bool success = result[0].GetInt32() == 200;
                        string msg = success ? "公告已更新,建议重启游戏检查效果" : result[1].GetString();
						if (success)
						{
							SettingsManager.MotdLastDownloadedData = newDownloadedData;
						}
						DialogsManager.ShowDialog(null, new MessageDialog("操作成功", msg, LanguageControl.Ok, null, null));
					}, delegate (Exception e)
					{
						DialogsManager.HideDialog(busyDialog);
						Log.Warning("SaveBulletin:" + e.Message);
					});
				});
				CommunityContentManager.IsAdmin(new CancellableProgress(), delegate (bool isAdmin)
				{
					m_isAdmin = isAdmin;
				}, delegate {
				});
				bulletinDialog.m_editButton.IsVisible = m_isAdmin;
				bulletinDialog.m_updateButton.IsVisible = m_isAdmin;
				DialogsManager.ShowDialog(null, bulletinDialog);
				CanShowBulletin = false;
			}
			catch (Exception ex)
			{
				Log.Warning("Failed ShowBulletin. Reason: " + ex.Message);
			}
		}

		public static bool IsCNLanguageType()
		{
			string languageType = (!ModsManager.Configs.TryGetValue("Language",out string value)) ? "zh-CN" : value;
			return languageType == "zh-CN";
		}

		public static string GetMotdUrl()
		{
			string languageType = (!ModsManager.Configs.TryGetValue("Language",out string value)) ? "zh-CN" : value;
			return string.Format(SettingsManager.MotdUpdateUrl, VersionsManager.SerializationVersion, languageType);
		}
	}
}

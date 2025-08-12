using Engine;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Game
{
	public class SchubExternalContentProvider : IExternalContentProvider, IDisposable
	{
		public class LoginProcessData
		{
			public bool IsTokenFlow;

			public Action Success;

			public Action<Exception> Failure;

			public CancellableProgress Progress;

			public void Succeed(SchubExternalContentProvider provider)
			{
				provider.m_loginProcessData = null;
				Success?.Invoke();
			}

			public void Fail(SchubExternalContentProvider provider, Exception error)
			{
				provider.m_loginProcessData = null;
				Failure?.Invoke(error);
			}
		}

		public const string m_appKey = "1uGA5aADX43p";

		public const string m_appSecret = "9aux67wg5z";

		public const string m_redirectUri = "https://m.schub.top";

		public LoginProcessData m_loginProcessData;

		public string DisplayName => "SC中文社区";

		public string Description
		{
			get
			{
				if (!IsLoggedIn)
				{
					return "未登录";
				}
				return "登录";
			}
		}

		public bool SupportsListing => true;

		public bool SupportsLinks => true;

		public bool RequiresLogin => true;

		public bool IsLoggedIn => !string.IsNullOrEmpty(SettingsManager.ScpboxAccessToken);

		public SchubExternalContentProvider()
		{
			Program.HandleUri += HandleUri;
			Window.Activated += WindowActivated;
		}

		public void Dispose()
		{
			Program.HandleUri -= HandleUri;
			Window.Activated -= WindowActivated;
		}

		public void Login(CancellableProgress progress, Action success, Action<Exception> failure)
		{
			try
			{
				if (m_loginProcessData != null)
				{
					throw new InvalidOperationException("登录已经在进程中");
				}
				if (!WebManager.IsInternetConnectionAvailable())
				{
					throw new InvalidOperationException("网络连接错误");
				}
				Logout();
				progress.Cancelled += delegate
				{
					if (m_loginProcessData != null)
					{
						LoginProcessData loginProcessData = m_loginProcessData;
						m_loginProcessData = null;
						loginProcessData.Fail(this, null);
					}
				};
				m_loginProcessData = new LoginProcessData();
				m_loginProcessData.Progress = progress;
				m_loginProcessData.Success = success;
				m_loginProcessData.Failure = failure;
				LoginLaunchBrowser();
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Logout()
		{
			m_loginProcessData = null;
			SettingsManager.ScpboxAccessToken = string.Empty;
			SettingsManager.ScpboxUserInfo = string.Empty;
		}

		public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				var dictionary = new Dictionary<string, string>
				{
					{ "Authorization", "Bearer " + SettingsManager.ScpboxAccessToken },
					{ "Content-Type", "application/json" }
				};
				var jsonObject = new JsonObject
				{
					{ "path", NormalizePath(path) },
					{ "recursive", false },
					{ "include_media_info", false },
					{ "include_deleted", false },
					{ "include_has_explicit_shared_members", false }
				};
				var data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToJsonString()));
				WebManager.Post(m_redirectUri + "/com/files/list_folder", null, dictionary, data, progress, delegate (byte[] result)
				{
					try
					{
						success(JsonElementToEntry(JsonDocument.Parse(result).RootElement));
					}
					catch (Exception obj2)
					{
						failure(obj2);
					}
				}, delegate (Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				var jsonObject = new JsonObject
				{
					{ "path", NormalizePath(path) }
				};
				var dictionary = new Dictionary<string, string>
				{
					{ "Authorization", "Bearer " + SettingsManager.ScpboxAccessToken },
					{ "Dropbox-API-Arg", jsonObject.ToJsonString() }
				};
				WebManager.Get(m_redirectUri + "/com/files/download", null, dictionary, progress, delegate (byte[] result)
				{
					success(new MemoryStream(result));
				}, delegate (Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				var jsonObject = new JsonObject
				{
					{ "path", NormalizePath(path) },
					{ "mode", "add" },
					{ "autorename", true },
					{ "mute", false }
				};
				var dictionary = new Dictionary<string, string>
				{
					{ "Authorization", "Bearer " + SettingsManager.ScpboxAccessToken },
					{ "Content-Type", "application/octet-stream" },
					{ "Dropbox-API-Arg", jsonObject.ToJsonString() }
				};
				WebManager.Post(m_redirectUri + "/com/files/upload", null, dictionary, stream, progress, delegate
				{
					success(null);
				}, delegate (Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				var dictionary = new Dictionary<string, string>
				{
					{ "Authorization", "Bearer " + SettingsManager.ScpboxAccessToken },
					{ "Content-Type", "application/json" }
				};
				var jsonObject = new JsonObject
				{
					{ "path", NormalizePath(path) },
					{ "short_url", false }
				};
				var data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToJsonString()));
				WebManager.Post(m_redirectUri + "/com/sharing/create_shared_link", null, dictionary, data, progress, delegate (byte[] result)
				{
					try
					{
                        success(JsonElementToLinkAddress(JsonDocument.Parse(result).RootElement));
                    }
					catch (Exception obj2)
					{
						failure(obj2);
					}
				}, delegate (Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void LoginLaunchBrowser()
		{
			try
			{
				var login = new LoginDialog();
				login.succ = delegate (byte[] a)
				{
					var streamReader = new StreamReader(new MemoryStream(a));
					JsonElement json = JsonDocument.Parse(streamReader.ReadToEnd()).RootElement;
					if (json.GetProperty("code").GetInt32() == 200)
					{
						JsonElement data = json.GetProperty("data");
						SettingsManager.ScpboxAccessToken = data.GetProperty("accessToken").GetString();
						SettingsManager.ScpboxUserInfo = string.Empty;
						string nickName = data.GetProperty("nickName").GetString();
						SettingsManager.ScpboxUserInfo += "昵称：" + nickName;
						SettingsManager.ScpboxUserInfo += "\n账号：" + data.GetProperty("user").GetString();
						SettingsManager.ScpboxUserInfo += "\n登录时间：" + TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(data.GetProperty("loginTime").GetInt64()), TimeZoneInfo.Local).ToString();
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Ok, "登录成功:" + nickName, LanguageControl.Ok, null, delegate
						{
							m_loginProcessData = null;
							DialogsManager.HideAllDialogs();
						}));
					}
					else
					{
						login.tip.Text = json.GetProperty("msg").GetString();
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Ok, "登录失败:" + login.tip.Text, LanguageControl.Ok, null, delegate
						{
							m_loginProcessData = null;
							DialogsManager.HideAllDialogs();
						}));
					}
				};
				login.fail = delegate (Exception e)
				{
					login.tip.Text = e.ToString();
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, "登录失败:" + e.Message, LanguageControl.Ok, null, delegate
					{
						m_loginProcessData = null;
						DialogsManager.HideAllDialogs();
					}));
				};
				login.cancel = delegate
				{
					m_loginProcessData = null;
					DialogsManager.HideAllDialogs();
				};
				DialogsManager.ShowDialog(null, login);
			}
			catch (Exception error)
			{
				m_loginProcessData.Fail(this, error);
			}
		}

		public void WindowActivated()
		{
			//不需要token验证
			/*
            if (m_loginProcessData != null && !m_loginProcessData.IsTokenFlow)
            {
                LoginProcessData loginProcessData = m_loginProcessData;
                m_loginProcessData = null;
                var dialog = new TextBoxDialog("输入用户登录Token:", "", 256, delegate (string s)
                {
                    if (s != null)
                    {
                        try
                        {
                            WebManager.Post(m_redirectUri + "/com/oauth2/token", new Dictionary<string, string>
                            {
                                {
                                    "code",
                                    s.Trim()
                                },
                                {
                                    "client_id",
                                    "1unnzwkb8igx70k"
                                },
                                {
                                    "client_secret",
                                    "3i5u3j3141php7u"
                                },
                                {
                                    "grant_type",
                                    "authorization_code"
                                }
                            }, null, new MemoryStream(), loginProcessData.Progress, delegate (byte[] result)
                            {
                                SettingsManager.ScpboxAccessToken = ((IDictionary<string, object>)WebManager.JsonFromBytes(result))["access_token"].ToString();
                                loginProcessData.Succeed(this);
                            }, delegate (Exception error)
                            {
                                loginProcessData.Fail(this, error);
                            });
                        }
                        catch (Exception error2)
                        {
                            loginProcessData.Fail(this, error2);
                        }
                    }
                    else
                    {
                        loginProcessData.Fail(this, null);
                    }
                });
                DialogsManager.ShowDialog(null, dialog);
            }
            */
		}

		public void HandleUri(Uri uri)
		{
			if (m_loginProcessData == null)
			{
				m_loginProcessData = new LoginProcessData();
				m_loginProcessData.IsTokenFlow = true;
			}
			LoginProcessData loginProcessData = m_loginProcessData;
			m_loginProcessData = null;
			if (loginProcessData.IsTokenFlow)
			{
				try
				{
					if (!(uri != null) || string.IsNullOrEmpty(uri.Fragment))
					{
						throw new Exception("不能接收来自Schub的身份验证信息");
					}
					Dictionary<string, string> dictionary = WebManager.UrlParametersFromString(uri.Fragment.TrimStart('#'));
					if (!dictionary.ContainsKey("access_token"))
					{
						if (dictionary.ContainsKey("error"))
						{
							throw new Exception(dictionary["error"]);
						}
						throw new Exception("不能接收来自Schub的身份验证信息");
					}
					SettingsManager.ScpboxAccessToken = dictionary["access_token"];
					loginProcessData.Succeed(this);
				}
				catch (Exception error)
				{
					loginProcessData.Fail(this, error);
				}
			}
		}

		public void VerifyLoggedIn()
		{
			if (!IsLoggedIn)
			{
				throw new InvalidOperationException("这个应用未登录到Schub中国社区");
			}
		}

        public static ExternalContentEntry JsonElementToEntry(JsonElement jsonElement)
        {
            ExternalContentEntry externalContentEntry = new();
            if (jsonElement.TryGetProperty("entries", out JsonElement entries))
            {
                foreach (JsonProperty item in entries.EnumerateObject())
                {
                    ExternalContentEntry externalContentEntry2 = new();
                    externalContentEntry2.Path = item.Value.GetProperty("path_display").GetString();
                    externalContentEntry2.Type = (item.Value.GetProperty(".tag").GetString() == "folder") ? ExternalContentType.Directory : ExternalContentManager.ExtensionToType(Storage.GetExtension(externalContentEntry2.Path));
                    if (externalContentEntry2.Type != ExternalContentType.Directory)
                    {
                        externalContentEntry2.Time = item.Value.TryGetProperty("server_modified", out JsonElement server_modified) ? DateTime.Parse(server_modified.GetString(), CultureInfo.InvariantCulture) : new DateTime(2000, 1, 1);
                        externalContentEntry2.Size = item.Value.TryGetProperty("size", out JsonElement size) ? size.GetInt64() : 0;
                    }
                    externalContentEntry.ChildEntries.Add(externalContentEntry2);
                }
            }
            return externalContentEntry;
        }
        //获取分享连接
        public static string JsonElementToLinkAddress(JsonElement jsonElement)
        {
            if (jsonElement.TryGetProperty("url", out JsonElement url))
            {
                return url.GetString().Replace("www.dropbox.", "dl.dropbox.").Replace("?dl=0", "") + "?dl=1";
            }
            throw new InvalidOperationException("Share information not found.");
        }

        public static string NormalizePath(string path)
		{
			if (path == "/")
			{
				return string.Empty;
			}
			if (path.Length > 0 && path[0] != '/')
			{
				return "/" + path;
			}
			return path;
		}
	}
}

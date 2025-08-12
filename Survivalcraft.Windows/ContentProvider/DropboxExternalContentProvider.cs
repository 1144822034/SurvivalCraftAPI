using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Game
{
	public class DropboxExternalContentProvider : IExternalContentProvider, IDisposable
	{
		public class LoginProcessData
		{
			public bool IsTokenFlow;

			public Action Success;

			public Action<Exception> Failure;

			public CancellableProgress Progress;

			public void Succeed(DropboxExternalContentProvider provider)
			{
				provider.m_loginProcessData = null;
				Success?.Invoke();
			}

			public void Fail(DropboxExternalContentProvider provider, Exception error)
			{
				provider.m_loginProcessData = null;
				Failure?.Invoke(error);
			}
		}

		public const string m_appKey = "1unnzwkb8igx70k";

		public const string m_appSecret = "3i5u3j3141php7u";

		public const string m_redirectUri = "com.candyrufusgames.survivalcraft2://redirect";

		public LoginProcessData m_loginProcessData;

		public string DisplayName => "Dropbox";

		public string Description
		{
			get
			{
				if (!IsLoggedIn)
				{
					return "Not logged in";
				}
				return "Logged in";
			}
		}

		public bool SupportsListing => true;

		public bool SupportsLinks => true;

		public bool RequiresLogin => true;

		public bool IsLoggedIn => !string.IsNullOrEmpty(SettingsManager.DropboxAccessToken);

		public DropboxExternalContentProvider()
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
					throw new InvalidOperationException("Login already in progress.");
				}
				if (!WebManager.IsInternetConnectionAvailable())
				{
					throw new InvalidOperationException("Internet connection is unavailable.");
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
			SettingsManager.DropboxAccessToken = string.Empty;
		}

		public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				var dictionary = new Dictionary<string, string>
				{
					{ "Authorization", "Bearer " + SettingsManager.DropboxAccessToken },
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
				WebManager.Post("https://api.dropboxapi.com/2/files/list_folder", null, dictionary, data, progress, delegate (byte[] result)
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
					{ "Authorization", "Bearer " + SettingsManager.DropboxAccessToken },
					{ "Dropbox-API-Arg", jsonObject.ToJsonString() }
				};
				WebManager.Get("https://content.dropboxapi.com/2/files/download", null, dictionary, progress, delegate (byte[] result)
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
					{ "Authorization", "Bearer " + SettingsManager.DropboxAccessToken },
					{ "Content-Type", "application/octet-stream" },
					{ "Dropbox-API-Arg", jsonObject.ToJsonString() }
				};
				WebManager.Post("https://content.dropboxapi.com/2/files/upload", null, dictionary, stream, progress, delegate
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
					{ "Authorization", "Bearer " + SettingsManager.DropboxAccessToken },
					{ "Content-Type", "application/json" }
				};
				var jsonObject = new JsonObject
				{
					{ "path", NormalizePath(path) },
					{ "short_url", false }
				};
				var data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToJsonString()));
				WebManager.Post("https://api.dropboxapi.com/2/sharing/create_shared_link", null, dictionary, data, progress, delegate (byte[] result)
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
				m_loginProcessData.IsTokenFlow = true;
				var dictionary = new Dictionary<string, string>
				{
					{ "response_type", "token" },
					{ "client_id", "1unnzwkb8igx70k" },
					{ "redirect_uri", "com.candyrufusgames.survivalcraft2://redirect" }
				};
				WebBrowserManager.LaunchBrowser("https://www.dropbox.com/oauth2/authorize?" + WebManager.UrlParametersToString(dictionary));
			}
			catch (Exception error)
			{
				m_loginProcessData.Fail(this, error);
			}
		}

		public void WindowActivated()
		{
			if (m_loginProcessData != null && !m_loginProcessData.IsTokenFlow)
			{
				LoginProcessData loginProcessData = m_loginProcessData;
				m_loginProcessData = null;
				var dialog = new TextBoxDialog("Enter Dropbox authorization code", "", 256, delegate (string s)
				{
					if (s != null)
					{
						try
						{
							WebManager.Post("https://api.dropboxapi.com/oauth2/token", new Dictionary<string, string>
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
                                SettingsManager.DropboxAccessToken = JsonDocument.Parse(result).RootElement.GetProperty("access_token").GetString();
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
						goto 标签;
					}
					Dictionary<string, string> dictionary = WebManager.UrlParametersFromString(uri.Fragment.TrimStart('#'));
					if (!dictionary.ContainsKey("access_token"))
					{
						if (dictionary.ContainsKey("error"))
						{
							throw new Exception(dictionary["error"]);
						}
						goto 标签;
					}
					SettingsManager.DropboxAccessToken = dictionary["access_token"];
					loginProcessData.Succeed(this);
					goto end_IL_0038;
				标签:
					throw new Exception("Could not retrieve Dropbox access token.");
				end_IL_0038:;
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
				throw new InvalidOperationException("Not logged in to Dropbox in this app.");
			}
		}

		public static ExternalContentEntry JsonElementToEntry(JsonElement jsonElement)
		{
            ExternalContentEntry externalContentEntry = new ();
			if (jsonElement.TryGetProperty("entries", out JsonElement entries))
			{
				foreach (JsonProperty item in entries.EnumerateObject())
				{
                    ExternalContentEntry externalContentEntry2 = new ();
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

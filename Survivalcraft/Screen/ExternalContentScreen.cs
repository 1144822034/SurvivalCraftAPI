using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class ExternalContentScreen : Screen
	{
		public LabelWidget m_directoryLabel;

		public ListPanelWidget m_directoryList;

		public LabelWidget m_providerNameLabel;

		public ButtonWidget m_changeProviderButton;

		public ButtonWidget m_loginLogoutButton;

		public ButtonWidget m_upDirectoryButton;

		public ButtonWidget m_actionButton;

		public ButtonWidget m_copyLinkButton;

		public string m_path;

		public bool m_listDirty;

		public Dictionary<string, bool> m_downloadedFiles = [];

		public IExternalContentProvider m_externalContentProvider = ExternalContentManager.DefaultProvider;

		public ExternalContentScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/ExternalContentScreen");
			LoadContents(this, node);
			m_directoryLabel = Children.Find<LabelWidget>("TopBar.Label");
			m_directoryList = Children.Find<ListPanelWidget>("DirectoryList");
			m_providerNameLabel = Children.Find<LabelWidget>("ProviderName");
			m_changeProviderButton = Children.Find<ButtonWidget>("ChangeProvider");
			m_loginLogoutButton = Children.Find<ButtonWidget>("LoginLogout");
			m_upDirectoryButton = Children.Find<ButtonWidget>("UpDirectory");
			m_actionButton = Children.Find<ButtonWidget>("Action");
			m_copyLinkButton = Children.Find<ButtonWidget>("CopyLink");
			m_directoryList.ItemWidgetFactory = delegate (object item)
			{
				var externalContentEntry2 = (ExternalContentEntry)item;
				XElement node2 = ContentManager.Get<XElement>("Widgets/ExternalContentItem");
				var containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
				string fileName = Storage.GetFileName(externalContentEntry2.Path);
				string text = m_downloadedFiles.ContainsKey(externalContentEntry2.Path) ? LanguageControl.Get(GetType().Name, 11) : string.Empty;
				string text2 = (externalContentEntry2.Type != ExternalContentType.Directory) ? $"{ExternalContentManager.GetEntryTypeDescription(externalContentEntry2.Type)} | {DataSizeFormatter.Format(externalContentEntry2.Size)} | {externalContentEntry2.Time:dd-MMM-yyyy HH:mm}{text}" : ExternalContentManager.GetEntryTypeDescription(externalContentEntry2.Type);
				containerWidget.Children.Find<RectangleWidget>("ExternalContentItem.Icon").Subtexture = ExternalContentManager.GetEntryTypeIcon(externalContentEntry2.Type);
				containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Text").Text = fileName;
				containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Details").Text = text2;
				return containerWidget;
			};
			m_directoryList.ItemClicked += delegate (object item)
			{
				if (m_directoryList.SelectedItem == item)
				{
					var externalContentEntry = item as ExternalContentEntry;
					if (externalContentEntry != null && externalContentEntry.Type == ExternalContentType.Directory)
					{
						SetPath(externalContentEntry.Path);
					}
				}
			};
		}

		public override void Enter(object[] parameters)
		{
			m_directoryList.ClearItems();
			SetPath(null);
			m_listDirty = true;
		}

		public override void Update()
		{
			if (m_listDirty)
			{
				m_listDirty = false;
				UpdateList();
			}
			ExternalContentEntry externalContentEntry = null;
			if (m_directoryList.SelectedIndex.HasValue)
			{
				externalContentEntry = m_directoryList.Items[m_directoryList.SelectedIndex.Value] as ExternalContentEntry;
			}
			if (externalContentEntry != null)
			{
				m_actionButton.IsVisible = true;
				if (externalContentEntry.Type == ExternalContentType.Directory)
				{
					m_actionButton.Text = LanguageControl.Get(GetType().Name, 1);
					m_actionButton.IsEnabled = true;
					m_copyLinkButton.IsEnabled = false;
				}
				else
				{
					m_actionButton.Text = LanguageControl.Get(GetType().Name, 2);
					if (ExternalContentManager.IsEntryTypeDownloadSupported(ExternalContentManager.ExtensionToType(Storage.GetExtension(externalContentEntry.Path).ToLower())))
					{
						m_actionButton.IsEnabled = true;
						m_copyLinkButton.IsEnabled = true;
					}
					else
					{
						m_actionButton.IsEnabled = false;
						m_copyLinkButton.IsEnabled = false;
					}
				}
			}
			else
			{
				m_actionButton.IsVisible = false;
				m_copyLinkButton.IsVisible = false;
			}
			m_directoryLabel.Text = m_externalContentProvider.IsLoggedIn ? string.Format(LanguageControl.Get(GetType().Name, 3), m_path) : LanguageControl.Get(GetType().Name, 4);
			m_providerNameLabel.Text = m_externalContentProvider.DisplayName;
			m_upDirectoryButton.IsEnabled = m_externalContentProvider.IsLoggedIn && m_path != "/";
			m_loginLogoutButton.Text = m_externalContentProvider.IsLoggedIn ? LanguageControl.Get(GetType().Name, 5) : LanguageControl.Get(GetType().Name, 6);
			m_loginLogoutButton.IsVisible = m_externalContentProvider.RequiresLogin;
			m_copyLinkButton.IsVisible = m_externalContentProvider.SupportsLinks;
			m_copyLinkButton.IsEnabled = externalContentEntry != null && ExternalContentManager.IsEntryTypeDownloadSupported(externalContentEntry.Type);
			if (m_changeProviderButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new SelectExternalContentProviderDialog(LanguageControl.Get(GetType().Name, 7), listingSupportRequired: true, delegate (IExternalContentProvider provider)
				{
					m_externalContentProvider = provider;
					m_listDirty = true;
					SetPath(null);
				}));
			}
			if (m_upDirectoryButton.IsClicked)
			{
				string directoryName = Storage.GetDirectoryName(m_path);
				SetPath(directoryName);
			}
			if (m_actionButton.IsClicked && externalContentEntry != null)
			{
				if (externalContentEntry.Type == ExternalContentType.Directory)
				{
					SetPath(externalContentEntry.Path);
				}
				else
				{
					DownloadEntry(externalContentEntry);
				}
			}
			if (m_copyLinkButton.IsClicked && externalContentEntry != null && ExternalContentManager.IsEntryTypeDownloadSupported(externalContentEntry.Type))
			{
				var busyDialog = new CancellableBusyDialog(LanguageControl.Get(GetType().Name, 8), autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				m_externalContentProvider.Link(externalContentEntry.Path, busyDialog.Progress, delegate (string link)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new ExternalContentLinkDialog(link));
				}, delegate (Exception error)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
				});
			}
			if (m_loginLogoutButton.IsClicked)
			{
				if (m_externalContentProvider.IsLoggedIn)
				{
					m_externalContentProvider.Logout();
					SetPath(null);
					m_listDirty = true;
				}
				else
				{
					ExternalContentManager.ShowLoginUiIfNeeded(m_externalContentProvider, showWarningDialog: false, delegate
					{
						SetPath(null);
						m_listDirty = true;
					});
				}
			}
            if (!String.IsNullOrEmpty(ExternalContentManager.openFilePath))
			{
				try
				{
					ExternalContentEntry externalContentEntry1 = new ExternalContentEntry();
					externalContentEntry1.Type = ExternalContentManager.ExtensionToType(System.IO.Path.GetExtension(ExternalContentManager.openFilePath));
					externalContentEntry1.Path = ExternalContentManager.openFilePath;
					externalContentEntry1.Size = new FileInfo(ExternalContentManager.openFilePath).Length;
					externalContentEntry1.Time = new FileInfo(ExternalContentManager.openFilePath).CreationTime;
					if (ExternalContentManager.IsEntryTypeDownloadSupported(externalContentEntry1.Type))
					{
						DownloadEntry(externalContentEntry1);
					}
					else
					{
                        DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(GetType().Name, 13), LanguageControl.Get(GetType().Name, 14) + ExternalContentManager.openFilePath, LanguageControl.Yes, null, delegate (MessageDialogButton button)
                        {

                        }));
                        Engine.Log.Error("Unsopported file type!");
					}
				}
				catch (Exception e)
				{
                    DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(GetType().Name, 13), e.ToString(), LanguageControl.Yes, null, delegate (MessageDialogButton button)
                    {

                    }));
                    Engine.Log.Error("Open File" + ExternalContentManager.openFilePath + "Failed! " + e);
				}
				ExternalContentManager.openFilePath = string.Empty;
            }
            if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen("Content");
			}
		}

		public void SetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
#if ANDROID
				path = Storage.GetSystemPath(RunPath.AndroidFilePath+"/files");
#else
				path = DiskExternalContentProvider.LocalPath;
#endif
			}
			path = path.Replace("\\", "/");
			if (path != m_path)
			{
				m_path = path;
				m_listDirty = true;
			}
		}

		public void UpdateList()
		{
			m_directoryList.ClearItems();
			if (m_externalContentProvider != null && m_externalContentProvider.IsLoggedIn)
			{
				var busyDialog = new CancellableBusyDialog(LanguageControl.Get(GetType().Name, 9), autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				m_externalContentProvider.List(m_path, busyDialog.Progress, delegate (ExternalContentEntry entry)
				{
					DialogsManager.HideDialog(busyDialog);
					var list = new List<ExternalContentEntry>(entry.ChildEntries.Where((ExternalContentEntry e) => EntryFilter(e)).Take(1000));
					m_directoryList.ClearItems();
					list.Sort(delegate (ExternalContentEntry e1, ExternalContentEntry e2)
					{
						return e1.Type == ExternalContentType.Directory && e2.Type != ExternalContentType.Directory
							? -1
							: (e1.Type != ExternalContentType.Directory && e2.Type == ExternalContentType.Directory) ? 1 : string.Compare(e1.Path, e2.Path);
					});
					foreach (ExternalContentEntry item in list)
					{
						m_directoryList.AddItem(item);
					}
				}, delegate (Exception error)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
				});
			}
		}

		public void DownloadEntry(ExternalContentEntry entry)
		{
			var busyDialog = new CancellableBusyDialog(LanguageControl.Get(GetType().Name, 10), autoHideOnCancel: false);
			DialogsManager.ShowDialog(null, busyDialog);
			m_externalContentProvider.Download(entry.Path, busyDialog.Progress, delegate (Stream stream)
			{
				busyDialog.LargeMessage = LanguageControl.Get(GetType().Name, 12);
				ExternalContentManager.ImportExternalContent(stream, entry.Type, Storage.GetFileName(entry.Path), delegate
				{
					stream.Dispose();
					DialogsManager.HideDialog(busyDialog);
				}, delegate (Exception error)
				{
					stream.Dispose();
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
				});
			}, delegate (Exception error)
			{
				DialogsManager.HideDialog(busyDialog);
				DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null));
			});
		}

		public static bool EntryFilter(ExternalContentEntry entry)
		{
			return entry.Type != ExternalContentType.Unknown;
		}
	}
}
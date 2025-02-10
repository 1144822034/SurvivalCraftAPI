using Engine;
using Engine.Graphics;
using Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ModsManageContentScreen;

public class ModsManageContentScreen : Screen
{
	public static string fName = "ModsManageContentScreen";

	public static string HeadingCode = "有头有脸天才少年,耍猴表演敢为人先";

	public static string HeadingCode2 = "修改他人mod请获得原作者授权，否则小心出名！";

	public enum StateFilter { UninstallState, InstallState };

	public class ModItem
	{
		public string Name;
		public ModInfo ModInfo;
		public ExternalContentEntry ExternalContentEntry;
		public Subtexture Subtexture;
		public ModItem() { }
		public ModItem(string Name, ModInfo ModInfo, ExternalContentEntry ExternalContentEntry, Subtexture Subtexture)
		{
			this.Name = Name;
			this.ModInfo = ModInfo;
			this.ExternalContentEntry = ExternalContentEntry;
			this.Subtexture = Subtexture;
		}
	}

	public ListPanelWidget m_modsContentList;

	public LabelWidget m_topBarLabel;

	public LabelWidget m_modsContentLabel;

	public ButtonWidget m_actionButton;

	public ButtonWidget m_actionButton2;

	public ButtonWidget m_actionButton3;

	public ButtonWidget m_uninstallFilterButton;

	public ButtonWidget m_installFilterButton;

	public ButtonWidget m_upDirectoryButton;

	public StateFilter m_filter;

	public List<ModItem> m_installModList = [];

	public List<ModItem> m_uninstallModList = [];

	public List<ModInfo> m_installModInfo = [];

	public List<ModInfo> m_lastInstallModInfo = [];

	public List<string> m_latestScanModList = [];

	public int m_count;

	public bool m_androidSystem;

	public bool m_androidDataPathEnterEnabled;

	public string m_androidDataPath = "android:/Android/data";

	public string m_path;

	public string m_lastPath;

	public string m_uninstallPath = ModsManager.ModDisPath;

	public string m_installPath = ModsManager.ModsPath;

	public bool m_updatable;

	public bool m_firstEnterInstallScreen;

	public bool m_firstEnterScreen;

	public bool m_cancelScan;

	public List<string> m_scanFailPaths = [];

	public CancellableBusyDialog m_cancellableBusyDialog;

	public List<string> m_commonPathList = [];

	public bool m_isAdmin;

	public string[] m_commonPaths = [];//Abandoned

	public static bool IsOldApiVersionMod(ModItem modItem, out string details)
	{
		ModInfo modInfo = modItem.ModInfo;
		if(modItem.ModInfo == null)
		{
			details = string.Format(LanguageControl.Get(fName,68),"1.3");
			return true;
		}
		if(modInfo.ApiVersion.StartsWith("1.4") || modInfo.ApiVersion.StartsWith("1.5") || modInfo.ApiVersion.StartsWith("1.6") || modInfo.ApiVersion.StartsWith("1.7"))
		{
			details = string.Format(LanguageControl.Get(fName,68),modItem.ModInfo.ApiVersion);
			return true;
		}
		else
		{
			details = string.Format(LanguageControl.Get(fName,3),modItem.ModInfo.Version,modItem.ModInfo.Author,MathF.Round(modItem.ExternalContentEntry.Size / 1000));
			return false;
		}
	}
	public ModsManageContentScreen()
	{
		m_androidSystem = Environment.CurrentDirectory == "/";
		if (m_androidSystem)
		{
			m_uninstallPath = m_uninstallPath.Replace("app:",RunPath.AndroidFilePath);
			m_installPath = m_installPath.Replace("app:",RunPath.AndroidFilePath);
			m_androidDataPathEnterEnabled = true;
			try
			{
				Storage.ListFileNames(m_androidDataPath);
			}
			catch
			{
				m_androidDataPathEnterEnabled = false;
			}
		}
		m_updatable = true;
		XElement node = ContentManager.Get<XElement>("Screens/ModsManageContentScreen");
		LoadContents(this, node);
		m_modsContentList = Children.Find<ListPanelWidget>("ModsContentList");
		m_topBarLabel = Children.Find<LabelWidget>("TopBar.Label");
		m_modsContentLabel = Children.Find<LabelWidget>("ModsContentLabel");
		m_actionButton = Children.Find<BevelledButtonWidget>("ActionButton");
		m_actionButton2 = Children.Find<BevelledButtonWidget>("ActionButton2");
		m_actionButton3 = Children.Find<BevelledButtonWidget>("ActionButton3");
		m_uninstallFilterButton = Children.Find<BevelledButtonWidget>("UninstallFilter");
		m_installFilterButton = Children.Find<BevelledButtonWidget>("InstallFilter");
		m_upDirectoryButton = Children.Find<BevelledButtonWidget>("UpDirectory");
		m_topBarLabel.Text = LanguageControl.Get(fName, 1);
		m_uninstallFilterButton.Text = LanguageControl.Get(fName, 44);
		m_installFilterButton.Text = LanguageControl.Get(fName, 45);
		m_firstEnterScreen = false;
		m_actionButton3.Text = LanguageControl.Get(fName, 73);
		m_modsContentList.ItemWidgetFactory = delegate (object item)
		{
			ModItem modItem = (ModItem)item;
			XElement node2 = ContentManager.Get<XElement>("Widgets/ExternalContentItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
			string details = LanguageControl.Get(fName, 2);
			Color color = Color.White;
			if (m_latestScanModList.Contains(modItem.Name))
			{
				color = Color.Green;
			}
			if (modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
			{
				if(IsOldApiVersionMod(modItem, out details))
				{
					color = Color.Red;
				}
			}
			containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Text").Text = modItem.Name;
			containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Text").Color = color;
			containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Details").Text = details;
			RectangleWidget iconWidget = containerWidget.Children.Find<RectangleWidget>("ExternalContentItem.Icon");
			iconWidget.Subtexture = modItem.Subtexture;
			iconWidget.Size = new Vector2(40, 40);
			iconWidget.Margin = new Vector2(8, 8);
			return containerWidget;
		};
		m_modsContentList.ItemClicked += delegate (object item)
		{
			if (item != null && m_modsContentList.SelectedItem == item)
			{
				ModItem modItem = (ModItem)item;
				if (modItem.ExternalContentEntry.Type == ExternalContentType.Directory && modItem.ExternalContentEntry.Path != m_installPath)
				{
					try
					{
						if (modItem.ExternalContentEntry.Path != m_androidDataPath)
						{
							SetPath(modItem.ExternalContentEntry.Path);
							UpdateListWithBusyDialog();
						}
						else
						{
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 71), LanguageControl.Get(fName, 72) + m_androidDataPath, LanguageControl.Ok, null, null));
						}
					}
					catch
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 5) + "\n" + modItem.ExternalContentEntry.Path, LanguageControl.Ok, null, null));
					}
				}
				else if (modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
				{
					if (m_filter == StateFilter.UninstallState)
					{
						string title;
						string modDescription;
						if (modItem.ModInfo != null)
						{
							title = modItem.ModInfo.Name;
							modDescription = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName + "，" + LanguageControl.Get(fName, 8);
						}
						else
						{
							title = LanguageControl.Get(fName, 8);
							modDescription = LanguageControl.Get(fName, 69);
						}
						DialogsManager.ShowDialog(null, new MessageDialog(title, modDescription, LanguageControl.Get(fName, 9), LanguageControl.Get(fName, 10), delegate (MessageDialogButton result)
						{
							if (result == MessageDialogButton.Button1)
							{
								Storage.DeleteFile(modItem.ExternalContentEntry.Path);
								UpdateListWithBusyDialog();
							}
						}));
					}
					else
					{
						if (modItem.ModInfo == null) return;
						string modDescription = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName;
						DialogsManager.ShowDialog(null, new MessageDialog(modItem.ModInfo.Name, modDescription, LanguageControl.Get(fName, 60), LanguageControl.Get(fName, 10), delegate (MessageDialogButton result)
						{
							if (result == MessageDialogButton.Button1)
							{
								//UpdateModFromCommunity(modItem.ModInfo);
								DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 61), null, LanguageControl.Ok, null, null));
							}
						}));
					}
				}
			}
		};
	}

	public override void Enter(object[] parameters)
	{
		CommunityContentManager.IsAdmin(new CancellableProgress(), delegate (bool isAdmin)
		{
			m_isAdmin = isAdmin;
		}, delegate (Exception e)
		{
		});
		if (!Storage.DirectoryExists(m_uninstallPath)) Storage.CreateDirectory(m_uninstallPath);
		BusyDialog busyDialog = new(LanguageControl.Get(fName, 26), LanguageControl.Get(fName, 32));
		DialogsManager.ShowDialog(null, busyDialog);
		foreach (string commonPath in m_commonPaths)
		{
			if ((m_androidSystem && commonPath.StartsWith("android:")) || (!m_androidSystem && !commonPath.StartsWith("android:")))
			{
				AddCommonPath(commonPath);
			}
		}
		string commonPathsFile = Storage.CombinePaths(m_uninstallPath, "CommonPaths.txt");
		if (Storage.FileExists(commonPathsFile))
		{
			Stream stream = Storage.OpenFile(commonPathsFile, OpenFileMode.Read);
			StreamReader streamReader = new(stream);
			string line;
			while ((line = streamReader.ReadLine()) != null)
			{
				AddCommonPath(line.Replace("\n", "").Replace("\r", ""));
			}
			stream.Dispose();
		}
		if (!m_firstEnterScreen)
		{
			m_firstEnterScreen = true;
			string explanation = "";
			if (m_androidSystem && !m_androidDataPathEnterEnabled)
			{
				explanation += LanguageControl.Get(fName, 46) + "\n\n";
			}
			explanation += LanguageControl.Get(fName, 47);
			if (m_commonPathList.Count > 0)
			{
				explanation += "\n\n" + LanguageControl.Get(fName, 48);
				for (int i = 0; i < m_commonPathList.Count; i++)
				{
					explanation += "\n" + (i + 1) + ". " + m_commonPathList[i];
				}
				explanation += "\n\n" + LanguageControl.Get(fName, 12);
			}
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 14), explanation, LanguageControl.Get(fName, 15), null, null));
		}
		Task.Run(delegate
		{
			FastScanModFile(false);
			SetPath(m_installPath);
			m_filter = StateFilter.InstallState;
			UpdateList();
			SetPath(m_uninstallPath);
			m_filter = StateFilter.UninstallState;
			UpdateList();
			m_updatable = true;
			m_firstEnterInstallScreen = false;
			Dispatcher.Dispatch(delegate
			{
				foreach (ModInfo modInfo in m_installModInfo)
				{
					m_lastInstallModInfo.Add(modInfo);
				}
				if (parameters.Length > 0 && (bool)parameters[0])
				{
					SetPath(m_installPath);
					m_filter = StateFilter.InstallState;
					UpdateList();
				}
				UpdateList(true);
				DialogsManager.HideDialog(busyDialog);
			});
		});
	}

	public override void Leave()
	{
		m_modsContentList.ClearItems();
		m_installModInfo.Clear();
		m_lastInstallModInfo.Clear();
		m_uninstallModList.Clear();
		m_installModList.Clear();
		m_scanFailPaths.Clear();
		m_latestScanModList.Clear();
		if (!Storage.DirectoryExists(m_uninstallPath)) Storage.CreateDirectory(m_uninstallPath);
		string commonPathsFile = Storage.CombinePaths(m_uninstallPath, "CommonPaths.txt");
		if (m_commonPathList.Count > 0)
		{
			Stream stream = Storage.OpenFile(commonPathsFile, OpenFileMode.Create);
			StreamWriter streamWriter = new(stream);
			foreach (string commonPath in m_commonPathList)
			{
				streamWriter.WriteLine(commonPath);
			}
			streamWriter.Flush();
			stream.Dispose();
		}
		m_commonPathList.Clear();
	}

	public override void Update()
	{
		m_actionButton3.IsVisible = m_isAdmin;
		m_uninstallFilterButton.IsChecked = m_filter != StateFilter.InstallState;
		m_installFilterButton.IsChecked = m_filter == StateFilter.InstallState;
		m_uninstallFilterButton.Color = (m_filter == StateFilter.InstallState) ? Color.White : Color.Green;
		m_installFilterButton.Color = (m_filter == StateFilter.InstallState) ? Color.Green : Color.White;
		m_upDirectoryButton.IsVisible = m_filter != StateFilter.InstallState;
		if (m_filter != StateFilter.InstallState)
		{
			m_actionButton2.IsVisible = true;
			m_actionButton2.Text = (m_path == m_uninstallPath) ? LanguageControl.Get(fName, 16) : LanguageControl.Get(fName, 17);
		}
		else
		{
			m_actionButton2.IsVisible = false;
			m_actionButton2.Text = LanguageControl.Get(fName, 63);
		}
		ModItem modItem = null;
		if (m_modsContentList.SelectedIndex.HasValue)
		{
			modItem = m_modsContentList.Items[m_modsContentList.SelectedIndex.Value] as ModItem;
		}
		if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
		{
			m_actionButton.Text = (m_filter == StateFilter.InstallState) ? LanguageControl.Get(fName, 18) : LanguageControl.Get(fName, 19);
			m_actionButton.IsEnabled = !(modItem.ModInfo == null && m_filter != StateFilter.InstallState);
			m_actionButton2.IsEnabled = false;
		}
		else if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Directory)
		{
			m_actionButton.IsEnabled = true;
			m_actionButton.Text = LanguageControl.Get(fName, 20);
			m_actionButton2.IsEnabled = m_filter != StateFilter.InstallState;
		}
		else
		{
			m_actionButton.Text = LanguageControl.Get(fName, 21);
			m_actionButton.IsEnabled = false;
			m_actionButton2.IsEnabled = m_filter != StateFilter.InstallState;
		}
		if (m_actionButton.IsClicked)
		{
			if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
			{
				string fileName = Storage.GetFileName(modItem.ExternalContentEntry.Path);
				string installPathName = Storage.CombinePaths(m_installPath, fileName);
				string uninstallPathName = modItem.ExternalContentEntry.Path;
				if (m_filter == StateFilter.InstallState)
				{
					string modDescription;
					if (modItem.ModInfo != null)
					{
						modDescription = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName + "，" + LanguageControl.Get(fName, 8);
					}
					else
					{
						modDescription = LanguageControl.Get(fName, 70);
					}
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 49), modDescription, LanguageControl.Ok, LanguageControl.Cancel, delegate (MessageDialogButton result)
					{
						if (result == MessageDialogButton.Button1)
						{
							try
							{
								Storage.DeleteFile(installPathName);
								UpdateListWithBusyDialog();
								m_updatable = false;
							}
							catch (Exception e)
							{
								DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 50), LanguageControl.Get(fName, 51) + e.ToString(), LanguageControl.Get("Usual", "ok"), null, null));
							}
						}
					}));
				}
				else
				{
					ModInfo samePackmModInfo = null;
					foreach (ModInfo modInfo in m_installModInfo)
					{
						if (modInfo.PackageName == modItem.ModInfo.PackageName)
						{
							samePackmModInfo = modInfo;
						}
					}
					if (!Storage.FileExists(installPathName) && samePackmModInfo == null)
					{
						Storage.CopyFile(uninstallPathName, installPathName);
						m_installModInfo.Add(modItem.ModInfo);
						m_installModList.Add(modItem);
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 23), fileName, LanguageControl.Get("Usual", "ok"), null, null));
					}
					else if (samePackmModInfo != null)
					{
						if (samePackmModInfo.Version == modItem.ModInfo.Version)
						{
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 52), LanguageControl.Get(fName, 53), LanguageControl.Get("Usual", "ok"), null, null));
						}
						else
						{
							string tips = string.Format(LanguageControl.Get(fName, 54), modItem.ModInfo.Version, samePackmModInfo.Version);
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 55), tips, LanguageControl.Ok, LanguageControl.Cancel, delegate (MessageDialogButton result)
							{
								if (result == MessageDialogButton.Button1)
								{
									foreach (ModItem modItem3 in m_installModList)
									{
										if (modItem3.ModInfo.PackageName == samePackmModInfo.PackageName)
										{
											try
											{
												Storage.DeleteFile(modItem3.ExternalContentEntry.Path);
												Storage.CopyFile(uninstallPathName, installPathName);
												m_updatable = true;
											}
											catch (Exception e)
											{
												DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 56), LanguageControl.Get(fName, 51) + e.ToString(), LanguageControl.Get("Usual", "ok"), null, null));
											}
											break;
										}
									}
								}
							}));
						}
					}
					else if (Storage.FileExists(installPathName))
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 24), fileName + LanguageControl.Get(fName, 57), LanguageControl.Get("Usual", "ok"), null, null));
					}
				}
			}
			else if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Directory)
			{
				CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 26), true);
				ReadyForScan(busyDialog);
				Task.Run(delegate
				{
					string scanPath = modItem.ExternalContentEntry.Path;
					int allCount = ScanModFile(scanPath, busyDialog);
					DialogsManager.HideDialog(busyDialog);
					if (allCount == 0)
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 33), LanguageControl.Get(fName, 34), LanguageControl.Get(fName, 10), delegate (MessageDialogButton result)
						{
							if (result == MessageDialogButton.Button1)
							{
								ScreensManager.SwitchScreen("CommunityContent", "Mod");
							}
						}));
					}
					else
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), string.Format(LanguageControl.Get(fName, 29), allCount), LanguageControl.Get(fName, 30), LanguageControl.Get(fName, 31), delegate (MessageDialogButton result)
						{
							if (result == MessageDialogButton.Button1)
							{
								SetPath(m_uninstallPath);
								UpdateListWithBusyDialog();
							}
						}));
					}
				});
			}
		}
		if (m_actionButton2.IsClicked)
		{
			if (m_filter == StateFilter.InstallState)
			{
			}
			else
			{
				if (m_path == m_uninstallPath)
				{
					if (m_cancellableBusyDialog != null)
					{
						DialogsManager.ShowDialog(null, m_cancellableBusyDialog);
						return;
					}
					m_cancellableBusyDialog = new CancellableBusyDialog(LanguageControl.Get(fName, 26), LanguageControl.Get(fName, 62), true);
					ReadyForScan(m_cancellableBusyDialog);
					Task.Run(delegate
					{
						string scanPath;
						if (m_androidSystem)
						{
							scanPath = "android:";
						}
						else
						{
							string systemPath = Storage.GetSystemPath(m_path);
							systemPath = systemPath.Replace("\\", "/");
							int index = systemPath.IndexOf('/');
							scanPath = "system:" + systemPath.Substring(0, index) + "/";
						}
						int allCount = ScanModFile(scanPath, m_cancellableBusyDialog);
						DialogsManager.HideDialog(m_cancellableBusyDialog);
						m_cancellableBusyDialog = null;
						if (allCount == 0)
						{
							string tips = LanguageControl.Get(fName, 33);
							if (m_scanFailPaths.Count > 0)
							{
								tips += "\n\n" + LanguageControl.Get(fName, 58) + "\n";
								foreach (string p in m_scanFailPaths)
								{
									tips += p + "\n";
								}
							}
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), tips, LanguageControl.Get(fName, 34), LanguageControl.Get(fName, 10), delegate (MessageDialogButton result)
							{
								if (result == MessageDialogButton.Button1)
								{
									ScreensManager.SwitchScreen("CommunityContent", "Mod");
								}
							}));
						}
						else
						{
							string tips = string.Format(LanguageControl.Get(fName, 35), allCount);
							if (m_scanFailPaths.Count > 0)
							{
								tips += "\n\n" + LanguageControl.Get(fName, 58) + "\n";
								foreach (string p in m_scanFailPaths)
								{
									tips += p + "\n";
								}
							}
							if (ScreensManager.CurrentScreen == this)
							{
								DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), tips, LanguageControl.Get(fName, 30), null, delegate (MessageDialogButton result)
								{
									SetPath(m_uninstallPath);
									UpdateListWithBusyDialog();
								}));
							}
							else
							{
								DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), tips, LanguageControl.Ok, null, null));
							}
						}
					});
				}
				else
				{
					SetPath(m_uninstallPath);
					UpdateListWithBusyDialog();
				}
			}
		}
		if (m_uninstallFilterButton.IsClicked && m_filter == StateFilter.InstallState)
		{
			m_filter = StateFilter.UninstallState;
			SetPath(m_uninstallPath);
			UpdateList(true);
		}
		if (m_installFilterButton.IsClicked && m_filter != StateFilter.InstallState)
		{
			m_latestScanModList.Clear();
			m_filter = StateFilter.InstallState;
			SetPath(m_installPath);
			if (!m_firstEnterInstallScreen)
			{
				m_firstEnterInstallScreen = true;
				m_updatable = true;
			}
			UpdateList(true);
		}
		if (m_actionButton3.IsClicked && modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
		{
			Stream stream = Storage.OpenFile(modItem.ExternalContentEntry.Path, OpenFileMode.ReadWrite);
			if (stream == null) return;
			Stream stream2 = GetDecipherStream(stream);
			FileStream fileStream = new(Storage.GetSystemPath(ModsManager.ModDisPath) + "/Original.scmod", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			byte[] buff = new byte[stream2.Length];
			stream2.ReadExactly(buff);
			fileStream.Write(buff, 0, buff.Length);
			fileStream.Flush();
			fileStream.Dispose();
			stream.Dispose();
			stream2.Dispose();
			DialogsManager.ShowDialog(null, new MessageDialog("操作成功", Storage.GetSystemPath(ModsManager.ModDisPath) + "/Original.scmod", LanguageControl.Ok, null, null));
		}
		if (m_upDirectoryButton.IsClicked)
		{
			string directory = Storage.GetDirectoryName(m_path);
			if (m_path != "android:" && m_path != "app:")
			{
				if (directory.StartsWith("system:") && !directory.Contains("/")) directory = directory + "/";
				SetPath(directory);
				UpdateListWithBusyDialog();
			}
			else if (m_path == "app:")
			{
				string systemPath = Storage.GetSystemPath(m_path);
				systemPath = systemPath.Replace("\\", "/");
				int index = systemPath.LastIndexOf('/');
				directory = "system:" + systemPath.Substring(0, index);
				SetPath(directory);
				UpdateListWithBusyDialog();
			}
		}
		if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			if (InstallModChange())
			{
				DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 38), LanguageControl.Get(fName, 39), LanguageControl.Get(fName, 31), delegate (MessageDialogButton result)
				{
					if (result == MessageDialogButton.Button1)
					{
						Environment.Exit(0);
					}
					if (result == MessageDialogButton.Button2)
					{
						ScreensManager.SwitchScreen("MainMenu");
					}
				}));
			}
			else
			{
				ScreensManager.SwitchScreen("MainMenu");
			}
		}
	}

	public void UpdateListWithBusyDialog(bool fast = false)
	{
		BusyDialog busyDialog = new(LanguageControl.Get(fName, 43), null);
		DialogsManager.ShowDialog(null, busyDialog);
		Task.Run(delegate
		{
			UpdateList(fast);
			Dispatcher.Dispatch(delegate
			{
				UpdateList(true);
				DialogsManager.HideDialog(busyDialog);
			});
		});
	}

	public void UpdateList(bool fast = false)
	{
		m_modsContentLabel.Text = LanguageControl.Get(fName, 40) + SetPathText(m_path);
		if (!fast || m_updatable)
		{
			SetModItemList();
			if (fast) m_updatable = false;
		}
		m_modsContentList.ClearItems();
		if (m_filter == StateFilter.InstallState)
		{
			foreach (ModItem modItem in m_installModList)
			{
				m_modsContentList.AddItem(modItem);
			}
		}
		else
		{
			foreach (ModItem modItem in m_uninstallModList)
			{
				m_modsContentList.AddItem(modItem);
			}
		}
	}

	public void SetModItemList()
	{
		m_updatable = true;
		if (m_filter == StateFilter.InstallState)
		{
			m_installModInfo.Clear();
			m_installModList.Clear();
		}
		else
		{
			m_uninstallModList.Clear();
		}
		try
		{
			IEnumerable<string> fileNameList = Storage.ListFileNames(m_path);
			foreach (string fileName in fileNameList)
			{
				string extension = Storage.GetExtension(fileName);
				if (!string.IsNullOrEmpty(extension) && extension.ToLower() == ".scmod")
				{
					ModItem modItem = GetModItem(fileName, false);
					if (modItem == null || (modItem.ModInfo != null && string.IsNullOrEmpty(modItem.ModInfo.PackageName))) continue;
					if (modItem.ModInfo != null && modItem.ModInfo.ApiVersion.StartsWith("1.3"))
					{
						modItem.ModInfo = null;
					}
					if (m_filter == StateFilter.InstallState)
					{
						if (modItem.ModInfo != null)
						{
							m_installModInfo.Add(modItem.ModInfo);
						}
						m_installModList.Add(modItem);
					}
					else
					{
						m_uninstallModList.Add(modItem);
					}
				}
			}
			IEnumerable<string> directoryNameList = Storage.ListDirectoryNames(m_path);
			foreach (string directoryName in directoryNameList)
			{
				ModItem modItem = GetModItem(directoryName, true);
				if (m_filter == StateFilter.InstallState)
				{
					m_installModList.Add(modItem);
				}
				else
				{
					m_uninstallModList.Add(modItem);
				}
			}
		}
		catch (Exception e)
		{
			Log.Warning("SetModItemList:" + e.ToString());
		}
	}

	public void ReadyForScan(CancellableBusyDialog busyDialog)
	{
		m_cancelScan = false;
		m_scanFailPaths.Clear();
		m_count = 0;
		DialogsManager.ShowDialog(null, busyDialog);
		busyDialog.ShowProgressMessage = false;
		busyDialog.Progress.Cancelled += delegate
		{
			m_cancelScan = true;
		};
	}

	public int ScanModFile(string path, CancellableBusyDialog busyDialog = null)
	{
		string validPath = path;
		if (m_cancelScan) return m_count;
		try
		{
			string systemPath = Storage.GetSystemPath(path);
			if (systemPath != Storage.GetSystemPath(m_uninstallPath))
			{
				foreach (string fileName in Storage.ListFileNames(validPath))
				{
					if (m_cancelScan) return m_count;
					if (validPath.EndsWith("/"))
					{
						validPath = path.Substring(0, validPath.Length - 1);
					}
					if (busyDialog != null)
					{
						string showName = validPath;
						if (validPath.Length > 40)
						{
							showName = validPath.Substring(0, 40) + "...";
						}
						busyDialog.SmallMessage = string.Format(LanguageControl.Get(fName, 59) + showName, m_count);
					}
					string extension = Storage.GetExtension(fileName);
					if (!string.IsNullOrEmpty(extension) && extension.ToLower() == ".scmod")
					{
						string pathName = Storage.CombinePaths(validPath, fileName);
						Stream stream = null;
						ModInfo modInfo = null;
						try
						{
							stream = Storage.OpenFile(pathName, OpenFileMode.Read);
							stream = GetDecipherStream(stream);
							ZipArchive zipArchive = ZipArchive.Open(stream, false);
							foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.ReadCentralDir())
							{
								if (zipArchiveEntry.FilenameInZip == "modinfo.json")
								{
									MemoryStream memoryStream = new();
									zipArchive.ExtractFile(zipArchiveEntry, memoryStream);
									memoryStream.Position = 0L;
									modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(memoryStream));
									memoryStream.Dispose();
									break;
								}
							}
							stream.Dispose();
						}
						catch
						{
						}
						if (stream == null) continue;
						if (modInfo != null && string.IsNullOrEmpty(modInfo.PackageName)) continue;
						string uninstallPathName = Storage.CombinePaths(m_uninstallPath, fileName);
						if (!Storage.FileExists(uninstallPathName))
						{
							Storage.CopyFile(pathName, uninstallPathName);
							if (systemPath != Storage.GetSystemPath(m_installPath))
							{
								Storage.DeleteFile(pathName);
							}
							AddCommonPath(validPath);
							if (modInfo != null && !modInfo.ApiVersion.StartsWith("1.3"))
							{
								m_latestScanModList.Add(fileName);
								m_count++;
							}
						}
						if (stream != null) stream.Close();
					}
				}
			}
			foreach (string directory in Storage.ListDirectoryNames(path))
			{
				if (m_cancelScan) return m_count;
				if (validPath.EndsWith("/"))
				{
					validPath = path.Substring(0, validPath.Length - 1);
				}
				string subPath = Storage.CombinePaths(validPath, directory);
				ScanModFile(subPath, busyDialog);
			}
		}
		catch
		{
			m_scanFailPaths.Add(validPath);
		}
		return m_count;
	}

	public int FastScanModFile(bool showTips = true)
	{
		int allCount = 0;
		foreach (string commonPath in m_commonPathList)
		{
			if ((m_androidSystem && commonPath.StartsWith("android:")) || (!m_androidSystem && !commonPath.StartsWith("android:")))
			{
				try
				{
					if (Storage.DirectoryExists(commonPath))
					{
						m_count = 0;
						int sucesssCount = ScanModFile(commonPath);
						allCount += sucesssCount;
					}
				}
				catch
				{
					if (showTips)
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), string.Format(LanguageControl.Get(fName, 41), commonPath), LanguageControl.Get(fName, 15), null, null));
					}
				}
			}
		}
		return allCount;
	}

	public ModItem GetModItem(string fileName, bool IsDirectory)
	{
		ModItem modItem = new();
		string pathName = Storage.CombinePaths(m_path, fileName);
		modItem.Name = fileName;
		modItem.Subtexture = ExternalContentManager.GetEntryTypeIcon(IsDirectory ? ExternalContentType.Directory : ExternalContentType.Mod);
		modItem.ExternalContentEntry = new ExternalContentEntry
		{
			Type = IsDirectory ? ExternalContentType.Directory : ExternalContentType.Mod,
			Path = pathName,
			Size = IsDirectory ? 0 : Storage.GetFileSize(pathName),
			Time = Storage.GetFileLastWriteTime(pathName)
		};
		if (IsDirectory) return modItem;
		Stream stream = Storage.OpenFile(pathName, OpenFileMode.Read);
		try
		{
			stream = GetDecipherStream(stream);
			ZipArchive zipArchive = ZipArchive.Open(stream, false);
			foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.ReadCentralDir())
			{
				if (Path.GetFileNameWithoutExtension(zipArchiveEntry.FilenameInZip) == "icon")//兼容多种格式
				{
					MemoryStream memoryStream = new();
					zipArchive.ExtractFile(zipArchiveEntry, memoryStream);
					memoryStream.Position = 0L;
					modItem.Subtexture = new Subtexture(Texture2D.Load(memoryStream), Vector2.Zero, Vector2.One);
					memoryStream.Dispose();
				}
				else if (zipArchiveEntry.FilenameInZip == "modinfo.json")
				{
					MemoryStream memoryStream = new();
					zipArchive.ExtractFile(zipArchiveEntry, memoryStream);
					memoryStream.Position = 0L;
					modItem.ModInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(memoryStream));
					memoryStream.Dispose();
				}
			}
		}
		catch (Exception)
		{
			modItem = null;
		}
		finally
		{
			stream.Dispose();
		}
		return modItem;
	}

	public bool InstallModChange()
	{
		bool IsChange = false;
		if (m_installModInfo.Count != m_lastInstallModInfo.Count)
		{
			return true;
		}
		foreach (ModInfo modInfo in m_installModInfo)
		{
			if (!m_lastInstallModInfo.Contains(modInfo))
			{
				IsChange = true;
				break;
			}
		}
		return IsChange;
	}

	public void SetPath(string path)
	{
		path = path.Replace("\\", "/");
		if (path != m_path)
		{
			m_lastPath = m_path;
			m_path = path;
		}
	}

	public string SetPathText(string path)
	{
		string newText = Storage.GetSystemPath(path);
		string[] arPath = path.Split(new char[] { '/' });
		if (arPath.Length > 5)
		{
			newText = ".../" + arPath[^3] + "/" + arPath[^2] + "/" + arPath[^1];
		}
		return newText;
	}

	public void AddCommonPath(string path)
	{
		if (!m_commonPathList.Contains(path) && !string.IsNullOrEmpty(path))
		{
			m_commonPathList.Add(path);
		}
	}

	public static Stream GetDecipherStream(Stream stream)
	{
		MemoryStream keepOpenStream = new();
		byte[] buff = new byte[stream.Length];
		stream.ReadExactly(buff);
		byte[] hc = Encoding.UTF8.GetBytes(HeadingCode);
		bool decipher = true;
		for (int i = 0; i < hc.Length; i++)
		{
			if (hc[i] != buff[i])
			{
				decipher = false;
				break;
			}
		}
		byte[] hc2 = Encoding.UTF8.GetBytes(HeadingCode2);
		bool decipher2 = true;
		for (int i = 0; i < hc2.Length; i++)
		{
			if (hc2[i] != buff[i])
			{
				decipher2 = false;
				break;
			}
		}
		if (decipher)
		{
			byte[] buff2 = new byte[buff.Length - hc.Length];
			for (int i = 0; i < buff2.Length; i++)
			{
				buff2[i] = buff[buff.Length - 1 - i];
			}
			keepOpenStream.Write(buff2, 0, buff2.Length);
			keepOpenStream.Flush();
		}
		else if (decipher2)
		{
			byte[] buff2 = new byte[buff.Length - hc2.Length];
			int k = 0;
			int t = 0;
			int l = (buff2.Length + 1) / 2;
			for (int i = 0; i < buff2.Length; i++)
			{
				if (i % 2 == 0)
				{
					buff2[i] = buff[hc2.Length + k];
					k++;
				}
				else
				{
					buff2[i] = buff[hc2.Length + l + t];
					t++;
				}
			}
			keepOpenStream.Write(buff2, 0, buff2.Length);
			keepOpenStream.Flush();
		}
		else
		{
			stream.Position = 0L;
			stream.CopyTo(keepOpenStream);
		}
		stream.Dispose();
		keepOpenStream.Position = 0L;
		return keepOpenStream;
	}

	public static bool StrengtheningMod(string path)
	{
		Stream stream = Storage.OpenFile(path,OpenFileMode.Read);
		byte[] buff = new byte[stream.Length];
		stream.ReadExactly(buff);
		byte[] hc = Encoding.UTF8.GetBytes(HeadingCode);
		bool decipher = true;
		for(int i = 0; i < hc.Length; i++)
		{
			if(hc[i] != buff[i])
			{
				decipher = false;
				break;
			}
		}
		byte[] hc2 = Encoding.UTF8.GetBytes(HeadingCode2);
		bool decipher2 = true;
		for(int i = 0; i < hc2.Length; i++)
		{
			if(hc2[i] != buff[i])
			{
				decipher2 = false;
				break;
			}
		}
		if(decipher || decipher2) return false;
		byte[] buff2 = new byte[buff.Length + hc2.Length];
		int k = 0;
		int l = hc2.Length;
		for(int i = 0; i < hc2.Length; i++)
		{
			buff2[i] = hc2[i];
		}
		for(int i = 0; i < buff.Length; i++)
		{
			if(i % 2 == 0)
			{
				buff2[k + l] = buff[i];
				k++;
			}
		}
		k = 0;
		l = hc2.Length + ((buff.Length + 1) / 2);
		for(int i = 0; i < buff.Length; i++)
		{
			if(i % 2 != 0)
			{
				buff2[k + l] = buff[i];
				k++;
			}
		}
		string newPath = string.Format("{0}({1}).scmod",path.Substring(0,path.LastIndexOf('.')),LanguageControl.Get(fName,63));
		FileStream fileStream = new(Storage.GetSystemPath(newPath),FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite);
		fileStream.Write(buff2,0,buff2.Length);
		fileStream.Flush();
		stream.Dispose();
		fileStream.Dispose();
		return true;
	}

	public void UpdateModFromCommunity(ModInfo modInfo)
	{
		//从社区拉取MOD并更新
		//CommunityContentScreen communityContentScreen = ScreensManager.FindScreen<CommunityContentScreen>("CommunityContent");
		//communityContentScreen.m_filter = ExternalContentType.Mod;
		//communityContentScreen.PopulateList(null);
		//foreach(var item2 in communityContentScreen.m_listPanel.Items)
		//{
		//    CommunityContentEntry communityContentEntry = item2 as CommunityContentEntry;
		//}
	}
}

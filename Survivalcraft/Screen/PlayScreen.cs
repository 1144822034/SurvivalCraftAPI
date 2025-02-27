using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using System.Xml.Linq;
using TemplatesDatabase;

namespace Game
{
	public class PlayScreen : Screen
	{
		public ListPanelWidget m_worldsListWidget;

		public ButtonWidget m_playButton;

		public ButtonWidget m_newWorldButton;

		public ButtonWidget m_propertiesButton;

		public static int MaxWorlds = 300;

		public double m_modTipsTime;

		public static string fName = "PlayScreen";

		public virtual void OnWorldsListWidgetItemClicked(Object item)
		{
			if(item != null && m_worldsListWidget.SelectedItem == item)
			{
				Play(item);
			}
		}

		public Widget WorldInfoWidget(Object item)
		{
            var worldInfo = (WorldInfo)item;
            XElement node2 = ContentManager.Get<XElement>("Widgets/SavedWorldItem");
            var containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
            LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("WorldItem.Name");
            LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("WorldItem.Details");
            containerWidget.Tag = worldInfo;
            labelWidget.Text = worldInfo.WorldSettings.Name;
            labelWidget2.Text = string.Format("{0} | {1:dd MMM yyyy HH:mm} | {2} | {3} | {4}", DataSizeFormatter.Format(worldInfo.Size),
                worldInfo.LastSaveTime.ToLocalTime(),
                (worldInfo.PlayerInfos.Count > 1) ? string.Format(LanguageControl.GetContentWidgets(fName, 9), worldInfo.PlayerInfos.Count) : string.Format(LanguageControl.GetContentWidgets(fName, 10), 1),
                LanguageControl.Get("GameMode", worldInfo.WorldSettings.GameMode.ToString()),
                LanguageControl.Get("EnvironmentBehaviorMode", worldInfo.WorldSettings.EnvironmentBehaviorMode.ToString()));
            if (worldInfo.SerializationVersion != VersionsManager.SerializationVersion)
            {
                labelWidget2.Text = labelWidget2.Text + " | " + (string.IsNullOrEmpty(worldInfo.SerializationVersion) ? LanguageControl.GetContentWidgets("Usual", "Unknown") : ("(" + worldInfo.SerializationVersion + ")"));
            }
			ModsManager.HookAction("LoadWorldInfoWidget", loader =>
			{
				loader.LoadWorldInfoWidget(worldInfo, node2, ref containerWidget);
				return false;
			});
            return containerWidget;
        }
		public PlayScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/PlayScreen");
			LoadContents(this, node);
			m_worldsListWidget = Children.Find<ListPanelWidget>("WorldsList");
			m_playButton = Children.Find<ButtonWidget>("Play");
			m_newWorldButton = Children.Find<ButtonWidget>("NewWorld");
			m_propertiesButton = Children.Find<ButtonWidget>("Properties");
			ListPanelWidget worldsListWidget = m_worldsListWidget;
			worldsListWidget.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(worldsListWidget.ItemWidgetFactory, WorldInfoWidget);
			m_worldsListWidget.ScrollPosition = 0f;
			m_worldsListWidget.ScrollSpeed = 0f;
			m_worldsListWidget.ItemClicked += OnWorldsListWidgetItemClicked;
			m_modTipsTime = -10000000f;
		}

		public override void Enter(object[] parameters)
		{
			var dialog = new BusyDialog(LanguageControl.GetContentWidgets(fName, 5), null);
			DialogsManager.ShowDialog(null, dialog);
			Task.Run(delegate
			{
				var selectedItem = (WorldInfo)m_worldsListWidget.SelectedItem;
				WorldsManager.UpdateWorldsList();
				var worldInfos = new List<WorldInfo>(WorldsManager.WorldInfos);
				worldInfos.Sort((WorldInfo w1, WorldInfo w2) => DateTime.Compare(w2.LastSaveTime, w1.LastSaveTime));
				Dispatcher.Dispatch(delegate
				{
					m_worldsListWidget.ClearItems();
					foreach (WorldInfo item in worldInfos)
					{
						m_worldsListWidget.AddItem(item);
					}
					if (selectedItem != null)
					{
						m_worldsListWidget.SelectedItem = worldInfos.FirstOrDefault((WorldInfo wi) => wi.DirectoryName == selectedItem.DirectoryName);
					}
					DialogsManager.HideDialog(dialog);
				});
			});
		}

		public override void Update()
		{
			Vector2 size = new(310, 60);
			if (SettingsManager.UIScale > 1f) size = new Vector2(250, 60);
			m_playButton.Size = size;
			m_newWorldButton.Size = size;
			if (m_worldsListWidget.SelectedItem != null && WorldsManager.WorldInfos.IndexOf((WorldInfo)m_worldsListWidget.SelectedItem) < 0)
			{
				m_worldsListWidget.SelectedItem = null;
			}
			Children.Find<LabelWidget>("TopBar.Label").Text = string.Format(LanguageControl.GetContentWidgets(fName, 6), m_worldsListWidget.Items.Count);
			m_playButton.IsEnabled = m_worldsListWidget.SelectedItem != null;
			m_propertiesButton.IsEnabled = m_worldsListWidget.SelectedItem != null;
			if (m_playButton.IsClicked && m_worldsListWidget.SelectedItem != null)
			{
				Play(m_worldsListWidget.SelectedItem);
			}
			if (m_newWorldButton.IsClicked)
			{
				if (WorldsManager.WorldInfos.Count >= MaxWorlds)
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.GetContentWidgets(fName, 7), string.Format(LanguageControl.GetContentWidgets(fName, 8), MaxWorlds), LanguageControl.GetContentWidgets("Usual", "ok"), null, null));
				}
				else
				{
					ScreensManager.SwitchScreen("NewWorld");
					m_worldsListWidget.SelectedItem = null;
				}
			}
			if (m_propertiesButton.IsClicked && m_worldsListWidget.SelectedItem != null)
			{
				var worldInfo = (WorldInfo)m_worldsListWidget.SelectedItem;
				ScreensManager.SwitchScreen("ModifyWorld", worldInfo.DirectoryName, worldInfo.WorldSettings);
			}
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen("MainMenu");
				m_worldsListWidget.SelectedItem = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item">实际类型为WorldInfo</param>
		public void Play(object item)
		{
			bool flag = false;
			WorldInfo worldInfo = item as WorldInfo;
			string languageType = (!ModsManager.Configs.ContainsKey("Language")) ? "zh-CN" : ModsManager.Configs["Language"];
			if (languageType == "zh-CN" && Time.RealTime - m_modTipsTime > 3600f)
			{
				m_modTipsTime = Time.RealTime;
				flag |= ShowTips(item);
			}
			List<ValuesDictionary> modsNotLoaded = new List<ValuesDictionary>();
			List<ValuesDictionary> modsVersionNotCapable = new List<ValuesDictionary>();
			if(worldInfo != null)
			{
				XElement projectNode = WorldsManager.GetProjectNode(worldInfo);
				if(projectNode != null)
				{
					XElement subsystemUsedModsNode = WorldsManager.GetSubsystemNode(projectNode, "UsedMods", false);
					if(subsystemUsedModsNode != null)
					{
						ValuesDictionary subsystemValuesDictionary = new ValuesDictionary();
						subsystemValuesDictionary.ApplyOverrides(subsystemUsedModsNode);
						int modsCount = subsystemValuesDictionary.GetValue("ModsCount",0);
						ValuesDictionary valuesDictionary = subsystemValuesDictionary.GetValue<ValuesDictionary>("Mods",null);
						if(valuesDictionary != null)
						{
							for(int i = 0; i < modsCount; i++)
							{
								ValuesDictionary modDictionary = valuesDictionary.GetValue<ValuesDictionary>(i.ToString(),null);
								if(modDictionary == null) continue;
								bool entityGotten = ModsManager.GetModEntity(modDictionary.GetValue("PackageName",string.Empty),out ModEntity modEntity);
								if(!entityGotten)
								{
									modsNotLoaded.Add(modDictionary);
									continue;
								}
								bool versionComparePass = modEntity?.Loader?.CompareModVersion(modEntity.modInfo.Version,modDictionary.GetValue("Version","?")) ?? true;
								modDictionary.SetValue("CurrentVersion",modEntity.modInfo.Version);
								if(!versionComparePass)
								{
									modsVersionNotCapable.Add(modDictionary);
									continue;
								}
							}
						}
					}
				}
			}
			if(!flag)
			{
				if(modsNotLoaded.Count > 0 || modsVersionNotCapable.Count > 0)
				{
					string text = string.Empty;
					if(modsNotLoaded.Count > 0) text += "缺少以下模组：\n";
					foreach(ValuesDictionary modDictionary in modsNotLoaded)
					{
						text += string.Format("模组名：{0}, 版本号：{1}\n",modDictionary.GetValue("Name", "?"),modDictionary.GetValue("Version", "?"));
					}
					if(modsVersionNotCapable.Count > 0) text += "以下模组版本不兼容：\n";
					foreach(ValuesDictionary modDictionary in modsVersionNotCapable)
					{
						text += string.Format("模组名：{0}，需求版本号：{1}，当前版本号：{2}\n",modDictionary.GetValue("Name","?"),modDictionary.GetValue("Version","?"),modDictionary.GetValue("CurrentVersion","?"));
					}
					text += "你确定要继续吗？";
					DialogsManager.ShowDialog(this,new MessageDialog("Mod缺失",text,LanguageControl.Yes,LanguageControl.No,delegate (MessageDialogButton button)
					{
						if(button == MessageDialogButton.Button1)
						{
							GameLoad(item);
						}
					}));
				}
				else GameLoad(item);
			}
		}

		public void GameLoad(object item)
		{
			ModsManager.HookAction("BeforeGameLoading", loader =>
			{
				item = loader.BeforeGameLoading(this, item);
				return false;
			});
			if(item != null)
				ScreensManager.SwitchScreen("GameLoading", item, null);
			m_worldsListWidget.SelectedItem = null;
		}

		public bool ShowTips(object item)
		{
			string tips = string.Empty;
			int num = 1;
			try
			{
				foreach (ModEntity modEntity in ModsManager.ModListAll)
				{
					foreach (var value in MotdManager.FilterModAll)
					{
						if (value.FilterAPIVersion == ModsManager.APIVersionString && value.PackageName == modEntity.modInfo.PackageName && CompareVersion(value.Version, modEntity.modInfo.Version))
						{
							tips += string.Format("{0}.{1}(v{2})  {3}\n", num, modEntity.modInfo.Name, modEntity.modInfo.Version, value.Explanation);
							num++;
						}
					}
				}
			}
			catch
			{
				return false;
			}
			if (!string.IsNullOrEmpty(tips))
			{
				DialogsManager.ShowDialog(null, new MessageDialog("Mod不兼容警告！", tips, "继续", "返回", delegate (MessageDialogButton button)
				{
					if (button == MessageDialogButton.Button1)
					{
						GameLoad(item);
					}
				}));
				return true;
			}
			return false;
		}

		public bool CompareVersion(string v1, string v2)
		{
			if (v1 == "all")
			{
				return true;
			}
			else if (v1.Contains("~"))
			{
				string[] versions = v1.Split(new char[1] { '~' }, StringSplitOptions.RemoveEmptyEntries);
				try
				{
					double minv = double.Parse(versions[0]);
					double maxv = double.Parse(versions[1]);
					double v = double.Parse(v2);
					return v >= minv && v <= maxv;
				}
				catch
				{
					return false;
				}
			}
			else if (v1.Contains(";"))
			{
				string[] versions = v1.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string v in versions)
				{
					if (v == v2)
					{
						return true;
					}
				}
				return false;
			}
			else
			{
				return v1 == v2;
			}
		}
	}
}

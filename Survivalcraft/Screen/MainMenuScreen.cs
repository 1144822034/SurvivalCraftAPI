using Engine;
using Engine.Graphics;
using Engine.Input;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Game
{
	public class MainMenuScreen : Screen
	{
		public string m_versionString = string.Empty;

		public bool m_versionStringTrial;

		public ButtonWidget m_showBulletinButton;

		public StackPanelWidget m_bulletinStackPanel;

		public LabelWidget m_copyrightLabel;

		public ButtonWidget m_languageSwitchButton;

		public ButtonWidget m_updateCheckButton;

		public Subtexture m_needToUpdateIcon;

		public Subtexture m_dontNeedUpdateIcon;

		public RectangleWidget m_updateButtonIcon;

		public StackPanelWidget m_leftBottomBar;

		public StackPanelWidget m_rightBottomBar;

		public const string fName = "MainMenuScreen";

		public MainMenuScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/MainMenuScreen");
			LoadContents(this, node);
			m_showBulletinButton = Children.Find<ButtonWidget>("BulletinButton");
			m_bulletinStackPanel = Children.Find<StackPanelWidget>("BulletinStackPanel");
			m_copyrightLabel = Children.Find<LabelWidget>("CopyrightLabel");
			m_languageSwitchButton = Children.Find<ButtonWidget>("LanguageSwitchButton");
			m_leftBottomBar = Children.Find<StackPanelWidget>("LeftBottomBar");
			m_rightBottomBar = Children.Find<StackPanelWidget>("RightBottomBar");
			m_updateCheckButton = Children.Find<ButtonWidget>("UpdateCheckButton");
			m_updateButtonIcon = Children.Find<RectangleWidget>("UpdateIcon");
			m_needToUpdateIcon = ContentManager.Get<Subtexture>("Textures/Gui/NeedToUpdate");
			m_dontNeedUpdateIcon = ContentManager.Get<Subtexture>("Textures/Gui/UpdateChecking");
			string languageType = ModsManager.Configs.GetValueOrDefault("Language", "zh-CN");
			m_bulletinStackPanel.IsVisible = languageType == "zh-CN";
			m_copyrightLabel.IsVisible = languageType != "zh-CN";
			ModsManager.HookAction("OnMainMenuScreenCreated",loader => { loader.OnMainMenuScreenCreated(this,m_leftBottomBar,m_rightBottomBar); return false; });
		}

		public override void Enter(object[] parameters)
		{
			MusicManager.CurrentMix = MusicManager.Mix.Menu;
			Children.Find<MotdWidget>().Restart();
			if (SettingsManager.IsolatedStorageMigrationCounter < 3)
			{
				SettingsManager.IsolatedStorageMigrationCounter++;
				VersionConverter126To127.MigrateDataFromIsolatedStorageWithDialog();
			}
			if (MotdManager.CanShowBulletin) MotdManager.ShowBulletin();
		}

		public override void Leave()
		{
			Keyboard.BackButtonQuitsApp = false;
		}

		public override void Update()
		{
			Keyboard.BackButtonQuitsApp = !MarketplaceManager.IsTrialMode;
			if (string.IsNullOrEmpty(m_versionString) || MarketplaceManager.IsTrialMode != m_versionStringTrial)
			{
				m_versionString = string.Format("Version {0}{1}", VersionsManager.Version, MarketplaceManager.IsTrialMode ? " (Day One)" : string.Empty);
				m_versionStringTrial = MarketplaceManager.IsTrialMode;
			}
			Children.Find("Buy").IsVisible = MarketplaceManager.IsTrialMode;
			Children.Find<LabelWidget>("Version").Text = m_versionString + " -  API " + ModsManager.APIVersionString;
			RectangleWidget rectangleWidget = Children.Find<RectangleWidget>("Logo");
			float num = 1f + (0.02f * MathF.Sin(1.5f * (float)MathUtils.Remainder(Time.FrameStartTime, 10000.0)));
			rectangleWidget.RenderTransform = Matrix.CreateTranslation((0f - rectangleWidget.ActualSize.X) / 2f, (0f - rectangleWidget.ActualSize.Y) / 2f, 0f) * Matrix.CreateScale(num, num, 1f) * Matrix.CreateTranslation(rectangleWidget.ActualSize.X / 2f, rectangleWidget.ActualSize.Y / 2f, 0f);
			if (m_languageSwitchButton.IsClicked)
			{
				DialogsManager.ShowDialog(null,new ListSelectionDialog(null,LanguageControl.LanguageTypes,70f,(object item) => ((KeyValuePair<string, CultureInfo>)item).Value.NativeName,delegate (object item)
				{
					LanguageControl.ChangeLanguage(((KeyValuePair<string, CultureInfo>)item).Key);
				}));
			}
			//更新控制
			if (!APIUpdateManager.IsNeedUpdate.HasValue)
			{
				float angle = (float)Time.RealTime * 2;//获取更新时旋转图标
				float scale = (angle + MathF.PI / 4) / (MathF.PI / 2);
				scale -= MathF.Round(scale);
				scale *= (MathF.PI / 2);
				scale = new Vector2(1,MathF.Tan(scale)).Length() / MathF.Sqrt(2);
				m_updateButtonIcon.LayoutTransform = Matrix.CreateRotationZ(angle) * Matrix.CreateScale(scale);
			}
			else
			{
				m_updateButtonIcon.LayoutTransform = Matrix.CreateRotationZ(0) * Matrix.CreateScale(1);
				m_updateButtonIcon.Subtexture = APIUpdateManager.IsNeedUpdate.Value ? m_needToUpdateIcon : m_dontNeedUpdateIcon;
			}
			if (m_updateCheckButton.IsClicked)
			{
				if (!APIUpdateManager.IsNeedUpdate.HasValue) DialogsManager.ShowDialog(this, new MessageDialog(LanguageControl.Get(fName,7), LanguageControl.Get(fName, 6), LanguageControl.Ok, null, null));
				else
				{
					if(APIUpdateManager.IsNeedUpdate.Value)
						DialogsManager.ShowDialog(this,new MessageDialog(LanguageControl.Get(fName,7),string.Format(LanguageControl.Get(fName,4),APIUpdateManager.LatestVersion,APIUpdateManager.CurrentVersion),LanguageControl.Get(fName,5),LanguageControl.Cancel,
								(button) => {
									if(button == MessageDialogButton.Button2)
									{
										WebBrowserManager.LaunchBrowser(ModsManager.APIReleaseLink_Client);
									}
								}));
					else DialogsManager.ShowDialog(this,new MessageDialog(LanguageControl.Get(fName,7),LanguageControl.Get(fName,3),LanguageControl.Ok,null,null));
				}
			}
			if (Children.Find<ButtonWidget>("Play").IsClicked)
			{
				ScreensManager.SwitchScreen("Play");
			}
			if (Children.Find<ButtonWidget>("Help").IsClicked)
			{
				ScreensManager.SwitchScreen("Help");
			}
			if (Children.Find<ButtonWidget>("Content").IsClicked)
			{
				ScreensManager.SwitchScreen("Content");
			}
			if (Children.Find<ButtonWidget>("Settings").IsClicked)
			{
				ScreensManager.SwitchScreen("Settings");
			}
			if (Children.Find<ButtonWidget>("Buy").IsClicked)
			{
				MarketplaceManager.ShowMarketplace();
			}
			if (Children.Find<ButtonWidget>("ResourcesManagement").IsClicked)
			{
				ScreensManager.m_screens.TryGetValue("Content",out Screen screen);
				ContentScreen contentScreen = screen as ContentScreen;
				contentScreen.OpenManageSelectDialog();
			}
			if (m_showBulletinButton.IsClicked)
			{
				if (MotdManager.m_bulletin != null && !MotdManager.m_bulletin.Title.Equals("null",StringComparison.CurrentCultureIgnoreCase))
				{
					MotdManager.ShowBulletin();
				}
				else
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, "1"), LanguageControl.Get(fName, "2"), LanguageControl.Ok, null, null));
				}
			}
			if ((Input.Back && !Keyboard.BackButtonQuitsApp) || Input.IsKeyDownOnce(Key.Escape))
			{
				if (MarketplaceManager.IsTrialMode)
				{
					ScreensManager.SwitchScreen("Nag");
				}
				else
				{
					Window.Close();
				}
			}
			if (!String.IsNullOrEmpty(ExternalContentManager.openFilePath))
			{
				ScreensManager.SwitchScreen("ExternalContent");
			}
		}
	}
}

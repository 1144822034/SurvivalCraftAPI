using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
namespace Game
{
	public class SettingsUiScreen : Screen
	{
		public ContainerWidget m_windowModeContainer;

		public ButtonWidget m_windowModeButton;

		public ButtonWidget m_languageButton;

		public ButtonWidget m_displayLogButton;

		public SliderWidget m_uiScaleSlider;

		public ButtonWidget m_upsideDownButton;

		public ButtonWidget m_hideMoveLookPadsButton;

		public ButtonWidget m_showGuiInScreenshotsButton;

		public ButtonWidget m_showLogoInScreenshotsButton;

		public ButtonWidget m_screenshotSizeButton;

		public ButtonWidget m_communityContentModeButton;

		public ButtonWidget m_originalCommunityContentModeButton;

		public ButtonWidget m_deleteWorldNeedToTextButton;

		public static string fName = "SettingsUiScreen";

		public SettingsUiScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsUiScreen");
			LoadContents(this, node);
			m_windowModeContainer = Children.Find<ContainerWidget>("WindowModeContainer");
			m_languageButton = Children.Find<ButtonWidget>("LanguageButton");
			m_displayLogButton = Children.Find<ButtonWidget>("DisplayLogButton");
			m_windowModeButton = Children.Find<ButtonWidget>("WindowModeButton");
			m_uiScaleSlider = Children.Find<SliderWidget>("UIScaleSlider");
			m_upsideDownButton = Children.Find<ButtonWidget>("UpsideDownButton");
			m_hideMoveLookPadsButton = Children.Find<ButtonWidget>("HideMoveLookPads");
			m_showGuiInScreenshotsButton = Children.Find<ButtonWidget>("ShowGuiInScreenshotsButton");
			m_showLogoInScreenshotsButton = Children.Find<ButtonWidget>("ShowLogoInScreenshotsButton");
			m_screenshotSizeButton = Children.Find<ButtonWidget>("ScreenshotSizeButton");
			m_communityContentModeButton = Children.Find<ButtonWidget>("CommunityContentModeButton");
			m_originalCommunityContentModeButton = Children.Find<ButtonWidget>("OriginalCommunityContentModeButton");
            m_deleteWorldNeedToTextButton = Children.Find<ButtonWidget>("DeleteWorldNeedToTextButton");
            if(VersionsManager.PlatformString == "Android")
            {
	            m_windowModeButton.IsEnabled = false;
            }
        }

		public override void Enter(object[] parameters)
		{
			m_windowModeContainer.IsVisible = true;
		}

		public override void Update()
		{
			if (m_windowModeButton.IsClicked)
			{
				SettingsManager.WindowMode = (Engine.WindowMode)((int)(SettingsManager.WindowMode + 1) % EnumUtils.GetEnumValues(typeof(Engine.WindowMode)).Count);
			}
			if (m_uiScaleSlider.SlidingCompleted)
			{
				SettingsManager.UIScale = m_uiScaleSlider.Value;
			}
			if (m_languageButton.IsClicked)
			{
				DialogsManager.ShowDialog(null,new ListSelectionDialog(null,LanguageControl.LanguageTypes,70f,(object item) => ((KeyValuePair<string, CultureInfo>)item).Value.NativeName,delegate (object item)
				{
					LanguageControl.ChangeLanguage(((KeyValuePair<string, CultureInfo>)item).Key);
				}));
				/*if (LanguageControl.LanguageTypes.Count > 2)
				{

				}
				else
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 1), LanguageControl.Get(fName, 2), LanguageControl.Yes, LanguageControl.No, delegate (MessageDialogButton button)
					{
						if (button == MessageDialogButton.Button1)
						{
							int next = LanguageControl.LanguageTypes.IndexOf(ModsManager.Configs["Language"]) + 1;
							if (next == LanguageControl.LanguageTypes.Count) next = 0;
							ChangeLanguage(LanguageControl.LanguageTypes[next]);
						}
					}));
				}*/
			}
			if (m_displayLogButton.IsClicked)
			{
				SettingsManager.DisplayLog = !SettingsManager.DisplayLog;
			}
			if (!m_uiScaleSlider.IsSliding)
			{
				m_uiScaleSlider.Value = SettingsManager.UIScale;
			}
			m_uiScaleSlider.Text = $"{m_uiScaleSlider.Value * 100f:0}%";
			if (m_upsideDownButton.IsClicked)
			{
				SettingsManager.UpsideDownLayout = !SettingsManager.UpsideDownLayout;
			}
			if (m_hideMoveLookPadsButton.IsClicked)
			{
				SettingsManager.HideMoveLookPads = !SettingsManager.HideMoveLookPads;
			}
			if (m_showGuiInScreenshotsButton.IsClicked)
			{
				SettingsManager.ShowGuiInScreenshots = !SettingsManager.ShowGuiInScreenshots;
			}
			if (m_showLogoInScreenshotsButton.IsClicked)
			{
				SettingsManager.ShowLogoInScreenshots = !SettingsManager.ShowLogoInScreenshots;
			}
			if (m_screenshotSizeButton.IsClicked)
			{
				SettingsManager.ScreenshotSize = (ScreenshotSize)((int)(SettingsManager.ScreenshotSize + 1) % EnumUtils.GetEnumValues(typeof(ScreenshotSize)).Count);
			}
			if (m_deleteWorldNeedToTextButton.IsClicked)
			{
				SettingsManager.DeleteWorldNeedToText = !SettingsManager.DeleteWorldNeedToText;
			}
			if (m_communityContentModeButton.IsClicked)
			{
				SettingsManager.CommunityContentMode = (CommunityContentMode)((int)(SettingsManager.CommunityContentMode + 1) % EnumUtils.GetEnumValues(typeof(CommunityContentMode)).Count);
			}
			if (m_originalCommunityContentModeButton.IsClicked)
			{
				SettingsManager.OriginalCommunityContentMode = (CommunityContentMode)((int)(SettingsManager.OriginalCommunityContentMode + 1) % EnumUtils.GetEnumValues(typeof(CommunityContentMode)).Count);
			}
			m_windowModeButton.Text = LanguageControl.Get("WindowMode", SettingsManager.WindowMode.ToString());
			m_languageButton.Text = LanguageControl.Get("Language", "Name");
			m_displayLogButton.Text = SettingsManager.DisplayLog ? LanguageControl.Yes : LanguageControl.No;
			m_upsideDownButton.Text = SettingsManager.UpsideDownLayout ? LanguageControl.Yes : LanguageControl.No;
			m_hideMoveLookPadsButton.Text = SettingsManager.HideMoveLookPads ? LanguageControl.Yes : LanguageControl.No;
			m_showGuiInScreenshotsButton.Text = SettingsManager.ShowGuiInScreenshots ? LanguageControl.Yes : LanguageControl.No;
			m_showLogoInScreenshotsButton.Text = SettingsManager.ShowLogoInScreenshots ? LanguageControl.Yes : LanguageControl.No;
			m_screenshotSizeButton.Text = LanguageControl.Get("ScreenshotSize", SettingsManager.ScreenshotSize.ToString());
			m_deleteWorldNeedToTextButton.Text = SettingsManager.DeleteWorldNeedToText ? LanguageControl.Yes : LanguageControl.No;
			m_communityContentModeButton.Text = LanguageControl.Get("CommunityContentMode", SettingsManager.CommunityContentMode.ToString());
			m_originalCommunityContentModeButton.Text = LanguageControl.Get("CommunityContentMode", SettingsManager.OriginalCommunityContentMode.ToString());
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
	}
}

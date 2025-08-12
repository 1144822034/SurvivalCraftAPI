using System.Xml.Linq;

namespace Game
{
	public class SettingsCompatibilityScreen : Screen
	{
		//public ButtonWidget m_singlethreadedTerrainUpdateButton;

		public ButtonWidget m_viewGameLogButton;

		public ButtonWidget m_reportButton;

		public ButtonWidget m_resetDefaultsButton;

		public LabelWidget m_descriptionLabel;

		public SettingsCompatibilityScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsCompatibilityScreen");
			LoadContents(this, node);
			//m_singlethreadedTerrainUpdateButton = Children.Find<ButtonWidget>("SinglethreadedTerrainUpdateButton");
			m_viewGameLogButton = Children.Find<ButtonWidget>("ViewGameLogButton");
			m_reportButton = Children.Find<ButtonWidget>("ReportButton");
			m_resetDefaultsButton = Children.Find<ButtonWidget>("ResetDefaultsButton");
			m_descriptionLabel = Children.Find<LabelWidget>("Description");
		}

		public override void Enter(object[] parameters)
		{
			m_descriptionLabel.Text = string.Empty;
		}

		public override void Update()
		{
			//if (m_singlethreadedTerrainUpdateButton.IsClicked)
			//{
			//	SettingsManager.MultithreadedTerrainUpdate = !SettingsManager.MultithreadedTerrainUpdate;
			//	m_descriptionLabel.Text = StringsManager.GetString("Settings.Compatibility.SinglethreadedTerrainUpdate.Description");
			//}

			if (m_viewGameLogButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new ViewGameLogDialog());
			}
			if (m_reportButton.IsClicked)
			{
				WebBrowserManager.LaunchBrowser(ModsManager.ReportLink);
			}
			if (m_resetDefaultsButton.IsClicked)
			{
				SettingsManager.MultithreadedTerrainUpdate = true;
			}
			//m_singlethreadedTerrainUpdateButton.Text = "已弃用";
			m_resetDefaultsButton.IsEnabled = !SettingsManager.MultithreadedTerrainUpdate;
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
	}
}

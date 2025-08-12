using Engine;
using System.Xml.Linq;

namespace Game
{
	public class SettingsGraphicsScreen : Screen
	{
		public BevelledButtonWidget m_virtualRealityButton;

		public SliderWidget m_brightnessSlider;

		private SliderWidget m_viewAngleSlider;

		public ContainerWidget m_vrPanel;

		public SettingsGraphicsScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsGraphicsScreen");
			LoadContents(this, node);
			m_virtualRealityButton = Children.Find<BevelledButtonWidget>("VirtualRealityButton");
			m_brightnessSlider = Children.Find<SliderWidget>("BrightnessSlider");
			m_viewAngleSlider = Children.Find<SliderWidget>("ViewAngleSlider");
			m_vrPanel = Children.Find<ContainerWidget>("VrPanel");
			m_vrPanel.IsVisible = false;
		}

		public override void Update()
		{

			if (m_brightnessSlider.IsSliding)
			{
				SettingsManager.Brightness = m_brightnessSlider.Value;
			}
			if (m_viewAngleSlider.IsSliding)
			{
				SettingsManager.ViewAngle = m_viewAngleSlider.Value;
			}
			m_virtualRealityButton.IsEnabled = false;
			m_virtualRealityButton.Text = SettingsManager.UseVr ? "Enabled" : "Disabled";
			m_brightnessSlider.Value = SettingsManager.Brightness;
			m_brightnessSlider.Text = MathF.Round(SettingsManager.Brightness * 10f).ToString();
			m_viewAngleSlider.Value = SettingsManager.ViewAngle;
			m_viewAngleSlider.Text = $"{MathF.Round(SettingsManager.ViewAngle * 100f)}%";
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
	}
}

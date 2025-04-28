using Engine;
using Engine.Serialization;
using NAudio.Flac;
using System.Xml.Linq;
using Engine.Input;

namespace Game
{
	public class CameraManageScreen : Screen
	{
		public Widget KeyInfoWidget(Object item)
		{
			XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
			var containerWidget = (ContainerWidget)LoadWidget(this,node,null);
			LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("Name");
			LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BoundKey");
			bool enable = Convert.ToInt32(SettingsManager.CameraManageSettings.GetValue(item.ToString(),default(object))) >= 0;
			labelWidget.Text = LanguageControl.Get("CameraManage", item.ToString());
			labelWidget2.Text = LanguageControl.Get("ContainerWidget","CameraManageScreen", enable ? "Enabled" : "Disabled");
			labelWidget2.Color = enable ? Color.White : Color.Gray;
			m_widgetsByString[item.ToString()] = containerWidget;
			return containerWidget;
		}

		public ListPanelWidget m_camerasList;
		public BevelledButtonWidget m_enableButton;
		public BevelledButtonWidget m_disableButton;
		public BevelledButtonWidget m_upButton;
		public BevelledButtonWidget m_downButton;
		public Dictionary<string, ContainerWidget> m_widgetsByString = new Dictionary<string, ContainerWidget>();

		public static int EnabledCamerasCount => SettingsManager.CameraManageSettings.Count(item => Convert.ToInt32(item.Value) >= 0);
		public CameraManageScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/CameraManageScreen");
			LoadContents(this, node);
			m_camerasList = Children.Find<ListPanelWidget>("KeysList");
			m_camerasList.ItemWidgetFactory = (Func<object,Widget>)Delegate.Combine(m_camerasList.ItemWidgetFactory,KeyInfoWidget);
			m_camerasList.ScrollPosition = 0f;
			m_camerasList.ScrollSpeed = 0f;
			m_camerasList.ItemClicked += (item) =>
			{
				if(m_camerasList.SelectedItem == item)
				{
					m_camerasList.SelectedItem = null;
				}
				else
				{
					m_camerasList.SelectedItem = item;
				}
			};
			m_enableButton = Children.Find<BevelledButtonWidget>("EnableCamera");
			m_disableButton = Children.Find<BevelledButtonWidget>("DisableCamera");
			m_upButton = Children.Find<BevelledButtonWidget>("Up");
			m_downButton = Children.Find<BevelledButtonWidget>("Down");
		}

		public override void Update()
		{
			string selectedCameraName = m_camerasList.SelectedItem?.ToString() ?? string.Empty;
			int enabledCount = EnabledCamerasCount;
			int selectedItemValue = Convert.ToInt32(SettingsManager.CameraManageSettings.GetValue(selectedCameraName,default(object)));
			m_enableButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue < 0;
			m_disableButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue >= 0 && selectedCameraName != "Game.FppCamera" && enabledCount > 2;//至少保留2个摄像机，当现存小于等于2个时无法点击禁用按钮
			m_upButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue > 0;
			m_downButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue >= 0 && selectedItemValue < enabledCount - 1;
			foreach(var key in m_widgetsByString.Keys)
			 {
				LabelWidget labelWidget = m_widgetsByString[key].Children.Find<LabelWidget>("BoundKey");
				bool enable = Convert.ToInt32(SettingsManager.CameraManageSettings.GetValue(key,default(object))) >= 0;
				//labelWidget.Text = SettingsManager.CameraManageSettings.GetValue(key,default(object)).ToString();
				labelWidget.Text = LanguageControl.Get("ContentWidgets","CameraManageScreen",enable ? "Enabled" : "Disabled");
				labelWidget.Color = enable ? Color.White : Color.Gray;
			}
			if(m_disableButton.IsClicked)
			{
				SettingsManager.CameraManageSettings[selectedCameraName] = -1;//-1表示禁用
				RefreshList();
			}
			if(m_enableButton.IsClicked)
			{
				SettingsManager.CameraManageSettings[selectedCameraName] = enabledCount;//将禁用的启用后自动放在最后面
				RefreshList();
				m_camerasList.SelectedIndex = SettingsManager.CameraManageSettings.Count - 1;//列表自动选中最后一个
			}
			if(m_upButton.IsClicked)
			{
				foreach(var item in SettingsManager.CameraManageSettings)
				{//找到选中摄像机的上一个并将其序号进行替换
					string key = item.Key;
					if(Convert.ToInt32(SettingsManager.CameraManageSettings.GetValue(key,default(object))) == selectedItemValue - 1)
					{
						SettingsManager.CameraManageSettings[key] = selectedItemValue;
						break;
					}
				}
				SettingsManager.CameraManageSettings[selectedCameraName] = selectedItemValue - 1;
				int i = m_camerasList.SelectedIndex ?? 1;
				RefreshList();
				m_camerasList.SelectedIndex = i - 1;//刷新列表后重新选中
			}
			if(m_downButton.IsClicked)
			{
				foreach(var item in SettingsManager.CameraManageSettings)
				{//找到选中摄像机的下一个并将其序号进行替换
					string key = item.Key;
					if(Convert.ToInt32(SettingsManager.CameraManageSettings.GetValue(key,default(object))) == selectedItemValue + 1)
					{
						SettingsManager.CameraManageSettings[key] = selectedItemValue;
						break;
					}
				}
				SettingsManager.CameraManageSettings[selectedCameraName] = selectedItemValue + 1;
				int i = m_camerasList.SelectedIndex ?? -1;
				RefreshList();
				m_camerasList.SelectedIndex = i + 1;//刷新列表后重新选中
			}
			if (Children.Find<ButtonWidget>("TopBar.Back").IsClicked || Input.Back || Input.Cancel)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
		public override void Enter(object[] parameters)
		{
			RefreshList();
		}

		void RefreshList()
		{
			m_camerasList.ClearItems();
			var list = SettingsManager.CameraManageSettings.OrderBy(x => Convert.ToInt32(x.Value)).ToList();
			int num = 0;
			foreach(var item in list)
			{
				string name = item.Key;
				m_camerasList.AddItem(name);
				int value = Convert.ToInt32(item.Value);
				if(value >= 0)
				{//刷新列表时重新按顺序分配值，避免出现空缺
					SettingsManager.CameraManageSettings[name] = num;
					num++;
				}
			}
		}
	}
}

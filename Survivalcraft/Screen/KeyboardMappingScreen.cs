using Engine;
using Engine.Serialization;
using NAudio.Flac;
using System.Xml.Linq;
using Engine.Input;

namespace Game
{
	public class KeyboardMappingScreen : Screen
	{
		public const string fName = "KeyboardMappingScreen";
		public const string keyName = "KeyboardMappingScreenKeys";
		public Widget KeyInfoWidget(Object item)
		{
			XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
			var containerWidget = (ContainerWidget)LoadWidget(this,node,null);
			LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("Name");
			LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BoundKey");
			labelWidget.Text = LanguageControl.Get(fName, item.ToString());
			labelWidget2.Text = HumanReadableConverter.ConvertToString(SettingsManager.KeyboardMappingSettings.GetValue(item.ToString(), default(object)));
			m_widgetsByString[item.ToString()] = containerWidget;
			return containerWidget;
		}

		public ListPanelWidget m_keysList;
		public BevelledButtonWidget m_setKeyButton;
		public BevelledButtonWidget m_disableKeyButton;
		public bool IsWaitingForKeyInput = false;
		public Dictionary<string, ContainerWidget> m_widgetsByString = new Dictionary<string, ContainerWidget>();
		public KeyboardMappingScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/KeyboardMappingScreen");
			LoadContents(this, node);
			m_keysList = Children.Find<ListPanelWidget>("KeysList");
			m_keysList.ItemWidgetFactory = (Func<object,Widget>)Delegate.Combine(m_keysList.ItemWidgetFactory,KeyInfoWidget);
			m_keysList.ScrollPosition = 0f;
			m_keysList.ScrollSpeed = 0f;
			m_keysList.ItemClicked += (item) =>
			{
				if(m_keysList.SelectedItem == item)
				{
					m_keysList.SelectedItem = null;
				}
				else
				{
					m_keysList.SelectedItem = item;
				}
			};
			m_setKeyButton = Children.Find<BevelledButtonWidget>("SetKey");
			m_disableKeyButton = Children.Find<BevelledButtonWidget>("DisableKey");
		}

		public override void Update()
		{
			string selectedKeyName = m_keysList.SelectedItem?.ToString() ?? string.Empty;
			m_setKeyButton.IsEnabled = !string.IsNullOrEmpty(selectedKeyName);
			m_disableKeyButton.IsEnabled = !string.IsNullOrEmpty(selectedKeyName);
			if(Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
				return;
			}
			foreach(var key in m_widgetsByString.Keys)
			{
				LabelWidget labelWidget = m_widgetsByString[key].Children.Find<LabelWidget>("BoundKey");
				object value = SettingsManager.KeyboardMappingSettings.GetValue(key,default(object));
				if(value is Key valueKey && valueKey == Key.Null) labelWidget.Text = string.Empty;
				else
				{
					string text = LanguageControl.Get(keyName,HumanReadableConverter.ConvertToString(value));
					if(text.StartsWith(keyName + ":"))
					{
						text =  text.Substring((keyName + ":").Length);
					}
					labelWidget.Text = text;
				}
			}
			if(m_disableKeyButton.IsClicked)
			{
				SettingsManager.KeyboardMappingSettings[selectedKeyName] = Key.Null;
				IsWaitingForKeyInput = false;
			}
			if(IsWaitingForKeyInput)
			{
				m_setKeyButton.IsChecked = true;
				foreach(Key key in EnumUtils.GetEnumValues(typeof(Key)))
				{
					if(key != Key.Null && Input.IsKeyDown(key))
					{
						SettingsManager.KeyboardMappingSettings[selectedKeyName] = key;
						IsWaitingForKeyInput = false;
						return;
					}
				}
				foreach(MouseButton mouseButton in EnumUtils.GetEnumValues(typeof(MouseButton)))
				{
					if(Input.IsMouseButtonDown(mouseButton))
					{
						SettingsManager.KeyboardMappingSettings[selectedKeyName] = mouseButton;
						IsWaitingForKeyInput = false;
						return;
					}
				}
			}
			else
			{
				m_setKeyButton.IsChecked = false;
			}
			if(m_setKeyButton.IsClicked)
			{
				IsWaitingForKeyInput = true;
			}
			if (!IsWaitingForKeyInput && Input.Back || Input.Cancel)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
		public override void Enter(object[] parameters)
		{
			m_keysList.ClearItems();
			foreach(string keyName in SettingsManager.KeyboardMappingSettings.Keys)
			{
				m_keysList.AddItem(keyName);
			}
		}
	}
}

using Engine;
using Engine.Serialization;
using NAudio.Flac;
using System.Xml.Linq;
using Engine.Input;
using TemplatesDatabase;

namespace Game
{
	public class KeyboardMappingScreen : Screen
	{
		public Widget KeyInfoWidget(Object item)
		{
			XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
			node.SetAttributeValue("Name","KeyboardMappingItem_" + item.ToString());
			var containerWidget = (ContainerWidget)LoadWidget(this,node,null);
			LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("Name");
			LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BoundKey");
			labelWidget.Text = LanguageControl.Get(fName,item.ToString());
			labelWidget2.Text = HumanReadableConverter.ConvertToString(SettingsManager.GetKeyboardMapping(item.ToString()));
			m_widgetsByString[item.ToString()] = containerWidget;
			return containerWidget;
		}

		public const string fName = "KeyboardMappingScreen";
		public const string keyName = "KeyboardMappingScreenKeys";

		public ListPanelWidget m_keysList;
		public BevelledButtonWidget m_resetButton;
		public BevelledButtonWidget m_setKeyButton;
		public BevelledButtonWidget m_disableKeyButton;
		public bool IsWaitingForKeyInput = false;
		public Dictionary<string, ContainerWidget> m_widgetsByString = new Dictionary<string, ContainerWidget>();
		public Dictionary<object, List<string>> m_conflicts = new Dictionary<object,List<string>>();
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
				m_keysList.SelectedItem = m_keysList.SelectedItem == item ? null : item;
			};
			m_resetButton = Children.Find<BevelledButtonWidget>("Reset");
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
				object value = SettingsManager.GetKeyboardMapping(key);
				if(value is Key valueKey && valueKey == Key.Null) labelWidget.Text = string.Empty;
				else
				{
					string text = LanguageControl.Get(keyName,HumanReadableConverter.ConvertToString(value));
					if(text.StartsWith(keyName + ":"))
					{
						text =  text.Substring((keyName + ":").Length);
					}
					labelWidget.Text = text;
					bool hasConflict = false;
					if(m_conflicts.TryGetValue(value,out List<string> valueList))
						hasConflict = KeyCompatibleGroupsManager.HasConflict(valueList);
					labelWidget.Color = hasConflict ? Color.Red : Color.White;
				}
			}
			if(m_disableKeyButton.IsClicked)
			{
				SetKeyboardMapping(selectedKeyName,Key.Null);
				IsWaitingForKeyInput = false;
			}
			if(m_resetButton.IsClicked)
			{
				MessageDialog dialog = new MessageDialog(LanguageControl.Get("ContentWidgets",fName,"ResetTitle"),
					LanguageControl.Get("ContentWidgets",fName,"ResetText"),LanguageControl.Yes,LanguageControl.No,
					delegate (MessageDialogButton button)
					{
						if(button == MessageDialogButton.Button1)
						{//重设所有按键
							ResetAll();
						}
					});
				DialogsManager.ShowDialog(null,dialog);
				IsWaitingForKeyInput = false;
			}
			if(IsWaitingForKeyInput)
			{
				m_setKeyButton.IsChecked = true;
				if(Input.Back || Input.Cancel)
				{
					IsWaitingForKeyInput = false;
					return;
				}
				foreach(Key key in EnumUtils.GetEnumValues(typeof(Key)))
				{
					if(key != Key.Null && Input.IsKeyDown(key))
					{
						SetKeyboardMapping(selectedKeyName,key);
						IsWaitingForKeyInput = false;
						return;
					}
				}
				foreach(MouseButton mouseButton in EnumUtils.GetEnumValues(typeof(MouseButton)))
				{
					if(Input.IsMouseButtonDown(mouseButton))
					{
						SetKeyboardMapping(selectedKeyName,mouseButton);
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
			if (!IsWaitingForKeyInput && (Input.Back || Input.Cancel))
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
		public override void Enter(object[] parameters)
		{
			m_keysList.ClearItems();
			foreach(string keyName in ModSettingsManager.CombinedKeyboardMappingSettings.Keys)
			{
				m_keysList.AddItem(keyName);
			}
			RefreshConflicts();
		}

		public void SetKeyboardMapping(string keyName,object value)
		{
			SettingsManager.SetKeyboardMapping(keyName, value);
			RefreshConflicts();
		}
		public void ResetAll()
		{
			SettingsManager.InitializeKeyboardMappingSettings();
			ModSettingsManager.ResetModsKeyboardMappingSettings();
			RefreshConflicts();
		}
		public void RefreshConflicts()
		{
			m_conflicts.Clear();
			foreach(var item in ModSettingsManager.CombinedKeyboardMappingSettings)
			{
				string name = item.Key;
				object obj = item.Value;
				if(!m_conflicts.TryGetValue(obj,out List<string> value))
				{
					value = new List<string>();
					m_conflicts[obj] = value;
				}
				if(!value.Contains(name))
					value.Add(name);
			}
		}
	}
}

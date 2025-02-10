using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Game
{
	public class ContentScreen : Screen
	{
		public static string fName = "ContentScreen";

		public ButtonWidget m_externalContentButton;

		public ButtonWidget m_communityContentButton;

		public ButtonWidget m_originalCommunityContentButton;

		public ButtonWidget m_linkButton;

		public ButtonWidget m_manageButton;

		public bool m_isAdmin;
		public ContentScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/ContentScreen");
			LoadContents(this, node);
			m_externalContentButton = Children.Find<ButtonWidget>("External");
			m_communityContentButton = Children.Find<ButtonWidget>("Community");
			m_originalCommunityContentButton = Children.Find<ButtonWidget>("OriginalCommunity");
			m_linkButton = Children.Find<ButtonWidget>("Link");
			m_manageButton = Children.Find<BevelledButtonWidget>("Manage");
		}

		public override void Enter(object[] parameters)
		{
			base.Enter(parameters);
			CommunityContentManager.IsAdmin(new CancellableProgress(), delegate (bool isAdmin)
			{
				m_isAdmin = isAdmin;
			}, delegate (Exception e)
			{
			});
		}
		public void OpenManageSelectDialog()
		{
			List<string> list = [LanguageControl.Get(fName,1),LanguageControl.Get(fName,2)];
			if(m_isAdmin)
			{
				list = [LanguageControl.Get(fName,1),LanguageControl.Get(fName,2),"用户管理"];
			}
			DialogsManager.ShowDialog(null,new ListSelectionDialog(null,list,70f,(object item) => (string)item,delegate (object item)
			{
				string selectionResult = (string)item;
				if(selectionResult == LanguageControl.Get(fName,1))
				{
					ScreensManager.SwitchScreen("ModsManageContent");
				}
				else if(selectionResult == LanguageControl.Get(fName,2))
				{
					ScreensManager.SwitchScreen("ManageContent");
				}
				else
				{
					ScreensManager.SwitchScreen("ManageUser");
				}
			}));
		}
		public override void Update()
		{
			m_communityContentButton.IsEnabled = SettingsManager.CommunityContentMode != CommunityContentMode.Disabled;
			m_originalCommunityContentButton.IsEnabled = SettingsManager.OriginalCommunityContentMode != CommunityContentMode.Disabled;
			if (m_externalContentButton.IsClicked)
			{
				ScreensManager.SwitchScreen("ExternalContent");
			}
			if (m_communityContentButton.IsClicked)
			{
				ScreensManager.SwitchScreen("CommunityContent");
			}
			if (m_originalCommunityContentButton.IsClicked)
			{
				ScreensManager.SwitchScreen("OriginalCommunityContent");
			}
			if (m_linkButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new DownloadContentFromLinkDialog());
			}
			if (m_manageButton.IsClicked)
			{
				OpenManageSelectDialog();
			}
			if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen("MainMenu");
			}
		}
	}
}
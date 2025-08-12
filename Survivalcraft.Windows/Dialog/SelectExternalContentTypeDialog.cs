using System;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class SelectExternalContentTypeDialog : ListSelectionDialog
	{
		public SelectExternalContentTypeDialog(string title, Action<ExternalContentType> selectionHandler)
			: base(title, from v in EnumUtils.GetEnumValues(typeof(ExternalContentType))
						  where ExternalContentManager.IsEntryTypeDownloadSupported((ExternalContentType)v)
						  select v, 64f, delegate (object item)
					  {
						  var type = (ExternalContentType)item;
						  XElement node = ContentManager.Get<XElement>("Widgets/SelectExternalContentTypeItem");
						  var obj = (ContainerWidget)LoadWidget(null, node, null);
						  obj.Children.Find<RectangleWidget>("SelectExternalContentType.Icon").Subtexture = ExternalContentManager.GetEntryTypeIcon(type);
						  obj.Children.Find<LabelWidget>("SelectExternalContentType.Text").Text = ExternalContentManager.GetEntryTypeDescription(type);
						  return obj;
					  }, delegate (object item)
					  {
						  selectionHandler((ExternalContentType)item);
					  })
		{
		}
	}
}

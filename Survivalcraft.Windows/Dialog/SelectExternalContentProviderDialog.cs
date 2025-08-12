using Engine;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class SelectExternalContentProviderDialog : ListSelectionDialog
	{
		public SelectExternalContentProviderDialog(string title, bool listingSupportRequired, Action<IExternalContentProvider> selectionHandler)
			: base(title, ExternalContentManager.Providers.Where((IExternalContentProvider p) => !listingSupportRequired || p.SupportsListing), 100f, delegate (object item)
			{
				var externalContentProvider = (IExternalContentProvider)item;
				XElement node = ContentManager.Get<XElement>("Widgets/SelectExternalContentProviderItem");
				var obj = (ContainerWidget)LoadWidget(null, node, null);
				obj.Children.Find<LabelWidget>("SelectExternalContentProvider.Text").Text = externalContentProvider.DisplayName;
				obj.Children.Find<LabelWidget>("SelectExternalContentProvider.Details").Text = externalContentProvider.Description;
				return obj;
			}, delegate (object item)
			{
				selectionHandler((IExternalContentProvider)item);
			})
		{
			ContentSize = new Vector2(700f, ContentSize.Y);
		}
	}
}

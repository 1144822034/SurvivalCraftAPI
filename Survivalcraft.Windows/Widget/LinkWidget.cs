using Engine;
using Engine.Graphics;
using Engine.Media;
using System.Xml.Linq;

namespace Game
{
	public class LinkWidget : FixedSizePanelWidget
	{
		public LabelWidget m_labelWidget;

		public ClickableWidget m_clickableWidget;

		public Vector2 Size
		{
			get
			{
				return m_labelWidget.Size;
			}
			set
			{
				m_labelWidget.Size = value;
			}
		}

		public bool IsClicked => m_clickableWidget.IsClicked;

		public bool IsPressed => m_clickableWidget.IsPressed;

		public string Text
		{
			get
			{
				return m_labelWidget.Text;
			}
			set
			{
				m_labelWidget.Text = value;
			}
		}

		public float FontScale
		{
			get { return m_labelWidget.FontScale; }
			set { m_labelWidget.FontScale = value; }
		}

		public TextAnchor TextAnchor
		{
			get
			{
				return m_labelWidget.TextAnchor;
			}
			set
			{
				m_labelWidget.TextAnchor = value;
			}
		}

		public BitmapFont Font
		{
			get
			{
				return m_labelWidget.Font;
			}
			set
			{
				m_labelWidget.Font = value;
			}
		}

		public Color Color
		{
			get
			{
				return m_labelWidget.Color;
			}
			set
			{
				m_labelWidget.Color = value;
			}
		}

		public bool DropShadow
		{
			get
			{
				return m_labelWidget.DropShadow;
			}
			set
			{
				m_labelWidget.DropShadow = value;
			}
		}

		public string Url
		{
			get;
			set;
		}

		public LinkWidget()
		{
			XElement node = ContentManager.Get<XElement>("Widgets/LinkContents");
			LoadChildren(this, node);
			m_labelWidget = Children.Find<LabelWidget>("Label");
			m_clickableWidget = Children.Find<ClickableWidget>("Clickable");
			LoadProperties(this, node);
		}

		public override void Update()
		{
			if (!string.IsNullOrEmpty(Url) && IsClicked)
			{
				WebBrowserManager.LaunchBrowser(Url);
			}
		}
	}
}

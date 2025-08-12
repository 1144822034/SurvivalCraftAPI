using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;

namespace Game
{
	public class ListPanelWidget : ScrollPanelWidget
	{
		public List<object> m_items = [];

		public int? m_selectedItemIndex;

		public Dictionary<int, Widget> m_widgetsByIndex = [];

		public int m_firstVisibleIndex;

		public int m_lastVisibleIndex;

		public bool PlayClickSound = true;

		public float m_itemSize;

		public bool m_widgetsDirty;

		public bool m_clickAllowed;

		public Vector2 lastActualSize = new(-1f);

		public Func<object, Widget> ItemWidgetFactory
		{
			get;
			set;
		}

		public override LayoutDirection Direction
		{
			get
			{
				return base.Direction;
			}
			set
			{
				if (value != Direction)
				{
					base.Direction = value;
					m_widgetsDirty = true;
				}
			}
		}

		public override float ScrollPosition
		{
			get
			{
				return base.ScrollPosition;
			}
			set
			{
				if (value != ScrollPosition)
				{
					base.ScrollPosition = value;
					m_widgetsDirty = true;
				}
			}
		}

		public float ItemSize
		{
			get
			{
				return m_itemSize;
			}
			set
			{
				if (value != m_itemSize)
				{
					m_itemSize = value;
					m_widgetsDirty = true;
				}
			}
		}

		public int? SelectedIndex
		{
			get
			{
				return m_selectedItemIndex;
			}
			set
			{
				if (value.HasValue && (value.Value < 0 || value.Value >= m_items.Count))
				{
					value = null;
				}
				if (value != m_selectedItemIndex)
				{
					m_selectedItemIndex = value;
					SelectionChanged?.Invoke();
				}
			}
		}

		public object SelectedItem
		{
			get
			{
				if (!m_selectedItemIndex.HasValue)
				{
					return null;
				}
				return m_items[m_selectedItemIndex.Value];
			}
			set
			{
				int num = m_items.IndexOf(value);
				SelectedIndex = (num >= 0) ? new int?(num) : null;
			}
		}

		public ReadOnlyList<object> Items => new(m_items);

		public Color SelectionColor
		{
			get;
			set;
		}

		public virtual Action<object> ItemClicked { get; set; }

		public virtual Action SelectionChanged { get; set; }

		public ListPanelWidget()
		{
			SelectionColor = Color.Gray;
			ItemWidgetFactory = (object item) => new LabelWidget
			{
				Text = (item != null) ? item.ToString() : string.Empty,
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			};
			ItemSize = 48f;
		}

		public void AddItem(object item)
		{
			m_items.Add(item);
			m_widgetsDirty = true;
		}

		public void AddItems(IEnumerable<object> items)
		{
			m_items.AddRange(items);
			m_widgetsDirty = true;
		}

		public void RemoveItem(object item)
		{
			int num = m_items.IndexOf(item);
			if (num >= 0)
			{
				RemoveItemAt(num);
			}
		}

		public void RemoveItemAt(int index)
		{
			_ = m_items[index];
			m_items.RemoveAt(index);
			m_widgetsByIndex.Clear();
			m_widgetsDirty = true;
			if (index == SelectedIndex)
			{
				SelectedIndex = null;
			}
		}

		public void ClearItems()
		{
			m_items.Clear();
			m_widgetsByIndex.Clear();
			m_widgetsDirty = true;
			SelectedIndex = null;
		}

		public override float CalculateScrollAreaLength()
		{
			return Items.Count * ItemSize;
		}

		public void ScrollToItem(object item)
		{
			int num = m_items.IndexOf(item);
			if (num >= 0)
			{
				float num2 = num * ItemSize;
				float num3 = (Direction == LayoutDirection.Horizontal) ? ActualSize.X : ActualSize.Y;
				if (num2 < ScrollPosition)
				{
					ScrollPosition = num2;
				}
				else if (num2 > ScrollPosition + num3 - ItemSize)
				{
					ScrollPosition = num2 - num3 + ItemSize;
				}
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			IsDrawRequired = true;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					if (Direction == LayoutDirection.Horizontal)
					{
						child.Measure(new Vector2(ItemSize, MathUtils.Max(parentAvailableSize.Y - (2f * child.Margin.Y), 0f)));
					}
					else
					{
						child.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - (2f * child.Margin.X), 0f), ItemSize));
					}
				}
			}
			if (m_widgetsDirty)
			{
				m_widgetsDirty = false;
				CreateListWidgets((Direction == LayoutDirection.Horizontal) ? ActualSize.X : ActualSize.Y);
			}
		}

		public override void ArrangeOverride()
		{
			if (ActualSize != lastActualSize)
			{
				m_widgetsDirty = true;
			}
			lastActualSize = ActualSize;
			int num = m_firstVisibleIndex;
			foreach (Widget child in Children)
			{
				if (Direction == LayoutDirection.Horizontal)
				{
					var vector = new Vector2((num * ItemSize) - ScrollPosition, 0f);
					ArrangeChildWidgetInCell(vector, vector + new Vector2(ItemSize, ActualSize.Y), child);
				}
				else
				{
					var vector2 = new Vector2(0f, (num * ItemSize) - ScrollPosition);
					ArrangeChildWidgetInCell(vector2, vector2 + new Vector2(ActualSize.X, ItemSize), child);
				}
				num++;
			}
		}

		public override void Update()
		{
			bool flag = ScrollSpeed != 0f;
			base.Update();
			if (Input.Tap.HasValue && HitTestPanel(Input.Tap.Value))
			{
				m_clickAllowed = !flag;
			}
			if (Input.Click.HasValue && m_clickAllowed && HitTestPanel(Input.Click.Value.Start) && HitTestPanel(Input.Click.Value.End))
			{
				int num = PositionToItemIndex(Input.Click.Value.End);
				if (ItemClicked != null && num >= 0 && num < m_items.Count)
				{
					ItemClicked(Items[num]);
				}
				SelectedIndex = num;
				if (SelectedIndex.HasValue && PlayClickSound)
				{
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
			}
		}

		public override void Draw(DrawContext dc)
		{
			if (SelectedIndex.HasValue && SelectedIndex.Value >= m_firstVisibleIndex && SelectedIndex.Value <= m_lastVisibleIndex)
			{
				Vector2 vector = (Direction == LayoutDirection.Horizontal) ? new Vector2((SelectedIndex.Value * ItemSize) - ScrollPosition, 0f) : new Vector2(0f, (SelectedIndex.Value * ItemSize) - ScrollPosition);
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				int count = flatBatch2D.TriangleVertices.Count;
				Vector2 v = (Direction == LayoutDirection.Horizontal) ? new Vector2(ItemSize, ActualSize.Y) : new Vector2(ActualSize.X, ItemSize);
				flatBatch2D.QueueQuad(vector, vector + v, 0f, SelectionColor * GlobalColorTransform);
				flatBatch2D.TransformTriangles(GlobalTransform, count);
			}
			base.Draw(dc);
		}

		public int PositionToItemIndex(Vector2 position)
		{
			Vector2 vector = ScreenToWidget(position);
			if (Direction == LayoutDirection.Horizontal)
			{
				return (int)((vector.X + ScrollPosition) / ItemSize);
			}
			return (int)((vector.Y + ScrollPosition) / ItemSize);
		}

		public void CreateListWidgets(float size)
		{
			Children.Clear();
			if (m_items.Count <= 0)
			{
				return;
			}
			int x = (int)MathF.Floor(ScrollPosition / ItemSize);
			int x2 = (int)MathF.Floor((ScrollPosition + size) / ItemSize);
			m_firstVisibleIndex = MathUtils.Max(x, 0);
			m_lastVisibleIndex = MathUtils.Min(x2, m_items.Count - 1);
			for (int i = m_firstVisibleIndex; i <= m_lastVisibleIndex; i++)
			{
				object obj = m_items[i];
				if (!m_widgetsByIndex.TryGetValue(i, out Widget value))
				{
					value = ItemWidgetFactory(obj);
					value.Tag = obj;
					m_widgetsByIndex.Add(i, value);
				}
				Children.Add(value);
			}
		}
	}
}

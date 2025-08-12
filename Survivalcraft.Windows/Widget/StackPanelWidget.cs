using Engine;

namespace Game
{
	public class StackPanelWidget : ContainerWidget
	{
		public float m_fixedSize;

		public int m_fillCount;

		public LayoutDirection Direction
		{
			get;
			set;
		}

		public bool IsInverted
		{
			get;
			set;
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			m_fixedSize = 0f;
			m_fillCount = 0;
			float num = 0f;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					child.Measure(Vector2.Max(parentAvailableSize - (2f * child.Margin), Vector2.Zero));
					if (Direction == LayoutDirection.Horizontal)
					{
						if (child.ParentDesiredSize.X != float.PositiveInfinity)
						{
							m_fixedSize += child.ParentDesiredSize.X + (2f * child.Margin.X);
							parentAvailableSize.X = MathUtils.Max(parentAvailableSize.X - (child.ParentDesiredSize.X + (2f * child.Margin.X)), 0f);
						}
						else
						{
							m_fillCount++;
						}
						num = MathUtils.Max(num, child.ParentDesiredSize.Y + (2f * child.Margin.Y));
					}
					else
					{
						if (child.ParentDesiredSize.Y != float.PositiveInfinity)
						{
							m_fixedSize += child.ParentDesiredSize.Y + (2f * child.Margin.Y);
							parentAvailableSize.Y = MathUtils.Max(parentAvailableSize.Y - (child.ParentDesiredSize.Y + (2f * child.Margin.Y)), 0f);
						}
						else
						{
							m_fillCount++;
						}
						num = MathUtils.Max(num, child.ParentDesiredSize.X + (2f * child.Margin.X));
					}
				}
			}
			if (Direction == LayoutDirection.Horizontal)
			{
				DesiredSize = m_fillCount == 0 ? new Vector2(m_fixedSize, num) : new Vector2(float.PositiveInfinity, num);
			}
			else
			{
				DesiredSize = m_fillCount == 0 ? new Vector2(num, m_fixedSize) : new Vector2(num, float.PositiveInfinity);
			}
		}

		public override void ArrangeOverride()
		{
			float num = 0f;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					if (Direction == LayoutDirection.Horizontal)
					{
						float num2 = (child.ParentDesiredSize.X == float.PositiveInfinity) ? ((m_fillCount > 0) ? (MathUtils.Max(ActualSize.X - m_fixedSize, 0f) / m_fillCount) : 0f) : (child.ParentDesiredSize.X + (2f * child.Margin.X));
						Vector2 c;
						Vector2 c2;
						if (!IsInverted)
						{
							c = new Vector2(num, 0f);
							c2 = new Vector2(num + num2, ActualSize.Y);
						}
						else
						{
							c = new Vector2(ActualSize.X - (num + num2), 0f);
							c2 = new Vector2(ActualSize.X - num, ActualSize.Y);
						}
						ArrangeChildWidgetInCell(c, c2, child);
						num += num2;
					}
					else
					{
						float num3 = (child.ParentDesiredSize.Y == float.PositiveInfinity) ? ((m_fillCount > 0) ? (MathUtils.Max(ActualSize.Y - m_fixedSize, 0f) / m_fillCount) : 0f) : (child.ParentDesiredSize.Y + (2f * child.Margin.Y));
						Vector2 c3;
						Vector2 c4;
						if (!IsInverted)
						{
							c3 = new Vector2(0f, num);
							c4 = new Vector2(ActualSize.X, num + num3);
						}
						else
						{
							c3 = new Vector2(0f, ActualSize.Y - (num + num3));
							c4 = new Vector2(ActualSize.X, ActualSize.Y - num);
						}
						ArrangeChildWidgetInCell(c3, c4, child);
						num += num3;
					}
				}
			}
		}
	}
}

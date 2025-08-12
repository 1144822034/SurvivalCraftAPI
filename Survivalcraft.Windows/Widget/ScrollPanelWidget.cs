using Engine;
using Engine.Graphics;

namespace Game
{
	public class ScrollPanelWidget : ContainerWidget
	{
		public Vector2? m_lastDragPosition;

		public float m_dragSpeed;

		public float m_scrollBarAlpha;

		public float m_scrollAreaLength;

		public virtual LayoutDirection Direction
		{
			get;
			set;
		}

		public virtual float ScrollPosition
		{
			get;
			set;
		}

		public virtual float ScrollSpeed
		{
			get;
			set;
		}

		public ScrollPanelWidget()
		{
			ClampToBounds = true;
			StartInitialScroll();
		}

		public void StartInitialScroll()
		{
			ScrollPosition = 12f;
			ScrollSpeed = -70f;
		}

		public virtual float CalculateScrollAreaLength()
		{
			float num = 0f;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					num = (Direction != 0) ? MathUtils.Max(num, child.ParentDesiredSize.Y + (2f * child.Margin.Y)) : MathUtils.Max(num, child.ParentDesiredSize.X + (2f * child.Margin.X));
				}
			}
			return num;
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
						child.Measure(new Vector2(float.MaxValue, MathUtils.Max(parentAvailableSize.Y - (2f * child.Margin.Y), 0f)));
					}
					else
					{
						child.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - (2f * child.Margin.X), 0f), float.MaxValue));
					}
				}
			}
		}

		public override void ArrangeOverride()
		{
			foreach (Widget child in Children)
			{
				Vector2 zero = Vector2.Zero;
				Vector2 actualSize = ActualSize;
				if (Direction == LayoutDirection.Horizontal)
				{
					zero.X -= ScrollPosition;
					actualSize.X = zero.X + child.ParentDesiredSize.X;
				}
				else
				{
					zero.Y -= ScrollPosition;
					actualSize.Y = zero.Y + child.ParentDesiredSize.Y;
				}
				ArrangeChildWidgetInCell(zero, actualSize, child);
			}
		}

		public override void Update()
		{
			float num = 50f;
			m_scrollAreaLength = CalculateScrollAreaLength();
			m_scrollBarAlpha = MathUtils.Max(m_scrollBarAlpha - (2f * Time.FrameDuration), 0f);
			if (Input.Tap.HasValue && HitTestPanel(Input.Tap.Value))
			{
				m_lastDragPosition = ScreenToWidget(Input.Tap.Value);
			}
			if (m_lastDragPosition.HasValue)
			{
				if (Input.Press.HasValue)
				{
					float num2 = 0f;
					Vector2 vector = ScreenToWidget(Input.Press.Value);
					Vector2 vector2 = vector - m_lastDragPosition.Value;
					if (Direction == LayoutDirection.Horizontal)
					{
						ScrollPosition += 0f - vector2.X;
						num2 = vector2.X / Time.FrameDuration;
					}
					else
					{
						ScrollPosition += 0f - vector2.Y;
						num2 = vector2.Y / Time.FrameDuration;
					}
					float num3 = (MathF.Abs(num2) < MathF.Abs(m_dragSpeed)) ? 20f : 16f;
					m_dragSpeed += MathUtils.Saturate(num3 * Time.FrameDuration) * (num2 - m_dragSpeed);
					m_scrollBarAlpha = 4f;
					m_lastDragPosition = vector;
					ScrollSpeed = 0f;
				}
				else
				{
					ScrollSpeed = 0f - m_dragSpeed;
					m_dragSpeed = 0f;
					m_lastDragPosition = null;
				}
			}
			if (ScrollSpeed != 0f)
			{
				ScrollSpeed *= MathF.Pow(0.33f, Time.FrameDuration);
				if (MathF.Abs(ScrollSpeed) < 40f)
				{
					ScrollSpeed = 0f;
				}
				ScrollPosition += ScrollSpeed * Time.FrameDuration;
				m_scrollBarAlpha = 3f;
			}
			if (Input.Scroll.HasValue && HitTestPanel(Input.Scroll.Value.XY))
			{
				ScrollPosition -= 40f * Input.Scroll.Value.Z;
				ScrollSpeed = 0f;
				num = 0f;
				m_scrollBarAlpha = 3f;
			}
			float num4 = MathUtils.Max(m_scrollAreaLength - ActualSize.Y, 0f);
			if (ScrollPosition < 0f)
			{
				if (!m_lastDragPosition.HasValue)
				{
					ScrollPosition = MathUtils.Min(ScrollPosition + (6f * Time.FrameDuration * (0f - ScrollPosition + 5f)), 0f);
				}
				ScrollPosition = MathUtils.Max(ScrollPosition, 0f - num);
				ScrollSpeed = 0f;
			}
			if (ScrollPosition > num4)
			{
				if (!m_lastDragPosition.HasValue)
				{
					ScrollPosition = MathUtils.Max(ScrollPosition + (6f * Time.FrameDuration * (num4 - ScrollPosition - 5f)), num4);
				}
				ScrollPosition = MathUtils.Min(ScrollPosition, num4 + num);
				ScrollSpeed = 0f;
			}
			if (m_lastDragPosition.HasValue && (Input.Drag.HasValue || Input.Hold.HasValue))
			{
				Input.Clear();
			}
		}

		public override void Draw(DrawContext dc)
		{
			Color color = new Color((byte)80, (byte)80, (byte)80) * GlobalColorTransform * MathUtils.Saturate(m_scrollBarAlpha);
			if (color.A > 0 && m_scrollAreaLength > 0f)
			{
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				int count = flatBatch2D.TriangleVertices.Count;
				if (Direction == LayoutDirection.Horizontal)
				{
					float scrollPosition = ScrollPosition;
					float x = ActualSize.X;
					var corner = new Vector2(scrollPosition / m_scrollAreaLength * x, ActualSize.Y - 5f);
					var corner2 = new Vector2((scrollPosition + x) / m_scrollAreaLength * x, ActualSize.Y - 1f);
					flatBatch2D.QueueQuad(corner, corner2, 0f, color);
				}
				else
				{
					float scrollPosition2 = ScrollPosition;
					float y = ActualSize.Y;
					var corner3 = new Vector2(ActualSize.X - 5f, scrollPosition2 / m_scrollAreaLength * y);
					var corner4 = new Vector2(ActualSize.X - 1f, (scrollPosition2 + y) / m_scrollAreaLength * y);
					flatBatch2D.QueueQuad(corner3, corner4, 0f, color);
				}
				flatBatch2D.TransformTriangles(GlobalTransform, count);
			}
		}

		public bool HitTestPanel(Vector2 position)
		{
			bool found = false;
			HitTestGlobal(position, delegate (Widget widget)
			{
				found = widget.IsChildWidgetOf(this) || widget == this;
				return true;
			});
			return found;
		}
	}
}

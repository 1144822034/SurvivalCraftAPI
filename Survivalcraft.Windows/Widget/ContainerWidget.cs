using Engine;
using System.Collections.Generic;

namespace Game
{
	public abstract class ContainerWidget : Widget
	{
		public readonly WidgetsList Children;

		public IEnumerable<Widget> AllChildren
		{
			get
			{
				foreach (Widget childWidget in Children)
				{
					yield return childWidget;
					var containerWidget = childWidget as ContainerWidget;
					if (containerWidget != null)
					{
						foreach (Widget allChild in containerWidget.AllChildren)
						{
							yield return allChild;
						}
					}
				}
			}
		}

		public ContainerWidget()
		{
			Children = new WidgetsList(this);
		}

		public override void UpdateCeases()
		{
			foreach (Widget child in Children)
			{
				child.UpdateCeases();
			}
		}

		public void AddChildren(Widget widget) { if (Children.IndexOf(widget) < 0) Children.Add(widget); }
		public void RemoveChildren(Widget widget) { if (Children.IndexOf(widget) >= 0) Children.Remove(widget); }
		public void ClearChildren() { Children.Clear(); }

		public virtual void WidgetAdded(Widget widget)
		{
		}

		public virtual void WidgetRemoved(Widget widget)
		{
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			foreach (Widget child in Children)
			{
				child.Measure(Vector2.Max(parentAvailableSize - (2f * child.Margin), Vector2.Zero));
			}
		}

		public override void ArrangeOverride()
		{
			foreach (Widget child in Children)
			{
				ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
			}
		}
		public static void ArrangeChildWidgetInCell(Vector2 c1, Vector2 c2, Widget widget)
		{
			Vector2 zero = Vector2.Zero;
			Vector2 zero2 = Vector2.Zero;
			Vector2 vector = c2 - c1;
			Vector2 margin = widget.Margin;
			Vector2 parentDesiredSize = widget.ParentDesiredSize;
			if (float.IsPositiveInfinity(parentDesiredSize.X) || parentDesiredSize.X > vector.X - (2f * margin.X))
			{
				parentDesiredSize.X = MathUtils.Max(vector.X - (2f * margin.X), 0f);
			}
			if (float.IsPositiveInfinity(parentDesiredSize.Y) || parentDesiredSize.Y > vector.Y - (2f * margin.Y))
			{
				parentDesiredSize.Y = MathUtils.Max(vector.Y - (2f * margin.Y), 0f);
			}
			if (widget.HorizontalAlignment == WidgetAlignment.Near)
			{
				zero.X = c1.X + margin.X;
				zero2.X = parentDesiredSize.X;
			}
			else if (widget.HorizontalAlignment == WidgetAlignment.Center)
			{
				zero.X = c1.X + ((vector.X - parentDesiredSize.X) / 2f);
				zero2.X = parentDesiredSize.X;
			}
			else if (widget.HorizontalAlignment == WidgetAlignment.Far)
			{
				zero.X = c2.X - parentDesiredSize.X - margin.X;
				zero2.X = parentDesiredSize.X;
			}
			else if (widget.HorizontalAlignment == WidgetAlignment.Stretch)
			{
				zero.X = c1.X + margin.X;
				zero2.X = MathUtils.Max(vector.X - (2f * margin.X), 0f);
			}
			if (widget.VerticalAlignment == WidgetAlignment.Near)
			{
				zero.Y = c1.Y + margin.Y;
				zero2.Y = parentDesiredSize.Y;
			}
			else if (widget.VerticalAlignment == WidgetAlignment.Center)
			{
				zero.Y = c1.Y + ((vector.Y - parentDesiredSize.Y) / 2f);
				zero2.Y = parentDesiredSize.Y;
			}
			else if (widget.VerticalAlignment == WidgetAlignment.Far)
			{
				zero.Y = c2.Y - parentDesiredSize.Y - margin.Y;
				zero2.Y = parentDesiredSize.Y;
			}
			else if (widget.VerticalAlignment == WidgetAlignment.Stretch)
			{
				zero.Y = c1.Y + margin.Y;
				zero2.Y = MathUtils.Max(vector.Y - (2f * margin.Y), 0f);
			}
			widget.Arrange(zero, zero2);
		}

		public override void Dispose()
		{
			foreach (Widget child in Children)
			{
				child.Dispose();
			}
		}
	}
}

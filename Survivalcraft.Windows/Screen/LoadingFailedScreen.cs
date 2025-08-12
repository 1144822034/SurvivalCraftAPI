using Engine;
using Engine.Graphics;

namespace Game;

public class LoadingFailedScreen : Screen
{
    public LoadingFailedScreen(string title, IEnumerable<string> details, IEnumerable<string> solveMethods)
    {
        Children.Clear();
        Children.Add(new RectangleWidget()
        {
            Size = new Vector2(float.PositiveInfinity),
            FillColor = Color.Black,
            OutlineColor = Color.Black,
            OutlineThickness = 0
        });
        
        ScrollPanelWidget scrollPanelWidget = new() { HorizontalAlignment = WidgetAlignment.Center, Direction = LayoutDirection.Vertical };
        StackPanelWidget widget = new()
            { Direction = LayoutDirection.Vertical, HorizontalAlignment = WidgetAlignment.Center };
        
        scrollPanelWidget.Children.Add(widget);
        
        {
            widget.Children.Add(new LabelWidget
                { Text = title, FontScale = 2, Color = Color.Red, HorizontalAlignment = WidgetAlignment.Center, WordWrap = true });
            
            widget.Children.Add(new RectangleWidget()
            {
                ColorTransform = Color.Transparent,
                Size = new Vector2(float.PositiveInfinity, 80)
            });
            
            foreach (string detail in details)
            {
                widget.Children.Add(new LabelWidget { Text = detail, HorizontalAlignment = WidgetAlignment.Center, WordWrap = true });
            }
            
            widget.Children.Add(new RectangleWidget()
            {
                ColorTransform = Color.Transparent,
                Size = new Vector2(float.PositiveInfinity, 80)
            });
            
            widget.Children.Add(new LabelWidget { Text = "For solving this problem, please try: 要解决此问题，请尝试：", HorizontalAlignment = WidgetAlignment.Center, Color = Color.Green, WordWrap = true});
            foreach (string method in solveMethods)
            {
                widget.Children.Add(new LabelWidget { Text = method, HorizontalAlignment = WidgetAlignment.Center, WordWrap = true});
            }
        }
        Children.Add(scrollPanelWidget);
    }
}
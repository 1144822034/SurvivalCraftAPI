using CoreGraphics;
using Engine;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace SurvivalCraft.IOS {
    class TouchView : UIKit.UIView {
        private UIView parent;
        private float pixelScale;
        public TouchView(UIView parentView) {
            parent = parentView;
            Bounds = new CGRect(0, 0, Engine.Window.Size.X, Engine.Window.Size.Y);
            pixelScale = (float)Bounds.Width / (float)parent.Bounds.Width;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
            //禁止多个视图同时接收触摸
            ExclusiveTouch = false;
        }
        public override void TouchesBegan(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {                
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                int processId = cc.GetHashCode();
                Vector2 vector = new Vector2(x, y);
                Engine.Input.Touch.ProcessTouchPressed(processId, vector);
            }
        }
        public override void TouchesMoved(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                int processId = cc.GetHashCode();
                Vector2 vector = new Vector2(x, y);
                Engine.Input.Touch.ProcessTouchMoved(processId, vector);
            }
        }
        public override void TouchesEnded(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                int processId = cc.GetHashCode();
                Vector2 vector = new Vector2(x, y);
                Engine.Input.Touch.ProcessTouchReleased(processId, vector);
            }
        }
    }
}

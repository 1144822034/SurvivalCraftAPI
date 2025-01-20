#if ANDROID
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Engine.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using OpenTK.Platform.Android;

namespace Engine
{
    public class EngineView : AndroidGameView
    {
        public EngineView(Context context)
            : base(context)
        {
            RequestFocus();
            base.FocusableInTouchMode = true;
            EnableImmersiveMode();
        }

        protected override void CreateFrameBuffer()
        {
            base.ContextRenderingApi = GLVersion.ES3;
            GraphicsMode[] obj =
            [
                new GraphicsMode(16, 16, 0, 0, ColorFormat.Empty, 2),
                new AndroidGraphicsMode(16, 0, 0, 0, 0, stereo: false),
                new AndroidGraphicsMode(0, 4, 0, 0, 0, stereo: false)
            ];
            Exception ex = null;
            GraphicsMode[] array = obj;
            foreach (GraphicsMode graphicsMode in array)
            {
                try
                {
                    base.GraphicsMode = graphicsMode;
                    base.CreateFrameBuffer();
                    ex = null;
                }
                catch (Exception ex2)
                {
                    ex = ex2;
                    continue;
                }
                break;
            }
            if (ex != null)
            {
                throw new InvalidOperationException($"Error creating framebuffer, reason: {ex}");
            }
            GLWrapper.GL.GetInteger(GetPName.RedBits, out int @params);
            GLWrapper.GL.GetInteger(GetPName.GreenBits, out int params2);
            GLWrapper.GL.GetInteger(GetPName.BlueBits, out int params3);
            GLWrapper.GL.GetInteger(GetPName.AlphaBits, out int params4);
            GLWrapper.GL.GetInteger(GetPName.DepthBits, out int params5);
            GLWrapper.GL.GetInteger(GetPName.StencilBits, out int params6);
            GLWrapper.GL.GetInteger(GetPName.MajorVersion, out int params7);
            GLWrapper.GL.GetInteger(GetPName.MinorVersion, out int params8);
            GLWrapper.GL.GetInteger(GetPName.MaxTextureSize, out int params9);
            Log.Information("OpenGL{6}.{7} framebuffer created, R={0} G={1} B={2} A={3}, D={4} S={5}, MaxTextureSize={8}", @params, params2, params3, params4, params5, params6, params7, params8, params9);
        }

        public void EnableImmersiveMode()
        {
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)19)
            {
                try
                {
                    int value = 6150;
                    IntPtr methodID = JNIEnv.GetMethodID(base.Class.Handle, "setSystemUiVisibility", "(I)V");
                    JNIEnv.CallVoidMethod(base.Handle, methodID, new JValue(value));
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to enable immersive mode. Reason: {0}", ex.Message);
                }
            }
        }

        public override void MakeCurrent()
        {
            try
            {
                base.MakeCurrent();
            }
            catch
            {
            }
        }

        public override void OnWindowFocusChanged(bool hasWindowFocus)
        {
            base.OnWindowFocusChanged(hasWindowFocus);
            if (hasWindowFocus)
            {
                Window.m_focusRegained = true;
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                case Keycode.VolumeUp:
                    ((AudioManager)base.Context.GetSystemService("audio")).AdjustStreamVolume(Android.Media.Stream.Music, Adjust.Raise, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
                case Keycode.VolumeDown:
                    ((AudioManager)base.Context.GetSystemService("audio")).AdjustStreamVolume(Android.Media.Stream.Music, Adjust.Lower, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
            }
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                GamePad.HandleKeyDown(e.DeviceId, keyCode);
            }
            else
            {
                Keyboard.HandleKeyDown(keyCode);
                if (e.UnicodeChar != 0)
                {
                    Keyboard.HandleKeyPress(e.UnicodeChar);
                }
            }
            return true;
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                GamePad.HandleKeyUp(e.DeviceId, keyCode);
            }
            else
            {
                Keyboard.HandleKeyUp(keyCode);
            }
            return true;
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            if (((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick) && e.Action == MotionEventActions.Move)
            {
                GamePad.HandleMotionEvent(e);
                return true;
            }
            return base.OnGenericMotionEvent(e);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            Input.Touch.HandleTouchEvent(e);
            return true;
        }
        
        // TODO: 待完成。
        /*
        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            outAttrs.ImeOptions = ImeFlags.NoExtractUi;
            outAttrs.InputType = InputTypes.Null;
            return new EngineInputConnection(this, true);
        }
        
        public override bool OnCheckIsTextEditor()
        {
            return true;
        }
        */
    }
}
#endif
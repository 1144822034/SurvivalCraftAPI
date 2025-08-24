#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Views;
using Engine.Input;
using Silk.NET.Windowing.Sdl.Android;
using Debug = System.Diagnostics.Debug;
using Environment = System.Environment;
using Stream = Android.Media.Stream;
using Uri = Android.Net.Uri;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Engine
{

    public class EngineActivity : SilkActivity
    {
        internal static EngineActivity m_activity;

        public event Action Paused;

        public event Action Resumed;

        public event Action Destroyed;

        public event Action<Intent> NewIntent;

        public event Func<KeyEvent, bool> OnDispatchKeyEvent;
        
        public static string BasePath = RunPath.AndroidFilePath;
        public static string ConfigPath = RunPath.AndroidFilePath;

        public EngineActivity()
        {
            m_activity = this;
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(savedInstanceState);
            Window?.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            EnableImmersiveMode();
            VolumeControlStream = Stream.Music;
            RequestedOrientation = ScreenOrientation.SensorLandscape;
        }

        public void Vibrate(long ms)
        {
            Vibrator vibrator = (Vibrator)GetSystemService("vibrator");
            vibrator?.Vibrate(VibrationEffect.CreateOneShot(ms, VibrationEffect.DefaultAmplitude));
        }
        public void OpenLink(string link)
        {
            StartActivity(new Intent(Intent.ActionView, Uri.Parse(link)));
        }


        protected override void OnPause()
        {
            base.OnPause();
            Paused?.Invoke();
        }

        protected override void OnResume()
        {
            base.OnResume();
            Resumed?.Invoke();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            NewIntent?.Invoke(intent);
        }

        protected override void OnRun()
        {
        }

        protected override void OnDestroy()
        {
            try
            {
                base.OnDestroy();
                Destroyed?.Invoke();
            }
            finally
            {
                Thread.Sleep(250);
                Environment.Exit(0);
            }
        }

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            Touch.HandleTouchEvent(e);
            return true;
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e == null)
            {
                return true;
            }
            Debug.WriteLine($"[DispatchKeyEvent]action:{e.Action} keyCode:{e.KeyCode} unicodeChar:{e.UnicodeChar} flags:{e.Flags} metaState:{e.MetaState} source:{e.Source} deviceId:{e.DeviceId}");
            bool handled = false;
            Delegate[] invocationList = OnDispatchKeyEvent?.GetInvocationList();
            if (invocationList != null)
            {
                foreach (Delegate invocation in invocationList)
                {
                    handled |= (bool)invocation.DynamicInvoke(e)!;
                }
            }
            if (!handled)
            {
                _ = e.Action switch
                {
                    KeyEventActions.Down => OnKeyDown(e.KeyCode, e),
                    KeyEventActions.Up => OnKeyUp(e.KeyCode, e),
                    _ => false
                };
            }

            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                case Keycode.VolumeUp:
                    ((AudioManager)Context?.GetSystemService("audio"))?.AdjustStreamVolume(Stream.Music, Adjust.Raise, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
                case Keycode.VolumeDown:
                    ((AudioManager)Context?.GetSystemService("audio"))?.AdjustStreamVolume(Stream.Music, Adjust.Lower, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
            }
            if (e == null)
            {
                return true;
            }
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                GamePad.HandleKeyEvent(e);
            }
            else
            {
                Keyboard.HandleKeyEvent(e);
            }
            return true;
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if (e == null)
            {
                return true;
            }
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                GamePad.HandleKeyEvent(e);
            }
            else
            {
                Keyboard.HandleKeyEvent(e);
            }
            return true;
        }

        public override bool DispatchGenericMotionEvent(MotionEvent e)
        {
            if (e == null)
            {
                return true;
            }
            Debug.WriteLine($"[OnGenericMotionEvent]source:{e.Source} action:{e.Action}");
            if (((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick) && e.Action == MotionEventActions.Move)
            {
                GamePad.HandleMotionEvent(e);
            }
            if ((e.Source & InputSourceType.Mouse) == InputSourceType.Mouse || (e.Source & InputSourceType.ClassPointer) == InputSourceType.ClassPointer || (e.Source & InputSourceType.MouseRelative) == InputSourceType.MouseRelative)
            {
                Mouse.HandleMotionEvent(e);
            }
            return true;
        }

        public void EnableImmersiveMode()
        {
            if (Window != null)
            {
                switch (Build.VERSION.SdkInt)
                {
                    case >= (BuildVersionCodes)30:
                        IWindowInsetsController insetsController = Window.InsetsController;
                        if (insetsController != null)
                        {
                            insetsController.Hide(WindowInsets.Type.SystemBars());
                            insetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                        }
                        break;
                    case > (BuildVersionCodes)19:
                        Window.DecorView.SystemUiFlags = SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.Immersive | SystemUiFlags.ImmersiveSticky;
                        break;
                }
            }
        }

        public void GetGlEsVersion(out int major, out int minor)
        {
            try
            {
                int reqGlEsVersion = ((ActivityManager)GetSystemService(ActivityService))?.DeviceConfigurationInfo?.ReqGlEsVersion ?? 0x20000;
                major = reqGlEsVersion >> 16;
                minor = reqGlEsVersion & 0xFFFF;
            }
            catch
            {
                major = 2;
                minor = 0;
            }
        }
    }
}
#endif

#if ANDROID
using Android.Content;
using Android.OS;
#else
using System.Reflection;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
#endif
using System.Runtime.CompilerServices;
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;
using Silk.NET.Core;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Monitor = Silk.NET.Windowing.Monitor;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Engine
{
    public static class Window
    {
        private enum State
        {
            Uncreated,
            Inactive,
            Active
        }

        private static State m_state;
#if ANDROID
        public static bool m_contextLost;

        internal static bool m_focusRegained;

        public static int m_presentationInterval = 1;

        public static double m_frameStartTime;

        public static bool IsCreated => m_state != State.Uncreated;

        public static bool IsActive => m_state == State.Active;

        public static EngineActivity Activity => EngineActivity.m_activity;

        public static EngineView View
        {
            get;
            set;
        }

        public static Point2 ScreenSize => new(EngineActivity.m_activity.Resources.DisplayMetrics.WidthPixels, EngineActivity.m_activity.Resources.DisplayMetrics.HeightPixels);

        public static WindowMode WindowMode
        {
            get
            {
                VerifyWindowOpened();
                return WindowMode.Fullscreen;
            }
            set
            {
                VerifyWindowOpened();
            }
        }

        public static Point2 Position
        {
            get
            {
                VerifyWindowOpened();
                return Point2.Zero;
            }
            set
            {
                VerifyWindowOpened();
            }
        }

        public static Point2 Size
        {
            get
            {
                VerifyWindowOpened();
                return new Point2(View.Size.Width, View.Size.Height);
            }
            set
            {
                VerifyWindowOpened();
            }
        }

        public static string Title
        {
            get
            {
                VerifyWindowOpened();
                return string.Empty;
            }
            set
            {
                VerifyWindowOpened();
            }
        }

        public static int PresentationInterval
        {
            get
            {
                VerifyWindowOpened();
                return m_presentationInterval;
            }
            set
            {
                VerifyWindowOpened();
                m_presentationInterval = Math.Clamp(value, 1, 4);
            }
        }
#else
        public static IWindow m_gameWindow;

        public static IInputContext m_inputContext;

        private static bool m_closing;

        public static float m_dpiScale;

        private static int? m_swapInterval;


        public static string m_titlePrefix = string.Empty;

        public static string m_titleSuffix = string.Empty;

        public static Point2 ScreenSize
        {
            get
            {
                var size = m_gameWindow?.Monitor?.Bounds.Size ?? Monitor.GetMainMonitor(null).Bounds.Size;
                return new Point2(size.X, size.Y);
            }
        }

        public static WindowMode WindowMode
        {
            get
            {
                VerifyWindowOpened();
                return m_gameWindow.WindowState == WindowState.Fullscreen
                    ? WindowMode.Fullscreen
                    : m_gameWindow.WindowBorder != 0 ? WindowMode.Fixed : WindowMode.Resizable;
            }
            set
            {
                VerifyWindowOpened();
                switch (value)
                {
                    case WindowMode.Fixed:
                        m_gameWindow.WindowBorder = WindowBorder.Fixed;
                        if (m_gameWindow.WindowState == WindowState.Fullscreen)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Resizable:
                        m_gameWindow.WindowBorder = WindowBorder.Resizable;
                        if (m_gameWindow.WindowState == WindowState.Fullscreen)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Borderless:
                        m_gameWindow.WindowBorder = WindowBorder.Hidden;
                        if (m_gameWindow.WindowState == WindowState.Fullscreen)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Fullscreen:
                        m_gameWindow.WindowState = WindowState.Fullscreen;
                        break;
                }
            }
        }

        public static Point2 Position
        {
            get
            {
                VerifyWindowOpened();
                return new Point2(m_gameWindow.Position.X, m_gameWindow.Position.Y);
            }
            set
            {
                VerifyWindowOpened();
                m_gameWindow.Position = new (value.X, value.Y);
            }
        }

        public static Point2 Size
        {
            get
            {
                VerifyWindowOpened();
                return new Point2(m_gameWindow.Size.X, m_gameWindow.Size.Y);
            }
            set
            {
                VerifyWindowOpened();
                m_gameWindow.Size = new (value.X, value.Y);
            }
        }


        public static string TitlePrefix
        {
            get
            {
                VerifyWindowOpened();
                return m_titlePrefix;
            }
            set
            {
                VerifyWindowOpened();
                m_titlePrefix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
            }
        }

        public static string TitleSuffix
        {
            get
            {
                VerifyWindowOpened();
                return m_titleSuffix;
            }
            set
            {
                VerifyWindowOpened();
                m_titleSuffix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
            }
        }

        public static string Title
        {
            get
            {
                VerifyWindowOpened();
                return m_gameWindow.Title;
            }
            set
            {
                VerifyWindowOpened();
                m_gameWindow.Title = value;
                m_titlePrefix = value;
                m_titleSuffix = string.Empty;
            }
        }
        /*
		public static Icon Icon
		{
			get
			{
				VerifyWindowOpened();
				return m_gameWindow.Icon;
			}
			set
			{
				VerifyWindowOpened();
				m_gameWindow.Icon = value;
			}
		}
		*/

        public static int PresentationInterval
        {
            get
            {
                VerifyWindowOpened();
                if (!m_swapInterval.HasValue)
                {
                    m_swapInterval = m_gameWindow.VSync ? 1 : 0;
                }
                return m_swapInterval.Value;
            }
            set
            {
                VerifyWindowOpened();
                value = Math.Clamp(value, 0, 4);
                if (value != PresentationInterval)
                {
                    m_gameWindow.GLContext?.SwapInterval(value);
                    m_swapInterval = value;
                }
            }
        }
        
        public static bool IsCreated => m_state != State.Uncreated;
        public static bool IsActive => m_state == State.Active;
#endif

        public static event Action Created;

        public static event Action Resized;

        public static event Action Activated;

        public static event Action Deactivated;

        public static event Action Closed;

        public static event Action Frame;

        public static event Action<UnhandledExceptionInfo> UnhandledException;

        public static event Action<Uri> HandleUri;

        public static event Action LowMemory;
#if !ANDROID

        static Window()
        {
            m_dpiScale = 1f;
        }

        public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fixed, string title = "")
        {
            if (m_gameWindow != null)
            {
                throw new InvalidOperationException("Window is already opened.");
            }
            if ((width != 0 || height != 0) && (width <= 0 || height <= 0))
            {
                throw new ArgumentOutOfRangeException("size");
            }
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                Exception ex = args.ExceptionObject as Exception;
                ex ??= new Exception($"Unknown exception. Additional information: {args.ExceptionObject}");
                UnhandledExceptionInfo unhandledExceptionInfo = new(ex);
                UnhandledException?.Invoke(unhandledExceptionInfo);
                if (!unhandledExceptionInfo.IsHandled)
                {
                    Log.Error("Application terminating due to unhandled exception {0}", unhandledExceptionInfo.Exception);
                    Environment.Exit(1);
                }
            };
            width = (width == 0) ? (ScreenSize.X * 4 / 5) : width;
            height = (height == 0) ? (ScreenSize.Y * 4 / 5) : height;
            WindowOptions windowOptions = WindowOptions.Default with
            {
                Title = title,
                PreferredDepthBufferBits = 24,
                PreferredStencilBufferBits = 8,
                API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Debug, new APIVersion(3, 2)),
                Size = new (width, height)
            };
            m_gameWindow = Silk.NET.Windowing.Window.Create(windowOptions);
            m_titlePrefix = title;
            m_gameWindow.Position = new (Math.Max((ScreenSize.X - m_gameWindow.Size.X) / 2, 0), Math.Max((ScreenSize.Y - m_gameWindow.Size.Y) / 2, 0));
            WindowMode = windowMode;
            m_gameWindow.ShouldSwapAutomatically = false;
            m_gameWindow.Load += LoadHandler;
            m_gameWindow.Run();//会阻塞，不要放置在前边
            GLWrapper.GL.Dispose();
            m_gameWindow.Dispose();
        }

        public static void Close()
        {
            VerifyWindowOpened();
            m_closing = true;
        }

        private static void LoadHandler()
        {
            InitializeAll();
            SubscribeToEvents();
            m_state = State.Inactive;
            Created?.Invoke();
            if (m_state == State.Inactive)
            {
                m_state = State.Active;
                Activated?.Invoke();
            }
        }

        private static void FocusedChangedHandler(bool focused)
        {
            if (focused)
            {
                if (m_state == State.Inactive)
                {
                    m_state = State.Active;
                    Activated?.Invoke();
                }
                return;
            }
            if (m_state == State.Active)
            {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            Keyboard.Clear();
            Mouse.Clear();
            Touch.Clear();
        }

        private static void ClosedHandler()
        {
            if (m_state == State.Active)
            {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            if (m_state == State.Inactive)
            {
                m_state = State.Uncreated;
                Closed?.Invoke();
            }
            UnsubscribeFromEvents();
            DisposeAll();
        }
#else
        public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fullscreen, string title = "")
        {
            if (View != null)
            {
                throw new InvalidOperationException("Window is already opened.");
            }
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                if (Window.UnhandledException != null)
                {
                    Exception ex = args.ExceptionObject as Exception;
                    if (ex == null)
                    {
                        ex = new Exception($"Unknown exception. Additional information: {args.ExceptionObject}");
                    }
                    Window.UnhandledException(new UnhandledExceptionInfo(ex));
                }
            };
            Log.Information("Android.OS.Build.Display: " + Build.Display);
            Log.Information("Android.OS.Build.Device: " + Build.Device);
            Log.Information("Android.OS.Build.Hardware: " + Build.Hardware);
            Log.Information("Android.OS.Build.Manufacturer: " + Build.Manufacturer);
            Log.Information("Android.OS.Build.Model: " + Build.Model);
            Log.Information("Android.OS.Build.Product: " + Build.Product);
            Log.Information("Android.OS.Build.Brand: " + Build.Brand);
            Log.Information("Android.OS.Build.VERSION.SdkInt: " + ((int)Build.VERSION.SdkInt).ToString());
            View = new EngineView(Activity);
            View.ContextSet += ContextSetHandler;
            View.Resize += ResizeHandler;
            View.ContextLost += ContextLostHandler;
            View.RenderFrame += RenderFrameHandler;
            Activity.Paused += PausedHandler;
            Activity.Resumed += ResumedHandler;
            Activity.Destroyed += DestroyedHandler;
            Activity.NewIntent += NewIntentHandler;
            Activity.SetContentView(View);
            View.RequestFocus();
            View.Run();
        }

        public static void Close()
        {
            VerifyWindowOpened();
            Activity.Finish();
        }

        public static void VerifyWindowOpened()
        {
            if (View == null)
            {
                throw new InvalidOperationException("Window is not opened.");
            }
        }

        public static void PausedHandler()
        {
            if (m_state == State.Active)
            {
                m_state = State.Inactive;
                Keyboard.Clear();
                Deactivated?.Invoke();
            }
        }

        public static void ResumedHandler()
        {
            if (m_state == State.Inactive)
            {
                m_state = State.Active;
                View.EnableImmersiveMode();
                Activated?.Invoke();
            }
        }

        public static void DestroyedHandler()
        {
            if (m_state == State.Active)
            {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            m_state = State.Uncreated;
            Closed?.Invoke();
            DisposeAll();
        }

        public static void NewIntentHandler(Intent intent)
        {
            if (HandleUri != null && intent != null)
            {
                Uri uriFromIntent = GetUriFromIntent(intent);
                if (uriFromIntent != null)
                {
                    HandleUri(uriFromIntent);
                }
            }
        }
#endif

        private static void ResizeHandler(Vector2D<int> _)
        {
#if ANDROID
            if (m_state != 0)
            {
                Display.Resize();
                Resized?.Invoke();
            }
#else
			Display.Resize();
			Resized?.Invoke();
#endif
		}
#if ANDROID
        public static void ContextSetHandler(object sender, EventArgs args)
        {
            if (m_contextLost)
            {
                m_contextLost = false;
                Display.HandleDeviceReset();
            }
        }

        public static void ContextLostHandler(object sender, EventArgs args)
        {
            m_contextLost = true;
            Display.HandleDeviceLost();
        }
#endif
#if !ANDROID
        private static void RenderFrameHandler(double _)
        {
            BeforeFrameAll();
            Frame?.Invoke();
            AfterFrameAll();
            if (!m_closing)
            {
                m_gameWindow.GLContext?.SwapBuffers();
            }
            else
            {
                m_gameWindow.Close();
            }
        }

        private static void VerifyWindowOpened()
        {
            if (m_gameWindow == null)
            {
                throw new InvalidOperationException("Window is not opened.");
            }
        }

        private static void SubscribeToEvents()
        {
            m_gameWindow.FocusChanged += FocusedChangedHandler;
            m_gameWindow.Closing += ClosedHandler;
            m_gameWindow.Resize += ResizeHandler;
            m_gameWindow.Render += RenderFrameHandler;
        }

        private static void UnsubscribeFromEvents()
        {
            m_gameWindow.FocusChanged -= FocusedChangedHandler;
            m_gameWindow.Closing -= ClosedHandler;
            m_gameWindow.Resize -= ResizeHandler;
            m_gameWindow.Render -= RenderFrameHandler;
        }

#else
        private static void RenderFrameHandler(object sender, EventArgs args)
        {
            if (m_state == State.Uncreated)
            {
                InitializeAll();
                m_state = State.Inactive;
                Created?.Invoke();
                m_state = State.Active;
                Activated?.Invoke();
                NewIntentHandler(Activity.Intent);
            }
            if (m_state != State.Active)
            {
                return;
            }
            BeforeFrameAll();
            Frame?.Invoke();
            AfterFrameAll();
            View.GraphicsContext.SwapBuffers();
            if (m_presentationInterval >= 2)
            {
                double num = Time.RealTime - m_frameStartTime;
                int num2 = (int)(1000.0 * ((double)((float)m_presentationInterval / 60f) - num));
                if (num2 > 0)
                {
                    Task.Delay(num2).Wait();
                }
            }
            m_frameStartTime = Time.RealTime;
            if (m_focusRegained)
            {
                m_focusRegained = false;
                View.EnableImmersiveMode();
            }
        }

        public static Uri GetUriFromIntent(Intent intent)
        {
            Uri result = null;
            if (!string.IsNullOrEmpty(intent.DataString))
            {
                Uri.TryCreate(intent.DataString, UriKind.RelativeOrAbsolute, out result);
            }
            return result;
        }
#endif
		private static void InitializeAll()
        {
            try
		    {
#if WINDOWS
                Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(Image.DefaultImageSharpDecoderOptions, typeof(Window).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.icon.png"));
                byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
                image.CopyPixelDataTo(pixelBytes);
                m_gameWindow.SetWindowIcon([new RawImage(image.Width, image.Height, pixelBytes)]);
#endif
              Dispatcher.Initialize();
               Display.Initialize();
                m_inputContext = m_gameWindow.CreateInput();
              Keyboard.Initialize();
              Mouse.Initialize();
              Touch.Initialize();
              GamePad.Initialize();
              Mixer.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error("初始化时出错: " + ex.Message);
            }

        }

        private static void DisposeAll()
        {
            Display.Dispose();
            Keyboard.Dispose();
            Mouse.Dispose();
            Touch.Dispose();
            GamePad.Dispose();
            Mixer.Dispose();
        }

        private static void BeforeFrameAll()
        {
            Time.BeforeFrame();
            Dispatcher.BeforeFrame();
            Display.BeforeFrame();
            Keyboard.BeforeFrame();
            Mouse.BeforeFrame();
            Touch.BeforeFrame();
            GamePad.BeforeFrame();
            Mixer.BeforeFrame();
        }

        private static void AfterFrameAll()
        {
            Time.AfterFrame();
            Dispatcher.AfterFrame();
            Display.AfterFrame();
            Keyboard.AfterFrame();
            Mouse.AfterFrame();
            Touch.AfterFrame();
            GamePad.AfterFrame();
            Mixer.AfterFrame();
        }
    }
}

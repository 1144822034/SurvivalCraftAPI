#if ANDROID
using Android.Content;
using Android.OS;
#else
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Silk.NET.Input;
#endif
using Monitor = Silk.NET.Windowing.Monitor;
using System.Runtime.CompilerServices;
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;
using Silk.NET.Windowing;
using Silk.NET.Core;
using Silk.NET.Maths;
using Environment = System.Environment;

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

        public static IView m_view;

#if ANDROID
        public static EngineActivity Activity => EngineActivity.m_activity;
#else
        public static IWindow m_gameWindow;

        public static IInputContext m_inputContext;
#endif

        private static bool m_closing;

        private static int? m_swapInterval;


        public static string m_titlePrefix = string.Empty;

        public static string m_titleSuffix = string.Empty;

        public static float m_lastRenderDelta;


        public static Point2 ScreenSize
        {
            get
            {
                #if ANDROID
                return new Point2(m_view.Size.X, m_view.Size.Y);
#else
                var size = m_gameWindow?.Monitor?.Bounds.Size ?? Monitor.GetMainMonitor(null).Bounds.Size;
                return new Point2(size.X, size.Y);
#endif
            }
        }

        public static WindowMode WindowMode
        {
            get
            {
#if ANDROID
                return WindowMode.Fullscreen;
#else
                VerifyWindowOpened();
                return m_gameWindow.WindowState == WindowState.Fullscreen
                    ? WindowMode.Fullscreen
                    : m_gameWindow.WindowBorder != 0 ? WindowMode.Fixed : WindowMode.Resizable;
#endif
            }
            set
            {
#if ANDROID
                return;
#else
                VerifyWindowOpened();
                switch (value)
                {
                    case WindowMode.Fixed:
                        m_gameWindow.WindowBorder = WindowBorder.Fixed;
                        if (m_gameWindow.WindowState != WindowState.Normal)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Resizable:
                        m_gameWindow.WindowBorder = WindowBorder.Resizable;
                        if (m_gameWindow.WindowState != WindowState.Normal)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Borderless:
                        m_gameWindow.WindowBorder = WindowBorder.Hidden;
                        if (m_gameWindow.WindowState != WindowState.Normal)
                        {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Fullscreen:
                        m_gameWindow.WindowBorder = WindowBorder.Resizable;
                        m_gameWindow.WindowState = WindowState.Normal;
                        m_gameWindow.WindowState = WindowState.Fullscreen;
                        break;
                }
#endif
            }
        }

        public static Point2 Position
        {
            get
            {
                VerifyWindowOpened();
#if ANDROID
                return Point2.Zero;
#else
                return new Point2(m_gameWindow.Position.X, m_gameWindow.Position.Y);
#endif
            }
            set
            {
#if ANDROID
                return;
#else
                VerifyWindowOpened();
                m_gameWindow.Position = new (value.X, value.Y);
#endif
            }
        }

        public static Point2 Size
        {
            get
            {
                VerifyWindowOpened();
                return new Point2(m_view.Size.X, m_view.Size.Y);
            }
            set
            {
#if ANDROID
                return;
#else
                VerifyWindowOpened();
                m_gameWindow.Size = new (value.X, value.Y);
#endif
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
#if !ANDROID
                VerifyWindowOpened();
                m_titlePrefix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
#endif
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
#if !ANDROID
                VerifyWindowOpened();
                m_titleSuffix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
#endif
            }
        }

        public static string Title
        {
            get
            {
#if ANDROID
                return String.Empty;
#else
                VerifyWindowOpened();
                return m_gameWindow.Title;
#endif
            }
            set
            {
#if !ANDROID
                VerifyWindowOpened();
                m_gameWindow.Title = value;
                m_titlePrefix = value;
                m_titleSuffix = string.Empty;
#endif
            }
        }

        public static int PresentationInterval
        {
            get
            {
                VerifyWindowOpened();
                if (!m_swapInterval.HasValue)
                {
                    m_swapInterval = m_view.VSync ? 1 : 0;
                }
                return m_swapInterval.Value;
            }
            set
            {
                VerifyWindowOpened();
                value = Math.Clamp(value, 0, 4);
                if (value != PresentationInterval)
                {
                    m_view.GLContext?.SwapInterval(value);
                    m_swapInterval = value;
                }
            }
        }
        
        public static bool IsCreated => m_state != State.Uncreated;
        public static bool IsActive => m_state == State.Active;

        public static event Action Created;

        public static event Action Resized;

        public static event Action Activated;

        public static event Action Deactivated;

        public static event Action Closed;

        public static event Action Frame;

        public static event Action<UnhandledExceptionInfo> UnhandledException;

        public static event Action<Uri> HandleUri;

        public static event Action LowMemory;

#if ANDROID
        public const string WindowingLibrary = "Silk.NET.Windowing.Sdl";
#else
        public const string WindowingLibrary = "Silk.NET.Windowing.Glfw";
        public const string InputLibrary = "Silk.NET.Input.Glfw";
#endif

        public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fixed, string title = "")
        {
            if (m_view != null)
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
            Silk.NET.Windowing.Window.ShouldLoadFirstPartyPlatforms(false);
            Silk.NET.Windowing.Window.TryAdd(WindowingLibrary);
#if DEBUG
            GraphicsAPI api = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Debug, new APIVersion(3, 2));
#else
            GraphicsAPI api = new GraphicsAPI(ContextAPI.OpenGLES, new APIVersion(3, 2));
#endif
#if ANDROID
            Log.Information("Android.OS.Build.Display: " + Build.Display);
            Log.Information("Android.OS.Build.Device: " + Build.Device);
            Log.Information("Android.OS.Build.Hardware: " + Build.Hardware);
            Log.Information("Android.OS.Build.Manufacturer: " + Build.Manufacturer);
            Log.Information("Android.OS.Build.Model: " + Build.Model);
            Log.Information("Android.OS.Build.Product: " + Build.Product);
            Log.Information("Android.OS.Build.Brand: " + Build.Brand);
            Log.Information("Android.OS.Build.VERSION.SdkInt: " + ((int)Build.VERSION.SdkInt).ToString());
            ViewOptions options = ViewOptions.Default with { API = api };
            m_view = Silk.NET.Windowing.Window.GetView(options);
            Activity.Paused += PausedHandler;
            Activity.Resumed += ResumedHandler;
            Activity.Destroyed += DestroyedHandler;
            Activity.NewIntent += NewIntentHandler;
#else
            width = (width == 0) ? (ScreenSize.X * 4 / 5) : width;
            height = (height == 0) ? (ScreenSize.Y * 4 / 5) : height;
            WindowOptions windowOptions = WindowOptions.Default with
            {
                Title = title,
                PreferredDepthBufferBits = 24,
                PreferredStencilBufferBits = 8,
                API = api,
                Size = new (width, height)
            };
            m_gameWindow = Silk.NET.Windowing.Window.Create(windowOptions);
            m_view = m_gameWindow;
            m_titlePrefix = title;
            Position = new Point2(Math.Max((ScreenSize.X - m_gameWindow.Size.X) / 2, 0), Math.Max((ScreenSize.Y - m_gameWindow.Size.Y) / 2, 0));
            WindowMode = windowMode;
#endif
            m_view.ShouldSwapAutomatically = false;
            m_view.Load += LoadHandler;
            m_view.Run();//会阻塞，不要放置在前边
            GLWrapper.GL.Dispose();
            m_view?.Dispose();
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

        private static void RenderFrameHandler(double lastRenderDelta)
        {
            m_lastRenderDelta = (float)lastRenderDelta;
            BeforeFrameAll();
            Frame?.Invoke();
            AfterFrameAll();
            if (!m_closing)
            {
                m_view.GLContext?.SwapBuffers();
            }
            else
            {
#if ANDROID
                Activity.Finish();
#else
                m_gameWindow.Close();
#endif
            }
        }

#if ANDROID
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
                Activity.EnableImmersiveMode();
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

        private static void VerifyWindowOpened()
        {
            if(m_view == null)
            {
                throw new InvalidOperationException("Window is not opened.");
            }
        }

        private static void SubscribeToEvents()
        {
            m_view.FocusChanged += FocusedChangedHandler;
            m_view.Closing += ClosedHandler;
            m_view.Resize += ResizeHandler;
            m_view.Render += RenderFrameHandler;
        }

        private static void UnsubscribeFromEvents()
        {
            m_view.FocusChanged -= FocusedChangedHandler;
            m_view.Closing -= ClosedHandler;
            m_view.Resize -= ResizeHandler;
            m_view.Render -= RenderFrameHandler;
        }

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
#if !ANDROID
                InputWindowExtensions.ShouldLoadFirstPartyPlatforms(false);
                InputWindowExtensions.TryAdd(InputLibrary);
                m_inputContext = m_view.CreateInput();
#endif
              Keyboard.Initialize();
              Mouse.Initialize();
              Touch.Initialize();
              GamePad.Initialize();
              Mixer.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error("Error occupies in InitializeAll: " + ex);
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

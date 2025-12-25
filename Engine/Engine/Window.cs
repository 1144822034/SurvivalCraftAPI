#if ANDROID
#pragma warning disable CA1416
using Android.Content;
using Android.OS;
using Android.Views;
using Org.Libsdl.App;
#else
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Silk.NET.Core;
using Silk.NET.GLFW;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Silk.NET.Input;
using Monitor = Silk.NET.Windowing.Monitor;
#if WINDOWS
using System.Runtime.InteropServices;
#endif
#endif
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Display = Engine.Graphics.Display;
using Environment = System.Environment;

namespace Engine {
    public static class Window {
        enum State {
            Uncreated,
            Inactive,
            Active
        }

        static State m_state;

        public static IView m_view;

#if ANDROID
        public static EngineActivity Activity => EngineActivity.m_activity;

        public static SDLSurface m_surface;
#else
        public static IWindow m_gameWindow;

        public static IInputContext m_inputContext;
#endif

        static bool m_closing;
        static bool m_restarting;

        static int? m_swapInterval;

        public static string m_titlePrefix = string.Empty;

        public static string m_titleSuffix = string.Empty;

        public static float m_lastRenderDelta;

        public static Point2 ScreenSize {
            get {
#if ANDROID
                return new Point2(m_view.Size.X, m_view.Size.Y);
#else
                IMonitor monitor = m_gameWindow?.Monitor;
                if (monitor == null) {
                    try {
                        monitor = Monitor.GetMainMonitor(null);
                    }
                    catch (Exception e) {
                        if (e is PlatformNotSupportedException) {
#if WINDOWS
                            string str =
                                "GLFW Window Platform is not applicable. Please install Microsoft Visual C++ Redistributable. Click \"OK\" to open download page.\nGLFW 窗口平台无法使用。请安装 Microsoft Visual C++ Redistributable，点击\"确定\"来打开下载页面。";
                            const string downloadLink =
                                "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-microsoft-visual-c-redistributable-version";
                            Log.Error($"GLFW Window Platform is not applicable. Please install Microsoft Visual C++ Redistributable. GLFW 窗口平台无法使用。请安装 Microsoft Visual C++ Redistributable。\nDownload page 下载页: {downloadLink}\nException details: {e}");
                            new Thread(() => {
                                    if (MessageBox(IntPtr.Zero, str, null, 0x11u) == 1) {
                                        Process.Start(
                                            new ProcessStartInfo(
                                                downloadLink
                                            ) { UseShellExecute = true }
                                        );
                                    }
                                }
                            ).Start();
#else
                            if (OperatingSystem.IsLinux()) {
                                const string str =
                                    "GLFW Window Platform is not applicable. Please check: https://dotnet.github.io/Silk.NET/docs/hlu/troubleshooting.html";
                                Process.Start("notify-send", $"-a \"Survivalcraft API\" -u critical \"Error\" \"{str}\"");
                                Log.Error(str);
                            }
#endif
                        }
                        else {
                            Log.Error($"Get screen size failed.\n{e}");
                        }
                        return Point2.Zero;
                    }
                }
                Vector2D<int> size = monitor.Bounds.Size;
                return new Point2(size.X, size.Y);
#endif
            }
        }

        public static WindowMode WindowMode {
            get {
#if ANDROID
                return WindowMode.Fullscreen;
#else
                VerifyWindowOpened();
                return m_gameWindow.WindowState == WindowState.Fullscreen ? WindowMode.Fullscreen :
                    m_gameWindow.WindowBorder != 0 ? WindowMode.Fixed : WindowMode.Resizable;
#endif
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if ANDROID
#else
                if (!IsWindowOpened()) {
                    return;
                }
                switch (value) {
                    case WindowMode.Fixed:
                        m_gameWindow.WindowBorder = WindowBorder.Fixed;
                        if (m_gameWindow.WindowState != WindowState.Normal) {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Resizable:
                        m_gameWindow.WindowBorder = WindowBorder.Resizable;
                        if (m_gameWindow.WindowState != WindowState.Normal) {
                            m_gameWindow.WindowState = WindowState.Normal;
                        }
                        break;
                    case WindowMode.Borderless:
                        m_gameWindow.WindowBorder = WindowBorder.Hidden;
                        if (m_gameWindow.WindowState != WindowState.Normal) {
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

        public static Point2 Position {
            get {
                VerifyWindowOpened();
#if ANDROID
                return Point2.Zero;
#else
                return new Point2(m_gameWindow.Position.X, m_gameWindow.Position.Y);
#endif
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if ANDROID
#else
                if (!IsWindowOpened()) {
                    return;
                }
                m_gameWindow.Position = new Vector2D<int>(value.X, value.Y);
#endif
            }
        }

        public static Point2 Size {
            get {
                VerifyWindowOpened();
                return new Point2(m_view.Size.X, m_view.Size.Y);
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if ANDROID
#else
                if (!IsWindowOpened()) {
                    return;
                }
                m_gameWindow.Size = new Vector2D<int>(value.X, value.Y);
#endif
            }
        }

        public static bool HasWideNotch { get; set; }

        /// <summary>
        /// 刘海/水滴/挖孔在屏幕边缘的宽度。X: 左边，Y: 顶部，Z: 右边，W: 底部
        /// </summary>
        public static Vector4 DisplayCutoutInsets { get; set; } = Vector4.Zero;

        public static string TitlePrefix {
            get {
                VerifyWindowOpened();
                return m_titlePrefix;
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if !ANDROID
                if (!IsWindowOpened()) {
                    return;
                }
                m_titlePrefix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
#endif
            }
        }

        public static string TitleSuffix {
            get {
                VerifyWindowOpened();
                return m_titleSuffix;
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if !ANDROID
                if (!IsWindowOpened()) {
                    return;
                }
                m_titleSuffix = value;
                m_gameWindow.Title = $"{m_titlePrefix}{m_titleSuffix}";
#endif
            }
        }

        public static string Title {
            get {
#if ANDROID
                return string.Empty;
#else
                VerifyWindowOpened();
                return m_gameWindow.Title;
#endif
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
#if !ANDROID
                if (!IsWindowOpened()) {
                    return;
                }
                m_gameWindow.Title = value;
                m_titlePrefix = value;
                m_titleSuffix = string.Empty;
#endif
            }
        }

        public static int PresentationInterval {
            get {
                VerifyWindowOpened();
                m_swapInterval ??= m_view.VSync ? 1 : 0;
                return m_swapInterval.Value;
            }
            set {
                if (!IsWindowOpened()) {
                    return;
                }
                value = Math.Clamp(value, 0, 4);
                if (value != PresentationInterval) {
                    m_view.GLContext?.SwapInterval(value);
                    m_swapInterval = value;
                }
            }
        }

        public static IntPtr Handle {
            get {
                INativeWindow native = m_view.Native;
                if (native != null) {
                    NativeWindowFlags kind = native.Kind;
                    if (kind.HasFlag(NativeWindowFlags.Win32)) {
                        return native.Win32?.Hwnd ?? IntPtr.Zero;
                    }
                    if (kind.HasFlag(NativeWindowFlags.Android)) {
                        return native.Android?.Window ?? IntPtr.Zero;
                    }
                    if (kind.HasFlag(NativeWindowFlags.X11)) {
                        return (IntPtr)(native.X11?.Window.ToUInt64() ?? 0UL);
                    }
                    if (kind.HasFlag(NativeWindowFlags.Wayland)) {
                        return native.Wayland?.Surface ?? IntPtr.Zero;
                    }
                }
                return IntPtr.Zero;

            }
        }

        public static bool IsCreated => m_state != State.Uncreated;
        public static bool IsActive => m_state == State.Active;

        public static event Action Created;

        public static event Action Resized;

        public static event Action<Vector4, bool> DisplayCutoutInsetsChanged;

        public static event Action Activated;

        public static event Action Deactivated;

        public static event Action Closed;

        public static event Action ToRestart;

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

        public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fixed, string title = "") {
            if (m_view != null) {
                throw new InvalidOperationException("Window is already opened.");
            }
            /*if ((width != 0 || height != 0) && (width <= 0 || height <= 0))
            {
                throw new ArgumentOutOfRangeException("size");
            }*/
            width = Math.Max(width, 0);
            height = Math.Max(height, 0);
            if (width > 0
                && height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            if (width <= 0
                && height > 0) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            AppDomain.CurrentDomain.UnhandledException += delegate(object _, UnhandledExceptionEventArgs args) {
                Exception ex = args.ExceptionObject as Exception;
                ex ??= new Exception($"Unknown exception. Additional information: {args.ExceptionObject}");
                UnhandledExceptionInfo unhandledExceptionInfo = new(ex);
                UnhandledException?.Invoke(unhandledExceptionInfo);
                if (!unhandledExceptionInfo.IsHandled) {
                    Log.Error("Application terminating due to unhandled exception {0}", unhandledExceptionInfo.Exception);
                    Environment.Exit(1);
                }
            };
            Silk.NET.Windowing.Window.ShouldLoadFirstPartyPlatforms(false);
            Silk.NET.Windowing.Window.TryAdd(WindowingLibrary);
#if DIRECT3D11
            GraphicsAPI api = GraphicsAPI.None;
#elif DEBUG
            GraphicsAPI api = new(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Debug, new APIVersion(3, 2));
#elif ANDROID
            Activity.GetGlEsVersion(out int major, out int minor);
            GraphicsAPI api = new(ContextAPI.OpenGLES, new APIVersion(major, minor));
#else
            GraphicsAPI api = new(ContextAPI.OpenGLES, new APIVersion(3, 2));
#endif
#if ANDROID
            Log.Information($"Android.OS.Build.Display: {Build.Display}");
            Log.Information($"Android.OS.Build.Device: {Build.Device}");
            Log.Information($"Android.OS.Build.Hardware: {Build.Hardware}");
            Log.Information($"Android.OS.Build.Manufacturer: {Build.Manufacturer}");
            Log.Information($"Android.OS.Build.Model: {Build.Model}");
            Log.Information($"Android.OS.Build.Product: {Build.Product}");
            Log.Information($"Android.OS.Build.Brand: {Build.Brand}");
            Log.Information($"Android.OS.Build.VERSION.SdkInt: {(int)Build.VERSION.SdkInt}");
            ViewOptions options = ViewOptions.Default with { API = api };
            m_view = Silk.NET.Windowing.Window.GetView(options);
            Activity.Paused += PausedHandler;
            Activity.Resumed += ResumedHandler;
            Activity.Destroyed += DestroyedHandler;
            Activity.NewIntent += NewIntentHandler;
#else
            Point2 screenSize = ScreenSize;
            if (screenSize.X == 0
                && screenSize.Y == 0) {
                return;
            }
            width = width == 0 ? screenSize.X * 4 / 5 : width;
            height = height == 0 ? screenSize.Y * 4 / 5 : height;
            WindowOptions windowOptions = WindowOptions.Default with {
                Title = title, PreferredDepthBufferBits = 24, PreferredStencilBufferBits = 8, API = api, Size = new Vector2D<int>(width, height)
            };
            m_gameWindow = Silk.NET.Windowing.Window.Create(windowOptions);
            m_view = m_gameWindow;
            m_titlePrefix = title;
            Position = new Point2(Math.Max((screenSize.X - m_gameWindow.Size.X) / 2, 0), Math.Max((screenSize.Y - m_gameWindow.Size.Y) / 2, 0));
            WindowMode = windowMode;
#endif
            m_view.ShouldSwapAutomatically = false;
            m_view.Load += LoadHandler;
            try {
                m_view.Run(); //会阻塞，不要放置在前边
            }
#if !ANDROID
            catch (GlfwException e) {
                if (e.ErrorCode == ErrorCode.VersionUnavailable) {
                    const string str =
                        "Your graphics card driver does not support the graphics API used by the current program. Please try updating your graphics card driver or using the compatible patch.\n你的显卡驱动不支持当前程序使用的图形API，请尝试更新显卡驱动，或使用兼容补丁。";
                    Log.Error($"str\n{e}");
#if WINDOWS
                    new Thread(() => { MessageBox(IntPtr.Zero, str, null, 0x10u); }).Start();
#else
                    if (OperatingSystem.IsLinux()) {
                        Process.Start("notify-send", $"-a \"Survivalcraft API\" -u critical \"Error\" \"{str}\"");
                    }
#endif
                }
                else {
                    Log.Error($"Unhandled exception.\n{e}");
                }
            }
#endif
            finally {
#if !DIRECT3D11
                GLWrapper.GL.Dispose();
#endif
                m_view?.Dispose();
            }
        }

        public static void Close() {
            VerifyWindowOpened();
            m_closing = true;
        }

        public static void Restart() {
            VerifyWindowOpened();
            m_closing = true;
            m_restarting = true;
        }

        static void LoadHandler() {
            InitializeAll();
            SubscribeToEvents();
            m_state = State.Inactive;
            Created?.Invoke();
            if (m_state == State.Inactive) {
                m_state = State.Active;
                Activated?.Invoke();
            }
        }

        static void FocusedChangedHandler(bool focused) {
            if (focused) {
                if (m_state == State.Inactive) {
                    m_state = State.Active;
                    Activated?.Invoke();
                }
                return;
            }
            if (m_state == State.Active) {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            Keyboard.Clear();
            Mouse.Clear();
            Touch.Clear();
        }

        static void ClosedHandler() {
            if (m_state == State.Active) {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            if (m_state == State.Inactive) {
                m_state = State.Uncreated;
                Closed?.Invoke();
            }
            UnsubscribeFromEvents();
            DisposeAll();
        }

        static void ResizeHandler(Vector2D<int> _) {
#if ANDROID
            if (m_state != 0) {
                Display.Resize();
                Resized?.Invoke();
            }
#else
            Display.Resize();
            Resized?.Invoke();
#endif
        }

        public static void DisplayCutoutInsetsChangedHandler(Vector4 insets, bool hasWideNotch) {
            if (HasWideNotch == hasWideNotch && DisplayCutoutInsets == insets) {
                return;
            }
            HasWideNotch = hasWideNotch;
            DisplayCutoutInsets = insets;
            DisplayCutoutInsetsChanged?.Invoke(insets, hasWideNotch);
        }

        static void RenderFrameHandler(double lastRenderDelta) {
            m_lastRenderDelta = (float)lastRenderDelta;
            BeforeFrameAll();
            Frame?.Invoke();
            AfterFrameAll();
            if (!m_closing) {
#if DIRECT3D11
                DXWrapper.Present(m_swapInterval ?? 1);
#else
                m_view.GLContext?.SwapBuffers();
#endif
            }
            else {
#if ANDROID
                if (Build.VERSION.SdkInt >= (BuildVersionCodes)21) {
                    Activity.FinishAndRemoveTask();
                }
                else {
                    Activity.FinishAffinity();
                }
#else
                m_gameWindow.Close();
#endif
                if (m_restarting) {
                    ToRestart?.Invoke();
                }
            }
        }

#if ANDROID
        public static void PausedHandler() {
            if (m_state == State.Active) {
                m_state = State.Inactive;
                Keyboard.Clear();
                Deactivated?.Invoke();
            }
        }

        public static void ResumedHandler() {
            if (m_state == State.Inactive) {
                m_state = State.Active;
                Activity.EnableImmersiveMode();
                if ((m_swapInterval ?? 1) == 0) {
                    Time.QueueFrameIndexDelayedExecution(10, () => { m_view.GLContext?.SwapInterval(0); });
                }
                Activated?.Invoke();
            }
        }

        public static void DestroyedHandler() {
            if (m_state == State.Active) {
                m_state = State.Inactive;
                Deactivated?.Invoke();
            }
            m_state = State.Uncreated;
            Closed?.Invoke();
            DisposeAll();
        }

        public static void NewIntentHandler(Intent intent) {
            if (HandleUri != null
                && intent != null) {
                Uri uriFromIntent = GetUriFromIntent(intent);
                if (uriFromIntent != null) {
                    HandleUri(uriFromIntent);
                }
            }
        }

        public static Uri GetUriFromIntent(Intent intent) {
            Uri result = null;
            if (!string.IsNullOrEmpty(intent.DataString)) {
                Uri.TryCreate(intent.DataString, UriKind.RelativeOrAbsolute, out result);
            }
            return result;
        }
#endif

        static void VerifyWindowOpened() {
            if (m_view == null) {
                throw new InvalidOperationException("Window is not opened.");
            }
        }

        static bool IsWindowOpened() => m_view != null;

        static void SubscribeToEvents() {
            m_view.FocusChanged += FocusedChangedHandler;
            m_view.Closing += ClosedHandler;
            m_view.Resize += ResizeHandler;
            m_view.Render += RenderFrameHandler;
        }

        static void UnsubscribeFromEvents() {
            m_view.FocusChanged -= FocusedChangedHandler;
            m_view.Closing -= ClosedHandler;
            m_view.Resize -= ResizeHandler;
            m_view.Render -= RenderFrameHandler;
        }

        static void InitializeAll() {
            try {
#if ANDROID
                if (SDLActivity.ContentView is ViewGroup viewGroup
                    && viewGroup.ChildCount >= 1
                    && viewGroup.GetChildAt(0) is SDLSurface surface) {
                    m_surface = surface;
                }
#else
                using (Stream iconStream = typeof(Window).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.icon.png")) {
                    if (iconStream != null) {
                        Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(Image.DefaultImageSharpDecoderOptions, iconStream);
                        byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
                        image.CopyPixelDataTo(pixelBytes);
                        m_gameWindow.SetWindowIcon([new RawImage(image.Width, image.Height, pixelBytes)]);
                    }
                }
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
            catch (Exception ex) {
                Log.Error($"Error occupies in InitializeAll: {ex}");
            }
        }

        static void DisposeAll() {
            Display.Dispose();
            Keyboard.Dispose();
            Mouse.Dispose();
            Touch.Dispose();
            GamePad.Dispose();
            Mixer.Dispose();
            Log.Dispose();
        }

        static void BeforeFrameAll() {
            Time.BeforeFrame();
            Dispatcher.BeforeFrame();
            Display.BeforeFrame();
            Keyboard.BeforeFrame();
            Mouse.BeforeFrame();
            Touch.BeforeFrame();
            GamePad.BeforeFrame();
            Mixer.BeforeFrame();
        }

        static void AfterFrameAll() {
            Time.AfterFrame();
            Dispatcher.AfterFrame();
            Display.AfterFrame();
            Keyboard.AfterFrame();
            Mouse.AfterFrame();
            Touch.AfterFrame();
            GamePad.AfterFrame();
            Mixer.AfterFrame();
        }

#if WINDOWS
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
#endif
    }
}
#if WINDOWS
using ImeSharp;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
#endif
using System.Globalization;
using Engine;
using Engine.Graphics;
#if !ANDROID
using Engine.Input;
#endif

namespace Game {
    public static class Program {
        public static double m_frameBeginTime;
        public static double m_cpuEndTime;
        public static List<Uri> m_urisToHandle = [];
#if WINDOWS
        public static Mutex m_mutex;
        public static bool m_mutexHandled;
#endif
        public static string SystemLanguage { get; set; }
        public static float LastFrameTime { get; set; }

        public static float LastCpuFrameTime { get; set; }

        public static event Action<Uri> HandleUri;
#if ANDROID
        public static bool m_firstFramePrepared;
#endif

#if !ANDROID
        // ReSharper disable UnusedMember.Local
        static void Main(string[] args) {
            // ReSharper restore UnusedMember.Local
#if WINDOWS
            string mutexName;
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetEntryAssembly()?.Location ?? "SurvivalcraftApi"));
                mutexName = $"Global\\{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}";
            }
            m_mutex = new Mutex(true, mutexName, out m_mutexHandled);
            if (!m_mutexHandled) {
                if (args != null
                    && args.Length > 0) {
                    string path = args[0];
                    ExternalContentType type = ExternalContentManager.ExtensionToType(Storage.GetExtension(path));
                    if (ExternalContentManager.IsEntryTypeDownloadSupported(type)
                        && File.Exists(path)) {
                        using (FileStream fileStream = File.OpenRead(path)) {
                            string fileName = Storage.GetFileName(path);
                            try {
                                switch (type) {
                                    case ExternalContentType.World: WorldsManager.ImportWorld(fileStream); break;
                                    case ExternalContentType.BlocksTexture: BlocksTexturesManager.ImportBlocksTexture(fileName, fileStream); break;
                                    case ExternalContentType.CharacterSkin: CharacterSkinsManager.ImportCharacterSkin(fileName, fileStream); break;
                                    case ExternalContentType.FurniturePack: FurniturePacksManager.ImportFurniturePack(fileName, fileStream); break;
                                    case ExternalContentType.Mod: ModsManager.ImportMod(fileName, fileStream); break;
                                }
                                Window.MessageBox(IntPtr.Zero, $"Successfully imported {fileName}.\n导入 {fileName} 成功", "Success 成功", 0x40u);
                            }
                            catch (Exception e) {
                                Window.MessageBox(IntPtr.Zero, $"Failed to import {fileName}, reason:\n导入 {fileName} 失败，原因：\n{e}", null, 0x10u);
                            }
                        }
                    }
                    return;
                }
                string str =
                    "This game is already running! If you cannot find the window, please stop it from the Task Manager, and check the log file in Bugs directory.\n游戏已经在运行！如果找不到游戏窗口，请从任务管理器终止它，并检查 Bugs 目录中的日志文件。";
                Window.MessageBox(IntPtr.Zero, str, null, 0x10u);
                return;
            }
#endif
            if (args != null
                && args.Length > 0) {
                //拖动到exe的文件解析
                if (Path.GetExtension(args[0]) == ".scmodList") {
                    ModsManager.ModsPath = ModListManager.AnalysisModList(args[0]);
                }
                else {
                    ExternalContentManager.openFilePath = args[0];
                    //var externalContentScreen=new ExternalContentScreen();
                    //sexternalContentScreen.Update();
                }
            }

            // Process.Start("C:\\Windows\\System32\\msg.exe",  "/server:127.0.0.1 * \"此版本为预览版 不建议长期使用");
#if WINDOWS
            Window.Created += () => {
                InputMethod.Initialize(Process.GetCurrentProcess().MainWindowHandle);
                InputMethod.Enabled = false;
            };
#endif
            EntryPoint();
#if WINDOWS
            m_mutex.ReleaseMutex();
            m_mutex.Dispose();
#endif
            /*AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                //在程序目录下面寻找dll,解决部分设备找不到目录下程序集的问题
                string location = new FileInfo(typeof(Program).Assembly.Location).Directory!.FullName;
                return Assembly.LoadFrom(Path.Combine(location, e.Name));
            };*/

            //RootCommand rootCommand =
            //[
            //    new Option<string>(["-m", "--mod-import"], ""),
            //    new Option<string>(["-l", "--language"], "")
            //];
        }
#endif

        [STAThread]
        public static void EntryPoint() {
            SystemLanguage = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrEmpty(SystemLanguage)) {
                Log.Debug(RegionInfo.CurrentRegion.DisplayName);
                SystemLanguage = RegionInfo.CurrentRegion.DisplayName != "United States" ? "zh-CN" : "en-US";
            }
            //预加载
            VersionsManager.Initialize();
            Window.HandleUri += HandleUriHandler;
            Window.Deactivated += DeactivatedHandler;
            Window.Frame += FrameHandler;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            string title = $"Survivalcraft {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
            Log.RemoveAllLogSinks();
            Log.AddLogSink(new GameLogSink());
#if DEBUG
            Log.AddLogSink(new ConsoleLogSink());
            title = "[DEBUG]" + title;
#endif
            Window.UnhandledException += delegate(UnhandledExceptionInfo e) {
                ExceptionManager.ReportExceptionToUser("Unhandled exception.", e.Exception);
                e.IsHandled = true;
            };
            Window.Run(0, 0, WindowMode.Resizable, title);
        }

        public static void HandleUriHandler(Uri uri) {
            m_urisToHandle.Add(uri);
        }

        public static void DeactivatedHandler() {
            GC.Collect();
        }

        public static void FrameHandler() {
            if (Time.FrameIndex < 0) {
                Display.Clear(Color.White, 1f);
            }
            else if (Time.FrameIndex == 0) {
                Display.Clear(Color.White, 1f);
                Initialize();
            }
            else {
                Run();
            }
        }

        public static void Initialize() {
            Log.Information(
                $"Survivalcraft starting up at {DateTime.Now}, GameVersion={VersionsManager.Version}, BuildConfiguration={VersionsManager.BuildConfiguration}, Platform={VersionsManager.PlatformString}, Storage.AvailableFreeSpace={Storage.FreeSpace / 1024 / 1024}MB, ApproximateScreenDpi={ScreenResolutionManager.ApproximateScreenDpi:0.0}, ApproxScreenInches={ScreenResolutionManager.ApproximateScreenInches:0.0}, ScreenResolution={Window.Size}, ProcessorsCount={Environment.ProcessorCount}, APIVersion={ModsManager.APIVersionString}, 64bit={Environment.Is64BitProcess}"
            );
            try {
                SettingsManager.Initialize();
                ExternalContentManager.Initialize();
                MusicManager.Initialize();
                ScreensManager.Initialize();
                APIUpdateManager.Initialize();
                Log.Information("Program Initialize Success");
            }
            catch (Exception e) {
                Log.Error(e.ToString());
            }
        }

        public static void Run() {
#if ANDROID
            // TODO: 待完成。
            // EngineInputConnection.Implement = new SurvivalcraftInputConnection();
#endif
            LastFrameTime = (float)(Time.RealTime - m_frameBeginTime);
            LastCpuFrameTime = (float)(m_cpuEndTime - m_frameBeginTime);
            m_frameBeginTime = Time.RealTime;
#if !ANDROID
            if (Keyboard.IsKeyDownOnce(Key.F11)) {
                SettingsManager.WindowMode = SettingsManager.WindowMode == WindowMode.Fullscreen ? WindowMode.Resizable : WindowMode.Fullscreen;
                Mouse.m_lastMousePosition = null;
            }
#endif
            try {
                if (ExceptionManager.Error == null) {
                    foreach (Uri obj in m_urisToHandle) {
                        HandleUri?.Invoke(obj);
                    }
                    m_urisToHandle.Clear();
                    PerformanceManager.Update();
                    MotdManager.Update();
                    MusicManager.Update();
                    ScreensManager.Update();
                    DialogsManager.Update();
                    JsInterface.Update();
                }
                else {
                    ExceptionManager.UpdateExceptionScreen();
                }
            }
            catch (Exception e) {
                //ModsManager.AddException(e);
                Log.Error("Game Running Error!");
                Log.Error(e);
                try {
                    ScreensManager.SwitchScreen("MainMenu");
                    ViewGameLogDialog dialog = new();
                    dialog.SetErrorHead(9, 10);
                    DialogsManager.ShowDialog(null, dialog);
                    GameManager.DisposeProject();
                }
                catch (Exception e3) {
                    Log.Error(e3);
                }
            }
            m_cpuEndTime = Time.RealTime;
            try {
                Display.RenderTarget = null;
                if (ExceptionManager.Error == null) {
                    if (LoadingScreen.m_isContentLoaded) {
                        ScreensManager.Draw();
                        PerformanceManager.Draw();
                        ScreenCaptureManager.Run();
                    }
                    else {
                        Display.Clear(Color.White, 1f);
                    }
                }
                else {
                    ExceptionManager.DrawExceptionScreen();
                }
            }
            catch (Exception e2) {
                if (GameManager.Project != null) {
                    GameManager.DisposeProject();
                }
                Log.Error(e2);
                ExceptionManager.ReportExceptionToUser(null, e2);
                ScreensManager.SwitchScreen("MainMenu");
            }
#if ANDROID
            finally {
                if (LoadingScreen.m_isContentLoaded) {
                    m_firstFramePrepared = true;
                }
            }
#endif
        }
    }
}
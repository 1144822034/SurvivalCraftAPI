using System.Diagnostics;
using Engine;
using Engine.Graphics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine.Input;
using Timer = System.Threading.Timer;

#if WINDOWS
using ImeSharp;
#endif

namespace Game
{
	public static class Program
	{
		public static double m_frameBeginTime;

		public static double m_cpuEndTime;

		public static List<Uri> m_urisToHandle = [];
#nullable enable
		public static string? SystemLanguage { get; set; }
#nullable disable
		public static float LastFrameTime { get; set; }

		public static float LastCpuFrameTime { get; set; }

		public static event Action<Uri> HandleUri;
		private static Timer JamTimer = new(JamChecker,null,0,8266);
		private static int JamCounter = 0;
#if ANDROID
		public static bool m_firstFramePrepared = false;
#endif
		
#if WINDOWS
		private static void Main(string[] args)
		{


			if(args != null && args.Length > 0)
			{
				//拖动到exe的文件解析
				if(Path.GetExtension(args[0])== ".scmodList")
				{
					ModsManager.ModsPath=ModListManager.AnalysisModList(args[0]);
				}
				else
				{
					ExternalContentManager.openFilePath = args[0];
					//var externalContentScreen=new ExternalContentScreen();
					//sexternalContentScreen.Update();
				}

			}
			
			
			// Process.Start("C:\\Windows\\System32\\msg.exe",  "/server:127.0.0.1 * \"此版本为预览版 不建议长期使用");
#if WINDOWS
			Window.Created += () =>
			{
				InputMethod.Initialize(Process.GetCurrentProcess().MainWindowHandle, true);
				InputMethod.Enabled = false;
			};
#endif
			EntryPoint();
			AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
				//在程序目录下面寻找dll,解决部分设备找不到目录下程序集的问题
				var location = new FileInfo(typeof(Program).Assembly.Location).Directory!.FullName;
				return Assembly.LoadFrom(Path.Combine(location, e.Name));
			};

			//RootCommand rootCommand =
			//[
			//    new Option<string>(["-m", "--mod-import"], ""),
			//    new Option<string>(["-l", "--language"], "")
			//];
		}
#endif

		[STAThread]
		public static void EntryPoint()
		{
			SystemLanguage = CultureInfo.CurrentUICulture.Name;
			if (string.IsNullOrEmpty(SystemLanguage))
			{
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
			string title = $"Survivalcraft {ModsManager.ShortGameVersion} - API {ModsManager.ApiVersionString}";
			Log.RemoveAllLogSinks();
			Log.AddLogSink(new GameLogSink());
#if DEBUG
			Log.AddLogSink(new ConsoleLogSink());
			title = "[DEBUG]" + title;
#endif
			Window.UnhandledException += delegate(UnhandledExceptionInfo e)
			{
				ExceptionManager.ReportExceptionToUser("Unhandled exception.", e.Exception);
				e.IsHandled = true;
			};
			Window.Run(0, 0, WindowMode.Resizable, title);
		}

		public static void HandleUriHandler(Uri uri)
		{
			m_urisToHandle.Add(uri);
		}

		public static void DeactivatedHandler()
		{
			GC.Collect();
		}

		public static void FrameHandler()
		{
			if (Time.FrameIndex < 0)
			{
				Display.Clear(Vector4.Zero, 1f);
			}
			else if (Time.FrameIndex == 0)
			{
				Initialize();
			}
			else
			{
				Run();
			}
		}

		public static void Initialize()
		{
			Log.Information(
				$"Survivalcraft starting up at {DateTime.Now}, GameVersion={VersionsManager.Version}, BuildConfiguration={VersionsManager.BuildConfiguration}, Platform={VersionsManager.PlatformString}, Storage.AvailableFreeSpace={Storage.FreeSpace / 1024 / 1024}MB, ApproximateScreenDpi={ScreenResolutionManager.ApproximateScreenDpi:0.0}, ApproxScreenInches={ScreenResolutionManager.ApproximateScreenInches:0.0}, ScreenResolution={Window.Size}, ProcessorsCount={Environment.ProcessorCount}, APIVersion={ModsManager.ApiVersionString}, 64bit={Environment.Is64BitProcess}");
			try
			{
				SettingsManager.Initialize();
				ExternalContentManager.Initialize();
				MusicManager.Initialize();
				ScreensManager.Initialize();
				Log.Information("Program Initialize Success");
			}
			catch (Exception e)
			{
				Log.Error(e.ToString());
			}
		}
		public static void JamChecker(object o)
		{
			if(JamCounter >= 5)
			{
				Window.Close();
				Thread.Sleep(500);
				ModsManager.Reboot();//重新启动
				//Environment.Exit(0); // 正常关闭程序
			}
			else
			{
				JamCounter += 1;
			}
		}

		public static void Run()
		{
#if ANDROID
			// TODO: 待完成。
			// EngineInputConnection.Implement = new SurvivalcraftInputConnection();
#endif
			JamCounter = 0;
			
			LastFrameTime = (float)(Time.RealTime - m_frameBeginTime);
			LastCpuFrameTime = (float)(m_cpuEndTime - m_frameBeginTime);
			m_frameBeginTime = Time.RealTime;
			if (Keyboard.IsKeyDown(Key.F11))
			{
				SettingsManager.WindowMode = SettingsManager.WindowMode == WindowMode.Fullscreen
					? WindowMode.Resizable
					: WindowMode.Fullscreen;
			}

			try
			{
				if (ExceptionManager.Error == null)
				{
					foreach (Uri obj in m_urisToHandle)
					{
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
				else
				{
					ExceptionManager.UpdateExceptionScreen();
				}
			}
			catch (Exception e)
			{
				//ModsManager.AddException(e);
				Log.Error("Game Running Error!");
				Log.Error(e);
				try
				{
					ScreensManager.SwitchScreen("MainMenu");
					ViewGameLogDialog dialog = new ViewGameLogDialog();
					dialog.SetErrorHead(9,10);
					DialogsManager.ShowDialog(null,dialog);
					GameManager.DisposeProject();
				}
				catch(Exception e3)
				{
					Log.Error(e3);
				}
			}

			m_cpuEndTime = Time.RealTime;
			try
			{
				Display.RenderTarget = null;
				if (ExceptionManager.Error == null)
				{
					if(LoadingScreen.m_isContentLoaded)
					{
						ScreensManager.Draw();
						PerformanceManager.Draw();
						ScreenCaptureManager.Run();
					}
				}
				else
				{
					ExceptionManager.DrawExceptionScreen();
				}
			}
			catch (Exception e2)
			{
				if (GameManager.Project != null) GameManager.DisposeProject();
				Log.Error(e2);
				ExceptionManager.ReportExceptionToUser(null, e2);
				ScreensManager.SwitchScreen("MainMenu");
			}
#if ANDROID
			finally
			{
				if(LoadingScreen.m_isContentLoaded)
				{
					m_firstFramePrepared = true;
				}
			}
#endif
		}
	}
}
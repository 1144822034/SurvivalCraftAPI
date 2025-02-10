#if ANDROID
using Android.Content;
using Android.Graphics;
using Java.Nio;
#endif
using Engine;
using Engine.Graphics;
using Engine.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game
{
	public static class ScreenCaptureManager
	{
		public static readonly string ScreenshotDir = "app:/ScreenCapture";

		public static bool m_captureRequested;

		public static Action m_successHandler;

		public static Action<Exception> m_failureHandler;

		public static void Run()
		{
			if (m_captureRequested)
			{
				try
				{
					int num;
					int height;
					switch (SettingsManager.ScreenshotSize)
					{
						case ScreenshotSize.ScreenSize:
							{
								num = Math.Max(Window.ScreenSize.X, Window.ScreenSize.Y);
								height = Math.Min(Window.ScreenSize.X, Window.ScreenSize.Y);
								float num2 = num / (float)height;
								num = Math.Min(num, 2048);
								height = (int)MathF.Round(num / num2);
								break;
							}
						case ScreenshotSize.FullHD:
							num = 5760;
							height = 3240;
							break;
						default:
							num = 3840;
							height = 2160;
							break;
					}
					DateTime now = DateTime.Now;
					Capture(num, height, $"Survivalcraft {now.Year:D4}-{now.Month:D2}-{now.Day:D2} {now.Hour:D2}-{now.Minute:D2}-{now.Second:D2}.png");
					m_successHandler?.Invoke();
					GC.Collect();
				}
				catch (Exception ex)
				{
					Log.Error($"Error capturing screen. Reason: {ex.Message}");
					m_failureHandler?.Invoke(ex);
				}
				finally
				{
					m_captureRequested = false;
					m_successHandler = null;
					m_failureHandler = null;
				}
			}
		}

		public static void CapturePhoto(Action success, Action<Exception> failure)
		{
			if (!m_captureRequested)
			{
				m_captureRequested = true;
				m_successHandler = success;
				m_failureHandler = failure;
			}
		}

		public static void Capture(int width, int height, string filename)
		{
			if (GameManager.Project != null)
			{
				using (var renderTarget2D = new RenderTarget2D(width, height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8))
				{
					RenderTarget2D renderTarget = Display.RenderTarget;
					var dictionary = new Dictionary<ComponentGui, bool>();
					ResolutionMode resolutionMode = ResolutionMode.High;
					try
					{
						if (!SettingsManager.ShowGuiInScreenshots)
						{
							foreach (ComponentPlayer componentPlayer in GameManager.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
							{
								dictionary[componentPlayer.ComponentGui] = componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible;
								componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible = false;
							}
						}
						resolutionMode = SettingsManager.ResolutionMode;
						SettingsManager.ResolutionMode = ResolutionMode.High;
						Display.RenderTarget = renderTarget2D;
						ScreensManager.Draw();
						if (SettingsManager.ShowLogoInScreenshots)
						{
							var primitivesRenderer2D = new PrimitivesRenderer2D();
							Texture2D texture2D = ContentManager.Get<Texture2D>("Textures/Gui/ScreenCaptureOverlay");
							var vector = new Vector2((width - texture2D.Width) / 2, 0f);
							Vector2 corner = vector + new Vector2(texture2D.Width, texture2D.Height);
							primitivesRenderer2D.TexturedBatch(texture2D, useAlphaTest: false, 0, DepthStencilState.None).QueueQuad(vector, corner, 0f, new Vector2(0f, 0f), new Vector2(1f, 1f), Color.White);
							primitivesRenderer2D.Flush();
						}
					}
					finally
					{
						Display.RenderTarget = renderTarget;
						foreach (KeyValuePair<ComponentGui, bool> item in dictionary)
						{
							item.Key.ControlsContainerWidget.IsVisible = item.Value;
						}
						SettingsManager.ResolutionMode = resolutionMode;
					}
#if !ANDROID
					if (!Storage.DirectoryExists(ScreenshotDir))
						Storage.CreateDirectory(ScreenshotDir);
					using (Stream stream = Storage.OpenFile(Storage.CombinePaths(ScreenshotDir, filename), OpenFileMode.CreateOrOpen))
					{
						Image.Save(renderTarget2D.GetData(new Rectangle(0, 0, renderTarget2D.Width, renderTarget2D.Height)), stream, ImageFileFormat.Png, saveAlpha: false);
					}
#else
					string path = Storage.CombinePaths(ModsManager.ScreenCapturePath, filename);
					if (!Storage.DirectoryExists(ModsManager.ScreenCapturePath)) Storage.CreateDirectory(ModsManager.ScreenCapturePath);
					using (Stream stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen))
					{
                        Image.Save(renderTarget2D.GetData(new Rectangle(0, 0, renderTarget2D.Width, renderTarget2D.Height)), stream, ImageFileFormat.Png, saveAlpha: false);
                    }
					Intent intent = new("android.intent.action.MEDIA_SCANNER_SCAN_FILE");
					intent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(Storage.GetSystemPath(path))));
					Window.Activity.SendBroadcast(intent);
#endif
				}
				ModsManager.HookAction("OnCapture", loader => { loader.OnCapture(); return false; });
			}
		}
	}
}
//修复Image问题后将54、129、136行修改为WebP
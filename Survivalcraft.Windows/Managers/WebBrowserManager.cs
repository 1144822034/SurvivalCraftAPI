using Engine;
using System;
using System.Diagnostics;

namespace Game
{
	public static class WebBrowserManager
	{
		public static void LaunchBrowser(string url)
		{

			if (!url.Contains("://"))
			{
				url = "http://" + url;
			}
			try
			{
#if ANDROID
				Engine.Window.Activity.OpenLink(url);
#else
				System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#endif
			}
			catch (Exception ex)
			{
				Log.Error(string.Format("Error launching web browser with URL \"{0}\". Reason: {1}", new object[2]
				{
					url,
					ex.Message
				}));
			}
		}
	}
}

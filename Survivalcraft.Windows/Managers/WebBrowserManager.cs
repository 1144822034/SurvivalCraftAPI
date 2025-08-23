using Engine;
using System.Diagnostics;

namespace Game
{
	public static class WebBrowserManager
	{
		public static void LaunchBrowser(string url)
		{

			if (!url.Contains("://"))
			{
				url = $"https://{url}";
			}
			try
			{
#if ANDROID
				Engine.Window.Activity.OpenLink(url);
#else
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#endif
			}
			catch (Exception ex)
			{
				Log.Error("Error launching web browser with URL \"" + url + "\". Reason: " + ex.Message);
			}
		}
	}
}

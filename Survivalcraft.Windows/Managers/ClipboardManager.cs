#if WINDOWS || LINUX
using TextCopy;
#elif ANDROID
using Android.OS;
using System.Runtime.Versioning;
#endif

namespace Game
{
	public static class ClipboardManager
	{
#if ANDROID
#pragma warning disable CA1416
		internal static Android.Content.ClipboardManager m_clipboardManager {get;} = GetClipboardManager();

		public static string ClipboardString
		{
			get => m_clipboardManager?.Text ?? string.Empty;
			set
			{
				if(m_clipboardManager != null)
				{
					m_clipboardManager.Text = value;
				}
			}
		}

		private static Android.Content.ClipboardManager GetClipboardManager()
		{
			if(Build.VERSION.SdkInt < (BuildVersionCodes)21)
			{
				return null;
			}
			return Engine.Window.Activity.GetSystemService("clipboard") as Android.Content.ClipboardManager;
		}
#pragma warning restore CA1416
#elif WINDOWS || LINUX
		public static string ClipboardString
		{
			get => ClipboardService.GetText()??"";
			set => ClipboardService.SetText(value??"");
		}
#else
		public static string ClipboardString
		{
			get => "";
			// ReSharper disable ValueParameterNotUsed
			set {}
			// ReSharper restore ValueParameterNotUsed
		}
#endif
	}
}
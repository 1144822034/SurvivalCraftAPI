#if WINDOWS || LINUX
using TextCopy;
#endif

namespace Game
{
	public static class ClipboardManager
	{
#if ANDROID
		internal static Android.Content.ClipboardManager? m_clipboardManager {get;} = Engine.Window.Activity.GetSystemService("clipboard") as Android.Content.ClipboardManager;
		public static string ClipboardString
		{
			get
			{
				return m_clipboardManager?.Text ?? string.Empty;
			}
			set
			{
				if(m_clipboardManager != null)
				{
					m_clipboardManager.Text = value;
				}
			}
		}
#elif WINDOWS || LINUX
		public static string ClipboardString
		{
			get
			{
				return ClipboardService.GetText()??"";
			}
			set
			{
				ClipboardService.SetText(value??"");
			}
		}
#else
		public static string ClipboardString
		{
			get => "";
			set {}
		}
#endif
	}
}
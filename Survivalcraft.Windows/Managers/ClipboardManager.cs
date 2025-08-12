namespace Game
{
	public static class ClipboardManager
	{
#if ANDROID
		internal static Android.Content.ClipboardManager m_clipboardManager {get;} =
 (Android.Content.ClipboardManager)Engine.Window.Activity.GetSystemService("clipboard");
		public static string ClipboardString
		{
			get
			{
				return m_clipboardManager.Text;
			}
			set
			{
				m_clipboardManager.Text = value;
			}
		}
#elif WINDOWS
		public static string ClipboardString
		{
			get
			{
				return TextCopy.ClipboardService.GetText()??"";
			}
			set
			{
				TextCopy.ClipboardService.SetText(value??"");
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
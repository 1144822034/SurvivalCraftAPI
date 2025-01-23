using Android.Runtime;
using Google.Android.Material.Color;
using System.Diagnostics;

namespace SC4Android
{
	[Application]
	public class MainApplication : Application
	{
		public MainApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}
		public override void OnCreate()
		{
			DynamicColors.ApplyToActivitiesIfAvailable(this);
			base.OnCreate();
		}
	}
}
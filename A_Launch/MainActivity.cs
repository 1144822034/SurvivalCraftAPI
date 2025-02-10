using System.Diagnostics;
using Android;
using Android.Animation;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Window;
using Environment = Android.OS.Environment;
using Permission = Android.Content.PM.Permission;

#pragma warning disable CA1416
namespace SC4Android
{
	[Activity(Label = "@string/ShortTitle",LaunchMode = LaunchMode.SingleTask,Icon = "@mipmap/icon",Theme = "@style/MainTheme",MainLauncher = true,ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
	[IntentFilter(["android.intent.action.VIEW"],DataScheme = "com.candy.survivalcraft",Categories = ["android.intent.category.DEFAULT","android.intent.category.BROWSABLE"])]

	public class MainActivity : EngineActivity
	{
		private static bool GraterThanAndroid11 { get; } = (int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.R;
		private static bool GraterThanAndroid6 { get; } = (int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.M;
		
		private bool CheckAndRequestPermission()
		{
			bool arePermissionsGranted = true;
			
			if(GraterThanAndroid11)
			{
				//当版本大于安卓11时
				if(!Environment.IsExternalStorageManager)
				{
					arePermissionsGranted = false;
					RunOnUiThread(() => Toast.MakeText(this, "Need Permission 需要权限", ToastLength.Short)!.Show());
					StartActivity(new Intent(Settings.ActionManageAllFilesAccessPermission));
				}

				return arePermissionsGranted;
			}
			
			if(GraterThanAndroid6)
			{
				//当版本大于安卓6
				var readPermissionStatus = CheckSelfPermission(Manifest.Permission.ReadExternalStorage);
				if(readPermissionStatus != Permission.Granted)
				{
					arePermissionsGranted = false;
					RunOnUiThread(() => Toast.MakeText(this, "Need Permission 需要权限", ToastLength.Short)!.Show());
					RequestPermissions([Manifest.Permission.ReadExternalStorage],0);
				}

				var writePermissionStatus = CheckSelfPermission(Manifest.Permission.WriteExternalStorage);
				if(writePermissionStatus != Permission.Granted)
				{
					if (arePermissionsGranted)
					{
						RunOnUiThread(() => Toast.MakeText(this, "Need Permission 需要权限", ToastLength.Short)!.Show());
						arePermissionsGranted = false;
					}
					RequestPermissions([Manifest.Permission.WriteExternalStorage],1);
				}
			}
			return arePermissionsGranted;
		}

		private bool isPermissionGranted()
		{
			if(GraterThanAndroid11)
			{
				return Environment.IsExternalStorageManager;
			}
			else if(GraterThanAndroid6)
			{
				return CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted && CheckSelfPermission(Manifest.Permission.WriteExternalStorage) == Permission.Granted;
			}
			return true;
		}

		protected override void OnRun()
		{
			base.OnRun();

			if(CheckAndRequestPermission())
			{
				RunRequired = true;
			}
			else
			{
				while (true)
				{
					Thread.Sleep(100);
					if (RunRequired)
					{
						break;
					}
					else
					{
						RunRequired = isPermissionGranted();
						if(RunRequired)
						{
							break;
						}
					}
				}
			}
			Program.EntryPoint();
		}
		private static bool RunRequired { get; set; }
		public override void OnRequestPermissionsResult(int requestCode,string[] permissions,[GeneratedEnum] Permission[] grantResults)
		{
			if (GraterThanAndroid11)
			{
				RunRequired = Environment.IsExternalStorageManager;
			}
			else if (GraterThanAndroid6)
			{
				bool allGranted = 
					grantResults.All(x => x == Permission.Granted);

				if (allGranted)
				{
					RunRequired = true;
				}
			}
		}

		private bool isPaused = false;

		protected override void OnPause()
		{
			base.OnPause();
			isPaused = true;
		}

		protected override void OnResume()
		{
			base.OnResume();
			if(isPaused && !RunRequired)
			{
				isPaused = false;
				RunRequired = CheckAndRequestPermission();
			}
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Window.DecorView.ViewTreeObserver.AddOnPreDrawListener(new ViewTreeObserverListener());
			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			{
				SplashScreen?.SetOnExitAnimationListener(new SplashScreenOnExitAnimationListener());
			}
		}
		public class ViewTreeObserverListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
		{
			public bool OnPreDraw()
			{
				return Program.m_firstFramePrepared;
			}
		}
		public class SplashScreenOnExitAnimationListener : Java.Lang.Object, ISplashScreenOnExitAnimationListener
		{
			public void OnSplashScreenExit(SplashScreenView view)
			{
				var slideUp = ObjectAnimator.OfFloat(view, "alpha", 1f, 0f);
				slideUp.SetInterpolator(new AnticipateInterpolator());
				slideUp.SetDuration(800L);
				slideUp.AnimationEnd += (_, _) =>
				{
					view.Remove();
				};
				slideUp.Start();
			}
		}
	}
}
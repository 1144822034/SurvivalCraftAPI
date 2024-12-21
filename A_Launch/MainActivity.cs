using System.Diagnostics;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
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
		
		private void CheckAndRequestPermission(out bool arePermissionsGranted)
		{
			arePermissionsGranted = true;
			
			if(GraterThanAndroid11)
			{
				//当版本大于安卓11时
				if(!Environment.IsExternalStorageManager)
				{
					arePermissionsGranted = false;
					StartActivity(new Intent(Settings.ActionManageAllFilesAccessPermission));
				}

				return;
			}
			
			if(GraterThanAndroid6)
			{
				//当版本大于安卓6
				var readPermissionStatus = CheckSelfPermission(Manifest.Permission.ReadExternalStorage);
				if(readPermissionStatus != Permission.Granted)
				{
					arePermissionsGranted = false;
					RequestPermissions([Manifest.Permission.ReadExternalStorage],0);
				}

				var writePermissionStatus = CheckSelfPermission(Manifest.Permission.WriteExternalStorage);
				if(writePermissionStatus != Permission.Granted)
				{
					arePermissionsGranted = false;
					RequestPermissions([Manifest.Permission.WriteExternalStorage],1);
				}
			}
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

		private Thread m_thread = null!;
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			
			m_thread = new(() =>
			{
				var counter = 0;
				while (true)
				{
					Thread.Sleep(100);
					counter += 1;
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

					// 15 秒后仍未成功申请
					if (counter >= 150)
					{
						RunOnUiThread(() => Toast.MakeText(this, "申请外部权限失败！正在退出……", ToastLength.Short)!.Show());
						System.Environment.Exit(1);
					}
				}
				RunOnUiThread(Program.EntryPoint);
			});
			
			GC.KeepAlive(m_thread);
			m_thread.Start();
			
			CheckAndRequestPermission(out var granted);
			RunRequired = granted;
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
	}
}
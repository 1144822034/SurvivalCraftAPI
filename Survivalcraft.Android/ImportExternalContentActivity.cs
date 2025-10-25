using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using Engine;
using Game;
using Uri = Android.Net.Uri;

namespace SC4Android {
    [Activity(
         Label = "@string/ImportExternalContentActivityLabel",
         LaunchMode = LaunchMode.SingleTop,
         Icon = "@mipmap/icon",
         Theme = "@style/ImportExternalContentDialog",
         Exported = true
     ),
     IntentFilter(
         ["android.intent.action.VIEW"],
         DataSchemes = ["file", "content"],
         DataMimeType = "*/*",
         DataPathPatterns = [@".*\.scworld", @".*\.scbtex", @".*\.scskin", @".*\.scfpack", @".*\.scmod"],
         Categories = ["android.intent.category.DEFAULT", "android.intent.category.BROWSABLE"]
     ), IntentFilter(["android.intent.action.SEND"], DataMimeType = "*/*", Categories = ["android.intent.category.DEFAULT"])]
    public class ImportExternalContentActivityLabel : Activity {
        bool permissionGranted;

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            Intent intent = Intent;
            if (intent == null) {
                Toast.MakeText(this, "File Not Found\n未找到文件", ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            Uri uri = null;
            if (intent.Action == Intent.ActionView) {
                uri = intent.Data;
            }
            else if (intent.Action == Intent.ActionSend) {
                uri = intent.GetParcelableExtra(Intent.ExtraStream) as Uri;
            }
            if (uri == null) {
                Toast.MakeText(this, "File Not Found\n未找到文件", ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            GetFileNameFromUri(uri, out string fileName, out long fileSize, out Stream fileStream);
            if (fileSize == 0
                || fileStream == null) {
                Toast.MakeText(this, "File Not Found\n未找到文件", ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            string extension = Storage.GetExtension(fileName)?.ToLowerInvariant();
            ExternalContentType type = ExternalContentManager.ExtensionToType(extension);
            if (ExternalContentManager.IsEntryTypeDownloadSupported(type)) {
                if (MainActivity.CheckAndRequestPermission(this)) {
                    permissionGranted = true;
                }
                else {
                    while (true) {
                        Thread.Sleep(100);
                        if (permissionGranted) {
                            break;
                        }
                        permissionGranted = MainActivity.IsPermissionGranted(this);
                        if (permissionGranted) {
                            break;
                        }
                    }
                }
                new AlertDialog.Builder(this).SetTitle("Import 导入")
                    ?.SetMessage($"Do you want to import:\n是否导入文件：\n{fileName}？")
                    ?.SetPositiveButton("Yes 是", async void (_, _) => await ImportFileAsync(fileName, fileSize, fileStream))
                    ?.SetNegativeButton("No 否", (_, _) => FinishAndRemoveTask())
                    ?.Show();
            }
            else {
                new AlertDialog.Builder(this).SetTitle("Not supported type\n不支持的类型")
                    ?.SetMessage($"File type {extension} is not supported\n文件类型 {extension} 不支持")
                    ?.SetPositiveButton("OK", (_, _) => FinishAndRemoveTask())
                    ?.SetOnCancelListener(new DialogInterfaceOnCancelListener(FinishAndRemoveTask))
                    ?.Show();
            }
        }

        public async Task ImportFileAsync(string name, long size, Stream stream) {
            AlertDialog importingDialog = null;
            try {
                ExternalContentType type = ExternalContentManager.ExtensionToType(Storage.GetExtension(name)?.ToLowerInvariant());
                if (size > 10L * 1024 * 1024) {
                    RunOnUiThread(() => {
                            LinearLayout layout = new(this) { Orientation = Orientation.Vertical };
                            int margin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 24, Resources?.DisplayMetrics);
                            layout.SetPadding(0, margin, 0, margin);
                            layout.SetGravity(GravityFlags.Center);
                            ProgressBar progressBar = new(this) { Indeterminate = true };
                            layout.AddView(progressBar);
                            importingDialog = new AlertDialog.Builder(this).SetTitle("Importing 导入中...")
                                ?.SetView(layout)
                                ?.SetCancelable(false)
                                ?.Show();
                        }
                    );
                }
                await Task.Run(() => {
                        switch (type) {
                            case ExternalContentType.World: WorldsManager.ImportWorld(stream); break;
                            case ExternalContentType.BlocksTexture: BlocksTexturesManager.ImportBlocksTexture(name, stream); break;
                            case ExternalContentType.CharacterSkin: CharacterSkinsManager.ImportCharacterSkin(name, stream); break;
                            case ExternalContentType.FurniturePack: FurniturePacksManager.ImportFurniturePack(name, stream); break;
                            case ExternalContentType.Mod: ModsManager.ImportMod(name, stream, false); break;
                        }
                    }
                );
                stream.Close();
                await stream.DisposeAsync();
                RunOnUiThread(() => {
                        importingDialog?.Dismiss();
                        AlertDialog.Builder builder = new AlertDialog.Builder(this).SetTitle("Imported Successfully 导入成功")
                            ?.SetPositiveButton("OK", (_, _) => FinishAndRemoveTask())
                            ?.SetOnCancelListener(new DialogInterfaceOnCancelListener(FinishAndRemoveTask));
                        if (type == ExternalContentType.Mod) {
                            builder?.SetMessage("And you need to open Manage Mod screen to enable it manually.\n接下来你需要到 Mod 管理屏幕中手动启用它。");
                        }
                        builder?.Show();
                    }
                );
            }
            catch (Exception e) {
                RunOnUiThread(() => {
                        importingDialog?.Dismiss();
                        new AlertDialog.Builder(this).SetTitle("Failed to Import 导入失败")
                            ?.SetMessage(e.Message)
                            ?.SetPositiveButton("OK", (_, _) => FinishAndRemoveTask())
                            ?.SetOnCancelListener(new DialogInterfaceOnCancelListener(FinishAndRemoveTask))
                            ?.Show();
                    }
                );
            }
        }

        public void GetFileNameFromUri(Uri uri, out string name, out long size, out Stream stream) {
            name = null;
            size = 0L;
            stream = null;
            try {
                using (ICursor cursor = ContentResolver?.Query(uri, null, null, null, null)) {
                    if (cursor != null
                        && cursor.MoveToFirst()) {
                        int nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                        if (nameIndex >= 0) {
                            name = cursor.GetString(nameIndex);
                        }
                        int sizeIndex = cursor.GetColumnIndex(IOpenableColumns.Size);
                        if (sizeIndex >= 0) {
                            size = cursor.GetLong(sizeIndex);
                        }
                    }
                }
                stream = ContentResolver?.OpenInputStream(uri);
            }
            catch {
                // ignored
            }

            if (string.IsNullOrEmpty(name)) {
                name = Path.GetFileName(uri.Path);
            }
        }

        protected override void OnResume() {
            base.OnResume();
            if (!permissionGranted) {
                permissionGranted = MainActivity.CheckAndRequestPermission(this);
            }
        }

        public class DialogInterfaceOnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener {
            readonly Action _onCancel;
            public DialogInterfaceOnCancelListener(Action onCancel) => _onCancel = onCancel;
            public void OnCancel(IDialogInterface dialog) => _onCancel?.Invoke();
        }
    }
}
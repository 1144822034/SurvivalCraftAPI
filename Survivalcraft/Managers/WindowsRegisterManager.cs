#if WINDOWS
using Microsoft.Win32;

namespace Game.Managers
{
    internal static class WindowsRegisterManager
    {
        public static string SurvivalcraftPath
        {
            get
            {
                return ModsManager.ExternalPath + "Survivalcraft.exe";
            }
        }
        public static void RegisterFileType(string keyName, string keyValue, string extension)
        {
            //keyName = "WPCFile";
            //keyValue = "资源包文件";
            RegistryKey isExCommand = null;
            bool isCreateRegistry = true;

            try
            {
                /// 检查 文件关联是否创建 
                isExCommand = Registry.ClassesRoot.OpenSubKey(keyName);
                if (isExCommand == null)
                {
                    isCreateRegistry = true;
                }
                else
                {
                    if (isExCommand.GetValue("Create").ToString() == SurvivalcraftPath)
                    {
                        isCreateRegistry = false;
                    }
                    else
                    {
                        Registry.ClassesRoot.DeleteSubKeyTree(keyName);
                        isCreateRegistry = true;
                    }

                }
            }
            catch (Exception)
            {
                isCreateRegistry = true;
            }

            /// 假如 文件关联 还没有创建，或是关联位置已被改变 
            //if (isCreateRegistry) 
            {
                try
                {
                    RegistryKey key, keyico;
                    key = Registry.ClassesRoot.CreateSubKey(keyName);
                    key.SetValue("Create", SurvivalcraftPath);

                    keyico = key.CreateSubKey("DefaultIcon");
                    keyico.SetValue("", SurvivalcraftPath + ",0");

                    key.SetValue("", keyValue);
                    key = key.CreateSubKey("Shell");
                    key = key.CreateSubKey("Open");
                    key = key.CreateSubKey("Command");

                    /// 关联的位置 
                    key.SetValue("", SurvivalcraftPath + @" %1/");

                    /// 关联的文件扩展名,  
                    keyName = extension;
                    key = Registry.ClassesRoot.CreateSubKey(keyName);
                    key.SetValue("", keyValue);
                }
                catch (Exception)
                {
                }
            } 
        }
    }
}
#endif
# SurvivalCraft-API 生存战争插件版

## 介绍

生存战争插件版是基于 Candy Rufus Game 开发的 [生存战争 Survivalcraft](https://kaalus.wordpress.com/) 二次开发的支持加载模组的版本

## 用户下载和使用说明

[点击此处](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 进入发布页来下载

### Android 安卓系统看这里
> 需要 64 位 ARM 架构 CPU，最低 Android 5.0

可从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Android]`，后缀为`.apk`的安装包，安装后即可运行  
第一次运行可能会跳转到标题为`所有文件访问`的授权界面，请授权此APP（名称：`生存战争2.4 API插件版1.8`），否则此 APP 无法运行

### Windows 系统看这里
> 需要 x64 架构 CPU，最低 Windows 10 版本 1607，显卡驱动需要支持OpenGL ES 3.2 图形 API（对于兼容补丁，需要支持 Direct3D 9 图形 API）

请先从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Windows]`，后缀为`.7z`，名称不带`兼容补丁`的压缩包，之后使用你喜欢的解压缩软件进行解压，运行<font color="red">解压后</font>的`.exe`文件

第一次启动系统可能会提示您安装 [.NET 桌面运行时 9.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)，请按提示完成安装并重启您的电脑  
如果没有任何反应，可能是因为您的 Windows 系统不完整，请尝试手动安装 [.NET 桌面运行时 9.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)，如果安装且重启电脑后仍然没有任何反应，建议尝试重新安装微软官方且完整的Windows系统，或者使用虚拟机

如果弹窗提示`你的显卡驱动不支持当前程序使用的图形API，请尝试更新显卡驱动，或使用兼容补丁。`，如果显卡驱动更新后仍然弹窗，请尝试下载名称中有`兼容补丁`的压缩包，然后解压到之前解压到的目录，运行<font color="red">解压后的新的</font>`.exe`文件  
如果使用兼容补丁后仍然弹窗，建议为您的电脑购买并装上五年内发布的显卡

如果弹窗提示`GLFW 窗口平台无法使用。请安装 Microsoft Visual C++ Redistributable，点击"确定"来打开下载页面。`，请按提示完成下载和安装。或者[点击此处](https://learn.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-microsoft-visual-c-redistributable-version)打开下载页面

### Linux 系统看这里
> 需要 x64 架构 CPU，最低系统版本详见 [此处](https://github.com/dotnet/core/blob/main/release-notes/9.0/supported-os.md#linux)，显卡驱动需要支持 OpenGL ES 3.2 图形 API

1. 请先从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Linux]`，后缀为`.7z`的压缩包，之后使用你喜欢的解压缩软件进行解压
2. 安装以下包：
    * **dotnet-runtime-9.0** .NET 运行时 9.0，安装方法详见 [此处](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website)
    * **libopenal-dev** 一个声音API，对于 Ubuntu 系统可运行`sudo apt-get install libopenal-dev`来安装，其他分发版类似
    * **xsel** 一个剪贴板操作API，对于 Ubuntu 系统可运行`sudo apt-get install xsel`来安装

3. 有两种启动方法：
    * 在第 1 步解压出来的目录运行`dotnet Survivalcraft.dll`
    * 同样在解压出来的目录，先运行`chmod +x Survivalcraft`来添加可执行权限（只需要一次），再双击`Survivalcraft`即可

### 常见问题

* 如果游戏打开后语言不是系统语言，请点击左下角第二个图标，即可切换语言
* MOD 文件的后缀为`.scmod`，安装位置：
    * Android 系统：`/storage/emulated/0/Survivalcraft2.4_API1.8/Mods`
    * 其他系统：`(解压到的目录)/Mods`
* 按上面说明处理后仍然打不开游戏，或者运行遇到任何错误，请尝试移除所有 MOD，如果问题依旧，可在[此处](https://gitee.com/SC-SPM/SurvivalcraftApi/issues)反馈问题
* 安装 MOD 后打不开游戏，或者运行遇到任何错误，请先向 MOD 作者反馈问题，再由 MOD 作者向本仓库反馈问题
* 如果游戏帧数不低但鼠标调整视角感觉卡顿，关闭系统设置-鼠标设置-增强指针精度，即可解决

## 模组开发者引用

1. 首先复制本存储库根目录的`nuget.config`文件到你的解决方案文件夹（和`.sln`文件同一层级）

2. 有两种常规方式添加引用包 (nupkg)，请选择你喜欢的方式
   
    * **推荐：** 在解决方案目录运行以下命令：

    ```bat
    dotnet add package SurvivalcraftAPI.Engine
    dotnet add package SurvivalcraftAPI.EntitySystem
    dotnet add package SurvivalcraftAPI.Survivalcraft
    ```

    * 或者手动在`.csproj`文件的`<Project>...</Project>`中添加以下行（下面的版本号可能不是最新的）

    ```xml
    <ItemGroup>
      <PackageReference Include="SurvivalcraftAPI.Engine" Version="1.8.1.3"/>
      <PackageReference Include="SurvivalcraftAPI.EntitySystem" Version="1.8.1.3"/>
      <PackageReference Include="SurvivalcraftAPI.Survivalcraft" Version="1.8.1.3"/>
    </ItemGroup>
    ```

3. 不推荐以上方法之外的引用方式，如果网络实在不通畅无法完成 nupkg 的下载，可从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Nupkgs]`，后缀为`.7z`的压缩包，将其中的所有`nupkg`文件解压到你喜欢的目录，之后按照 [微软官方教程](https://learn.microsoft.com/zh-cn/nuget/hosting-packages/local-feeds) 手动添加
4. 当然还有更麻烦的引用方式，按照上一步提到的方式或其他方式得到 nupkg 后，将其逐一解压，找到其中的`Engine.dll`、`EntitySystem.dll`、`Survivalcraft.dll`，将它们的路径记录下来，在`.csproj`文件的`<Project>...</Project>`中添加以下行（大部分 IDE 支持在图形界面进行该操作，最终达成相同的效果就好）

```xml
<ItemGroup>
  <Reference Include="Engine" HintPath="（在此填写Engine.dll的文件路径，不要括号）" />
  <Reference Include="EntitySystem" HintPath="（EntitySystem.dll的文件路径，不要括号）" />
  <Reference Include="Survivalcraft" HintPath="（Survivalcraft.dll的文件路径，不要括号）" />
</ItemGroup>
```

## 项目构建说明

1. 首先使用 Git 克隆此仓库

    ```bat
    git clone https://gitee.com/SC-SPM/SurvivalcraftApi.git
    ```

    > 还没有 Git？[官网下载](https://git-scm.com/downloads)

2. 进入此仓库，使用 [Visual Studio](https://visualstudio.microsoft.com/) 或 [Rider](https://www.jetbrains.com/zh-cn/rider/) 打开`SurvivalcraftApi`目录中的`SurvivalCraftApi.sln`
3. 如果只是在 Windows 系统上进行调试，请右键卸载`Survivalcraft.Android`和`Survivalcraft.Linux`两个项目，之后在`Survivalcraft.Windows`项目上右键，点击`构建所选项目`即可
4. 如果需要生成 Android 系统上的`APK`安装包，请在`Survivalcraft.Android`项目上右键，点击`加载项目`，再点击`归档以用于发布`，之后按提示操作
5. 以上过程中，如果报错未安装相应功能，请按提示完成安装

## 感谢

* 西班牙语 (Español) 翻译
  * Fire Dragon (Discord: firedragon4095)
  * Kike13 (Discord: .kike.04)
* 越南语 (Tiếng Việt) 翻译
  * Long (Discord: daylong89)
* 罗马尼亚语 (Română) 翻译
  * NBG (Discord: nbgr)
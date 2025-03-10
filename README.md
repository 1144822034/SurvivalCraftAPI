# SurvivalCraft-API

## 介绍

生存战争插件版源码，项目重构版

<details>
<summary>点我展开 把红色赋予黑海 的话</summary>

SCAPI1.8已跟进2.4原版，建议换到1.8x
愿各位模组开发者踏上SCAPI的台阶，向着高处前进！！！

</details>

## 用户下载

[点击此处](https://gitee.com/THPRC/survivalcraft-api/releases/latest) 进入下载页面

* Android系统下载后缀为`.apk`的安装包，安装后即可运行，如果弹出标题为`所有文件访问`的授权窗口，请授权此APP
* Windows系统下载后缀为`.7z`的压缩包，推荐使用 [7-Zip](https://www.7-zip.org/download.html) 进行解压，运行<font color="red">解压后</font>的`.exe`文件

> Windows 系统请勿直接运行压缩包中的文件，务必解压后运行  
> 如果游戏打开后语言不是系统语言，请点击左下角地球图标，即可切换语言

## 模组开发者引用

1. 首先复制本存储库根目录的`Nuget.Config`文件到你的解决方案文件夹（和`.sln`文件同一层级）

2. 有以下两种方式添加引用包（二选一）
   
   * **推荐：** 在解决方案目录运行以下命令：
     
     ```bat
     dotnet add package SurvivalcraftAPI.Engine
     dotnet add package SurvivalcraftAPI.EntitySystem
     dotnet add package SurvivalcraftAPI.Survivalcraft
     ```
   
   * 或者手动在`.csproj`文件的`<Project>...</Project>`中添加以下行（下面的版本号可能不是最新的）
     
     ```xml
     <ItemGroup>
       <PackageReference Include="SurvivalcraftAPI.Engine" Version="1.7.2.2"/>
       <PackageReference Include="SurvivalcraftAPI.EntitySystem" Version="1.7.2.2"/>
       <PackageReference Include="SurvivalcraftAPI.Survivalcraft" Version="1.7.2.2"/>
     </ItemGroup>
     ```

> 不推荐以上方法之外的引用方式

## 项目构建说明

1. 首先使用 Git 克隆此仓库
   
   ```bat
   git clone https://gitee.com/SC-SPM/SurvivalcraftApi.git
   ```
   
   > 还没有 Git？[官网下载](https://git-scm.com/downloads)

2. 进入此仓库，使用 [Visual Studio](https://visualstudio.microsoft.com/)或[Rider](https://www.jetbrains.com/zh-cn/rider/) 打开`survivalcraft-api`目录中的`SurvivalCraft.sln`

3. 如果只是在Windows系统上进行调试，请右键卸载`安卓端`文件夹中的所有项目，在`电脑端`文件夹的`Survivalcraft`项目上右键，点击`构建所选项目`即可

4. 如果需要生成Android系统上的`APK`安装文件，需要切换配置为`Release`模式，在`安卓端`文件夹的`A_Launch2`和`A_Survivalcraft`两个项目上分别右键，点击`加载项目`，最后在`A_Launch2`上右键，点击`归档以用于发布`即可

5. 以上过程中，如果报错未安装相应功能，请按提示完成安装

> 关于项目还原  
> 请运行`dotnet restore /p:Configuration="Release"`

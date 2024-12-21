# SurvivalCraft-API

## 介绍

生存战争插件版源码，项目重构版

<details>
<summary>点我展开 把红色赋予黑海 的话</summary>

SCAPI翻新了引擎，性能大幅度提升，建议换到1.7x
⭐SCAPI1.7发布⭐，采用了全新加载器

推荐新开发者直接从1.7开始，少走弯路
就10分钟，直接进入API1.7新时代 ！
走的是直接穿越的康庄大道 ！！！

API的兄弟们， 让模组无序加载机制, 模组自动依赖检索护送大家远航 !
这么多年同舟共济，辛苦了！ 咱吃饱喝足，拿好开发工具包乘着SCAPI制作出优秀的模组，再上征途 !
莫把未来视虚幻，踏实迈步始为真。
夺得天机真造化，一往无前写华章！！
愿各位模组开发者踏上SCAPI的台阶，向着高处前进！！！
</details>

## 用户下载

[点击此处](https://gitee.com/THPRC/survivalcraft-api/releases/latest) 进入下载页面

* Android系统下载后缀为`.apk`的安装包，安装后即可运行，如果弹出标题为`所有文件访问`的授权窗口，请授权此APP
* Windows系统下载后缀为`.7z`的压缩包，推荐使用 [7-Zip](https://www.7-zip.org/download.html) 进行解压，运行解压后的`.exe`文件

> Windows 系统请勿直接运行压缩包中的文件，务必解压后运行  
> 如果游戏打开后语言不是系统语言，请依次点击：`左下角按钮`→`左下角按钮`→`顶部按钮`，即可切换语言

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
   git clone https://gitee.com/THPRC/survivalcraft-api.git
   ```
   
   > 还没有 Git？[官网下载](https://git-scm.com/downloads)

2. 进入此仓库
   
   ```bat
   cd survivalcraft-api
   ```

3. 更新子模块
   
   ```bat
   git submodule update --init
   ```

4. 使用 [Visual Studio](https://visualstudio.microsoft.com/)或[Rider](https://www.jetbrains.com/zh-cn/rider/) 打开`survivalcraft-api`目录中的`SurvivalCraft.sln`
5. 如果只是在Windows系统上进行调试，请右键卸载`安卓端`文件夹中的所有项目，在`电脑端`文件夹的`Survivalcraft`项目上右键，点击`构建所选项目`即可
6. 如果需要生成Android系统上的`APK`安装文件，需要切换配置为`Release`模式，在`安卓端`文件夹的`A_Launch2`和`A_Survivalcraft`两个项目上分别右键，点击`加载项目`，最后在`A_Launch2`上右键，点击`归档以用于发布`即可
7. 以上过程中，如果报错未安装相应功能，请按提示完成安装

> 关于项目还原  
> 请运行`dotnet restore /p:Configuration="Release"`
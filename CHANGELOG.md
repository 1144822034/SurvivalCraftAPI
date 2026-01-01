# 更新日志

> 说明：此更新日志和发布页的更新日志略有不同

## API 1.8.2.3 (2026-01-01)

### 新增
* 方块详情界面显示动态 ID 和特殊值（需进入存档）
* 创造模式的游戏菜单额外显示玩家重生位置坐标，以及所在位置的温度、湿度
* 支持 ASTC 格式的方块材质

### 修复
* 安卓端鼠标、手柄输入问题全面修复
* 低版本安卓可能闪退的问题
* 生物出生立即死亡后，尸体不消失的问题

### 对于开发者
* 新纹理类：CompressedTexture2D，可加载 ASTC 格式的压缩纹理。  
  优点：无需解码，直接传递给 GPU，压缩率和质量损失尚可。  
  官方工具：[https://github.com/ARM-software/astc-encoder](https://github.com/ARM-software/astc-encoder)  
  推荐命令：`.\astcenc-avx2.exe -cl 输入路径 输出路径 8x8 -exhaustive`
* 新增重启游戏的方法：Window.Restart

## API 1.8.2.2 (2025-12-24)

### 新增
* 全新中文翻译
* 适配安卓全面屏（设置-用户界面-适配全面屏，刘海屏默认开启）

### 修复
* 已禁用模组的资源仍然会加载的问题
* 蹲下仍然会知道跳跃的问题
* Android 端鼠标操作异常的问题
* Android 端编辑文字弹窗的标题和描述没翻译的问题
* 在高版本 Android 运行时，返回键会直接返回到桌面的问题
* Windows 端启用“增强指针精度”时，视角异常跳动的问题
* 一些翻译问题

### 对于开发者
* ModLoader 新增 OnVitalStatsEat，OnVitalStatsUpdateFood，OnVitalStatsUpdateStamina，OnVitalStatsUpdateSleep，OnVitalStatsUpdateTemperature，OnVitalStatsUpdateWetness，OnInventorySlotWidgetCalculateSplitCount 接口
* FastDebugMod 能加载非 Mods 根目录的各类资源文件了
* Widget 新增四个方向的 Margin
* ScreensManager 新增 HistoryStack、GoBack 等
* ComponentFirstPersonModel 新增 ForceDrawHandOnly
* CellFace 新增 FaceToTangents、GetFourVertices、GetSixVertices
* Terrain 等相关类新增大量参数类型为 Point3 的方法（以前只有int x, int y, int z 形式的）

## API 1.8.2.1 (2025-11-24)

### 新增
* 兼容设置中新增安全模式，启用后将停止加载所有模组

### 优化
* 全面优化 Mod 管理界面
* 将原版社区、中文社区按钮合并，并增加新中文社区网页版入口

### 修复
* Android 系统上鼠标能正常在游戏中使用了
* 攻击硬直累加计算导致霰弹超长硬直的问题
* 一个不应该的破坏性变更导致的旧模组不兼容（在插件版1.8.2，`ClothData.Texture` 从字段变成了属性，现已恢复为字段）

### 对于开发者
* 给`HumanReadableConverter`加上了 Nullable 支持
* `Mouse`新增`SetCursorType`方法（其中 Android 系统不生效，原因不明）
* 如前所述，`ClothData.Texture`已恢复为字段

## API 1.8.2 (2025-11-16)

### 新增
* Android 系统打开或分享`.scworld`、`.scmod`等格式文件时，如选择插件版，文件将自动导入到合适的位置，不再需要手动在文件管理器手动复制粘贴了；Windows 系统同理，第一次运行插件版后，将可以直接双击这些格式的文件来导入
* Android 系统上导出存档、家具包等文件后，能立即进行分享
* 再次支持 iOS 系统，安装注意事项详见[此处](https://gitee.com/SC-SPM/SurvivalcraftApi#iosipados-%E7%B3%BB%E7%BB%9F%E7%9C%8B%E8%BF%99%E9%87%8C)
* 创建世界时可编辑真种子（并在多个位置显示真种子），可选择地形生成器版本
* 扩大创建世界时多个选项的范围，包括：岛屿大小、生物群落大小、年天数、平坦地面方块、海平面高度
* 界面设置新增自定义截图分辨率
* 控制设置新增手柄相关设置，且可使用组合键；新增物品快捷栏循环滚动设置
* 视角设置会显示具体角度
* Mod 管理界面新增“查看主页”按钮
* 启动游戏失败会有更具体的提示
* 日志文件能直接从游戏内打开
* 插件版在 Android 系统退出后，会自动从多任务界面移除
* 给 Android 系统的自适应图标添加单色图标

### 修复
* 玩家皮肤显示错误的问题
* 背包空间不足，捡起过多的掉落物时，多余的掉落物会消失的问题
* 南瓜能种在不合适方块上的问题
* 蹲着不能跳的问题
* 创造模式使用桶时的行为与原版不一致的问题
* Windows 系统上文本框中光标与输入法位置不匹配、鼠标点击后光标位置错误、左右滚动过头的问题
* 使用手柄时，打开对话框会把实际鼠标也居中的问题
* Android 系统上第一次启动游戏，跳转到管理所有应用的所有文件访问权限的界面的问题（应该跳转到管理自身的所有文件访问权限的界面）
* 其他小问题

### 对于 mod 开发者
* .Net SDK 升级到 10.0（如果您开发的 mod 没有使用新版增加的东西，可以不用升级引用包和目标框架版本）
* `Engine.Storage`新增多个实用方法：`OpenFileWithExternalApplication`（使用其他应用打开文件）、`ShareFile`（分享文件到其他应用）、`ChooseFile`（使用系统文件选择器选择文件）
* `Engine.Display` 新增 `MaxTextureSize`
* `ClothingData` 新增 `UseLazyLoading`，用于设置纹理是否直接加载或者按需加载，当mod的高清纹理衣服过多时，可以采用按需加载，加快游戏启动速度
* 读取 json 时允许注释和尾随逗号
* 将优先读取 scmod 压缩包根目录的`icon.webp`作为 mod 图标（仍然支持`icon.png`）
* 建议积极设置`modinfo.json`中的 Link，因为玩家将能从 Mod 管理界面打开该链接

#### `modinfo.json`重大更新：

<details>
    <summary>点击展开</summary>
在之前的插件版中，Dependencies只支持固定的一个版本号，从该发布版开始，Dependencies 将支持范围版本，写法上支持 Nuget 风格和少量 SemVer 风格，规则如下表：

| Nuget 示例 | 范围 | 说明 |
| --- | --- | --- |
| 1.0 | x >= 1.0 | 直接一个版本号 |
| [1.0,) | x >= 1.0 | `[` 表示大于等于 |
| (1.0,) | x > 1.0 | `(` 表示大于 |
| (,1.0] | x <= 1.0 | `]` 表示小于等于 |
| (,1.0) | x < 1.0 | `)` 表示小于 |
| [1.0,2.0] | 1.0 <= x <= 2.0 | / |
| [1.0,2.0) | 1.0 <= x < 2.0 | / |
| (1.0,2.0] | 1.0 < x <= 2.0 | / |
| (1.0) | 无效 | 无效 |

| SemVer 示例 | 范围 | 说明 |
| --- | --- | --- |
| =1.0.0 | x = 1.0.0 | / |
| >1.0.0 | x > 1.0.0 | / |
| >=1.0.0 | x >= 1.0.0 | / |
| <2.0.0 | x < 2.0.0 | / |
| <=2.0.0 | x <= 2.0.0 | / |
| ^1.0.0 | 1.0.0 <= x < 2.0.0 | 主要版本号相同 |
| ~1.0.0 | 1.0 <= x < 1.1.0 | 次要版本号相同 |

> 两种写法不支持混合使用！

版本号的写法是 `Major.Minor.Patch.Revision[-Suffix]`，即：主要版本号、次要版本号、补丁版本号、修订版本号、可选后缀，四个版本号均为数字，使用小数点分隔，最后的后缀前面要有`-`，例如：`1.2.3.4-beta`。  
如果转换失败，加载依赖时将只对比字符串是否相同，而不判断范围（像以前的插件版那样）

同时 Dependencies 新增了一种写法，先看旧写法：
```json
{
    "Dependencies": [
        "PackageNameOfModA:1.2",
        "PackageNameOfModB:3.4",
        "PackageNameOfModC:5.6"
    ]
}
```
现在支持新写法：
```json
{
    "Dependencies": {
        "PackageNameOfModA": "1.2",
        "PackageNameOfModB": "3.4",
        "PackageNameOfModC": "5.6"
    }
}
```
两种写法效果相同，但新写法更直观清晰，希望大家喜欢

> ApiVersion 也支持上面写法，但和以前一样，没有实际作用

</details>

## API 1.8.1.3 (2025-8-31)

这是一个修复了很多问题的小型更新

### 新增
* Windows端新增使用Direct3D11图形API的兼容补丁
* 从这个版本开始正式支持Linux系统
* 家具上限翻倍（4096→8192）
* 罗马尼亚语，感谢Discord用户NBG

### 优化
* 扩宽视角设置的范围
* 新增隐藏十字准星的设置
* JS运行对话框添加复制输出的按钮
* 同时只能一个线程保存设置，避免冲突
* 调整一些提示的文字，意思更清晰

### 修复
* 中文以外语言基本不可用的问题
* Android端应用在后台会自杀的问题
* Android端日志超2MB不创建新日志文件的问题
* 主界面更新按钮图标颜色错误的问题
* Windows端切换全屏/窗口会导致视角突变的问题
* ogg音频文件只会播放一半的问题
* 存档选择界面不显示世界数量和总大小的问题
* 南瓜灯不能放在一般方块上的问题
* 射弹反弹速度过大的问题
* 一个mod有多个mod图标导致modinfo加载失败的问题
* ContentManager无法直接读取JsonArray、JsonObject的问题
* Widget.IsUpdateEnabled没用的问题
* 部分界面内容溢出屏幕、内容框的问题

### 对于开发者
* ModLoader新增接口：ClothingProcessSlotItems、IsLevelSufficientForTool、OnAppendModelMeshPart、OnAppendModelMesh
* 新增TransformedShader类
* ModelWidget支持自定义着色器
* 更多图形相关的类中的private改为public，给很多方法加上virtual，部分类新增Tag
* 可以通过EnableDressLimit去除衣服穿戴等级限制
* TextBoxWidget新增属性EnterAsNewLine、MaxLinesCount
* 攻击新增参数ArmorProtectionDivision，射弹新增参数MinVelocityToAttack
* Mount增加allowToStartRange，ScoreMount返回负数时禁止骑乘，ComponentRider增加DetectSurroundingMountRange
* 增加使用旧版参数的DrawCubeBlock的重载方法
* 项目源码已大幅调整结构，解决警告，并统一代码格式

## API 1.8.1.2 (2025-7-01)

### 优化
* Configs文件加载、时间子系统加入错误处理机制
* 重构了熔炉组件
* 更完善的界面国际化；未找到相应语言的字符串时，将使用英语语言中的字符串
* 以同时显示标识牌数量为代价提升标识牌渲染效果


### 修复
* 解决电脑版导出存档不正确的问题
* 修复SubsystemTime.GameTimeFactor变化异常问题
* 修复重置按键可能报错的问题
* 修复mod缺字段时可能无法进入存档问题
* 修复绘制渐变矩形异常问题
* 修复有时加载旧存档时，方块ID不按已有旧档分配方式分配的问题
* 修复找不到图像会导致界面组件停止加载的问题
* 修复烟花、南瓜汤、颜料桶合成信息错误
* 修复方块高亮边框绘制在第一人称手臂之上的问题
* 修复射弹不能打水漂的问题
* 修复部分情况下玩家模型组件显示错误的问题


### 新增
* 添加界面：API更新界面
* 添加性能设置：材质刷新频率


### 面向Mod开发者
* 添加重新计算摄像机投影矩阵的接口
* 添加新的EditBlockDescriptionScreen接口，传入BlockValue
* SubsystemDrawing，SubsystemElectricity，SubsystemUpdate加入性能分析
* 增加接口ClothingSlotProcessCapacity
* RectangleWidget添加BlendState属性
* 数据库的Model组件模板新增Transparent，ModelScale属性
* 增加接口OnProjectileRaycastBody，射弹更新寻找下一个可以命中的Body时执行
* 增加接口IComponentEscapeBehavior，表示水路空三种生物的逃跑行为

## API 1.8.1.1 (2025-5-25)

### 优化和修复
* 日志文件被占用时会显示原因
* 修复Windows端按键盘F11切换全屏为窗口时，窗口大小变为0的bug


### 面向Mod开发者的更新
* PlayerData等待地形的最大时间可修改
* 统一Windows端和Android端ModsManager内ExternalPath等多个成员为属性

## API 1.8.1 (2025-5-20)

### 优化
* 部分安卓设备游戏帧率提高4倍，大幅提升流畅度
* D板编辑后数量不变，即不会变为1
* 主界面布局调整
* 英文和中文排在语言列表前面
* 安卓版启动即横屏
* 伸长活塞能正常绘制
* 部分翻译补充和调整
* 解除掉落物最大可视距离仅为30的限制
* 日志显示界面加上省略号避免过长时卡死，调小网络连接类报错占的篇幅

### 修复
* 对地面使用家具锤可能导致死循环的问题
* 进入损坏存档再进入正常存档会多一个假玩家的问题
* 家具方块电路无法连接的问题
* 部分模型界面显示问题
* 部分区块植物不生长的问题
* 温度条显示错误问题
* 合成台在放入物品后会吞掉副产品的问题
* 文本框跨行显示与光标定位问题
* 电脑按F11全屏经常被恢复的问题
* 星星绘制错误的问题
* 运行时相关文件被其他程序锁死导致游戏无法正常运行

### 新增
* 新增语系（越南语、西班牙语）
* 更新对话框添加“前往发布页”按钮
* 进入缺失模组的世界时弹出警示的功能
* 电脑版键位设置
* 游戏摄像机列表设置
* 删除世界需要输入确认（设置里，默认关闭）
* 手动更新功能

> 以下更新日志来自[把红色赋予黑海的仓库](https://gitee.com/THPRC/survivalcraft-api/)

## API 1.80 (2025-1-16)

跟进原版2.4并修复大量bug

## API 1.72 (2024-11-06)

MOD接口方面（具体用法详见md文件）
### ModInfo结构
模组增加LoadOrder字段

建议：
主题模组的LoadOrder在-100000~-10000的范围，辅助模组的LoadOrder在10000~100000的范围

示例：
```json
{
  "Name": "Gigavolt 十亿伏特",
  "Version": "1.0",
  "ApiVersion": "1.72",
  "Description": "这是一个为生存战争游戏带来十亿伏特电力系统的mod，将原版的16个电压级别（0~1.5V）扩展到2^32个（0~2^32-1V）",
  "ScVersion": "2.3.0.0",
  "Link": "https://github.com/XiaofengdiZhu/Gigavolt",
  "Author": "销锋镝铸",
  "PackageName": "xfdz.Gigavolt",
  "Dependencies": [],
  "LoadOrder": 10000
}
```

### 动态方块ID

为提高模组之间的兼容性，减少方块ID冲突，API1.72采用 **动态方块ID**，方块Index在300及以上的方块会被采用动态方块ID分配，方块定义的Index不再是正确的方块ID。

使用方块的正确Index需要采用
* `BlocksManager.GetBlockIndex(string BlockName)`
* `BlocksManager.GetBlock(string BlockName)`
* `block.BlockIndex`

Block.Index由常量改为静态变量，会随着动态方块ID的分配而改变。建议模组将```Block.Index```修饰类型由const改为static，这样可以通过```Block.Index```来引用方块

SubsystemBlockBehavior不建议使用HandledBlocks，而是在方块的csv里面定义该行为掌控的方块

```csharp
public class xxxBlock : Block
{
    /*public const int Index = 400;*///以前的版本，固定ID
    public static int Index = 400;//改为static变量，使用可变方块ID
}
public class SubsystemXXXBlockBehavior : SubsystemBlockBehavior
{
      //仍然建议使用csv来编写HandledBlocks
      public override int[] HandledBlocks => new int[4]
      {
          BlocksManager.GetBlockIndex<EmptyBucketBlock>(),
          BlocksManager.GetBlockIndex<WaterBucketBlock>(),
          BlocksManager.GetBlockIndex<MagmaBucketBlock>(),
          BlocksManager.GetBlockIndex<MilkBucketBlock>()
      };
}
```

对于升级到1.72的mod，重点排查`Terrain.MakeBlockValue()`的调用，将这些调用均改为动态ID调用

### 常用组件调整
为了保证模组之间的兼容性，不同模组共同发展。API1.72对组件覆盖的行为进行了限制。  
当有模组覆盖了常用组件，或者不同的模组对同一个Guid进行了new-覆盖操作时，会在加载屏幕进行报错。玩家将会选择是否禁用覆盖操作。因此不再建议模组作者对数据库**尤其是常用组件**进行覆盖

覆盖警告的常用组件包含以下组件和guid：
```cs
ModifiedElement["7347a83f-2d46-4fdf-bce2-52677de0b568"] = "Game.ComponentBody";
ModifiedElement["4e14ce27-fdef-46ca-8ea0-26af43c215e5"] = "Game.ComponentHealth";
ModifiedElement["7ecfafc4-4603-424c-87dd-1df59e7ef413"] = "Game.ComponentPlayer";
ModifiedElement["9dc356e5-7dc8-45f6-8779-827ddee9966c"] = "Game.ComponentMiner";
ModifiedElement["6f538db3-f1fe-4e91-8ef5-627c0b1a74ba"] = "Game.ComponentRunAwayBehavior";
ModifiedElement["8b3d07dc-6498-4691-9686-cf4edabb8f3f"] = "Game.ComponentGui";
ModifiedElement["e2636c38-f179-4aa1-b087-ed6920d66e8e"] = "Game.SubsystemTerrain";
ModifiedElement["1c95cd40-26be-44cf-938a-157b318ff086"] = "Game.SubsystemPlantBlockBehavior";
ModifiedElement["937999c9-9570-4cbd-8390-23f1e4609cdd"] = "Game.SubsystemPistonBlockBehavior";
ModifiedElement["96e79f99-a082-4190-9ab6-835dc49ebbdd"] = "Game.SubsystemExplosions";
ModifiedElement["dafb8e14-11b9-44b7-a208-424b770aeaa9"] = "Game.SubsystemProjectiles";
ModifiedElement["32d392de-69c1-4d04-9e0b-5c7463201892"] = "Game.SubsystemPickables";
ModifiedElement["54a4f6d5-98dd-4dc3-bf6d-04dfd972c6b7"] = "Game.SubsystemTime";
```

#### ComponentPlayer
当Interact生效（玩家点击屏幕，或鼠标右键）时，逻辑会进行改变。首先通过方块的新增属性`GetPriorityUse, GetPriorityInteract, GetPriorityPlace`和`ModLoader.OnPlayerInputInteract`来确定<使用手中方块><交互面前方块><放置手中方块>这三个行为的优先级。之后根据三者优先级，选择优先级最高且大于0的行为执行。  
例如：口哨方块放置优先级为0，箱子方块交互优先级为2000，口哨方块使用优先级为3000。那么手持口哨点击箱子时，由于口哨方块使用优先级最高，则会执行“使用口哨”，即吹响口哨。

对玩家的输入行为增加了大量的ModLoader接口，详见下方的ModLoader接口介绍。

#### ComponentLevel
增加了一个ComponentFactors，用于控制生物的力量乘数、防御乘数、移速乘数。而ComponentLevel被改为只和经验值相关的组件。  
所有非玩家生物都拥有了ComponentFactors，玩家的ComponentLevel继承ComponentFactors。

ComponentLocomotion, ComponentMiner, ComponentHealth中引用到的力量、防御、移速乘数被改为引用ComponentFactors。

#### ComponentFactors
新增OnFactorsUpdate接口: 用于更新非玩家生物的力量、防御、移速倍率。

#### SubsystemProjectiles
Update进行了重大调整。射弹的更新逻辑被放到Projectile.Update()中执行。  
这意味着你可以在`SubsystemProjectiles.m_projectiles`容纳各种各样的Projectile。它们有不同的更新行为。  
新增了OnProjectileAdded、SaveProjectile、OnProjectileUpdate接口，用于控制弹射物的行动规律

#### Projectile
增加了以下字段和方法：
```csharp
public bool IsFireProof = false;//该弹射物和掉落物防火，不会被火焰或熔岩烧毁

public float? MaxTimeExist;

public float ExplosionMass = 20f;//处于爆炸中时，进行爆炸计算的质量

public GameEntitySystem.Entity OwnerEntity; //以Entity的形式指明弹射物来源（比如发射器）

public float Damping = -1f;//射弹的速度衰减

public float DampingInFluid = 0.001f;

public float Gravity = 10f;

public float TerrainKnockBack = 0.3f;

public bool StopTrailParticleInFluid = true;

public int DamageToPickable = 1;//弹射物结算时掉的耐久

public bool TerrainCollidable = true;

public bool BodyCollidable = true;

public List<ComponentBody> BodiesToIgnore = new List<ComponentBody>();//弹射物飞行的时候会忽略List中的ComponentBody

public virtual void Update(float dt) //执行弹射物的更新操作

public virtual void OnProjectileFlyOutOfLoadedChunks() //弹射物飞离加载区块时执行的操作

public override void UnderExplosion(Vector3 impulse, float damage)//弹射物受到爆炸时执行的操作
```

#### SubsystemProjectiles
增加了以下ModLoader接口
* OnProjectileHitBody: 弹射物在命中生物时执行。可以控制攻击力、命中后改变弹射物的移速。还可以设置ignoreBody = true来跳过弹射物判定当前Body。模组可以在此接口中增加其他行为。
* OnProjectileHitTerrain: 弹射物在命中方块时执行。可以控制是否摧毁方块，弹射物命中后移速等行为。模组可以在此接口中增加其他行为。
* SavePickable: 存储掉落物

#### SubsystemPickables
* 掉落物系统进行了重大调整。掉落物的更新逻辑被放到Pickable.Update()中执行。  
  这意味着你可以在`SubsystemPickables.m_pickables`容纳各种各样的Pickable。它们有不同的更新行为。
* 掉落物被玩家吸取的逻辑被转移到ComponentPickableGatherer中，拥有此组件并进行合理配置的实体（如玩家）可以收集掉落物。  
  如方块在吸取时并不是直接进入物品栏，而是进行其他操作（如经验球），特殊操作在SubsystemBlockBehavior.OnPickableGathered()中执行。
* 模组作者建议参考ComponentPickableGatherer, ComponentPickableGathererPlayer, SubsystemExperienceBlockBehavior仿照经验球来定义掉落物在拾起的特殊行为。
* 增加了以下ModLoader接口
    * OnPickableAdded：掉落物在创建世界时，或者生成时执行。可以在此时更改掉落物的属性，甚至类别。
    * OnPickableDraw: 控制掉落物的绘制
    * SavePickable
    * OnPickableUpdate

#### Pickable
增加了以下字段和方法：
```cs
    public bool IsFireProof = false;//该弹射物和掉落物防火，不会被火焰或熔岩烧毁
    
    public float? MaxTimeExist;
    
    public float ExplosionMass = 20f;//处于爆炸中时，进行爆炸计算的质量
    public virtual double TimeWaitToAutoPick => m_timeWaitToAutoPick;//掉落物只有在存在时长超过这个时间（初始值为0.5秒）后才能被捡起。
    public virtual float DistanceToPick => m_distanceToPick;//掉落物在距离目标这个范围（初始值为1格）内时，会立即进入玩家背包。
    public virtual float DistanceToFlyToTarget => m_distanceToFlyToTarget;//掉落物在距离目标这个单位（初始值为1.75格）内时，会飞向玩家。
    
    public override void UnderExplosion(Vector3 impulse, float damage)//处于爆炸状态下执行的操作
```

#### SubsystemBodies
增加BodyCountInRaycast接口：可以决断是否将一个ComponentBody纳入Raycast结果当中


#### SubsystemTime
新增ChangeGameTimeDelta接口，用于控制每帧的时间

#### SubsystemUpdate
增加OnIUpdateableAddOrRemove接口，用于控制更新队列

#### SubsystemDrawing
增加OnIDrawableAdded接口，用于控制绘制更新队列

#### Entity
增加RemoveComponent、ReplaceComponent接口，用于控制实体的组件

#### SubsystemFurnitureBlockBehavior
增加OnFurnitureDesigned接口：能够控制创建家具时，产生的家具个数、是否摧毁原有模型，以及家具锤掉落的耐久。如果家具锤耐久不足，则创建家具失败。Mod可以通过该接口修复“家具锤敲已有家具后，能够复制家具”的bug

#### DamageItem
提供接口用于控制物品掉耐久时执行的操作

#### ComponentFactors
增加OnFactorsUpdate接口，用于更新非玩家生物的力量、防御、移速倍率。

#### SubsystemExplosions
增加CalculateExplosionPower接口用于控制爆炸的爆炸强度

#### SubsystemSky
增加OnLightningStrike接口，用于控制雷电劈下时的爆炸强度，距离上一次闪电的时间间隔等属性，模组也可以在这个接口里面添加其他行为。

#### TerrainContentsGenerator23
增加OnTerrainBrushesGenerated接口，用于调整原版已有矿物、水域、植物等地形地貌的生成，例如减少原版矿物生成量。  
模组自身的建筑等生成建议调用OnTerrainContentsGenerated接口

#### ComponentBody
增加以下ModLoader接口：
* OnComponentBodyExploded：用于控制动物在受到爆炸时的伤害、击退等属性
* UpdateComponentBody: 进行实体的更新。建议只对自己mod的实体进行更新，不要插手其他mod的实体。

增加了以下变量：
* TerrainCollidable: 该ComponentBody是否能与地形碰撞。关闭状态意味着该生物能穿墙。
* BodyCollidable: 该ComponentBody是否能与其他动物碰撞
* FluidCollidable: 该ComponentBody是否能受到流体影响
* IsRaycastTransparent: 该生物不能被Raycast选中

#### ComponentHealth
有以下接口变化：
* 弃用ComponentHealth中OnCreatureInjure接口，改为使用以下接口：
    * CalculateCreatureInjuryAmount：用于控制受到伤害的数量
    * OnCreatureDying：在生物生命值低于0濒死时触发，如果将生命值改为大于0的数则取消死亡判定。用于mod生物的免死机制
    * OnCreatureDied：在生物死亡判定时触发，可以控制是否计入击杀、以及经验球的掉落数量
* 增加接口：
    * ChangeVisualEffectOnInjury: 用于控制玩家在受伤后的红屏效果、是否会叫
    * OnCreatureSpiked: 动物被仙人掌、海胆、钉板扎的时候执行，可以控制相关伤害。控制该伤害是否被护甲结算等操作。

增加了以下变量：
* m_regenerateLifeEnabled: 是否允许回血
* HealFactor: 在Heal中，回血的倍数
* VoidDamageFactor: y轴过高或者过低造成的伤害系数。该值为0时，不会显示"Come Back!"
* AirLackResilience: 溺水/搁浅伤害抗性，初始值为8.33
* MagmaResilience: 熔岩伤害抗性，初始值为0.5。当该值为Infinity时，生物寻路不再视熔岩为危险方块。
* CrushResilience: 挤压伤害抗性，初始值为6.67
* SpikeResilience: 尖刺伤害抗性，初始值为10
* ExplosionResilience: 爆炸伤害抗性，初始值为1
* StackExperienceOnKill: 生物在死亡时掉落的经验球的掉落物个数不超过100，超出部分进行堆叠。能够降低卡顿。

#### ComponentMiner
有以下接口变化：
* ComponentMiner.AddHitValueParticleSystem允许非玩家攻击：当攻击来源并非玩家时，仍然会创建一个透明的伤害显示粒子。Mod可以调整这个粒子的颜色来令其显现。
* ComponentMiner.AttackBody：变量attacker的类型由ComponentCreature改为Entity
* Raycast方法调整：可以忽略动物、方块或移动方块，达到更灵活的投影
* 增加OnBlockDug接口：用于控制在方块被挖掘完毕后触发的行为，也可以控制挖掘方块后损失的耐久数量

#### ComponentPlayer
增加以下接口：
* OnPlayerControlSteed, OnPlayerControlBoat, OnPlayerControlWalk：用于控制玩家的移动
* OnPlayerInputInteract：能够控制玩家通过点击屏幕、鼠标右键执行Interact操作的时间间隔，以及改变放置方块、使用手中方块、交互面前方块三个操作的优先级，以及模组在其中执行其他操作。
* UpdatePlayerInputAim：每帧都进行更新，可以控制玩家执行瞄准操作的时间间隔。（可以简单理解为投掷攻速），以及模组在其中执行其他操作。
* OnPlayerInputHit：控制玩家在执行攻击输入后，执行的操作。可以控制距离上一次的输入间隔（注意不是玩家攻击间隔）、近战攻击距离。当近战攻击距离≤0时，玩家无法再该情况下执行近战攻击。通常用于枪械等远程武器使用左键攻击。
* UpdatePlayerInputDig：每帧都进行更新，可以控制玩家挖掘方块之间的时间间隔，以及模组在其中执行其他操作。
* OnPlayerInputDrop：控制玩家在按下Q进行丢弃后，执行的操作

增加m_aimStartTime变量：瞄准操作开始的时间

#### InventorySlotWidget
增加以下接口：
* OnInventorySlotWidgetDefined：在InventorySlotWidget中添加模组自定义的元素
* InventorySlotWidgetMeasureOverride：控制物品格子的绘制，绘制耐久度、腐烂图标、物品数量等信息。模组可以在其中绘制模组自带的内容
* HandleMoveInventoryItem：控制物品栏移动物品执行的操作
* HandleInventoryDragProcess：控制物品栏拖动物品执行的操作（比如火药上枪）
* HandleInventoryDragMove：控制物品栏拖动物品，且Process不执行时进行的操作

### 新增变量
* ComponentOnFire增加了以下变量：`m_hideFireParticle`: 为true时，该生物会隐藏火焰音效和粒子
* ComponentRunAwayBehavior：增加变量`LowHealthToEscape`，用于调整动物在生命值低于多少时才触发逃跑行为
* ComponentMiner: 增加变量`HitInterval`，用于控制玩家和一般生物的攻击间隔
* SubsystemTime：将`MaxGameTimeDelta、MaxFixedGameTimeDelta、DefaultFixedTimeStep、DefaultFixedUpdateStep、GameMenuDialogTimeFactor`由常量改为可修改的变量
* SubsystemUpdate：增加`UpdateTimeDebug`字段，用于控制是否在日志中输出各组件更新性能，便于mod对卡顿进行调试
* ComponentHealth：部分字段改为virtual
* 在存档中增加了APIVersion字段
* SubsystemBlockBehavior：不需要再声明HandledBlocks，默认值为空表
* Entity.Components、Project.Subsystems权限改为可写。


### 方块调整
新增方法
* IsCollapseSupportBlock、IsCollapseDestructibleBlock：用于控制方块是否能够承受沙子等重力方块
* IsMovableByPiston、IsBlockingPiston：用于控制方块能否被活塞推动
* IsSuitableForPlants：用于控制方块能否种植植物（如扩展mod的黑土）
* IsFaceSuitableForElectricElements：用于控制方块是否能够放置电路元件（比如允许玻璃上面放置电路元件）
* ShouldBeAddedToProject：是否进入游戏文件动态ID列表
* CanBlockBeBuiltIntoFurniture：方块是否能被计入家具
* ShouldAvoid(int value, ComponentPilot componentPilot)：可以根据ComponentPilot不同来决定是否调整
* IsNonAttachable(int value)：方块能否附着其他方块。该方法是旧版本IsTransparent_的变体。设置为true时，该方块即使是透明方块，也能够放置门、火堆等以前不能放置在透明方块上的物体。
* GetPriorityUse(int value, ComponentMiner componentMiner)
* GetPriorityInteract(int value, ComponentMiner componentMiner)
* GetPriorityPlace(int value, ComponentMiner componentMiner)：用于控制在ComponentPlayer中，使用手中方块、交互面前的方块、放置手中方块这三个行为的优先级。

数据调整
* 空气方块在Content不为0时，显示为白色铁栅栏，名为“错误方块+方块完整值”，便于Mod作者进行调试
* 玩家穿着的衣物方块不必是ClothingBlock或者203号方块，模组可以控制玩家穿戴其他方块

### 其他调整
* 生物图鉴中，战利品数量为0的占位掉落物不显示
* ComponentHealth.Attacked，从受到伤害触发改为受到AttackBody时触发。部分ComponentBehavior也据此改为由ComponentHealth.Injured触发
* PlayerData中SpawnPlayer()执行出现异常时，会返回世界选择界面并记录游戏日志
* 桶堆叠使用bug修复：使用堆叠状态下的桶时，桶的数量正常变化，而不是强制让手上桶的个数减少为1。
* 游戏运行出现错误时，会直接弹出日志窗口，更方便协助定位错误。
* ComponentCraftingTable只在物品栏改变并进行更新后，才执行配方更新操作，降低卡顿。
* 在超平坦世界中，如果地表高度<0，那么会移除最底部的基岩层，达到类似空岛的效果

### 设置调整
设置选项增加：
* 水平创造飞行：开启时飞行方式和MC相同，关闭时飞行方式和移动版相同
* 创造模式拖动整组方块：在创造模式下拖动方块时，会一次拖动一组方块
* Split状态下拖动一半方块：在物品栏中长按一个格子进入Split状态时，拖动该格子的方块会转移该格子的一半数量的方块。
* 删除世界输入"yes"确认：在删除世界时，需要输入yes来确认删除。关闭后和旧版删除方式相同。

## API 1.71A (2024-08-10)

api1.7准正式版

> 中间更新记录丢失

## API 1.63 (2024-04-25)

* 得益于使用第三方图片库ImageSharp，现在额外支持Webp、Gif、Pbm、Qoi、Tiff、Tga图片格式，且Jpg格式不再偏色
   使用无损Webp格式，能比之前使用Png格式减少50%体积，加载快20%，现在Content.zip中的所有Png已替换为Webp；另外，加载Png格式要比之前快50%  
   开发者可以在项目中引用Nuget包SixLabors.ImageSharp来使用该库的完整功能，例如裁剪、缩放、滤镜等，注意最后不要将SixLabors.ImageSharp.dll打包进mod
* Image.Pixels从字段改成只读属性，新增字段m_trueImage，类型是SixLabors.ImageSharp.Image<Rgba32>，新增两个方法：
    * ProcessPixels(Func<Rgba32, Rgba32> pixelFunc, bool shouldUpdatePixelsCache = true)
      用法举例（将图片的所有像素的透明度改成128）：
      image.ProcessPixels(pixel => new Rgba32(pixel.R, pixel.G, pixel.B, 128), true);
    * ProcessPixelRows(PixelAccessorAction<Rgba32> accessorAction, bool shouldUpdatePixelsCache = true)
      用法举例（将图片的所有像素复制到Color数组中）：
```cs
Color pixels = new Color[image.Width * image.Height];
image.ProcessPixelRows(
    accessor => {
        Span<Color> pixelsSpan = m_pixels.AsSpan();
        for (int y = 0; y < accessor.Height; y++)
        {
            //GetRowSpan是获取该行所有像素的Span<Rgba32>，效率极高
            MemoryMarshal.Cast<Rgba32, Color>(accessor.GetRowSpan(y)).CopyTo(pixelsSpan.Slice(y * Width, Width));
        }
    }, false
);
```
* 额外支持音频格式Flac、Mp3，其中Flac必须为16位深  
   现在Content.zip中的所有Wav已替换为体积小解码快的无损Flac  
   （使用第三方音频库NAudio.Core、NAudio.Flac、NLayer.NAudioSupport，开发者可调用NAudio.Core来加载它支持的更多小众格式音频）
* Json加载库替换为.Net自带的System.Text.Json，它比之前的SimpleJson判定更严格，例如不允许多余的逗号之类的，如开发者发现相关报错，请规范自己的Json文件

> 中间更新记录丢失

## API 1.53 (2023-10-29)

* 支持Javascript脚本，可以在游戏菜单直接运行或放进mod中加载并运行。
* 世界名长度上限改为128。
* 解决mod管理器无法扫描0kb文件导致一直加载的问题。
* 修复鼠标指针问题，适配全面屏
* 在游戏社区【只看我的】可以看到自己上传的作品，可删除和发布。
* 修复告示牌报错。
* 部分翻译调整。
* 更改DrawFlatBlock接口的纹理位置获取，贴图异常请重写GetTextureSlotCount并return 1。
* 将BaseFlatBatch的顶点缓存数组从ushort改为int，理论上解决了所有拉丝。
* 解决mod不能使用其他mod中的dll的问题，优化了依赖项加载。
* 让存档导入导出时可保持原本的文件夹结构。
* 更改mod加固规则。
* 解决地形接口只有一个mod生效的问题。
* 解决合成配方界面说明的显示问题。
* 优化游戏内登录逻辑，游戏社区新增登录按钮。
* 新增方块亮度调整接口。
* 新增视图雾颜色调整接口。
* 新增社区管理员模式。

> 中间更新记录丢失

## API 1.44 (2023-09-27)

1. 解决音频卡顿问题，现在不用开音轨缓存了，不会没有声音了。
2. 完善mod管理器，现在进入管理器不会闪退了，除此之外，更新了很多功能。
3. 新增更多接口，mod能实现更多样的效果。
4. 电脑可连续打字，不用一个字一个字码了，同时修复误锁键盘的bug。
5. 优化外置材质的绘制，游玩更流畅。
6. 修复多人同屏地形不显示的bug，现在可以正常地添加或删除玩家。

> 更久以前的更新记录丢失
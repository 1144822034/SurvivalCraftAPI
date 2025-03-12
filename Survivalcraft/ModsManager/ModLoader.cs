using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System.Collections.Generic;
using System.Xml.Linq;
using TemplatesDatabase;
using static Game.ComponentLevel;

namespace Game
{
    public abstract class ModLoader
    {
        public ModEntity Entity;

        /// <summary>
        /// 当ModLoader类被实例化时执行
        /// </summary>
        public virtual void __ModInitialize()
        {
        }

		/// <summary>
		/// 在加载本模组的资源时触发。
		/// 注意：模组的dll只能由原版逻辑加载
		/// </summary>
		/// <param name="extension">准备调用的文件的扩展名</param>
		/// <param name="action">执行的操作</param>
		/// <param name="skip">跳过SC本体对模组获取文件的执行。由于该方法只会在调用你的模组的资源时触发，所以不必担心兼容性问题。</param>
		public virtual void GetModFiles(string extension, Action<string,Stream> action, out bool skip)
		{
			skip = false;
		}

		/// <summary>
		/// 在系统读取本模组的文件时触发
		/// 注意：modinfo文件、mod图标只能由原版逻辑加载。如果需要修改调整，请自己在模组中重新写一遍加载逻辑
		/// </summary>
		/// <param name="filename">获取模组文件的名称或前缀</param>
		/// <param name="stream">文件流</param>
		/// <param name="skip">跳过SC本体对模组获取文件的执行。由于该方法只会在调用你的模组的资源时触发，所以不必担心兼容性问题。</param>
		/// <param name="fileFound">在skip过后，返回是否得到文件</param>
		public virtual void GetModFile(string filename, Action<Stream> stream, out bool skip, out bool fileFound)
		{
			skip = false;
			fileFound = false;
		}

		/// <summary>
		/// 在加载存档前，比较当前模组的版本和存档上次使用模组的版本
		/// 仅比较本模组
		/// </summary>
		/// <param name="currentVersion">当前模组的版本</param>
		/// <param name="lastVersion">存档上一次使用模组的版本</param>
		/// <returns>返回true表示当前版本模组能加载目标版本模组的存档，返回false则不加载</returns>
		public virtual bool CompareModVersion(string currentVersion, string lastVersion)
		{
			return true;
		}

        /// <summary>
        /// Mod被卸载时执行
        /// </summary>
        public virtual void ModDispose()
        {
        }
        /// <summary>
        /// 视图雾颜色调整
        /// </summary>
        /// <param name="ViewUnderWaterDepth">大于0则表示在水下</param>
        /// <param name="ViewUnderMagmaDepth">大于0则表示在岩浆中</param>
        /// <param name="viewFogColor">视图雾颜色</param>
        public virtual void ViewFogColor(float ViewUnderWaterDepth, float ViewUnderMagmaDepth, ref Color viewFogColor)
        {

        }
        /// <summary>
        /// 方块亮度
        /// （黑暗区域亮度）
        /// </summary>
        /// <param name="brightness">亮度值</param>
        public virtual void CalculateLighting(ref float brightness)
        {

        }
        /// <param name="attackPower">伤害值</param>
        /// <param name="playerProbability">玩家命中率</param>
        /// <param name="creatureProbability">生物命中率</param>
        public virtual void OnMinerHit(ComponentMiner miner, ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection, ref float attackPower, ref float playerProbability, ref float creatureProbability, out bool Hitted)
        {
            Hitted = false;
        }
		/// <summary>
		/// 设置伤害间隔
		/// </summary>
		public virtual void SetHitInterval(ComponentMiner miner,ref double hitInterval)
        {

        }

		/// <summary>
		/// 在生物攻击时执行
		/// </summary>
		/// <param name="componentMiner">攻击者</param>
		/// <param name="componentBody">被攻击方的ComponentBody</param>
		/// <param name="hitPoint">命中点</param>
		/// <param name="hitDirection">命中时的击退方向</param>
		/// <param name="durabilityReduction">攻击后，手中武器掉的耐久量</param>
		/// <param name="attackment">发起的攻击</param>
        public virtual void OnMinerHit2(ComponentMiner componentMiner, ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection, ref int durabilityReduction, ref Attackment attackment)
        {

        }

        /// <summary>
        /// 当人物挖掘时执行
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="raycastResult"></param>
        /// <returns></returns>
        public virtual void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged)
        {
            Digged = false;
        }


		/// <summary>
		/// 在人物放置时执行，在OnMinerPlace之前
		/// </summary>
		/// <param name="componentMiner"></param>
		/// <param name="terrainRaycastResult"></param>
		/// <param name="x">放置方块的坐标x</param>
		/// <param name="y">放置方块的坐标y</param>
		/// <param name="z">放置方块的坐标z</param>
		/// <param name="placementData">包含放置方块的方块表面等信息</param>
		/// <param name="PlacementNotAllowed">返回true则玩家不能放置方块</param>
		public virtual void BeforeMinerPlace(ComponentMiner componentMiner, TerrainRaycastResult terrainRaycastResult, int x, int y, int z, BlockPlacementData placementData, out bool PlacementNotAllowed)
		{
			PlacementNotAllowed = false;
		}

        /// <summary>
        /// 当人物放置时执行，若Placed为true则不执行原放置操作
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="raycastResult"></param>
        /// <returns></returns>
        public virtual void OnMinerPlace(ComponentMiner miner, TerrainRaycastResult raycastResult, int x, int y, int z, int value, out bool Placed)
        {
            Placed = false;
        }

        /// <summary>
        /// 设置雨和雪的颜色
        /// </summary>
        /// <param name="rainColor"></param>
        /// <param name="snowColor"></param>
        /// <returns></returns>
        public virtual bool SetRainAndSnowColor(ref Color rainColor, ref Color snowColor)
        {
            return false;
        }

        /// <summary>
        /// 设置家具的颜色
        /// </summary>
        public virtual void SetFurnitureDesignColor(FurnitureDesign design, Block block, int value, ref int FaceTextureSlot, ref Color Color)
        {
        }

        /// <summary>
        /// 更改击退和晕眩效果
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="hitPoint">伤害位置</param>
        /// <param name="impulseFactor">击退效果</param>
        /// <param name="stunTimeFactor">眩晕时间</param>
        /// <param name="recalculate">是否重写眩晕？</param>
        [Obsolete("该方法已过时，请使用ProcessAttackment修改Attackment的击退和击晕属性")]
        public virtual void AttackPowerParameter(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, ref float impulseFactor, ref float stunTimeFactor, ref bool recalculate)
        {
        }
        /// <summary>
        /// 当人物吃东西时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="block"></param>
        /// <param name="value"></param>
        /// <param name="count"></param>
        // <param name="processCount"></param>
        // <param name="processedValue"></param>
        // <param name="processedCount"></param>
        /// <returns>如果为 true：不移交到下一个 mod 处理</returns>
        public virtual bool ClothingProcessSlotItems(ComponentPlayer componentPlayer, Block block, int slotIndex, int value, int count)
        {
            return false;
        }
        public virtual void SetClothes(ComponentClothing componentClothing, ClothingSlot slot, IEnumerable<int> clothes)
        {

        }

        /// <summary>
        /// 动物吃掉落物时执行
        /// </summary>
        public virtual void OnEatPickable(ComponentEatPickableBehavior eatPickableBehavior, Pickable EatPickable, out bool Dealed)
        {
            Dealed = false;
        }

        /// <summary>
        /// 人物出生时执行
        /// </summary>
        public virtual bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position)
        {
            return false;
        }

		/// <summary>
		/// 当人物死亡时执行。在玩家进入世界且玩家处于死亡状态时也会执行
		/// 可以通过playerData.m_stateMachine.PreviousState == "Playing"，来判断是刚死的，还是加载世界的时候就已经死了。并规避由于退出重进世界造成的“反复死亡判断”bug
		/// </summary>
		/// <param name="playerData"></param>
		public virtual void OnPlayerDead(PlayerData playerData)
        {
        }

        /// <summary>
        /// 当Miner执行AttackBody方法时执行
        /// </summary>
        /// <param name="target"></param>
        /// <param name="attacker"></param>
        /// <param name="hitPoint"></param>
        /// <param name="hitDirection"></param>
        /// <param name="attackPower"></param>
        /// <param name="isMeleeAttack"></param>
        /// <returns>false移交到下一个Mod处理,true不移交</returns>
        [Obsolete("该方法已弃用，请使用ProcessAttackment", true)]
        public virtual bool AttackBody(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, ref float attackPower, bool isMeleeAttack)
        {
            return false;
        }

        /// <summary>
        /// 在攻击时执行
        /// </summary>
        /// <param name="attackment"></param>
        public virtual void ProcessAttackment(Attackment attackment)
        {

        }

        /// <summary>
        /// 当模型对象进行模型设值时执行
        /// </summary>
        public virtual void OnSetModel(ComponentModel componentModel, Model model, out bool IsSet)
        {
            IsSet = false;
        }

        /// <summary>
        /// 当动物模型对象作出动画时执行
        /// Skip为是否跳过原动画代码
        /// </summary>
        public virtual void OnModelAnimate(ComponentCreatureModel componentCreatureModel, out bool Skip)
        {
            Skip = false;
        }

		/// <summary>
		/// 计算护甲免伤时执行
		/// </summary>
		/// <param name="componentClothing"></param>
		/// <param name="attackPower">未计算免伤前的伤害</param>
		/// <returns>免伤后的伤害，当多个mod都有免伤计算时，取最小值</returns>
		[Obsolete("护甲系统调整，该接口不再适用。对于调整护甲的结算顺序，请调用DecideArmorProtectionSequence接口；对于调整单件护甲的防御效果，请给被调整的护甲创建新的ClothingData。")]
        public virtual float ApplyArmorProtection(ComponentClothing componentClothing, float attackPower, bool appliedByOtherMods, out bool Applied)
        {
            Applied = false;
            return attackPower;
        }

		/// <summary>
		/// 等级组件更新时执行
		/// </summary>
		/// <param name="level"></param>
		[Obsolete("You can update your factors in your mod components")]
		public virtual void OnLevelUpdate(ComponentLevel level)
        {
        }

		/// <summary>
		/// 因素控制力量、抗性、速度、饥饿速率组件更新时执行
		/// </summary>
		/// <param name="componentFactors"></param>
		[Obsolete("You can update your factors in your mod components")]
        public virtual void OnFactorsUpdate(ComponentFactors componentFactors, float dt)
        {
        }

        /// <summary>
        /// Gui组件帧更新时执行
        /// </summary>
        /// <param name="componentGui"></param>
        public virtual void GuiUpdate(ComponentGui componentGui)
        {
        }

        /// <summary>
        /// Gui组件绘制时执行
        /// </summary>
        /// <param name="componentGui"></param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        public virtual void GuiDraw(ComponentGui componentGui, Camera camera, int drawOrder)
        {
        }

        /// <summary>
        /// 更新输入时执行
        /// </summary>
        /// <param name="componentInput"></param>
        public virtual void UpdateInput(ComponentInput componentInput, WidgetInput widgetInput)
        {
        }

        /// <summary>
        /// ViewWidget绘制屏幕时执行
        /// </summary>
        public virtual void DrawToScreen(ViewWidget viewWidget, Widget.DrawContext dc)
        {
        }

        /// <summary>
        /// 衣物背包界面被打开时执行
        /// </summary>
        /// <param name="componentGui"></param>
        /// <param name="clothingWidget"></param>
        public virtual void ClothingWidgetOpen(ComponentGui componentGui, ClothingWidget clothingWidget)
        {
        }

        /// <summary>
        /// 当方块被炸掉时执行
        /// </summary>
        public virtual void OnBlockExploded(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value)
        {
        }

        /// <summary>
        /// 当实体被添加时执行
        /// </summary>
        public virtual void OnEntityAdd(Entity entity)
        {
        }

        /// <summary>
        /// 当实体被移除时执行
        /// </summary>
        public virtual void OnEntityRemove(Entity entity)
        {
        }

        /// <summary>
        /// 自然生成生物列表初始化时执行
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="creatureTypes"></param>
        public virtual void InitializeCreatureTypes(SubsystemCreatureSpawn spawn, List<SubsystemCreatureSpawn.CreatureType> creatureTypes)
        {
        }

        /// <summary>
        /// 生物出生时执行
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="entity"></param>
        /// <param name="spawnEntityData"></param>
        public virtual void SpawnEntity(SubsystemSpawn spawn, Entity entity, SpawnEntityData spawnEntityData, out bool Spawned)
        {
            Spawned = false;
        }

        /// <summary>
        /// 当生物消失时执行
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentSpawn"></param>
        public virtual void OnDespawned(Entity entity, ComponentSpawn componentSpawn)
        {
        }

        public virtual void CalculateExplosionPower(SubsystemExplosions subsystemExplosions, ref float explosionPower)
        {

        }
        
		/// <summary>
		/// 生物等实体在遭受爆炸时执行
		/// </summary>
		/// <param name="componentBody">遭受爆炸的实体Body</param>
		/// <param name="explosionInjury">该爆炸的Injuty</param>
		/// <param name="Impulse">爆炸的击退力</param>
		/// <param name="SetOnFire">爆炸是否会让实体着火</param>
		/// <param name="Fluctuation">爆炸的击退、伤害浮动系数。目前只对非生物的船等实体有效</param>
        public virtual void OnComponentBodyExploded(ComponentBody componentBody, ref Injury explosionInjury, ref Vector3 Impulse, ref bool SetOnFire, ref float Fluctuation)
        {

        }

        /// <summary>
        /// 死亡前瞬间执行
        /// </summary>
        public virtual void DeadBeforeDrops(ComponentHealth componentHealth, ref KillParticleSystem killParticleSystem, ref bool dropAllItems)
        {
        }

        /// <summary>
        /// 重定义方块更改方法，Skip为true则不执行原ChangeCell代码
        /// </summary>
        public virtual void TerrainChangeCell(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value, out bool Skip)
        {
            Skip = false;
        }

        /// <summary>
        /// 重定义生物受伤方法，Skip为true则不执行原Injure代码
        /// </summary>
        [Obsolete("该方法已被弃用，请使用CalculateCreatureInjuryAmount, OnCreatureDying, OnCreatureDied代替", true)]
        public virtual void OnCreatureInjure(ComponentHealth componentHealth, float amount, ComponentCreature attacker, bool ignoreInvulnerability, string cause, out bool Skip)
        {
            Skip = false;
        }

        /// <summary>
        /// 计算生物收到伤害的量
        /// </summary>
        public virtual void CalculateCreatureInjuryAmount(Injury injury)
        {

        }

        /// <summary>
        /// 如果动物受到Injure且生命值小于0时，执行操作。
		/// 如果在函数执行完毕后Health > 0，则取消死亡判定。
		/// 通常用于各种模组的“不死图腾”机制
        /// </summary>
        /// <param name="componentHealth"></param>
        public virtual void OnCreatureDying(ComponentHealth componentHealth, Injury injury)
        {

        }

        /// <summary>
        /// 在动物收到Injure()且生命值低于0时，执行操作。
        /// </summary>
        /// <param name="componentHealth"></param>
        public virtual void OnCreatureDied(ComponentHealth componentHealth, Injury injury, ref int experienceOrbDrop, ref bool CalculateInKill)
        {

        }

		/// <summary>
		/// 每帧更新的时候，调整血量带来的视觉效果
		/// </summary>
		/// <param name="componentHealth"></param>
		/// <param name="lastHealth">在扣血之前的生命值</param>
		/// <param name="redScreenFactor">玩家的红屏效果</param>
		/// <param name="playPainSound">是否播放受伤音效</param>
		/// <param name="healthBarFlashCount">玩家血条闪烁次数</param>
		/// <param name="creatureModelRedFactor">生物模型变红，为0时不变红，为1时完全红色</param>
        public virtual void ChangeVisualEffectOnInjury(ComponentHealth componentHealth, float lastHealth, ref float redScreenFactor, ref bool playPainSound, ref int healthBarFlashCount, ref float creatureModelRedFactor)
        {

        }

        public virtual void OnCreatureSpiked(ComponentHealth componentHealth, SubsystemBlockBehavior spikeBlockBehavior, CellFace cellFace, float velocity, ref Injury blockInjury)
        {

        }

        /// <summary>
        /// 更改天空颜色
        /// </summary>
        public virtual Color ChangeSkyColor(Color oldColor, Vector3 direction, float timeOfDay, int temperature)
        {
            return oldColor;
        }

        /// <summary>
        /// 设置着色器参数
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="camera"></param>
        public virtual void SetShaderParameter(Shader shader, Camera camera)
        {
        }

        /// <summary>
        /// 更改模型着色器参数的值
        /// </summary>
        public virtual void ModelShaderParameter(Shader shader, Camera camera, List<SubsystemModelsRenderer.ModelData> modelsData, float? alphaThreshold)
        {
        }

        /// <summary>
        /// 天空额外绘制
        /// </summary>
        public virtual void SkyDrawExtra(SubsystemSky subsystemSky, Camera camera)
        {
        }

        /// <summary>
        /// 设置生物最大组件数，多个Mod时取最大
        /// </summary>
        /// <returns></returns>
        public virtual int GetMaxInstancesCount()
        {
            return 7;
        }

        /// <summary>
        /// 绘制额外模型数据的方法，如人物头顶的名字
        /// </summary>
        /// <param name="modelsRenderer"></param>
        /// <param name="modelData">正在绘制的模型</param>
        /// <param name="camera"></param>
        /// <param name="alphaThreshold"></param>
        public virtual void OnModelRendererDrawExtra(SubsystemModelsRenderer modelsRenderer, SubsystemModelsRenderer.ModelData modelData, Camera camera, float? alphaThreshold)
        {
        }

        /// <summary>
        /// 设定伤害粒子参数
        /// </summary>
        /// <param name="hitValueParticleSystem">粒子</param>
        /// <param name="attackment">产生该攻击粒子的攻击，为null表示攻击没有命中</param>
        public virtual void SetHitValueParticleSystem(HitValueParticleSystem hitValueParticleSystem, Attackment attackment)
        {
        }
        /// <summary>
        /// 当储存生物数据时
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="spawnEntityData"></param>
        public virtual void OnSaveSpawnData(ComponentSpawn spawn, SpawnEntityData spawnEntityData)
        {


        }
        /// <summary>
        /// 当读取生物数据时
        /// </summary>
        public virtual void OnReadSpawnData(Entity entity, SpawnEntityData spawnEntityData)
        {
        }

        /// <summary>
        /// 区块地形生成时
        /// 注意此方法运行在子线程中
        /// </summary>
        /// <param name="chunk"></param>
        public virtual void OnTerrainContentsGenerated(TerrainChunk chunk)
        {
        }

        /// <summary>
        /// 当区块即将被释放时
        /// KeepWorking为True时该区块会继续保持运作，不被释放
        /// </summary>
        public virtual void ToFreeChunks(TerrainUpdater terrainUpdater, TerrainChunk chunk, out bool KeepWorking)
        {
            KeepWorking = false;
        }

        /// <summary>
        /// 加载指定区块,如有区块数变动返回 true，否则返回 false
        /// </summary>
        // <param name="chunk"></param>
        public virtual bool ToAllocateChunks(TerrainUpdater terrainUpdater, TerrainUpdater.UpdateLocation[] locations)
        {
            return false;
        }

        /// <summary>
        /// 子系统帧更新时执行
        /// </summary>
        public virtual void SubsystemUpdate(float dt)
        {
        }

        /// <summary>
        /// 当Project被加载时执行
        /// </summary>
        /// <param name="project"></param>
        public virtual void OnProjectLoaded(Project project)
        {
        }

        /// <summary>
        /// 当Project被释放时执行
        /// </summary>
        public virtual void OnProjectDisposed()
        {
        }

        /// <summary>
        /// 方块初始化完成时执行
        /// </summary>
        public virtual void BlocksInitalized()
        {
        }

        /// <summary>
		/// 存档开始加载前执行
		/// </summary>
		/// <param name="playScreen">世界选择界面</param>
		/// <param name="item">类型为WorldInfo，存储即将进入的存档信息</param>
		/// <returns>返回类型应当保证为WorldInfo，返回null可以阻止玩家进入存档</returns>
        public virtual object BeforeGameLoading(PlayScreen playScreen, object item)
        {
            return item;
        }
        /// <summary>
        /// 加载任务开始时执行
        /// 在BlocksManager初始化之前
        /// </summary>
        public virtual void OnLoadingStart(List<System.Action> actions)
        {

        }

        /// <summary>
        /// 加载任务结束时执行
        /// 在BlocksManager初始化之后
        /// </summary>
        /// <param name="actions"></param>
        public virtual void OnLoadingFinished(List<System.Action> actions)
        {
        }

        /// <summary>
        /// 模组设置保存时执行
        /// </summary>
        /// <param name="xElement">一个空容器，模组作者需将自己的设置信息存入这个空容器内</param>
        public virtual void SaveSettings(XElement xElement)
        {
        }

        /// <summary>
        /// 模组设置加载时执行
        /// 模组作者有必要在读取模组设置后将设置数据放入自己的类或结构中
        /// API在运行时不会保存着模组设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadSettings(XElement xElement)
        {
        }

        /// <summary>
        /// Xdb文件加载时执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void OnXdbLoad(XElement xElement)
        {
        }

        /// <summary>
        /// Project.xml加载时执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void ProjectXmlLoad(XElement xElement)
        {
        }

        /// <summary>
        /// Project.xml保存时执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void ProjectXmlSave(XElement xElement)
        {
        }

		/// <summary>
		/// Project.xml在所有的Entity和Subsystem保存完毕后再执行
		/// </summary>
		/// <param name="xElement"></param>
		public virtual void OnProjectXmlSaved(XElement xElement)
		{

		}

		/// <summary>
		/// 配方解码时执行
		/// </summary>
		/// <param name="element">配方的Xelement</param>
		/// <param name="Decoded">是否解码成功，不成功交由下一个Mod处理</param>
		public virtual void OnCraftingRecipeDecode(List<CraftingRecipe> m_recipes, XElement element, out bool Decoded)
        {
            Decoded = false;
        }

        /// <summary>
        /// 配方匹配时执行
        /// </summary>
        /// <param name="requiredIngredients"></param>
        /// <param name="actualIngredient"></param>
        /// <param name="Matched">是否匹配成功，不成功交由下一个Mod处理</param>
        public virtual bool MatchRecipe(string[] requiredIngredients, string[] actualIngredient, out bool Matched)
        {
            Matched = false;
            return false;
        }

        /// <summary>
        /// 获得解码结果时执行
        /// </summary>
        /// <param name="result">结果字符串</param>
        /// <param name="Decoded">是否解码成功，不成功交由下一个Mod处理</param>
        /// <returns></returns>
        public virtual int DecodeResult(string result, out bool Decoded)
        {
            Decoded = false;
            return 0;
        }

        /// <summary>
        /// 解码配方
        /// </summary>
        /// <param name="ingredient"></param>
        /// <param name="craftingId"></param>
        /// <param name="data"></param>
        /// <param name="Decoded">是否解码成功，不成功交由下一个Mod处理</param>
        public virtual void DecodeIngredient(string ingredient, out string craftingId, out int? data, out bool Decoded)
        {
            Decoded = false;
            craftingId = string.Empty;
            data = null;
        }

        /// <summary>
        /// 改变相机模式时执行
        /// </summary>
        /// <param name="m_componentPlayer"></param>
        /// <param name="componentGui"></param>
        public virtual void OnCameraChange(ComponentPlayer m_componentPlayer, ComponentGui componentGui)
        {
        }

        /// <summary>
        /// 屏幕截图时执行
        /// </summary>
        public virtual void OnCapture()
        {
        }

        /// <summary>
        /// 更改主页背景音乐
        /// </summary>
        public virtual void MenuPlayMusic(out string ContentMusicPath)
        {
            ContentMusicPath = string.Empty;
        }

        /// <summary>
        /// 摇人行为
        /// </summary>
        /// <param name="herdBehavior"></param>
        /// <param name="target"></param>
        /// <param name="maxRange"></param>
        /// <param name="maxChaseTime"></param>
        /// <param name="isPersistent"></param>
        public virtual void CallNearbyCreaturesHelp(ComponentHerdBehavior herdBehavior, ComponentCreature target, float maxRange, float maxChaseTime, bool isPersistent)
        {
        }

        /// <summary>
        /// 挖掘触发宝物生成时，注意这里能获取到上个Mod生成宝物的情况
        /// </summary>
        /// <param name="BlockValue">宝物的方块值</param>
        /// <param name="Count">宝物数量</param>
        /// <param name="IsGenerate">是否继续让其它Mod处理</param>
        public virtual void OnTreasureGenerate(SubsystemTerrain subsystemTerrain, int x, int y, int z, int neighborX, int neighborY, int neighborZ, ref int BlockValue, ref int Count, out bool IsGenerate)
        {
            IsGenerate = false;
        }
        /// <summary>
        /// 当界面被创建时
        /// </summary>
        /// <param name="widget"></param>
        public virtual void OnWidgetConstruct(ref Widget widget)
        {

        }

        /// <summary>
        /// 在 DrawItem 被绘制前。
        /// </summary>
        /// <param name="drawItem">被绘制的 DrawItem。</param>
        /// <param name="skipVanillaDraw">是否跳过原版绘制代码。</param>
        /// <param name="afterWidgetDraw">原版绘制完成后的回调。</param>
        /// <param name="scissorRectangle">绘制时的 ScissorRectangle。</param>
        /// <param name="drawContext">绘制上下文。</param>
        public virtual void BeforeWidgetDrawItemRender(Widget.DrawItem drawItem, out bool skipVanillaDraw,
                                             out Action afterWidgetDraw, ref Rectangle scissorRectangle,
                                             Widget.DrawContext drawContext)
        {
            skipVanillaDraw = false;
            afterWidgetDraw = null;
        }

        /// <summary>
        /// 在 DrawItem 排序后。
        /// </summary>
        /// <param name="drawContext">绘制上下文。</param>
        public virtual void OnDrawItemAssigned(Widget.DrawContext drawContext)
        {

        }

        /// <summary>
        /// 当ModalPanelWidget被设置时执行
        /// </summary>
        /// <param name="Old"></param>
        /// <param name="New"></param>
        public virtual void OnModalPanelWidgetSet(ComponentGui gui, Widget Old, Widget New)
        {

        }

        /// <summary>
        /// 生成地形顶点时使用
        /// </summary>
        /// <param name="chunk"></param>
        public virtual void GenerateChunkVertices(TerrainChunk chunk, bool even)
        {

        }
        /// <summary>
        /// 生成光源数据
        /// </summary>
        /// <param name="lightSources">光源</param>
        /// <param name="chunk">区块</param>
        public virtual void GenerateChunkLightSources(DynamicArray<TerrainUpdater.LightSource> lightSources, TerrainChunk chunk)
        {

        }
        /// <summary>
        /// 计算动物模型光照
        /// </summary>
        /// <param name="subsystemTerrain"></param>
        /// <param name="p">动物位置</param>
        /// <param name="num">原版计算出来的强度</param>
        public virtual void CalculateSmoothLight(SubsystemTerrain subsystemTerrain, Vector3 p, ref float num)
        {

        }

        /// <summary>
        /// 当窗口模式改变时执行。
        /// </summary>
        public virtual void WindowModeChanged(WindowMode mode)
        {
        }
        /// <summary>
        /// 在执行DamageItem得到方块掉耐久后，得到的新方块值时执行
        /// </summary>
        /// <param name="block"></param>
        /// <param name="oldValue">方块的旧值</param>
        /// <param name="damageCount">损害的耐久量</param>
        /// <param name="owner">方块的拥有者</param>
        /// <param name="skipVanilla">跳过原版执行逻辑</param>
        /// <returns></returns>
        public virtual int DamageItem(Block block, int oldValue, int damageCount, Entity owner, out bool skipVanilla)
        {
            skipVanilla = false;
            return 0;
        }
        /// <summary>
        /// 当射弹击中方块时执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="terrainRaycastResult">地形映射结果</param>
        /// <param name="triggerBlocksBehavior">是否执行被命中的方块行为</param>
        /// <param name="destroyCell">是否破坏被击中的方块</param>
        /// <param name="impactSoundLoudness">发出的声音大小</param>
        /// <param name="projectileGetStuck">射弹是否会被卡在方块里面</param>
        /// <param name="velocityAfterHit">在击中方块后，射弹的速度</param>
        /// <param name="angularVelocityAfterHit">在击中方块后，射弹的角速度</param>
        public virtual void OnProjectileHitTerrain(Projectile projectile, TerrainRaycastResult terrainRaycastResult, ref bool triggerBlocksBehavior, ref bool destroyCell, ref float impactSoundLoudness, ref bool projectileGetStuck, ref Vector3 velocityAfterHit, ref Vector3 angularVelocityAfterHit)
        {

        }
        /// <summary>
        /// 当射弹击中生物、船只等实体时执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="bodyRaycastResult">实体映射结果</param>
        /// <param name="attackment">该射弹命中实体时，执行的攻击。可以调整attackment的攻击力等数据</param>
        /// <param name="velocityAfterAttack">在击中方块后，射弹的速度</param>
        /// <param name="angularVelocityAfterAttack">在击中方块后，射弹的角速度</param>
        /// <param name="ignoreBody">射弹行进直接穿过该生物。射弹后续的更新会忽略该生物，速度和角速度保持原状。攻击照常执行。</param>
        public virtual void OnProjectileHitBody(Projectile projectile, BodyRaycastResult bodyRaycastResult, ref Attackment attackment, ref Vector3 velocityAfterAttack, ref Vector3 angularVelocityAfterAttack, ref bool ignoreBody)
        {

        }

        /// <summary>
        /// 绘制射弹的时候执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="subsystemProjectiles">该子系统，可以从中获取项目和其他子系统</param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        /// <param name="shouldDrawBlock">是否执行原版绘制方块的方法</param>
        /// <param name="drawBlockSize">绘制方块大小</param>
        /// <param name="drawBlockColor">绘制方块颜色</param>
        public virtual void OnProjectileDraw(Projectile projectile, SubsystemProjectiles subsystemProjectiles, Camera camera, int drawOrder, ref bool shouldDrawBlock, ref float drawBlockSize, ref Color drawBlockColor)
        {
        }

        /// <summary>
        /// 射弹离开加载区块的时候执行
        /// </summary>
        /// <param name="projectile"></param>
        public virtual void OnProjectileFlyOutOfLoadedChunks(Projectile projectile)
        {

        }

        /// <summary>
        /// 绘制掉落物的时候执行
        /// </summary>
        /// <param name="pickable"></param>
        /// <param name="subsystemPickables"></param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        /// <param name="shouldDrawBlock">是否执行原版绘制方块的方法</param>
        /// <param name="drawBlockSize"></param>
        /// <param name="drawBlockColor"></param>
        public virtual void OnPickableDraw(Pickable pickable, SubsystemPickables subsystemPickables, Camera camera, int drawOrder, ref bool shouldDrawBlock, ref float drawBlockSize, ref Color drawBlockColor)
        {
        }

        /// <summary>
        /// 执行动物的Update操作。
		/// 建议只在对自己模组的动物才进行带skip的覆盖原版的操作；对原版和其他模组的动物，请不要过多干涉覆盖原有的操作
        /// </summary>
        /// <param name="componentBody"></param>
        /// <param name="dt">动物位置</param>
        /// <param name="skipVanilla">跳过原版的更新操作</param>
		/// <param name="skippedByOtherMods">前面的mod已经执行了带skip操作的Update</param>
        public virtual void UpdateComponentBody(ComponentBody componentBody, float dt, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
            return;
        }

        /// <summary>
        /// 计算动物在Raycast下的表现。输出null表示这个body不计入Raycast结果；输出一个具体的数值表示Raycast计算出来的距离。
        /// </summary>
        /// <param name="componentBody"></param>
        /// <param name="ray">动物位置</param>
        /// <param name="skip">原版计算出来的强度</param>
        public virtual float? BodyCountInRaycast(ComponentBody componentBody, Ray3 ray, out bool skip)
        {
            skip = false;
            return null;
        }

        /// <summary>
        /// 在添加移动方块时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
		/// <param name="subsystemMovingBlocks"></param>
		/// <param name="testCollision">对应原方法的TestCollision部分</param>
		/// <param name="doNotAdd">取消添加移动方块</param>
        public virtual void OnMovingBlockSetAdded(ref SubsystemMovingBlocks.MovingBlockSet movingBlockSet, SubsystemMovingBlocks subsystemMovingBlocks, ref bool testCollision, out bool doNotAdd)
        {
            doNotAdd = false;
        }

        /// <summary>
        /// 移除移动方块时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
        /// <param name="subsystemMovingBlocks"></param>
        public virtual void OnMovingBlockSetRemoved(IMovingBlockSet movingBlockSet, SubsystemMovingBlocks subsystemMovingBlocks)
        {

        }

        /// <summary>
        /// 在移动方块更新时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
        /// <param name="subsystemMovingBlocks"></param>
        /// <param name="skippedByOtherMods">是否已被其他模组抢先执行更新</param>
        /// <param name="skipVanilla">是否跳过原版执行更新（抢先更新）</param>
        public virtual void OnMovingBlockSetUpdate(IMovingBlockSet movingBlockSet, SubsystemMovingBlocks subsystemMovingBlocks, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 游戏中添加射弹时触发
        /// </summary>
        /// <param name="subsystemProjectiles"></param>
        /// <param name="projectile"></param>
        /// <param name="loadValuesDictionary">如果是加载世界过程中首次添加，那么会提供该射弹的相关ValuesDictionary；如果是游戏进行过程中添加，则为null</param>
        public virtual void OnProjectileAdded(SubsystemProjectiles subsystemProjectiles, ref Projectile projectile, ValuesDictionary loadValuesDictionary)
        {

        }

        /// <summary>
        /// 游戏中添加掉落物时触发
        /// </summary>
        /// <param name="subsystemPickables"></param>
        /// <param name="pickable"></param>
        /// <param name="loadValuesDictionary">如果是加载世界过程中首次添加，那么会提供该射弹的相关ValuesDictionary；如果是游戏进行过程中添加，则为null</param>
        public virtual void OnPickableAdded(SubsystemPickables subsystemPickables, ref Pickable pickable, ValuesDictionary loadValuesDictionary)
        {

        }

        /// <summary>
        /// 保存世界时，存储射弹信息
        /// </summary>
        /// <param name="subsystemProjectiles"></param>
        /// <param name="projectile"></param>
        /// <param name="valuesDictionary">存储射弹信息的ValuesDictionaey</param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void SaveProjectile(SubsystemProjectiles subsystemProjectiles, Projectile projectile, ref ValuesDictionary valuesDictionary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 保存世界时，存储掉落物信息
        /// </summary>
        /// <param name="subsystemPickables"></param>
        /// <param name="pickable"></param>
        /// <param name="valuesDictionary">存储掉落物信息的ValuesDictionary</param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void SavePickable(SubsystemPickables subsystemPickables, Pickable pickable, ref ValuesDictionary valuesDictionary)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 在方块被挖掘完毕时执行
        /// </summary>
        /// <param name="componentMiner"></param>
        /// <param name="digValue"></param>
        /// <param name="DurabilityReduction">挖掘方块所消耗工具的耐久</param>
        /// <param name="mute">挖掘方块是否取消播放音效</param>
        /// <param name="PlayerDataDugAdd">是否增加玩家统计信息的挖掘方块计数</param>
        public virtual void OnBlockDug(ComponentMiner componentMiner, BlockPlacementData digValue, int cellValue, ref int DurabilityReduction, ref bool mute, ref int PlayerDataDugAdd)
        {
            mute = false;
        }

        /// <summary>
        /// 改变SubsystemTime的时间推移速度，偏向底层，一般开发者不必了解
        /// </summary>
        /// <param name="subsystemTime"></param>
        /// <param name="dt"></param>
        public virtual void ChangeGameTimeDelta(SubsystemTime subsystemTime, ref float dt)
        {

        }

        /// <summary>
        /// 在IUpdateable添加或删除时执行，用于模组接管IUpdateable的更新行为
        /// （如恒泰模组将动物放在多线程中进行更新，降低怪物数量多导致的卡顿）
        /// </summary>
        /// <param name="subsystemUpdate"></param>
        /// <param name="updateable"></param>
        /// <param name="ToAdd1OrRemove0">这个IUpdateable是准备添加的，该变量为1；这个IUpdateable是准备移除的，该变量为0</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组接管</param>
        /// <param name="skip">宣布接管，则不会被原版的SubsystemUpdate执行Update()</param>
        public virtual void OnIUpdateableAddOrRemove(SubsystemUpdate subsystemUpdate, IUpdateable updateable, bool ToAdd1OrRemove0, bool skippedByOtherMods, out bool skip)
        {
            skip = false;
        }
        /// <summary>
        /// 在IDrawable添加或删除时执行，用于模组接管IDrawable的绘制行为
        /// </summary>
        /// <param name="subsystemDrawing"></param>
        /// <param name="drawable"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组接管</param>
        /// <param name="skip">宣布接管，该IDrawable不会放入SubsystemDrawing.m_drawbles</param>
        public virtual void OnIDrawableAdded(SubsystemDrawing subsystemDrawing, IDrawable drawable, bool skippedByOtherMods, out bool skip)
        {
            skip = false;
        }
        /// <summary>
        /// 在创建家具时执行
        /// </summary>
        /// <param name="furnitureDesign"></param>
        /// <param name="designedFromExistingFurniture">是否从已有家具方块创建，通常用于mod禁止家具复制</param>
        /// <param name="pickableCount">产生的掉落物数量</param>
        /// <param name="destroyDesignBlocks">是否移除搭建的建筑原型</param>
        /// <param name="toolDamageCount">家具锤消耗的耐久量，如果家具锤剩余耐久不足以支持消耗量，则玩家无法创建家具并弹出提示</param>
        public virtual void OnFurnitureDesigned(FurnitureDesign furnitureDesign, bool designedFromExistingFurniture, ref int pickableCount, ref bool destroyDesignBlocks, ref int toolDamageCount)
        {
        }


        /// <summary>
        /// 在创建InventorySlotWidget时执行，可以增加元素
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="childrenWidgetsToAdd">创建InventorySlotWidget时，返回增加的子Widget</param>
        public virtual void OnInventorySlotWidgetDefined(InventorySlotWidget inventorySlotWidget, out List<Widget> childrenWidgetsToAdd)
        {
            childrenWidgetsToAdd = null;
        }

        /// <summary>
        /// 绘制物品格子的耐久条、食物条等元素
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="parentAvailableSize">其父widget的大小</param>
        public virtual void InventorySlotWidgetMeasureOverride(InventorySlotWidget inventorySlotWidget, Vector2 parentAvailableSize)
        {

        }

        /// <summary>
        /// 当移动物品时执行。从sourceInventory的第sourceSlotIndex个格子，移动count个物品，到targetInventory的第targetSlotIndex个格子
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="count">留给后面模组和原版处理物品的数量</param>
        /// <param name="moved">是否完成移动操作，注意这个不影响跳过原版处理</param>
        public virtual void HandleMoveInventoryItem(InventorySlotWidget inventorySlotWidget, IInventory sourceInventory, int sourceSlotIndex, IInventory targetInventory, int targetSlotIndex, ref int count, out bool moved)
        {
            moved = false;
        }

        /// <summary>
        /// 在InventorySlotWidget.HandleDragDrop时执行，先执行物品的修改操作
        /// （比如原版火药拖到枪身上时执行上膛操作）
        /// </summary>
        /// <param name="inventorySlotWidget">目标格子的InventorySlotWidget</param>
        /// <param name="sourceInventory"></param>
        /// <param name="sourceSlotIndex"></param>
        /// <param name="targetInventory"></param>
        /// <param name="targetSlotIndex"></param>
        /// <param name="ProcessCapacity">目标格子接受物品的数量。设置为不大于0的数相当于跳过原版逻辑</param>
        public virtual void HandleInventoryDragProcess(InventorySlotWidget inventorySlotWidget, IInventory sourceInventory, int sourceSlotIndex, IInventory targetInventory, int targetSlotIndex, ref int ProcessCapacity)
        {
        }

        /// <summary>
        /// 在InventorySlotWidget.HandleDragDrop时执行，如果物品没有修改操作，则执行移动物品操作
        /// </summary>
        /// <param name="inventorySlotWidget">目标格子的InventorySlotWidget</param>
        /// <param name="sourceInventory"></param>
        /// <param name="sourceSlotIndex"></param>
        /// <param name="targetInventory"></param>
        /// <param name="targetSlotIndex"></param>
        /// <param name="skippedByOtherMods">执行逻辑是否已经被其他模组跳过</param>
        /// <param name="skip">跳过原版的执行逻辑</param>
        public virtual void HandleInventoryDragMove(InventorySlotWidget inventorySlotWidget, IInventory sourceInventory, int sourceSlotIndex, IInventory targetInventory, int targetSlotIndex, bool skippedByOtherMods, out bool skip)
        {
            skip = false;
        }

        /// <summary>
        /// 在玩家骑上坐骑时每帧执行，用于调整玩家骑行动物时的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlSteed(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在玩家乘坐船时每帧执行，用于调整玩家乘船时的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlBoat(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在玩家乘坐船、动物以外的物体时每帧执行，用于控制玩家骑模组坐骑的控制魔力
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过其他模组执行操作</param>
        public virtual void OnPlayerControlOtherMount(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 当玩家既不在坐骑上，也不在船上时执行，用于控制玩家行走的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlWalk(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }
        /// <summary>
        /// 当玩家输入交互逻辑时执行的操作
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="playerOperated">为true则停止之后的挖掘、攻击等操作</param>
        /// <param name="timeIntervalLastActionTime">距离上一次触发该操作距离的时长</param>
        /// <param name="priorityUse">控制使用优先级，使用优先级小于等于0则禁止玩家使用手中物品</param>
        /// <param name="priorityInteract">控制交互优先级，交互优先级小于等于0则禁止玩家交互方块</param>
        /// <param name="priorityPlace">控制放置优先级，放置优先级小于等于0则禁止玩家放置方块</param>
        public virtual void OnPlayerInputInteract(ComponentPlayer componentPlayer, ref bool playerOperated, ref double timeIntervalLastActionTime, ref int priorityUse, ref int priorityInteract, ref int priorityPlace)
        {

        }

        /// <summary>
        /// 在玩家正在瞄准时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="aiming">是否正在瞄准</param>
        /// <param name="playerOperated">为true则停止之后的挖掘、攻击等操作</param>
        /// <param name="timeIntervalAim">和上一次执行瞄准操作，要求的最小时间间隔</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void UpdatePlayerInputAim(ComponentPlayer componentPlayer, bool aiming, ref bool playerOperated, ref float timeIntervalAim, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在玩家执行“攻击”动作时执行，比如恒泰左键放箭，工业左键点击船
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="playerOperated">为true则停止之后的挖掘操作</param>
        /// <param name="timeIntervalHit">和上一次输入攻击操作，要求的最小时间间隔，小于该间隔时输入无效。（注意和ComponentMiner.HitInterval作区分）</param>
        /// <param name="meleeAttackRange">近战攻击距离，小于等于0时表示不进行近战操作（比如手持弓时近战距离改为0，就不会拿着弓拍敌人）</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void OnPlayerInputHit(ComponentPlayer componentPlayer, ref bool playerOperated, ref double timeIntervalHit, ref float meleeAttackRange, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在玩家执行“挖掘”动作时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="digging">玩家是否正在挖掘</param>
        /// <param name="playerOperated">为true则停止之后的创造模式中键选择物品等操作</param>
        /// <param name="timeIntervalDig">和上一次执行挖掘操作，要求的最小时间间隔。将该值降低可以像恒泰那样极速挖掘</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void UpdatePlayerInputDig(ComponentPlayer componentPlayer, bool digging, ref bool playerOperated, ref double timeIntervalDig, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在玩家电脑上“按Q释放剑弃”时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods"></param>
        /// <param name="skipVanilla"></param>
        public virtual void OnPlayerInputDrop(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla)
        {
            skipVanilla = false;
        }

        /// <summary>
        /// 在闪电劈下时执行
        /// </summary>
        /// <param name="subsystemSky"></param>
        /// <param name="targetPosition">闪电劈下的位置</param>
        /// <param name="strike">是否能成功执行</param>
        /// <param name="explosionPressure">闪电的爆炸威力</param>
        /// <param name="setBodyOnFire">是否点燃目标</param>
        public virtual void OnLightningStrike(SubsystemSky subsystemSky, ref Vector3 targetPosition, ref bool strike, ref float explosionPressure, ref bool setBodyOnFire)
        {

        }

        /// <summary>
        /// 用于调整原版已有矿物、水域、植物等地形地貌的生成，例如减少原版矿物生成量
        /// </summary>
        public virtual void OnTerrainBrushesCreated()
        {

        }

        /// <summary>
        /// 在创建世界时寻找玩家的初步生成大致位置
        /// </summary>
        /// <param name="spawnPosition">玩家初步生成大致位置</param>
        public virtual void FindCoarseSpawnPosition(ITerrainContentsGenerator terrainContentsGenerator, ref Vector3 spawnPosition)
        {

        }

        /// <summary>
        /// 在动物执行近战攻击命中目标时执行
        /// </summary>
        /// <param name="componentChaseBehavior"></param>
        /// <param name="chaseTimeBefore">在攻击之前的剩余追逐时间</param>
        /// <param name="chaseTime">在攻击之后的剩余追逐时间</param>
        /// <param name="hitBody">是否能够攻击</param>
        /// <param name="playAttackSound">是否发出攻击音效</param>
        public virtual void OnChaseBehaviorAttacked(ComponentChaseBehavior componentChaseBehavior, float chaseTimeBefore, ref float chaseTime, ref bool hitBody, ref bool playAttackSound)
        {

        }

        /// <summary>
        /// 在动物执行近战攻击没有命中目标时执行
        /// </summary>
        /// <param name="componentChaseBehavior"></param>
        /// <param name="chaseTime">在攻击之后的剩余追逐时间</param>
        public virtual void OnChaseBehaviorAttackFailed(ComponentChaseBehavior componentChaseBehavior, ref float chaseTime)
        {

        }

        /// <summary>
        /// 计算动物的坠落伤害
        /// </summary>
        /// <param name="componentHealth">生物的ComponentHealth，至于坠落速度等信息则从ComponentHealth出发寻找</param>
        /// <param name="damage">坠落伤害</param>
        public virtual void CalculateFallDamage(ComponentHealth componentHealth, ref float damage)
        {
        }

        /// <summary>
        /// 在动物晕眩或死亡时执行移动
        /// </summary>
        /// <param name="componentLocomotion"></param>
        /// <param name="fallsOnDeathOrStun">在晕眩或死亡时是否坠落</param>
        public virtual void OnLocomotionStopped(ComponentLocomotion componentLocomotion, ref bool fallsOnDeathOrStun)
        {

        }

        /// <summary>
        /// 在ComponentLocomotion加载时执行
        /// </summary>
        /// <param name="componentLocomotion"></param>
        /// <param name="mobWalkSpeedFactor">非玩家生物的移速乘数</param>
        /// <param name="mobFlySpeedFactor">非玩家生物的飞行速度乘数</param>
        /// <param name="mobSwimSpeedFactor">非玩家生物的游泳速度乘数</param>
        /// <param name="disableCreativeFlyInSurvivalMode">是否在生存模式中停止创造飞行（通常发生在创造模式切换到生存模式中）</param>
        public virtual void OnComponentLocomotionLoaded(ComponentLocomotion componentLocomotion, ref float mobWalkSpeedFactor, ref float mobFlySpeedFactor, ref float mobSwimSpeedFactor, ref bool disableCreativeFlyInSurvivalMode)
        {

        }

        /// <summary>
        /// 在发射器投掷物品时执行
        /// </summary>
        /// <param name="componentDispenser">该发射器的Component</param>
        /// <param name="pickable">要发射的掉落物</param>
        /// <param name="RemoveSlotCount">移除发射器物品栏中物品数量</param>
        public virtual void OnDispenserDispensePickable(ComponentDispenser componentDispenser, ref Pickable pickable, ref int RemoveSlotCount)
        {

        }

        /// <summary>
        /// 在发射器弹射物品时执行
        /// </summary>
        /// <param name="componentDispenser">该发射器的Component</param>
        /// <param name="projectile">要发射的弹射物</param>
        /// <param name="canDispensePickable">发射失败时，是否以掉落物的方式发射（即使不发射也会消耗）</param>
        /// <param name="RemoveSlotCount">移除发射器物品栏中物品数量</param>
        public virtual void OnDispenserShoot(ComponentDispenser componentDispenser, ref Projectile projectile, ref bool canDispensePickable, ref int RemoveSlotCount)
        {

        }

        /// <summary>
        /// 发射器选择消耗哪一个物品进行发射
        /// </summary>
        /// <param name="componentDispenser"></param>
        /// <param name="slot">选择消耗哪一个格子的物品</param>
        /// <param name="value">选择发射什么物品</param>
        /// <param name="chosen">是否已经选择。若已经选择，则会跳过后面模组中执行。为了兼容性，仅推荐发射器在有自己模组方块的时候才执行</param>
        public virtual void DispenserChooseItemToDispense(ComponentDispenser componentDispenser, ref int slot, ref int value, out bool chosen)
        {
            chosen = false;
        }

        /// <summary>
        /// 在世界选择列表时，调整存档的外观
        /// </summary>
        /// <param name="worldInfo">世界信息</param>
        /// <param name="savedWorldItemNode">存储世界信息的XElement</param>
        /// <param name="worldInfoWidget">要修改的Widget</param>
        public virtual void LoadWorldInfoWidget(WorldInfo worldInfo, XElement savedWorldItemNode, ref ContainerWidget worldInfoWidget)
        {

        }

        /// <summary>
        /// 在方块介绍页面中，增加或减少方块的属性字段
        /// </summary>
        /// <param name="blockProperties"></param>
        public virtual void EditBlockDescriptionScreen(Dictionary<string, string> blockProperties)
        {

        }

        /// <summary>
        /// 在合成表页面时每帧更新时，编辑该页面
        /// </summary>
        /// <param name="screen"></param>
        public virtual void EditRecipeScreenWidget(RecipaediaRecipesScreen screen)
        {

        }

        /// <summary>
        /// 在生物图鉴页面每帧更新时，编辑该页面
        /// </summary>
        /// <param name="bestiaryDescriptionScreen"></param>
        /// <param name="bestiaryCreatureInfo">该生物的基础信息</param>
        /// <param name="entityValuesDictionary">该生物在Database中的ValuesDictionary</param>
        public virtual void UpdateCreaturePropertiesInBestiaryDescriptionScreen(BestiaryDescriptionScreen bestiaryDescriptionScreen, BestiaryCreatureInfo bestiaryCreatureInfo, ValuesDictionary entityValuesDictionary)
        {

        }

        /// <summary>
        /// 在生物图鉴目录列表更新该条目时，编辑该条目
        /// </summary>
        /// <param name="bestiaryScreen"></param>
        /// <param name="creatureInfoWidget">可以更改的生物信息Widget</param>
        /// <param name="bestiaryCreatureInfo">该生物的基础信息</param>
        /// <param name="entityValuesDictionary">该生物在Database中的ValuesDictioanry</param>
        public virtual void LoadCreatureInfoInBestiaryScreen(BestiaryScreen bestiaryScreen, ContainerWidget creatureInfoWidget, BestiaryCreatureInfo bestiaryCreatureInfo, ValuesDictionary entityValuesDictionary)
        {

        }

        /// <summary>
        /// 在进行世界设置时，如果不是创造模式，则会修改设定
        /// </summary>
        /// <param name="worldSettings">要修改的世界设置</param>
        /// <param name="environmentBehaviorModeBefore"></param>
        /// <param name="timeOfDayModeBefore"></param>
        /// <param name="areWeatherEffectsEnabledBefore"></param>
        /// <param name="areSurvivalMechanicsEnabledBefore"></param>
        public virtual void ResetOptionsForNonCreativeMode(WorldSettings worldSettings, EnvironmentBehaviorMode environmentBehaviorModeBefore, TimeOfDayMode timeOfDayModeBefore, bool areWeatherEffectsEnabledBefore, bool areSurvivalMechanicsEnabledBefore)
        {

        }

        /// <summary>
        /// 在配方表加载的时候执行，用于删除原版配方
        /// </summary>
        /// <param name="recipes">已经加载的配方</param>
        /// <param name="sort">是否在删除后重新排序</param>
        public virtual void CraftingRecipesManagerInitialize(List<CraftingRecipe> recipes, ref bool sort)
        {

        }

        /// <summary>
        /// 在配方表加载完成后执行，在排序完成后执行
        /// </summary>
        public virtual void CraftingRecipesManagerInitialized()
        {

        }

        /// <summary>
        /// 在游戏游玩过程中时放音乐
        /// </summary>
        public virtual void PlayInGameMusic()
        {

        }
		[Obsolete("Use TerrainContentsGenerator24Initialize")]
		public virtual void TerrainContentsGenerator23Initialize(ITerrainContentsGenerator terrainContentsGenerator,SubsystemTerrain subsystemTerrain)
		{

		}
		public virtual void TerrainContentsGenerator24Initialize(ITerrainContentsGenerator terrainContentsGenerator, SubsystemTerrain subsystemTerrain)
        {
        }

        public virtual void PrepareModels(SubsystemModelsRenderer subsystemModelsRenderer, Camera camera, bool skippedByOtherMods, out bool skip)
        {
            skip = false;
        }

        public virtual void RenderModels(SubsystemModelsRenderer subsystemModelsRenderer, Camera camera, int drawOrder, bool skippedByOtherMods, out bool skip)
        {
            skip = false;
        }

		/// <summary>
		/// 在更新玩家死亡界面时执行
		/// </summary>
		/// <param name="playerData">具体死者</param>
		/// <param name="disableVanillaTapToRespawnAction">是否阻止原版点击任意键就执行复活等下一步的操作</param>
		/// <param name="respawn">是否复活</param>

		public virtual void UpdateDeathCameraWidget(PlayerData playerData, ref bool disableVanillaTapToRespawnAction, ref bool respawn)
		{

		}

		/// <summary>
		/// 在爆炸开始，计算动物摇晃程度时执行。
		/// 为了尽可能确保兼容性，建议只对属于自己模组的生物进行接管编辑
		/// </summary>
		/// <param name="componentBody">将要摇晃的动物</param>
		/// <param name="explosionCenter">爆炸中心地点</param>
		/// <param name="explosionPressure">爆炸强度</param>
		/// <param name="shakeStrength">动物摇晃的强度</param>
		public virtual void OnComponentBodyExplodedStart(ComponentBody componentBody, Vector3 explosionCenter, float explosionPressure, ref float shakeStrength)
		{

		}

		/// <summary>
		/// 在游戏执行植物生长判定时执行，为保证良好兼容性，建议只在处理属于自己模组的方块时改变控制
		/// </summary>
		/// <param name="subsystemPlantBlockBehavior">子系统，便于定位SubsystemTerrain等必要组件</param>
		/// <param name="x">植物的坐标x</param>
		/// <param name="y">植物的坐标y</param>
		/// <param name="z">植物的坐标z</param>
		/// <param name="pollPass">用途不确定</param>
		/// <param name="skipVanilla">是否跳过原版的执行进程，注意本接口不能跳过其他模组的执行进程</param>
		public virtual void GrowPlant(SubsystemPlantBlockBehavior subsystemPlantBlockBehavior, int x, int y,int z, int pollPass, out bool skipVanilla)
		{
			skipVanilla = false;
		}

		/// <summary>
		/// 射弹转化为掉落物时执行
		/// </summary>
		/// <param name="projectile">即将被转化并删除的射弹</param>
		/// <param name="pickable">即将加入的掉落物</param>
		public virtual void OnProjectileTurnIntoPickable(Projectile projectile, ref Pickable pickable)
		{

		}

		/// <summary>
		/// 在实体创建时执行，可用于移除实体的特定组件
		/// </summary>
		/// <param name="entity">被创建的实体</param>
		/// <param name="componentList">这个实体将会拥有的Component，KeyValuePair的第一个表示Component的加载顺序编号，第二个表示实体的Component</param>
		public virtual void EntityComponentsInitialized(Entity entity, List<KeyValuePair<int,Component>> componentList)
		{

		}

		/// <summary>
		/// 游戏Project创建时执行，可用于移除Subsystem、Entity、Entity.Component
		/// </summary>
		/// <param name="project"></param>
		public virtual void ProjectBeforeSubsystemsAndEntitiesLoad(Project project)
		{

		}

		/// <summary>
		/// 进入Screen时执行
		/// </summary>
		/// <param name="screen">进入的Screen</param>
		/// <param name="screenParameters">进入Screen时传入的参数，参数的详细信息需要去查阅对应screen的代码</param>
		public virtual void OnScreenEntered(Screen screen,object[] screenParameters)
		{

		}

		/// <summary>
		/// 离开Screen时执行
		/// </summary>
		/// <param name="screen">离开的Screen</param>
		public virtual void OnScreenLeaved(Screen screen) 
		{
		}

		/// <summary>
		/// 在Widget即将执行Update()时执行，能用于修改游戏中已有按钮的功能
		/// 并且可以通过一定的操作，拦截原版按钮的功能
		/// </summary>
		/// <param name="widget"></param>
		public virtual void BeforeWidgetUpdate(Widget widget)
		{

		}

		/// <summary>
		/// 在Widget完成Update()后立即执行，能用于修改游戏中已有按钮的功能
		/// </summary>
		/// <param name="widget"></param>
		public virtual void AfterWidgetUpdate(Widget widget)
		{

		}
		public virtual void OnChaseBehaviorStartChasing(ComponentChaseBehavior chaseBehavior)
		{

		}
		public virtual void OnChaseBehaviorStopChasing(ComponentChaseBehavior chaseBehavior)
		{

		}

		/// <summary>
		/// 生物在选择追击对象时，对被追击对象的评分。评分越高，则会被选中。
		/// 仅建议在该生物为本模组生物，或目标生物为本模组生物时，修改追击权重。
		/// </summary>
		/// <param name="chaseBehavior"></param>
		/// <param name="creatureToBeTarget">被评估的对象</param>
		/// <param name="score">给出的评分</param>
		public virtual void ChaseBehaviorScoreTarget(ComponentChaseBehavior chaseBehavior, ComponentCreature creatureToBeTarget, ref float score)
		{

		}

		public virtual void UpdateChaseBehaviorLookingForTarget(ComponentChaseBehavior chaseBehavior)
		{

		}
		public virtual void UpdateChaseBehaviorChasing(ComponentChaseBehavior chaseBehavior)
		{

		}
		/// <summary>
		/// 修改生物的战利品。
		/// 建议不要清空dropValues推倒重来，不利于模组兼容性。
		/// </summary>
		/// <param name="componentLoot"></param>
		/// <param name="dropValues">其中包含所有的战利品掉落物的方块值和数量</param>
		public virtual void DecideLoot(ComponentLoot componentLoot, List<BlockDropValue> dropValues)
		{

		}
		/// <summary>
		/// 在主界面初始化后执行，你可以通过这个给主界面加些你想要的按钮或者文字等
		/// 不过建议开发者使用BeforeWidgetUpdate和AfterWidgetUpdate这两个接口实现
		/// </summary>
		/// <param name="mainMenuScreen">初始化完毕后的主界面</param>
		/// <param name="leftBottomBar">主界面左下角的按钮栏，里面有着API的切换语言和资源管理按钮</param>
		/// <param name="rightBottomBar">主界面右下角的按钮栏，Mod作者们可以在这里面放入想要的按钮（例如Mod设置按钮、Mod作者介绍按钮等）</param>
		[Obsolete("建议在OnLoadingFinished()中，使用ScreensManager.FindScreen<MainMenuScreen>()查找到主菜单界面，再对其修改")]
		public virtual void OnMainMenuScreenCreated(MainMenuScreen mainMenuScreen, StackPanelWidget leftBottomBar, StackPanelWidget rightBottomBar)
		{

		}
		/// <summary>
		/// 在主界面更新
		/// </summary>
		/// <param name="leftBottomBar">主界面左下角的按钮栏，里面有着API的切换语言和资源管理按钮</param>
		/// <param name="rightBottomBar">主界面右下角的按钮栏，Mod作者们可以在这里面放入想要的按钮（例如Mod设置按钮、Mod作者介绍按钮等）</param>
		[Obsolete("建议在AfterWidgetUpdate()中使用if(widget is MainMenuScreen mainMenuScreen){//Your Code}")]
		public virtual void OnMainMenuScreenUpdate(MainMenuScreen mainMenuScreen,StackPanelWidget leftBottomBar,StackPanelWidget rightBottomBar)
		{

		}
		/// <summary>
		/// 在计算一个生物的Factors执行。
		/// ComponentFactors里面有四个List，这四个List储存了所有对该属性有影响的因子。模组可以修改其中的因子来实现自定义的操作。
		/// public List<Factor> m_strengthFactors = [];
		/// public List<Factor> m_speedFactors = [];
		/// public List<Factor> m_hungerFactors = [];
		/// public List<Factor> m_resilienceFactors = [];
		/// 在执行这个接口后，游戏会根据这四个List计算出StrengthFactor、SpeedFactor、HungerFactor、ResilienceFactor的最终值
		/// </summary>
		/// <param name="componentFactors"></param>
		[Obsolete("You can update your factors in your mod components.")]
		public virtual void OnFactorsGenerate(ComponentFactors componentFactors)
		{

		}

		/// <summary>
		/// 计算护甲防御时，修改参与护甲防御的衣物列表
		/// </summary>
		/// <param name="componentClothing">挨打者的护甲组件</param>
		/// <param name="attackment">导致该护甲结算的攻击</param>
		/// <param name="randomNumber">原版用来计算损伤部位的随机数</param>
		/// <param name="clothesList">参与护甲防御的衣物列表</param>
		public virtual void DecideArmorProtectionSequence(ComponentClothing componentClothing, Attackment attackment, float randomNumber, List<int> clothesList)
		{

		}

		/// <summary>
		/// 在计算护甲防御前，自定义新的结算
		/// 例如：玩家携带力场护盾时，优先使用力场护盾进行防御
		/// </summary>
		/// <param name="componentClothing">挨打者的护甲组件</param>
		/// <param name="attackment">导致该护甲结算的攻击</param>
		/// <param name="attackPowerAfteProtection">结算护甲后的攻击力</param>
		public virtual void ApplyProtectionBeforeClothes(ComponentClothing componentClothing, Attackment attackment, ref float attackPowerAfteProtection)
		{

		}

		/// <summary>
		/// 在计算护甲防御后，自定义新的结算
		/// </summary>
		/// <param name="componentClothing">挨打者的护甲组件</param>
		/// <param name="attackment">导致该护甲结算的攻击</param>
		/// <param name="attackPowerAfteProtection">结算护甲后的攻击力</param>
		public virtual void ApplyProtectionAfterClothes(ComponentClothing componentClothing, Attackment attackment, List<int> listAfterProtection, ref float attackPowerAfteProtection)
		{ 

		}

		/// <summary>
		/// 初始化ClothingSlot的时候执行。在此接口添加脖子、手臂等新的可穿戴部位。
		/// </summary>
		public virtual void InitializeClothingSlots()
		{

		}

		/// <summary>
		/// 在绘制人物界面中的人物模型的时候执行，可以通过在playerModelWidget.m_modelWidget中增删元素来在人物界面绘制新模型
		/// </summary>
		/// <param name="playerModelWidget"></param>
		public virtual void OnPlayerModelWidgetMeasureOverride(PlayerModelWidget playerModelWidget)
		{

		}
	}
}

using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using Jint;
using System.Reflection;

namespace Game
{
    public class SurvivalCraftModLoader : ModLoader
    {
        public override void __ModInitialize()
        {
            ModsManager.RegisterHook("OnCameraChange", this);
            ModsManager.RegisterHook("ManageCameras", this);
            ModsManager.RegisterHook("OnPlayerDead", this);
            ModsManager.RegisterHook("OnModelRendererDrawExtra", this);
            ModsManager.RegisterHook("GetMaxInstancesCount", this);
            ModsManager.RegisterHook("BeforeWidgetDrawItemRender", this);
            ModsManager.RegisterHook("OnDrawItemAssigned", this);
            ModsManager.RegisterHook("WindowModeChanged", this);
            
            TextBoxWidget.ShowCandidatesWindow = SettingsManager.FullScreenMode;
        }

        public override void OnCameraChange(ComponentPlayer m_componentPlayer, ComponentGui componentGui)
        {
            GameWidget gameWidget = m_componentPlayer.GameWidget;
			int currentIndex = Convert.ToInt32(SettingsManager.CameraManageSettings.First(item => Type.GetType(item.Key) == gameWidget.ActiveCamera.GetType()).Value);
			int enableCount = CameraManageScreen.EnabledCamerasCount;
			int nextCameraIndex = (currentIndex + 1) % enableCount;
			Camera camera;
			bool isEnable;
			string key;
			do
			{
				key = SettingsManager.CameraManageSettings.First(item2 => Convert.ToInt32(item2.Value) == nextCameraIndex).Key;
				camera = gameWidget.FindCamera(Type.GetType(key),out isEnable);
				nextCameraIndex = (nextCameraIndex + 1) % enableCount;
			}
			while(!isEnable);
			gameWidget.ActiveCamera = camera;
			componentGui.DisplaySmallMessage(LanguageControl.Get("CameraManage",key),Color.White,
				blinking: false,playNotificationSound: false);
        }
		public override void ManageCameras(GameWidget gameWidget)
		{//示例：添加调试视角
			DebugCamera debugCamera = new DebugCamera(gameWidget);
			//第一个参数声明一个新的摄像机
			//第二个参数为一个Func委托，输入gameWidget可对当前条件进行判断(例如判断是否为创造模式、是否乘坐载具等)，若不符合条件则在玩家切换视角时会跳过当前摄像机
			//如果不用判断条件(任何条件都不跳过该摄像机)，第二个参数可传入null或不填
			gameWidget.AddCamera(debugCamera,gameWidget => {
				SubsystemGameInfo subsystemGameInfo = gameWidget.Target.Project.FindSubsystem<SubsystemGameInfo>();
				return subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
			});
		}
		public override void OnPlayerDead(PlayerData playerData)
        {
            playerData.GameWidget.ActiveCamera = playerData.GameWidget.FindCamera<DeathCamera>();
            if (playerData.ComponentPlayer != null)
            {
                string text = playerData.ComponentPlayer.ComponentHealth.CauseOfDeath;
                if (string.IsNullOrEmpty(text))
                {
                    text = LanguageControl.Get(PlayerData.fName, 12);
                }

                string arg = string.Format(LanguageControl.Get(PlayerData.fName, 13), text);
                if (playerData.m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel)
                {
                    playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(
                        LanguageControl.Get(PlayerData.fName, 6),
                        string.Format(LanguageControl.Get(PlayerData.fName, 7), arg,
                            LanguageControl.Get("GameMode",
                                playerData.m_subsystemGameInfo.WorldSettings.GameMode.ToString())), 30f, 1.5f);
                }
                else if (playerData.m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure &&
                         !playerData.m_subsystemGameInfo.WorldSettings.IsAdventureRespawnAllowed)
                {
                    playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(
                        LanguageControl.Get(PlayerData.fName, 6),
                        string.Format(LanguageControl.Get(PlayerData.fName, 8), arg), 30f, 1.5f);
                }
                else
                {
                    playerData.ComponentPlayer.ComponentGui.DisplayLargeMessage(
                        LanguageControl.Get(PlayerData.fName, 6),
                        string.Format(LanguageControl.Get(PlayerData.fName, 9), arg), 30f, 1.5f);
                }
            }
        }

        public override void OnModelRendererDrawExtra(SubsystemModelsRenderer modelsRenderer,
                                                      SubsystemModelsRenderer.ModelData modelData, Camera camera,
                                                      float? alphaThreshold)
        {
            ComponentModel componentModel = modelData.ComponentModel;
            if (componentModel is ComponentHumanModel)
            {
                ComponentPlayer m_componentPlayer = componentModel.Entity.FindComponent<ComponentPlayer>();
                if (m_componentPlayer != null && camera.GameWidget.PlayerData != m_componentPlayer.PlayerData)
                {
                    ComponentCreature m_componentCreature = m_componentPlayer.ComponentMiner.ComponentCreature;
                    var position =
                        Vector3.Transform(
                            m_componentCreature.ComponentBody.Position +
                            (1.02f * Vector3.UnitY * m_componentCreature.ComponentBody.BoxSize.Y), camera.ViewMatrix);
                    if (position.Z < 0f)
                    {
                        var color = Color.Lerp(Color.White, Color.Transparent,
                            MathUtils.Saturate((position.Length() - 4f) / 3f));
                        if (color.A > 8)
                        {
                            var right = Vector3.TransformNormal(
                                0.005f * Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY)),
                                camera.ViewMatrix);
                            var down = Vector3.TransformNormal(-0.005f * Vector3.UnitY, camera.ViewMatrix);
                            BitmapFont font = LabelWidget.BitmapFont;
                            modelsRenderer.PrimitivesRenderer
                                .FontBatch(font, 1, DepthStencilState.DepthRead, RasterizerState.CullNoneScissor,
                                    BlendState.AlphaBlend, SamplerState.LinearClamp)
                                .QueueText(m_componentPlayer.PlayerData.Name, position, right, down, color,
                                    TextAnchor.HorizontalCenter | TextAnchor.Bottom);
                        }
                    }
                }
            }
        }

        public override int GetMaxInstancesCount()
        {
            return 7;
        }

        public override void BeforeWidgetDrawItemRender(Widget.DrawItem drawItem, out bool skipVanillaDraw,
                                              out Action? afterWidgetDraw, ref Rectangle scissorRectangle,
                                              Widget.DrawContext drawContext)
        {
            if (drawItem.Widget is TextBoxWidget apiTextBoxWidget && drawItem.IsOverdraw)
            {
                // 如果绘制的 Widget 是文本框控件，则提前取消 ScissorRectangle 并 Flush ，最后还原 ScissorRectangle 以达到显示候选窗内容的效果。
                var rect = scissorRectangle;
                Display.ScissorRectangle = Display.Viewport.Rectangle;
                afterWidgetDraw = () =>
                {
                    drawContext.PrimitivesRenderer2D.Flush();
                    Display.ScissorRectangle = rect;
                };
            }

            skipVanillaDraw = false;
            afterWidgetDraw = null;
        }

        public override void OnDrawItemAssigned(Widget.DrawContext drawContext)
        {
            int layer = drawContext.m_drawItems.LastOrDefault()?.Layer ?? 0;
            layer++;

            for (var i = 0; i < drawContext.m_drawItems.Count; i++)
            {
                Widget.DrawItem drawItem = drawContext.m_drawItems[i];

                if (drawItem.Widget is TextBoxWidget && drawItem.IsOverdraw)
                {
                    drawItem.Layer = layer;
                }
            }

            drawContext.m_drawItems.Sort();
            // 将 TextBoxWidget 的 Overdraw 绘制转移至最后面。
        }

        public override void WindowModeChanged(WindowMode mode)
        {
            TextBoxWidget.ShowCandidatesWindow = SettingsManager.FullScreenMode;
            
            #if WINDOWS
            ImeSharp.InputMethod.ShowOSImeWindow = !SettingsManager.FullScreenMode;
            #endif
        }
    }
}
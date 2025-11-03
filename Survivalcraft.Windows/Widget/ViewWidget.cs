using Engine;
using Engine.Graphics;

namespace Game {
    public class ViewWidget : TouchInputWidget, IDragTargetWidget {
        public SubsystemDrawing m_subsystemDrawing;

        public RenderTarget2D m_scalingRenderTarget;

        public static RenderTarget2D ScreenTexture = new(Window.Size.X, Window.Size.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);

        public GameWidget GameWidget { get; set; }

        public Point2? ScalingRenderTargetSize {
            get {
                if (m_scalingRenderTarget == null) {
                    return null;
                }
                return new Point2(m_scalingRenderTarget.Width, m_scalingRenderTarget.Height);
            }
        }

        public override void ChangeParent(ContainerWidget parentWidget) {
            if (parentWidget is GameWidget) {
                GameWidget = (GameWidget)parentWidget;
                m_subsystemDrawing = GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemDrawing>(true);
                base.ChangeParent(parentWidget);
                return;
            }
            throw new InvalidOperationException("ViewWidget must be a child of GameWidget.");
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            base.MeasureOverride(parentAvailableSize);
        }

        public override void Draw(DrawContext dc) {
            if (GameWidget.PlayerData.ComponentPlayer != null
                && GameWidget.PlayerData.IsReadyForPlaying) {
                DrawToScreen(dc);
            }
        }

        public override void Dispose() {
            base.Dispose();
            Utilities.Dispose(ref m_scalingRenderTarget);
        }

        public virtual void DragOver(Widget dragWidget, object data) { }

        public virtual void DragDrop(Widget dragWidget, object data) {
            if (data is InventoryDragData inventoryDragData
                && GameManager.Project != null) {
                SubsystemPickables subsystemPickables = GameManager.Project.FindSubsystem<SubsystemPickables>(true);
                ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
                int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
                int count = componentPlayer != null
                    && componentPlayer.ComponentInput.SplitSourceInventory == inventoryDragData.Inventory
                    && componentPlayer.ComponentInput.SplitSourceSlotIndex == inventoryDragData.SlotIndex ? 1 :
                    inventoryDragData.DragMode != DragMode.SingleItem ? inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex) :
                    MathUtils.Min(inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex), 1);
                int num = inventoryDragData.Inventory.RemoveSlotItems(inventoryDragData.SlotIndex, count);
                if (num > 0) {
                    Vector2 vector = dragWidget.WidgetToScreen(dragWidget.ActualSize / 2f);
                    Vector3 value = Vector3.Normalize(
                            GameWidget.ActiveCamera.ScreenToWorld(new Vector3(vector.X, vector.Y, 1f), Matrix.Identity)
                            - GameWidget.ActiveCamera.ViewPosition
                        )
                        * 12f;
                    subsystemPickables.AddPickable(slotValue, num, GameWidget.ActiveCamera.ViewPosition, value, null, componentPlayer.Entity);
                }
            }
        }

        public virtual void SetupScalingRenderTarget() {
            float num = SettingsManager.ResolutionMode == ResolutionMode.Low ? 0.5f : SettingsManager.ResolutionMode != ResolutionMode.Medium ? 1f : 0.75f;
            float num2 = GlobalTransform.Right.Length();
            float num3 = GlobalTransform.Up.Length();
            Vector2 vector = new(ActualSize.X * num2, ActualSize.Y * num3);
            Point2 point = default;
            point.X = (int)MathF.Round(vector.X * num);
            point.Y = (int)MathF.Round(vector.Y * num);
            Point2 point2 = point;
            if ((num < 1f || GlobalColorTransform != Color.White)
                && point2.X > 0
                && point2.Y > 0) {
                if (m_scalingRenderTarget == null
                    || m_scalingRenderTarget.Width != point2.X
                    || m_scalingRenderTarget.Height != point2.Y) {
                    Utilities.Dispose(ref m_scalingRenderTarget);
                    m_scalingRenderTarget = new RenderTarget2D(point2.X, point2.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
                }
                Display.RenderTarget = m_scalingRenderTarget;
                Display.Clear(Color.Black, 1f, 0);
            }
            else {
                Utilities.Dispose(ref m_scalingRenderTarget);
            }
        }

        public virtual void ApplyScalingRenderTarget(DrawContext dc) {
            if (m_scalingRenderTarget != null) {
                BlendState blendState = GlobalColorTransform.A < byte.MaxValue ? BlendState.AlphaBlend : BlendState.Opaque;
                TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(
                    m_scalingRenderTarget,
                    false,
                    0,
                    DepthStencilState.None,
                    RasterizerState.CullNoneScissor,
                    blendState,
                    SamplerState.PointClamp
                );
                int count = texturedBatch2D.TriangleVertices.Count;
                texturedBatch2D.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, GlobalColorTransform);
                texturedBatch2D.TransformTriangles(GlobalTransform, count);
                dc.PrimitivesRenderer2D.Flush();
            }
        }

        public virtual void DrawToScreen(DrawContext dc) {
            GameWidget.ActiveCamera.PrepareForDrawing();
            RenderTarget2D renderTarget = Display.RenderTarget;
            SetupScalingRenderTarget();
            try {
                m_subsystemDrawing.Draw(GameWidget.ActiveCamera);
            }
            finally {
                Display.RenderTarget = renderTarget;
            }
            ApplyScalingRenderTarget(dc);
            ModsManager.HookAction(
                "DrawToScreen",
                loader => {
                    loader.DrawToScreen(this, dc);
                    return false;
                }
            );
        }
    }
}
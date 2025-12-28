using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentBlockHighlight : Component, IDrawable, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

        public SubsystemSky m_subsystemSky;

        public ComponentPlayer m_componentPlayer;

        public PrimitivesRenderer3D m_primitivesRenderer3D = new();

        public Shader m_shader;

        public CellFace m_cellFace;

        public int m_value;

        public object m_highlightRaycastResult;

        public Geometry m_geometry;

        public static int[] m_drawOrders = [1, 2000];

        public Point3? NearbyEditableCell { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.BlockHighlight;

        public int[] DrawOrders => m_drawOrders;

        public virtual void Update(float dt) {
            Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
            Ray3 ray = new(activeCamera.ViewPosition, activeCamera.ViewDirection);
            NearbyEditableCell = null;
            m_highlightRaycastResult = m_componentPlayer.ComponentMiner.Raycast(ray, RaycastMode.Digging);
            if (!(m_highlightRaycastResult is TerrainRaycastResult terrainRaycastResult)) {
                return;
            }
            if (terrainRaycastResult.Distance < 3f) {
                CellFace cellFace = terrainRaycastResult.CellFace;
                Point3 point = cellFace.Point;
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
                Block obj = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
                if (obj is CrossBlock) {
                    terrainRaycastResult.Distance = MathUtils.Max(terrainRaycastResult.Distance, 0.1f);
                    m_highlightRaycastResult = terrainRaycastResult;
                }
                if (obj.IsEditable_(cellValue)) {
                    NearbyEditableCell = cellFace.Point;
                }
            }
#if DEBUG
            if (m_componentPlayer.GameWidget.GameWidgetIndex == 0) {
                CellFace cellFace = terrainRaycastResult.CellFace;
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                PerformanceManager.AddExtraStat($"Block Value: {cellValue}, CellFace: ({cellFace.X},{cellFace.Y},{cellFace.Z},{cellFace.Face}), Distance: {terrainRaycastResult.Distance:F1}");
            }
#endif
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            if (camera.GameWidget.PlayerData == m_componentPlayer.PlayerData) {
                if (drawOrder == m_drawOrders[0]) {
                    DrawFillHighlight(camera);
                    DrawOutlineHighlight(camera);
                    DrawReticleHighlight(camera);
                }
                else {
                    DrawRayHighlight(camera);
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_shader = new Shader(
                ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", "vsh"),
                ModsManager.GetInPakOrStorageFile<string>("Shaders/Highlight", "psh"),
                new ShaderMacro("ShadowShader")
            );
        }

        public virtual void DrawRayHighlight(Camera camera) {
            if (!camera.Eye.HasValue) {
                return;
            }
            Ray3 ray;
            float num;
            if (m_highlightRaycastResult is TerrainRaycastResult terrainRaycastResult) {
                ray = terrainRaycastResult.Ray;
                num = MathUtils.Min(terrainRaycastResult.Distance, 2f);
            }
            else if (m_highlightRaycastResult is BodyRaycastResult bodyRaycastResult) {
                ray = bodyRaycastResult.Ray;
                num = MathUtils.Min(bodyRaycastResult.Distance, 2f);
            }
            else if (m_highlightRaycastResult is MovingBlocksRaycastResult movingBlocksRaycastResult) {
                ray = movingBlocksRaycastResult.Ray;
                num = MathUtils.Min(movingBlocksRaycastResult.Distance, 2f);
            }
            else {
                if (!(m_highlightRaycastResult is Ray3 ray3)) {
                    return;
                }
                ray = ray3;
                num = 2f;
            }
            Color color = Color.White * 0.5f;
            Color color2 = Color.Lerp(color, Color.Transparent, MathUtils.Saturate(num / 2f));
            FlatBatch3D flatBatch3D = m_primitivesRenderer3D.FlatBatch();
            flatBatch3D.QueueLine(ray.Position, ray.Position + ray.Direction * num, color, color2);
            flatBatch3D.Flush(camera.ViewProjectionMatrix);
        }

        public virtual void DrawReticleHighlight(Camera camera) {
            if (camera.Eye != null) {
                if (!(m_highlightRaycastResult is TerrainRaycastResult result)) {
                    return;
                }
                Vector3 vector = result.HitPoint();
                Vector3 vector2;
                if (BlocksManager.Blocks[Terrain.ExtractContents(result.Value)] is CrossBlock) {
                    vector2 = -result.Ray.Direction;
                }
                else {
                    vector2 = CellFace.FaceToVector3(result.CellFace.Face);
                }
                float num = Vector3.Distance(camera.ViewPosition, vector);
                float num2 = 0.03f + MathUtils.Min(0.008f * num, 0.04f);
                float num3 = 0.01f * num;
                Vector3 vector3 = ((MathUtils.Abs(Vector3.Dot(vector2, Vector3.UnitY)) < 0.5f) ? Vector3.UnitY : Vector3.UnitX);
                Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector2, vector3));
                Vector3 vector5 = Vector3.Normalize(Vector3.Cross(vector2, vector4));
                Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Reticle");
                TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(
                    subtexture.Texture,
                    false,
                    0,
                    DepthStencilState.DepthRead,
                    null,
                    null,
                    SamplerState.LinearClamp
                );
                Vector3 vector6 = vector + num2 * (-vector4 + vector5) + num3 * vector2;
                Vector3 vector7 = vector + num2 * (vector4 + vector5) + num3 * vector2;
                Vector3 vector8 = vector + num2 * (vector4 - vector5) + num3 * vector2;
                Vector3 vector9 = vector + num2 * (-vector4 - vector5) + num3 * vector2;
                Vector2 vector10 = new Vector2(subtexture.TopLeft.X, subtexture.TopLeft.Y);
                Vector2 vector11 = new Vector2(subtexture.BottomRight.X, subtexture.TopLeft.Y);
                Vector2 vector12 = new Vector2(subtexture.BottomRight.X, subtexture.BottomRight.Y);
                Vector2 vector13 = new Vector2(subtexture.TopLeft.X, subtexture.BottomRight.Y);
                texturedBatch3D.QueueQuad(
                    vector6,
                    vector7,
                    vector8,
                    vector9,
                    vector10,
                    vector11,
                    vector12,
                    vector13,
                    Color.White
                );
                texturedBatch3D.Flush(camera.ViewProjectionMatrix);
            }
        }

        public virtual void DrawFillHighlight(Camera camera) {
            if (camera.Eye != null
                && m_highlightRaycastResult is TerrainRaycastResult result) {
                CellFace cellFace = result.CellFace;
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                int num = Terrain.ExtractContents(cellValue);
                Block block = BlocksManager.Blocks[num];
                if (m_geometry == null
                    || cellValue != m_value
                    || cellFace != m_cellFace) {
                    Utilities.Dispose(ref m_geometry);
                    m_geometry = new Geometry(Project.FindSubsystem<SubsystemAnimatedTextures>().AnimatedBlocksTexture);
                    block.GenerateTerrainVertices(
                        m_subsystemTerrain.BlockGeometryGenerator,
                        m_geometry,
                        cellValue,
                        cellFace.X,
                        cellFace.Y,
                        cellFace.Z
                    );
                    m_cellFace = cellFace;
                    m_value = cellValue;
                }
                DynamicArray<TerrainVertex> vertices = m_geometry.SubsetOpaque.Vertices;
                DynamicArray<int> indices = m_geometry.SubsetOpaque.Indices;
                Vector3 translation = camera.InvertedViewMatrix.Translation;
                Vector3 vector = new Vector3(MathUtils.Floor(translation.X), 0f, MathUtils.Floor(translation.Z));
                Matrix matrix = Matrix.CreateTranslation(vector - translation) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
                Display.BlendState = BlendState.NonPremultiplied;
                Display.DepthStencilState = DepthStencilState.Default;
                Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                m_shader.GetParameter("u_origin").SetValue(vector.XZ);
                m_shader.GetParameter("u_viewProjectionMatrix").SetValue(matrix);
                m_shader.GetParameter("u_viewPosition").SetValue(translation);
                m_shader.GetParameter("u_texture").SetValue(m_subsystemAnimatedTextures.AnimatedBlocksTexture);
                m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointWrap);
                m_shader.GetParameter("u_fogYMultiplier").SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
                m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
                m_shader.GetParameter("u_fogBottomTopDensity")
                    .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
                m_shader.GetParameter("u_hazeStartDensity").SetValue(new Vector2(m_subsystemSky.ViewHazeStart, m_subsystemSky.ViewHazeDensity));
                Display.DrawUserIndexed(
                    PrimitiveType.TriangleList,
                    m_shader,
                    TerrainVertex.VertexDeclaration,
                    vertices.Array,
                    0,
                    vertices.Count,
                    indices.Array,
                    0,
                    indices.Count
                );
            }
        }

        public virtual void DrawOutlineHighlight(Camera camera) {
            if (camera.UsesMovementControls
                || !(m_componentPlayer.ComponentHealth.Health > 0f)
                || !m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible) {
                return;
            }
            if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue) {
                CellFace value = m_componentPlayer.ComponentMiner.DigCellFace.Value;
                BoundingBox cellFaceBoundingBox = GetCellFaceBoundingBox(value.Point);
                float num = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox.Center());
                Color color = Color.MultiplyNotSaturated(Color.Black, 1f - num);
                DrawBoundingBoxFace(
                    m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None),
                    value.Face,
                    cellFaceBoundingBox.Min,
                    cellFaceBoundingBox.Max,
                    color
                );
            }
            else {
                if (!m_componentPlayer.ComponentAimingSights.IsSightsVisible
                    && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch)
                    && m_highlightRaycastResult is TerrainRaycastResult terrainRaycastResult) {
                    CellFace cellFace = terrainRaycastResult.CellFace;
                    BoundingBox cellFaceBoundingBox2 = GetCellFaceBoundingBox(cellFace.Point);
                    float num2 = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox2.Center());
                    Color color2 = Color.MultiplyNotSaturated(Color.Black, 1f - num2);
                    DrawBoundingBoxFace(
                        m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None),
                        cellFace.Face,
                        cellFaceBoundingBox2.Min,
                        cellFaceBoundingBox2.Max,
                        color2
                    );
                }
                if (NearbyEditableCell.HasValue) {
                    BoundingBox cellFaceBoundingBox3 = GetCellFaceBoundingBox(NearbyEditableCell.Value);
                    float num3 = m_subsystemSky.CalculateFog(camera.ViewPosition, cellFaceBoundingBox3.Center());
                    Color color3 = Color.MultiplyNotSaturated(Color.Black, 1f - num3);
                    m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None).QueueBoundingBox(cellFaceBoundingBox3, color3);
                }
            }
            m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
        }

        public static void DrawBoundingBoxFace(FlatBatch3D batch, int face, Vector3 c1, Vector3 c2, Color color) {
            switch (face) {
                case 0:
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
                    break;
                case 1:
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
                    break;
                case 2:
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
                    break;
                case 3:
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
                    break;
                case 4:
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
                    break;
                case 5:
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
                    batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
                    batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
                    break;
            }
        }

        public BoundingBox GetCellFaceBoundingBox(Point3 point) {
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
            BoundingBox[] customCollisionBoxes = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)]
                .GetCustomCollisionBoxes(m_subsystemTerrain, cellValue);
            Vector3 vector = new(point.X, point.Y, point.Z);
            if (customCollisionBoxes.Length != 0) {
                BoundingBox? boundingBox = null;
                for (int i = 0; i < customCollisionBoxes.Length; i++) {
                    if (customCollisionBoxes[i] != default) {
                        boundingBox = boundingBox.HasValue ? BoundingBox.Union(boundingBox.Value, customCollisionBoxes[i]) : customCollisionBoxes[i];
                    }
                }
                boundingBox ??= new BoundingBox(Vector3.Zero, Vector3.One);
                return new BoundingBox(boundingBox.Value.Min + vector, boundingBox.Value.Max + vector);
            }
            return new BoundingBox(vector, vector + Vector3.One);
        }

        public class Geometry : TerrainGeometry {
            public Geometry(Texture2D texture2D) : base(texture2D) {
                for (int l = 0; l < Subsets.Length; l++) {
                    Utilities.Dispose(ref Subsets[l]);
                }
                TerrainGeometrySubset terrainGeometrySubset = new ();
                TerrainGeometrySubset[] array = new[] {
                    terrainGeometrySubset,
                    terrainGeometrySubset,
                    terrainGeometrySubset,
                    terrainGeometrySubset,
                    terrainGeometrySubset,
                    terrainGeometrySubset
                };
                SubsetOpaque = terrainGeometrySubset;
                SubsetAlphaTest = terrainGeometrySubset;
                SubsetTransparent = terrainGeometrySubset;
                OpaqueSubsetsByFace = array;
                AlphaTestSubsetsByFace = array;
                TransparentSubsetsByFace = array;
                for (int i = 0; i < 7; i++) {
                    Subsets[i] = terrainGeometrySubset;
                }
            }
        }
    }
}
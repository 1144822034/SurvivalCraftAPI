using Engine;
using Engine.Graphics;

namespace Game {
    public class ModelWidget : Widget {
        public static LitShader m_shader = new(1, false, false, true, false, false);

        public static LitShader m_shaderAlpha = new(1, false, false, true, false, true);

        public List<Model> Models = new();

        public Dictionary<Model, Matrix?[]> m_boneTransforms = new();

        public Dictionary<Model, Matrix[]> m_absoluteBoneTransforms = new();

        public Dictionary<Model, Texture2D> Textures = new();

        public Vector2 Size { get; set; }

        public Color Color { get; set; }

        public bool UseAlphaThreshold { get; set; }

        public bool IsPerspective { get; set; }

        public Vector3 OrthographicFrustumSize { get; set; }

        public Vector3 ViewPosition { get; set; }

        public Vector3 ViewTarget { get; set; }

        public float ViewFov { get; set; }

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public Vector3 AutoRotationVector { get; set; }

        [Obsolete(
            "A ModelWidget may contains multiple models, please use Models field instead. This field only represents the first model of Models field."
        )]
        public Model Model {
            get => Models?[0] ?? null;
            set {
                if (value != null) {
                    if (Models.Count == 0) {
                        Models.Add(value);
                    }
                    else {
                        Models[0] = value;
                    }
                    m_boneTransforms[value] = new Matrix?[value.Bones.Count];
                    m_absoluteBoneTransforms[value] = new Matrix[value.Bones.Count];
                }
                else {
                    Models.RemoveAt(0);
                }
            }
        }

        public Action<ModelWidget, Shader, Model, ModelMesh> OnSetupShaderParameters;

        /// <summary>
        ///     �Զ�����ɫ������������ʹ��Ĭ����ɫ��������Ⱦ��
        ///     ��Ҫʹ�ã�������ͨ�� <see cref="OnSetupShaderParameters" /> ���ò��������������Ч
        /// </summary>
        public TransformedShader CustomShader { get; set; }

        public void AddModel(Model value) {
            if (value != null) {
                Models.Add(value);
                m_boneTransforms[value] = new Matrix?[value.Bones.Count];
                m_absoluteBoneTransforms[value] = new Matrix[value.Bones.Count];
            }
        }

        public bool RemoveModel(Model value) {
            if (value != null) {
                Models.Remove(value);
                m_boneTransforms.Remove(value);
                m_absoluteBoneTransforms.Remove(value);
                Textures.Remove(value);
                return true;
            }
            return false;
        }

        [Obsolete("A ModelWidget may contains multiple models. TextureOverride only represents the texture of the first model.")]
        public Texture2D TextureOverride {
            get => Textures[Models[0]];
            set => Textures[Models[0]] = value;
        }

        public ModelWidget() {
            Size = new Vector2(float.PositiveInfinity);
            IsHitTestVisible = false;
            Color = Color.White;
            UseAlphaThreshold = false;
            IsPerspective = true;
            ViewPosition = new Vector3(0f, 0f, -5f);
            ViewTarget = new Vector3(0f, 0f, 0f);
            ViewFov = 1f;
            OrthographicFrustumSize = new Vector3(0f, 10f, 10f);
        }

        public Matrix? GetBoneTransform(Model model, int boneIndex) => m_boneTransforms[model][boneIndex];

        public void SetBoneTransform(Model model, int boneIndex, Matrix? transformation) {
            m_boneTransforms[model][boneIndex] = transformation;
        }

        public override void Draw(DrawContext dc) {
            if (Models.Count == 0) {
                return;
            }
            TransformedShader shader;
            if (CustomShader != null) {
                shader = CustomShader;
            }
            else {
                shader = UseAlphaThreshold ? m_shaderAlpha : m_shader;
                LitShader litShader = shader as LitShader;
                litShader.SamplerState = SamplerState.PointClamp;
                litShader.MaterialColor = new Vector4(Color * GlobalColorTransform);
                litShader.AmbientLightColor = new Vector3(0.66f, 0.66f, 0.66f);
                litShader.DiffuseLightColor1 = new Vector3(1f, 1f, 1f);
                litShader.LightDirection1 = Vector3.Normalize(new Vector3(1f, 1f, 1f));
                if (UseAlphaThreshold) {
                    litShader.AlphaThreshold = 0f;
                }
            }
            shader.Transforms.View = Matrix.CreateLookAt(ViewPosition, ViewTarget, Vector3.UnitY);
            Viewport viewport = Display.Viewport;
            float num = ActualSize.X / ActualSize.Y;
            if (IsPerspective) {
                shader.Transforms.Projection = Matrix.CreatePerspectiveFieldOfView(ViewFov, num, 0.1f, 100f)
                    * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport.Width, -2f / viewport.Height, -1f, 1f);
            }
            else {
                Vector3 orthographicFrustumSize = OrthographicFrustumSize;
                if (orthographicFrustumSize.X < 0f) {
                    orthographicFrustumSize.X = orthographicFrustumSize.Y / num;
                }
                else if (orthographicFrustumSize.Y < 0f) {
                    orthographicFrustumSize.Y = orthographicFrustumSize.X * num;
                }
                shader.Transforms.Projection =
                    Matrix.CreateOrthographic(orthographicFrustumSize.X, orthographicFrustumSize.Y, 0f, OrthographicFrustumSize.Z)
                    * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport.Width, -2f / viewport.Height, -1f, 1f);
            }
            Display.DepthStencilState = DepthStencilState.Default;
            Display.BlendState = BlendState.AlphaBlend;
            Display.RasterizerState = RasterizerState.CullNoneScissor;
            foreach (Model model in Models) {
                ProcessBoneHierarchy(model.RootBone, Matrix.Identity);
            }
            float num2 = (float)Time.RealTime + GetHashCode() % 1000 / 100f;
            Matrix m = AutoRotationVector.LengthSquared() > 0f
                ? Matrix.CreateFromAxisAngle(Vector3.Normalize(AutoRotationVector), AutoRotationVector.Length() * num2)
                : Matrix.Identity;
            foreach (Model model in Models) {
                Texture2D texture = Textures[model];
                if (texture == null) {
                    continue;
                }
                shader.GetParameter("u_texture", true)?.SetValue(texture);
                foreach (ModelMesh mesh in model.Meshes) {
                    shader.Transforms.World[0] = m_absoluteBoneTransforms[mesh.ParentBone.Model][mesh.ParentBone.Index] * ModelMatrix * m;
                    OnSetupShaderParameters?.Invoke(this, shader, model, mesh);
                    foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                        if (meshPart.IndicesCount > 0) {
                            Display.DrawIndexed(
                                PrimitiveType.TriangleList,
                                shader,
                                meshPart.VertexBuffer,
                                meshPart.IndexBuffer,
                                meshPart.StartIndex,
                                meshPart.IndicesCount
                            );
                        }
                    }
                }
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = Models.Count > 0;
            DesiredSize = Size;
        }

        public void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform) {
            Matrix[] transforms = m_absoluteBoneTransforms[modelBone.Model];
            Matrix m = modelBone.Transform;
            if (m_boneTransforms[modelBone.Model][modelBone.Index].HasValue) {
                Vector3 translation = m.Translation;
                m.Translation = Vector3.Zero;
                m *= m_boneTransforms[modelBone.Model][modelBone.Index].Value;
                m.Translation += translation;
                Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
            }
            else {
                Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
            }
            foreach (ModelBone childBone in modelBone.ChildBones) {
                ProcessBoneHierarchy(childBone, transforms[modelBone.Index]);
            }
        }
    }
}
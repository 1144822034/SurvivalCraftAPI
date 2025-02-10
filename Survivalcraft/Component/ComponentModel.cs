using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Linq;
using TemplatesDatabase;
namespace Game
{
	public class ComponentModel : Component
	{
		public SubsystemSky m_subsystemSky;

		public bool IsSet;

		public bool Animated = false;

		public ComponentFrame m_componentFrame;

		public Model m_model;

		public Matrix?[] m_boneTransforms;

		public float m_boundingSphereRadius;
		/// <summary>
		/// 模型偏移
		/// </summary>
		public Vector3 ModelOffset
		{
			get;
			set;
		}
		/// <summary>
		/// 模型透明度
		/// </summary>
		public float Transparent
		{
			get;
			set;
		}
		/// <summary>
		/// 模型大小缩放
		/// </summary>
		public float ModelScale
		{
			get;
			set;
		}
		/// <summary>
		/// 纹理路径
		/// </summary>
		public string TextureRoute
		{
			get;
			set;
		}
		/// <summary>
		/// 模型路径
		/// </summary>
		public string ModelRoute
		{
			get;
			set;
		}
		public float? Opacity
		{
			get;
			set;
		}

		public Vector3? DiffuseColor
		{
			get;
			set;
		}

		public Vector4? EmissionColor
		{
			get;
			set;
		}

		public Model Model
		{
			get
			{
				return m_model;
			}
			set
			{
				SetModel(value);
			}
		}

		public Texture2D TextureOverride
		{
			get;
			set;
		}

		public virtual Func<bool> OnAnimate { get; set; }

		public bool CastsShadow
		{
			get;
			set;
		}

		public int PrepareOrder
		{
			get;
			set;
		}

		public virtual ModelRenderingMode RenderingMode
		{
			get;
			set;
		}

		public int[] MeshDrawOrders
		{
			get;
			set;
		}

		public bool IsVisibleForCamera
		{
			get;
			set;
		}

		public Matrix[] AbsoluteBoneTransformsForCamera
		{
			get;
			set;
		}

		public virtual Matrix? GetBoneTransform(int boneIndex)
		{
			return m_boneTransforms[boneIndex];
		}

		public virtual void SetBoneTransform(int boneIndex, Matrix? transformation)
		{
			var canScale = Model.m_bones[boneIndex].Name == "Body";
			var tf = canScale ? Matrix.CreateScale(ModelScale) * transformation : transformation;
			m_boneTransforms[boneIndex] = tf * Matrix.CreateTranslation(ModelOffset);
		}

		public virtual void CalculateAbsoluteBonesTransforms(Camera camera)
		{
			ProcessBoneHierarchy(Model.RootBone,camera.ViewMatrix, AbsoluteBoneTransformsForCamera);
		}

		public virtual void CalculateIsVisible(Camera camera)
		{
			if (camera.GameWidget.IsEntityFirstPersonTarget(Entity))
			{
				IsVisibleForCamera = false;
				return;
			}
			float num = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
			Vector3 vector = m_componentFrame.Position - camera.ViewPosition;
			vector.Y *= m_subsystemSky.VisibilityRangeYMultiplier;
			if (vector.LengthSquared() < num)
			{
				var sphere = new BoundingSphere(m_componentFrame.Position, m_boundingSphereRadius);
				IsVisibleForCamera = camera.ViewFrustum.Intersection(sphere);
			}
			else
			{
				IsVisibleForCamera = false;
			}
		}

		public virtual void Animate()
		{
			
		}

		public virtual void DrawExtras(Camera camera)
		{
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_componentFrame = Entity.FindComponent<ComponentFrame>(throwOnError: true);
			ModelRoute = valuesDictionary.GetValue<string>("ModelName","");
			string modeltype = valuesDictionary.GetValue<string>("ModelType", "Engine.Graphics.Model");
			Type type = Engine.Serialization.TypeCache.FindType(modeltype, true, true);
			Model = (Model)ContentManager.Get(type,ModelRoute);
			CastsShadow = valuesDictionary.GetValue<bool>("CastsShadow");
			TextureRoute = valuesDictionary.GetValue<string>("TextureOverride","");
			TextureOverride = string.IsNullOrEmpty(TextureRoute) ? null : ContentManager.Get<Texture2D>(TextureRoute);
			PrepareOrder = valuesDictionary.GetValue<int>("PrepareOrder");
			Transparent = valuesDictionary.GetValue<float>("Transparent",1f);
			ModelScale = valuesDictionary.GetValue<float>("ModelScale",1f);
			m_boundingSphereRadius = valuesDictionary.GetValue<float>("BoundingSphereRadius");
		}

		public virtual void SetModel(Model model)
		{
			IsSet = false;
			ModsManager.HookAction("OnSetModel", (modLoader) =>
			{
				modLoader.OnSetModel(this, model, out IsSet);
				return IsSet;
			});
			if (IsSet) return;
			m_model = model;
			if (m_model != null)
			{
				m_boneTransforms = new Matrix?[m_model.Bones.Count];
				AbsoluteBoneTransformsForCamera = new Matrix[m_model.Bones.Count];
				MeshDrawOrders = Enumerable.Range(0, m_model.Meshes.Count).ToArray();
			}
			else
			{
				m_boneTransforms = null;
				AbsoluteBoneTransformsForCamera = null;
				MeshDrawOrders = null;
			}
		}

		public virtual void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform, Matrix[] transforms)
		{
			Matrix m = modelBone.Transform;
			if (m_boneTransforms[modelBone.Index].HasValue)
			{
				Vector3 translation = m.Translation;
				m.Translation = Vector3.Zero;
				m *= m_boneTransforms[modelBone.Index].Value;
				m.Translation += translation;
				Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
			}
			else
			{
				Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
			}
			foreach (ModelBone childBone in modelBone.ChildBones)
			{
				ProcessBoneHierarchy(childBone, transforms[modelBone.Index], transforms);
			}
		}
	}
}

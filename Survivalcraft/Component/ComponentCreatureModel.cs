using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public abstract class ComponentCreatureModel : ComponentModel, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public ComponentCreature m_componentCreature;

		public Vector3? m_eyePosition;

		public Quaternion? m_eyeRotation;

		public float m_injuryColorFactor;

		public Vector3 m_randomLookPoint;

		public Random m_random = new();
		public float Bob
		{
			get;
			set;
		}

		public float MovementAnimationPhase
		{
			get;
			set;
		}

		public float DeathPhase
		{
			get;
			set;
		}

		public Vector3 DeathCauseOffset
		{
			get;
			set;
		}

		public Vector3? LookAtOrder
		{
			get;
			set;
		}

		public bool LookRandomOrder
		{
			get;
			set;
		}

		public float HeadShakeOrder
		{
			get;
			set;
		}

		public bool AttackOrder
		{
			get;
			set;
		}

		public bool FeedOrder
		{
			get;
			set;
		}

		public bool RowLeftOrder
		{
			get;
			set;
		}

		public bool RowRightOrder
		{
			get;
			set;
		}

		public float AimHandAngleOrder
		{
			get;
			set;
		}

		public Vector3 InHandItemOffsetOrder
		{
			get;
			set;
		}

		public Vector3 InHandItemRotationOrder
		{
			get;
			set;
		}

		public bool IsAttackHitMoment
		{
			get;
			set;
		}

		public Vector3 EyePosition
		{
			get
			{
				if (!m_eyePosition.HasValue)
				{
					m_eyePosition = CalculateEyePosition();
				}
				return m_eyePosition.Value;
			}
		}

		public Quaternion EyeRotation
		{
			get
			{
				if (!m_eyeRotation.HasValue)
				{
					m_eyeRotation = CalculateEyeRotation();
				}
				return m_eyeRotation.Value;
			}
		}

		public UpdateOrder UpdateOrder
		{
			get
			{
				ComponentBody parentBody = m_componentCreature.ComponentBody.ParentBody;
				if (parentBody != null)
				{
					ComponentCreatureModel componentCreatureModel = parentBody.Entity.FindComponent<ComponentCreatureModel>();
					if (componentCreatureModel != null)
					{
						return componentCreatureModel.UpdateOrder + 1;
					}
				}
				return UpdateOrder.CreatureModels;
			}
		}

		public override void Animate()
		{
			AnimateCreature();
			var qpacity = (m_componentCreature.ComponentSpawn.SpawnDuration > 0f) ? ((float)MathUtils.Saturate((m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.SpawnTime) / m_componentCreature.ComponentSpawn.SpawnDuration)) : 1f;
			Opacity = MathUtils.Min(qpacity, Transparent);
			if (m_componentCreature.ComponentSpawn.DespawnTime.HasValue)
			{
				Opacity = MathUtils.Min(Opacity.Value, (float)MathUtils.Saturate(1.0 - ((m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.DespawnTime.Value) / m_componentCreature.ComponentSpawn.DespawnDuration)));
			}
			DiffuseColor = Vector3.Lerp(Vector3.One, new Vector3(1f, 0f, 0f), m_injuryColorFactor);
			if (Opacity.HasValue && Opacity.Value < 1f)
			{
				bool num = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
				bool flag = m_subsystemSky.ViewUnderWaterDepth > 0f;
				RenderingMode = num == flag ? ModelRenderingMode.TransparentAfterWater : ModelRenderingMode.TransparentBeforeWater;
			}
			else
			{
				RenderingMode = ModelRenderingMode.Solid;
			}
		}

		public abstract void AnimateCreature();

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_componentCreature.ComponentHealth.Injured += delegate (Injury injury)
			{
				ComponentCreature attacker = injury.Attacker;
				if (attacker == null) return;
				if (DeathPhase == 0f && m_componentCreature.ComponentHealth.Health == 0f)
				{
					DeathCauseOffset = attacker.ComponentBody.BoundingBox.Center() - m_componentCreature.ComponentBody.BoundingBox.Center();
				}
			};
		}

		public override void OnEntityAdded()
		{
			m_componentCreature.ComponentBody.PositionChanged += delegate
			{
				m_eyePosition = null;
			};
			m_componentCreature.ComponentBody.RotationChanged += delegate
			{
				m_eyeRotation = null;
			};
		}

		public virtual void Update(float dt)
		{
			if (LookRandomOrder)
			{
				Matrix matrix = m_componentCreature.ComponentBody.Matrix;
				var v = Vector3.Normalize(m_randomLookPoint - m_componentCreature.ComponentCreatureModel.EyePosition);
				if (m_random.Float(0f, 1f) < 0.25f * dt || Vector3.Dot(matrix.Forward, v) < 0.2f)
				{
					float s = m_random.Float(-5f, 5f);
					float s2 = m_random.Float(-1f, 1f);
					float s3 = m_random.Float(3f, 8f);
					m_randomLookPoint = m_componentCreature.ComponentCreatureModel.EyePosition + (s3 * matrix.Forward) + (s2 * matrix.Up) + (s * matrix.Right);
				}
				LookAtOrder = m_randomLookPoint;
			}
			if (LookAtOrder.HasValue)
			{
				Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
				Vector3 v2 = LookAtOrder.Value - m_componentCreature.ComponentCreatureModel.EyePosition;
				float x = Vector2.Angle(new Vector2(forward.X, forward.Z), new Vector2(v2.X, v2.Z));
				float y = MathF.Asin(0.99f * Vector3.Normalize(v2).Y);
				m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(x, y) - m_componentCreature.ComponentLocomotion.LookAngles;
			}
			if (HeadShakeOrder > 0f)
			{
				HeadShakeOrder = MathUtils.Max(HeadShakeOrder - dt, 0f);
				float num = 1f * MathUtils.Saturate(4f * HeadShakeOrder);
				m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(num * (float)Math.Sin((16.0 * m_subsystemTime.GameTime) + (0.01f * GetHashCode())), 0f) - m_componentCreature.ComponentLocomotion.LookAngles;
			}
			if (m_componentCreature.ComponentHealth.Health == 0f)
			{
				DeathPhase = MathUtils.Min(DeathPhase + (3f * dt), 1f);
			}
			m_eyePosition = null;
			m_eyeRotation = null;
			LookRandomOrder = false;
			LookAtOrder = null;
		}

		public virtual Vector3 CalculateEyePosition()
		{
			Matrix matrix = m_componentCreature.ComponentBody.Matrix;
			return m_componentCreature.ComponentBody.Position + (matrix.Up * 0.95f * m_componentCreature.ComponentBody.BoxSize.Y) + (matrix.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z);
		}

		public virtual Quaternion CalculateEyeRotation()
		{
			return m_componentCreature.ComponentBody.Rotation * Quaternion.CreateFromYawPitchRoll(0f - m_componentCreature.ComponentLocomotion.LookAngles.X, m_componentCreature.ComponentLocomotion.LookAngles.Y, 0f);
		}
	}
}

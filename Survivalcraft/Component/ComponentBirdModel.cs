using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBirdModel : ComponentCreatureModel
	{
		public bool m_hasWings;

		public ModelBone m_bodyBone;

		public ModelBone m_neckBone;

		public ModelBone m_headBone;

		public ModelBone m_leg1Bone;

		public ModelBone m_leg2Bone;

		public ModelBone m_wing1Bone;

		public ModelBone m_wing2Bone;

		public float m_flyAnimationSpeed;

		public float m_walkAnimationSpeed;

		public float m_peckAnimationSpeed;

		public float m_walkBobHeight;

		public float m_peckPhase;

		public float m_kickPhase;
		public float FlyPhase
		{
			get;
			set;
		}

		public override void Update(float dt)
		{
			float num = Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
			if (MathF.Abs(num) > 0.1f)
			{
				MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
			}
			else
			{
				float num2 = MathF.Floor(MovementAnimationPhase);
				if (MovementAnimationPhase != num2)
				{
					MovementAnimationPhase = MovementAnimationPhase - num2 > 0.5f
						? MathUtils.Min(MovementAnimationPhase + (2f * dt), num2 + 1f)
						: MathUtils.Max(MovementAnimationPhase - (2f * dt), num2);
				}
			}
			float num3 = 0f;
			num3 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
			float num4 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
			Bob += num4 * (num3 - Bob);
			if (m_hasWings)
			{
				if (m_componentCreature.ComponentLocomotion.LastFlyOrder.HasValue)
				{
					float num5 = (m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.LengthSquared() > 0.99f) ? 1.5f : 1f;
					FlyPhase = MathUtils.Remainder(FlyPhase + (m_flyAnimationSpeed * num5 * dt), 1f);
					if (m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.Y < -0.1f && m_componentCreature.ComponentBody.Velocity.Length() > 4f)
					{
						FlyPhase = 0.72f;
					}
				}
				else if (FlyPhase != 1f)
				{
					FlyPhase = MathUtils.Min(FlyPhase + (m_flyAnimationSpeed * dt), 1f);
				}
			}
			if (FeedOrder)
			{
				m_peckPhase += m_peckAnimationSpeed * dt;
				if (m_peckPhase > 0.75f)
				{
					m_peckPhase -= 0.5f;
				}
			}
			else if (m_peckPhase != 0f)
			{
				m_peckPhase = MathUtils.Remainder(Math.Min(m_peckPhase + (m_peckAnimationSpeed * dt), 1f), 1f);
			}
			FeedOrder = false;
			IsAttackHitMoment = false;
			if(AttackOrder)
			{
				m_peckAnimationSpeed = MathUtils.Min(m_peckAnimationSpeed + (2f * dt),1f);
				float kickPhase = m_kickPhase;
				m_kickPhase = MathUtils.Remainder(m_kickPhase + (dt * 2f),1f);
				if(kickPhase < 0.5f && m_kickPhase >= 0.5f)
				{
					IsAttackHitMoment = true;
				}
			}
			else
			{
				m_peckAnimationSpeed = MathUtils.Max(m_peckAnimationSpeed - (2f * dt),0f);
				if(m_kickPhase != 0f)
				{
					if(m_kickPhase > 0.5f)
					{
						m_kickPhase = MathUtils.Remainder(MathUtils.Min(m_kickPhase + (dt * 2f),1f),1f);
					}
					else if(m_kickPhase > 0f)
					{
						m_kickPhase = MathUtils.Max(m_kickPhase - (dt * 2f),0f);
					}
				}
			}
			AttackOrder = false;
			base.Update(dt);
		}

		public override void AnimateCreature()
		{
			bool flag = false;
			bool skip = false;
			ModsManager.HookAction("OnModelAnimate", loader =>
			{
				loader.OnModelAnimate(this, out skip);
				flag = flag | skip;
				return false;
			});
			if (flag)
			{
				return;
			}
			float num = 0f;
			if (m_hasWings)
			{
				num += 1.2f * MathF.Sin((float)Math.PI * 2f * (FlyPhase + 0.75f));
				if (m_componentCreature.ComponentBody.StandingOnValue.HasValue)
				{
					num += 0.3f * MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
				}
			}
			float num2;
			float num3;
			if (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f || m_componentCreature.ComponentLocomotion.FlySpeed == 0f)
			{
				num2 = 0.6f * MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
				num3 = 0f - num2;
			}
			else
			{
				num2 = num3 = 0f - MathUtils.DegToRad(60f);
			}
			Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
			if (m_componentCreature.ComponentHealth.Health > 0f)
			{
				float yaw = m_componentCreature.ComponentLocomotion.LookAngles.X / 2f;
				float yaw2 = m_componentCreature.ComponentLocomotion.LookAngles.X / 2f;
				float num4 = 0f;
				float num5 = 0f;
				if (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f)
				{
					num4 = 0.5f * MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase / 2f);
					num5 = 0f - num4;
				}
				float num6 = MathF.Cos((float)Math.PI * 2f * m_kickPhase!=0? m_kickPhase : m_peckPhase);
				num4 -= 1.25f * (1f - ((num6 >= 0f) ? num6 : (-0.5f * num6)));
				num4 += m_componentCreature.ComponentLocomotion.LookAngles.Y;
				SetBoneTransform(m_bodyBone.Index, Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f) * Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, Bob, 0f)));
				SetBoneTransform(m_neckBone.Index, Matrix.CreateFromYawPitchRoll(yaw2, num4, 0f));
				SetBoneTransform(m_headBone.Index, Matrix.CreateFromYawPitchRoll(yaw, num5 + Math.Clamp(vector.Y, -(float)Math.PI / 4f, (float)Math.PI / 4f), vector.Z));
				if (m_hasWings)
				{
					SetBoneTransform(m_wing1Bone.Index, Matrix.CreateRotationY(num));
					SetBoneTransform(m_wing2Bone.Index, Matrix.CreateRotationY(0f - num));
				}
				SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(num2));
				SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(num3));
			}
			else
			{
				float num7 = 1f - DeathPhase;
				float num8 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
				Vector3 position = m_componentCreature.ComponentBody.Position + (0.5f * num8 * Vector3.Normalize(m_componentCreature.ComponentBody.Matrix.Forward * new Vector3(1f, 0f, 1f)));
				SetBoneTransform(m_bodyBone.Index, Matrix.CreateFromYawPitchRoll(vector.X, (float)Math.PI / 2f * DeathPhase, 0f) * Matrix.CreateTranslation(position));
				SetBoneTransform(m_neckBone.Index, Matrix.Identity);
				SetBoneTransform(m_headBone.Index, Matrix.Identity);
				if (m_hasWings)
				{
					SetBoneTransform(m_wing1Bone.Index, Matrix.CreateRotationY(num * num7));
					SetBoneTransform(m_wing2Bone.Index, Matrix.CreateRotationY((0f - num) * num7));
				}
				SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(num2 * num7));
				SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(num3 * num7));
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_flyAnimationSpeed = valuesDictionary.GetValue<float>("FlyAnimationSpeed");
			m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
			m_peckAnimationSpeed = valuesDictionary.GetValue<float>("PeckAnimationSpeed");
			m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
		}

		public override void SetModel(Model model)
		{
			base.SetModel(model);
			if (Model != null)
			{
				m_bodyBone = Model.FindBone("Body");
				m_neckBone = Model.FindBone("Neck");
				m_headBone = Model.FindBone("Head");
				m_leg1Bone = Model.FindBone("Leg1");
				m_leg2Bone = Model.FindBone("Leg2");
				m_wing1Bone = Model.FindBone("Wing1", throwIfNotFound: false);
				m_wing2Bone = Model.FindBone("Wing2", throwIfNotFound: false);
			}
			else
			{
				m_bodyBone = null;
				m_neckBone = null;
				m_headBone = null;
				m_leg1Bone = null;
				m_leg2Bone = null;
				m_wing1Bone = null;
				m_wing2Bone = null;
			}
			m_hasWings = m_wing1Bone != null && m_wing2Bone != null;
		}
	}
}

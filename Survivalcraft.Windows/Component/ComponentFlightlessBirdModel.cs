using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFlightlessBirdModel : ComponentCreatureModel {
        public ModelBone m_bodyBone;

        public ModelBone m_neckBone;

        public ModelBone m_headBone;

        public ModelBone m_leg1Bone;

        public ModelBone m_leg2Bone;

        public float m_walkAnimationSpeed;

        public float m_walkLegsAngle;

        public float m_walkBobHeight;

        public float m_feedFactor;

        public float m_footstepsPhase;

        public float m_kickFactor;

        public float m_kickPhase;

        public float m_legAngle1;

        public float m_legAngle2;

        public float m_headAngleY;

        public override float AttackPhase {
            get => m_kickPhase;
            set => m_kickPhase = value;
        }

        public override float AttackFactor {
            get => m_kickFactor;
            set => m_kickFactor = value;
        }

        public override void Update(float dt) {
            float footstepsPhase = m_footstepsPhase;
            float num = m_componentCreature.ComponentLocomotion.SlipSpeed
                ?? Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
            if (MathF.Abs(num) > 0.2f) {
                MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
                m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
            }
            else {
                MovementAnimationPhase = 0f;
                m_footstepsPhase = 0f;
            }
            float num2 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
            float num3 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
            Bob += num3 * (num2 - Bob);
            float num4 = MathF.Floor(m_footstepsPhase);
            if (m_footstepsPhase > num4
                && footstepsPhase <= num4) {
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
            }
            m_feedFactor = FeedOrder ? MathUtils.Min(m_feedFactor + 2f * dt, 1f) : MathUtils.Max(m_feedFactor - 2f * dt, 0f);
            IsAttackHitMoment = false;
            if (AttackOrder) {
                m_kickFactor = MathUtils.Min(m_kickFactor + 6f * dt, 1f);
                float kickPhase = m_kickPhase;
                m_kickPhase = MathUtils.Remainder(m_kickPhase + dt * 2f, 1f);
                if (kickPhase < 0.5f
                    && m_kickPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                m_kickFactor = MathUtils.Max(m_kickFactor - 6f * dt, 0f);
                if (m_kickPhase != 0f) {
                    if (m_kickPhase > 0.5f) {
                        m_kickPhase = MathUtils.Remainder(MathUtils.Min(m_kickPhase + dt * 2f, 1f), 1f);
                    }
                    else if (m_kickPhase > 0f) {
                        m_kickPhase = MathUtils.Max(m_kickPhase - dt * 2f, 0f);
                    }
                }
            }
            FeedOrder = false;
            AttackOrder = false;
            base.Update(dt);
        }

        public override void AnimateCreature() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
            if (m_componentCreature.ComponentHealth.Health > 0f) {
                float num = 0f;
                float num2 = 0f;
                float num3 = 0f;
                if (MovementAnimationPhase != 0f
                    && (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f)) {
                    float num4 = Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward)
                        > 0.75f * m_componentCreature.ComponentLocomotion.WalkSpeed
                            ? 1.5f * m_walkLegsAngle
                            : m_walkLegsAngle;
                    float num5 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
                    float num6 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
                    num = num4 * num5 + m_kickPhase;
                    num2 = num4 * num6;
                    num3 = MathUtils.DegToRad(5f) * MathF.Sin((float)Math.PI * 4f * MovementAnimationPhase);
                }
                if (m_kickFactor != 0f) {
                    float x = MathUtils.DegToRad(60f) * MathF.Sin((float)Math.PI * MathUtils.Sigmoid(m_kickPhase, 5f));
                    num = MathUtils.Lerp(num, x, m_kickFactor);
                }
                float num7 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
                m_legAngle1 += num7 * (num - m_legAngle1);
                m_legAngle2 += num7 * (num2 - m_legAngle2);
                m_headAngleY += num7 * (num3 - m_headAngleY);
                Vector2 vector2 = m_componentCreature.ComponentLocomotion.LookAngles;
                vector2.Y += m_headAngleY;
                if (m_feedFactor > 0f) {
                    float y = 0f - MathUtils.DegToRad(35f + 55f * SimplexNoise.OctavedNoise((float)m_subsystemTime.GameTime, 3f, 2, 2f, 0.75f));
                    vector2 = Vector2.Lerp(v2: new Vector2(0f, y), v1: vector2, f: m_feedFactor);
                }
                vector2.X = Math.Clamp(vector2.X, 0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f));
                vector2.Y = Math.Clamp(vector2.Y, 0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(50f));
                Vector2 vector3 = Vector2.Zero;
                if (m_neckBone != null) {
                    vector3 = 0.4f * vector2;
                    vector2 = 0.6f * vector2;
                }
                SetBoneTransform(
                    m_bodyBone.Index,
                    Matrix.CreateRotationY(vector.X) * Matrix.CreateTranslation(position.X, position.Y + Bob, position.Z)
                );
                SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(vector2.Y) * Matrix.CreateRotationZ(0f - vector2.X));
                if (m_neckBone != null) {
                    SetBoneTransform(m_neckBone.Index, Matrix.CreateRotationX(vector3.Y) * Matrix.CreateRotationZ(0f - vector3.X));
                }
                SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1));
                SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(m_legAngle2));
            }
            else {
                float num8 = 1f - DeathPhase;
                float num9 = Vector3.Dot(m_componentFrame.Matrix.Right, DeathCauseOffset) > 0f ? 1 : -1;
                float num10 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
                SetBoneTransform(
                    m_bodyBone.Index,
                    Matrix.CreateTranslation(-0.5f * num10 * DeathPhase * Vector3.UnitY)
                    * Matrix.CreateFromYawPitchRoll(vector.X, 0f, (float)Math.PI / 2f * DeathPhase * num9)
                    * Matrix.CreateTranslation(0.2f * num10 * DeathPhase * Vector3.UnitY)
                    * Matrix.CreateTranslation(position)
                );
                SetBoneTransform(m_headBone.Index, Matrix.Identity);
                if (m_neckBone != null) {
                    SetBoneTransform(m_neckBone.Index, Matrix.Identity);
                }
                SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1 * num8));
                SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(m_legAngle2 * num8));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
            m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
            m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (Model != null) {
                m_bodyBone = Model.FindBone("Body");
                m_neckBone = Model.FindBone("Neck", false);
                m_headBone = Model.FindBone("Head");
                m_leg1Bone = Model.FindBone("Leg1");
                m_leg2Bone = Model.FindBone("Leg2");
            }
            else {
                m_bodyBone = null;
                m_neckBone = null;
                m_headBone = null;
                m_leg1Bone = null;
                m_leg2Bone = null;
            }
        }
    }
}
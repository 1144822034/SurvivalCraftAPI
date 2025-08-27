using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFourLeggedModel : ComponentCreatureModel {
        public enum Gait {
            Walk,
            Trot,
            Canter
        }

        public SubsystemAudio m_subsystemAudio;

        public SubsystemSoundMaterials m_subsystemSoundMaterials;

        public ModelBone m_bodyBone;

        public ModelBone m_neckBone;

        public ModelBone m_headBone;

        public ModelBone m_leg1Bone;

        public ModelBone m_leg2Bone;

        public ModelBone m_leg3Bone;

        public ModelBone m_leg4Bone;

        public float m_walkAnimationSpeed;

        public float m_canterLegsAngleFactor;

        public float m_walkFrontLegsAngle;

        public float m_walkHindLegsAngle;

        public float m_walkBobHeight;

        public bool m_moveLegWhenFeeding;

        public bool m_canCanter;

        public bool m_canTrot;

        public bool m_useCanterSound;

        public Gait m_gait;

        public float m_feedFactor;

        public float m_buttFactor;

        public float m_buttPhase;

        public float m_footstepsPhase;

        public float m_legAngle1;

        public float m_legAngle2;

        public float m_legAngle3;

        public float m_legAngle4;

        public float m_headAngleY;

        public override float AttackPhase {
            get => m_buttPhase;
            set => m_buttPhase = value;
        }

        public override float AttackFactor {
            get => m_buttFactor;
            set => m_buttFactor = value;
        }

        public override void Update(float dt) {
            float footstepsPhase = m_footstepsPhase;
            float num = m_componentCreature.ComponentLocomotion.SlipSpeed
                ?? Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
            if (m_canCanter && num > 0.7f * m_componentCreature.ComponentLocomotion.WalkSpeed) {
                m_gait = Gait.Canter;
                MovementAnimationPhase += num * dt * 0.7f * m_walkAnimationSpeed;
                m_footstepsPhase += 0.7f * m_walkAnimationSpeed * num * dt;
            }
            else if (m_canTrot && num > 0.5f * m_componentCreature.ComponentLocomotion.WalkSpeed) {
                m_gait = Gait.Trot;
                MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
                m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
            }
            else if (MathF.Abs(num) > 0.2f) {
                m_gait = Gait.Walk;
                MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
                m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
            }
            else {
                m_gait = Gait.Walk;
                MovementAnimationPhase = 0f;
                m_footstepsPhase = 0f;
            }
            float num2 = 0f;
            if (m_gait == Gait.Canter) {
                num2 = (0f - m_walkBobHeight) * 1.5f * MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
            }
            else if (m_gait == Gait.Trot) {
                num2 = m_walkBobHeight * 1.5f * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
            }
            else if (m_gait == Gait.Walk) {
                num2 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase));
            }
            float num3 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
            Bob += num3 * (num2 - Bob);
            if (m_gait == Gait.Canter && m_useCanterSound) {
                float num4 = MathF.Floor(m_footstepsPhase);
                if (m_footstepsPhase > num4
                    && footstepsPhase <= num4) {
                    string footstepSoundMaterialName = m_subsystemSoundMaterials.GetFootstepSoundMaterialName(m_componentCreature);
                    if (!string.IsNullOrEmpty(footstepSoundMaterialName)
                        && footstepSoundMaterialName != "Water") {
                        m_subsystemAudio.PlayRandomSound(
                            "Audio/Footsteps/CanterDirt",
                            0.75f,
                            m_random.Float(-0.25f, 0f),
                            m_componentCreature.ComponentBody.Position,
                            3f,
                            true
                        );
                    }
                }
            }
            else {
                float num5 = MathF.Floor(m_footstepsPhase);
                if (m_footstepsPhase > num5
                    && footstepsPhase <= num5) {
                    m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
                }
            }
            m_feedFactor = FeedOrder ? MathUtils.Min(m_feedFactor + 2f * dt, 1f) : MathUtils.Max(m_feedFactor - 2f * dt, 0f);
            IsAttackHitMoment = false;
            if (AttackOrder) {
                m_buttFactor = MathUtils.Min(m_buttFactor + 4f * dt, 1f);
                float buttPhase = m_buttPhase;
                m_buttPhase = MathUtils.Remainder(m_buttPhase + dt * 2f, 1f);
                if (buttPhase < 0.5f
                    && m_buttPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                m_buttFactor = MathUtils.Max(m_buttFactor - 4f * dt, 0f);
                if (m_buttPhase != 0f) {
                    if (m_buttPhase > 0.5f) {
                        m_buttPhase = MathUtils.Remainder(MathUtils.Min(m_buttPhase + dt * 2f, 1f), 1f);
                    }
                    else if (m_buttPhase > 0f) {
                        m_buttPhase = MathUtils.Max(m_buttPhase - dt * 2f, 0f);
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
                float num4 = 0f;
                float num5 = 0f;
                if (MovementAnimationPhase != 0f
                    && (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f)) {
                    if (m_gait == Gait.Canter) {
                        float num6 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
                        float num7 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.25f));
                        float num8 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.15f));
                        float num9 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.4f));
                        num = m_walkFrontLegsAngle * m_canterLegsAngleFactor * num6;
                        num2 = m_walkFrontLegsAngle * m_canterLegsAngleFactor * num7;
                        num3 = m_walkHindLegsAngle * m_canterLegsAngleFactor * num8;
                        num4 = m_walkHindLegsAngle * m_canterLegsAngleFactor * num9;
                        num5 = MathUtils.DegToRad(8f) * MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
                    }
                    else if (m_gait == Gait.Trot) {
                        float num10 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
                        float num11 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
                        float num12 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
                        float num13 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
                        num = m_walkFrontLegsAngle * num10;
                        num2 = m_walkFrontLegsAngle * num11;
                        num3 = m_walkHindLegsAngle * num12;
                        num4 = m_walkHindLegsAngle * num13;
                        num5 = MathUtils.DegToRad(3f) * MathF.Sin((float)Math.PI * 4f * MovementAnimationPhase);
                    }
                    else {
                        float num14 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
                        float num15 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
                        float num16 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.25f));
                        float num17 = MathF.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.75f));
                        num = m_walkFrontLegsAngle * num14;
                        num2 = m_walkFrontLegsAngle * num15;
                        num3 = m_walkHindLegsAngle * num16;
                        num4 = m_walkHindLegsAngle * num17;
                        num5 = MathUtils.DegToRad(3f) * MathF.Sin((float)Math.PI * 4f * MovementAnimationPhase);
                    }
                }
                float num18 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
                m_legAngle1 += num18 * (num - m_legAngle1);
                m_legAngle2 += num18 * (num2 - m_legAngle2);
                m_legAngle3 += num18 * (num3 - m_legAngle3);
                m_legAngle4 += num18 * (num4 - m_legAngle4);
                m_headAngleY += num18 * (num5 - m_headAngleY);
                Vector2 vector2 = m_componentCreature.ComponentLocomotion.LookAngles;
                vector2.Y += m_headAngleY;
                vector2.X = Math.Clamp(vector2.X, 0f - MathUtils.DegToRad(65f), MathUtils.DegToRad(65f));
                vector2.Y = Math.Clamp(vector2.Y, 0f - MathUtils.DegToRad(55f), MathUtils.DegToRad(55f));
                Vector2 vector3 = Vector2.Zero;
                if (m_neckBone != null) {
                    vector3 = 0.6f * vector2;
                    vector2 = 0.4f * vector2;
                }
                if (m_feedFactor > 0f) {
                    float y = 0f - MathUtils.DegToRad(25f + 45f * SimplexNoise.OctavedNoise((float)m_subsystemTime.GameTime, 3f, 2, 2f, 0.75f));
                    vector2 = Vector2.Lerp(v2: new Vector2(0f, y), v1: vector2, f: m_feedFactor);
                    //if (m_moveLegWhenFeeding)
                    //{
                    //float x = MathUtils.DegToRad(20f) + (MathUtils.PowSign(SimplexNoise.OctavedNoise((float)m_subsystemTime.GameTime, 1f, 1, 1f, 1f) - 0.5f, 0.33f) / 0.5f * MathUtils.DegToRad(25f) * (float)Math.Sin(17.0 * m_subsystemTime.GameTime));
                    //num2 = MathUtils.Lerp(num2, x, m_feedFactor);
                    //}
                }
                if (m_buttFactor != 0f) {
                    float y2 = (0f - MathUtils.DegToRad(40f)) * MathF.Sin((float)Math.PI * 2f * MathUtils.Sigmoid(m_buttPhase, 4f));
                    vector2 = Vector2.Lerp(v2: new Vector2(0f, y2), v1: vector2, f: m_buttFactor);
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
                SetBoneTransform(m_leg3Bone.Index, Matrix.CreateRotationX(m_legAngle3));
                SetBoneTransform(m_leg4Bone.Index, Matrix.CreateRotationX(m_legAngle4));
            }
            else {
                float num19 = 1f - DeathPhase;
                float num20 = Vector3.Dot(m_componentFrame.Matrix.Right, DeathCauseOffset) > 0f ? 1 : -1;
                float num21 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
                SetBoneTransform(
                    m_bodyBone.Index,
                    Matrix.CreateTranslation(-0.5f * num21 * Vector3.UnitY * DeathPhase)
                    * Matrix.CreateFromYawPitchRoll(vector.X, 0f, (float)Math.PI / 2f * DeathPhase * num20)
                    * Matrix.CreateTranslation(0.2f * num21 * Vector3.UnitY * DeathPhase)
                    * Matrix.CreateTranslation(position)
                );
                SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(MathUtils.DegToRad(50f) * DeathPhase));
                if (m_neckBone != null) {
                    SetBoneTransform(m_neckBone.Index, Matrix.Identity);
                }
                SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1 * num19));
                SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(m_legAngle2 * num19));
                SetBoneTransform(m_leg3Bone.Index, Matrix.CreateRotationX(m_legAngle3 * num19));
                SetBoneTransform(m_leg4Bone.Index, Matrix.CreateRotationX(m_legAngle4 * num19));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(true);
            m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
            m_walkFrontLegsAngle = valuesDictionary.GetValue<float>("WalkFrontLegsAngle");
            m_walkHindLegsAngle = valuesDictionary.GetValue<float>("WalkHindLegsAngle");
            m_canterLegsAngleFactor = valuesDictionary.GetValue<float>("CanterLegsAngleFactor");
            m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
            m_moveLegWhenFeeding = valuesDictionary.GetValue<bool>("MoveLegWhenFeeding");
            m_canCanter = valuesDictionary.GetValue<bool>("CanCanter");
            m_canTrot = valuesDictionary.GetValue<bool>("CanTrot");
            m_useCanterSound = valuesDictionary.GetValue<bool>("UseCanterSound");
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
                m_leg3Bone = Model.FindBone("Leg3");
                m_leg4Bone = Model.FindBone("Leg4");
            }
            else {
                m_bodyBone = null;
                m_neckBone = null;
                m_headBone = null;
                m_leg1Bone = null;
                m_leg2Bone = null;
                m_leg3Bone = null;
                m_leg4Bone = null;
            }
        }
    }
}
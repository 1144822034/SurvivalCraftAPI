using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFishModel : ComponentCreatureModel {
        public ModelBone m_bodyBone;

        public ModelBone m_tail1Bone;

        public ModelBone m_tail2Bone;

        public ModelBone m_jawBone;

        public float m_swimAnimationSpeed;

        public bool m_hasVerticalTail;

        public float m_bitingPhase;

        public float m_tailWagPhase;

        public Vector2 m_tailTurn;

        public float m_digInDepth;

        public float m_digInTailPhase;

        public float? BendOrder { get; set; }

        public float DigInOrder { get; set; }

        public override float AttackPhase {
            get => m_bitingPhase;
            set => m_bitingPhase = value;
        }

        public override void Update(float dt) {
            if (m_componentCreature.ComponentLocomotion.LastSwimOrder.HasValue
                && m_componentCreature.ComponentLocomotion.LastSwimOrder.Value != Vector3.Zero) {
                float num = m_componentCreature.ComponentLocomotion.LastSwimOrder.Value.LengthSquared() > 0.99f ? 1.75f : 1f;
                MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + m_swimAnimationSpeed * num * dt, 1000f);
            }
            else {
                MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + 0.15f * m_swimAnimationSpeed * dt, 1000f);
            }
            if (BendOrder.HasValue) {
                if (m_hasVerticalTail) {
                    m_tailTurn.X = 0f;
                    m_tailTurn.Y = BendOrder.Value;
                }
                else {
                    m_tailTurn.X = BendOrder.Value;
                    m_tailTurn.Y = 0f;
                }
            }
            else {
                m_tailTurn.X += MathUtils.Saturate(2f * m_componentCreature.ComponentLocomotion.TurnSpeed * dt)
                    * (0f - m_componentCreature.ComponentLocomotion.LastTurnOrder.X - m_tailTurn.X);
            }
            if (DigInOrder > m_digInDepth) {
                float num2 = (DigInOrder - m_digInDepth) * MathUtils.Min(1.5f * dt, 1f);
                m_digInDepth += num2;
                m_digInTailPhase += 20f * num2;
            }
            else if (DigInOrder < m_digInDepth) {
                m_digInDepth += (DigInOrder - m_digInDepth) * MathUtils.Min(5f * dt, 1f);
            }
            float num3 = 0.33f * m_componentCreature.ComponentLocomotion.TurnSpeed;
            float num4 = 1f * m_componentCreature.ComponentLocomotion.TurnSpeed;
            IsAttackHitMoment = false;
            if (AttackOrder || FeedOrder) {
                if (AttackOrder) {
                    m_tailWagPhase = MathUtils.Remainder(m_tailWagPhase + num3 * dt, 1f);
                }
                float bitingPhase = m_bitingPhase;
                m_bitingPhase = MathUtils.Remainder(m_bitingPhase + num4 * dt, 1f);
                if (AttackOrder
                    && bitingPhase < 0.5f
                    && m_bitingPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                }
            }
            else {
                if (m_tailWagPhase != 0f) {
                    m_tailWagPhase = MathUtils.Remainder(MathUtils.Min(m_tailWagPhase + num3 * dt, 1f), 1f);
                }
                if (m_bitingPhase != 0f) {
                    m_bitingPhase = MathUtils.Remainder(MathUtils.Min(m_bitingPhase + num4 * dt, 1f), 1f);
                }
            }
            AttackOrder = false;
            FeedOrder = false;
            BendOrder = null;
            DigInOrder = 0f;
            base.Update(dt);
        }

        public override void AnimateCreature() {
            Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
            if (m_componentCreature.ComponentHealth.Health == 0f) {
                float num = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
                Vector3 position = m_componentCreature.ComponentBody.Position + 1f * num * DeathPhase * Vector3.UnitY;
                SetBoneTransform(
                    m_bodyBone.Index,
                    Matrix.CreateFromYawPitchRoll(vector.X, 0f, MathF.PI * DeathPhase) * Matrix.CreateTranslation(position)
                );
                SetBoneTransform(m_tail1Bone.Index, Matrix.Identity);
                SetBoneTransform(m_tail2Bone.Index, Matrix.Identity);
                if (m_jawBone != null) {
                    SetBoneTransform(m_jawBone.Index, Matrix.Identity);
                }
                return;
            }
            if (m_componentCreature.ComponentBody.IsEmbeddedInIce) {
                Matrix value = Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f)
                    * Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, 0f - m_digInDepth, 0f));
                SetBoneTransform(m_bodyBone.Index, value);
                return;
            }
            float num2 = m_digInTailPhase + m_tailWagPhase;
            float num3;
            float num4;
            float num5;
            float num6;
            if (m_hasVerticalTail) {
                num3 = MathUtils.DegToRad(25f) * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * num2) - m_tailTurn.X, -1f, 1f);
                num4 = MathUtils.DegToRad(30f)
                    * Math.Clamp(0.5f * MathF.Sin(2f * (MathF.PI * MathUtils.Max(num2 - 0.25f, 0f))) - m_tailTurn.X, -1f, 1f);
                num5 = MathUtils.DegToRad(25f) * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * MovementAnimationPhase) - m_tailTurn.Y, -1f, 1f);
                num6 = MathUtils.DegToRad(30f)
                    * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * MathUtils.Max(MovementAnimationPhase - 0.25f, 0f)) - m_tailTurn.Y, -1f, 1f);
            }
            else {
                num3 = MathUtils.DegToRad(25f)
                    * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * (MovementAnimationPhase + num2)) - m_tailTurn.X, -1f, 1f);
                num4 = MathUtils.DegToRad(30f)
                    * Math.Clamp(
                        0.5f * MathF.Sin(2f * (MathF.PI * MathUtils.Max(MovementAnimationPhase + num2 - 0.25f, 0f))) - m_tailTurn.X,
                        -1f,
                        1f
                    );
                num5 = MathUtils.DegToRad(25f) * Math.Clamp(0f - m_tailTurn.Y, -1f, 1f);
                num6 = MathUtils.DegToRad(30f) * Math.Clamp(0f - m_tailTurn.Y, -1f, 1f);
            }
            float radians = 0f;
            if (m_bitingPhase > 0f) {
                radians = (0f - MathUtils.DegToRad(30f)) * MathF.Sin(MathF.PI * m_bitingPhase);
            }
            Matrix value2 = Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f)
                * Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, 0f - m_digInDepth, 0f));
            SetBoneTransform(m_bodyBone.Index, value2);
            Matrix identity = Matrix.Identity;
            if (num3 != 0f) {
                identity *= Matrix.CreateRotationZ(num3);
            }
            if (num5 != 0f) {
                identity *= Matrix.CreateRotationX(num5);
            }
            Matrix identity2 = Matrix.Identity;
            if (num4 != 0f) {
                identity2 *= Matrix.CreateRotationZ(num4);
            }
            if (num6 != 0f) {
                identity2 *= Matrix.CreateRotationX(num6);
            }
            SetBoneTransform(m_tail1Bone.Index, identity);
            SetBoneTransform(m_tail2Bone.Index, identity2);
            if (m_jawBone != null) {
                SetBoneTransform(m_jawBone.Index, Matrix.CreateRotationX(radians));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_hasVerticalTail = valuesDictionary.GetValue<bool>("HasVerticalTail");
            m_swimAnimationSpeed = valuesDictionary.GetValue<float>("SwimAnimationSpeed");
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (Model != null) {
                m_bodyBone = Model.FindBone("Body");
                m_tail1Bone = Model.FindBone("Tail1");
                m_tail2Bone = Model.FindBone("Tail2");
                m_jawBone = Model.FindBone("Jaw", false);
            }
            else {
                m_bodyBone = null;
                m_tail1Bone = null;
                m_tail2Bone = null;
                m_jawBone = null;
            }
        }

        public override Vector3 CalculateEyePosition() {
            Matrix matrix = m_componentCreature.ComponentBody.Matrix;
            Vector3 result = m_componentCreature.ComponentBody.Position
                + matrix.Up * 1f * m_componentCreature.ComponentBody.BoxSize.Y
                + matrix.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z;
            ModsManager.HookAction(
                "RecalculateModelEyePosition",
                loader => {
                    loader.RecalculateModelEyePosition(this, ref result);
                    return false;
                }
            );
            return result;
        }
    }
}
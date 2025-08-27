using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSimpleModel : ComponentModel {
        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentSpawn m_componentSpawn;

        public override void Animate() {
            base.Animate();
            if (Animated) {
                return;
            }
            if (m_componentSpawn != null) {
                Opacity = m_componentSpawn.SpawnDuration > 0f
                    ? (float)MathUtils.Saturate(
                        (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.SpawnTime) / m_componentSpawn.SpawnDuration
                    )
                    : 1f;
                if (m_componentSpawn.DespawnTime.HasValue) {
                    Opacity = MathUtils.Min(
                        Opacity.Value,
                        (float)MathUtils.Saturate(
                            1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.DespawnTime.Value) / m_componentSpawn.DespawnDuration
                        )
                    );
                }
            }
            SetBoneTransform(Model.RootBone.Index, m_componentFrame.Matrix);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentSpawn = Entity.FindComponent<ComponentSpawn>();
            base.Load(valuesDictionary, idToEntityMap);
        }
    }
}
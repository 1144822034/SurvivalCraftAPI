using System;

namespace GameEntitySystem {
    public class EntityAddRemoveEventArgs : EventArgs {
        Entity m_entity;

        public Entity Entity => m_entity;

        public EntityAddRemoveEventArgs(Entity entity) => m_entity = entity;
    }
}
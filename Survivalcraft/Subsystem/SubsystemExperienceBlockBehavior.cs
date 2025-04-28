using Engine;

namespace Game
{
    public class SubsystemExperienceBlockBehavior : SubsystemBlockBehavior
    {
        public override void OnPickableGathered(Pickable pickable, ComponentPickableGatherer target, Vector3 distanceToTarget)
        {
            var distanceSquared = distanceToTarget.LengthSquared();
            if (!pickable.ToRemove && distanceSquared < pickable.DistanceToPick * pickable.DistanceToPick)
            {
                var targetComponentLevel = target.Entity.FindComponent<ComponentLevel>();
                if (targetComponentLevel != null)
                {
                    targetComponentLevel.AddExperience(pickable.Count, playSound: true);
                    pickable.ToRemove = true;
                }
            }
        }
    }
}

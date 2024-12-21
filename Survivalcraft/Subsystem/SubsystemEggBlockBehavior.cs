using Engine;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemEggBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemCreatureSpawn m_subsystemCreatureSpawn;

		public EggBlock m_eggBlock = (EggBlock)BlocksManager.Blocks[118];

		public Random m_random = new();

		public const string fName = "SubsystemEggBlockBehavior";

		public override int[] HandledBlocks => new int[0];

        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem)
        {
            int data = Terrain.ExtractData(worldItem.Value);
            bool isCooked = EggBlock.GetIsCooked(data);
            bool isLaid = EggBlock.GetIsLaid(data);

            if (!isCooked && (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_random.Float(0f, 1f) <= (isLaid ? 0.15f : 1f)))
            {
                try
                {
                    EggBlock.EggType eggType = m_eggBlock.GetEggType(data);
                    Entity entity = DatabaseManager.CreateEntity(base.Project, eggType.TemplateName, throwIfNotFound: true);
                    entity.FindComponent<ComponentBody>(throwOnError: true).Position = worldItem.Position;
                    entity.FindComponent<ComponentBody>(throwOnError: true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
                    entity.FindComponent<ComponentSpawn>(throwOnError: true).SpawnDuration = 0.25f;
                    base.Project.AddEntity(entity);
                }
                catch (Exception e)
                {
                    Engine.Log.Error($"Spawning creature from egg (index: {((data >> 4) & 0xFFF)}) error: {e}");
                    Projectile projectile = worldItem as Projectile;
                    if (projectile != null)
                    {
                        ComponentGui componentGui = projectile.Owner?.Entity.FindComponent<ComponentGui>();
                        if (componentGui != null)
                        {
                            componentGui.DisplaySmallMessage(LanguageControl.Get(fName, "1"), Color.White, true, false);
                        }
                    }
                }
            }

            return true;
        }

        public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemCreatureSpawn = Project.FindSubsystem<SubsystemCreatureSpawn>(throwOnError: true);
		}
	}
}

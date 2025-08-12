using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemCraftingTableBlockBehavior : SubsystemEntityBlockBehavior
	{
		public override int[] HandledBlocks => new int[1]
		{
			27
		};

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_databaseObject = Project.GameDatabase.Database.FindDatabaseObject("CraftingTable",Project.GameDatabase.EntityTemplateType,throwIfNotFound: true);
		}

		public override bool InteractBlockEntity(ComponentBlockEntity blockEntity, ComponentMiner componentMiner)
		{
			if (blockEntity != null && componentMiner.ComponentPlayer != null)
			{
				ComponentCraftingTable componentCraftingTable = blockEntity.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(componentMiner.Inventory, componentCraftingTable);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				return true;
			}
			return false;
		}
	}
}

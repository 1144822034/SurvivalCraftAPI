using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemBlockBehaviors : Subsystem
	{
		public SubsystemBlockBehavior[][] m_blockBehaviorsByContents;

		public List<SubsystemBlockBehavior> m_blockBehaviors = [];

		public ReadOnlyList<SubsystemBlockBehavior> BlockBehaviors => new(m_blockBehaviors);

		public SubsystemBlockBehavior[] GetBlockBehaviors(int contents)
		{
			return m_blockBehaviorsByContents[contents];
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_blockBehaviorsByContents = new SubsystemBlockBehavior[BlocksManager.Blocks.Length][];
			Dictionary<int, List<SubsystemBlockBehavior>> dictionary = [];
			for (int i = 0; i < m_blockBehaviorsByContents.Length; i++)
			{
				dictionary[i] = [];
				string[] array = BlocksManager.Blocks[i].Behaviors.Split(',', StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					SubsystemBlockBehavior item = base.Project.FindSubsystem<SubsystemBlockBehavior>(text.Trim(), throwOnError: true);
					dictionary[i].Add(item);
				}
			}
			foreach (SubsystemBlockBehavior item2 in base.Project.FindSubsystems<SubsystemBlockBehavior>())
			{
				m_blockBehaviors.Add(item2);
				int[] handledBlocks = item2.HandledBlocks;
				foreach (int key in handledBlocks)
				{
					dictionary[key].Add(item2);
				}
			}
			for (int k = 0; k < m_blockBehaviorsByContents.Length; k++)
			{
				m_blockBehaviorsByContents[k] = dictionary[k].ToArray();
			}
		}
	}
}

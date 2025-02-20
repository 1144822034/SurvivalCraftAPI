using Engine.Graphics;
using System;
using Engine;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public class ClothingData
	{
		public ClothingData() { }
		public ClothingData(XElement item){
			int.TryParse(item.Attribute("Index").Value,out int ClothIndex);
			ClothIndex &= 0x3FF;
			string newDescription = item.Attribute("Description")?.Value;
			string newDisplayName = item.Attribute("DisplayName")?.Value;
			if(newDescription != null && newDescription.StartsWith("[") && newDescription.EndsWith("]") && LanguageControl.TryGetBlock(string.Format("{0}:{1}",typeof(ClothingBlock).Name,ClothIndex),"Description",out var d))
			{
				newDescription = d;
			}
			if(newDisplayName != null && newDisplayName.StartsWith("[") && newDisplayName.EndsWith("]") && LanguageControl.TryGetBlock(string.Format("{0}:{1}",typeof(ClothingBlock).Name,ClothIndex),"DisplayName",out string n))
			{
				newDisplayName = n;
			}
			Index = ClothIndex;
			DisplayName = newDisplayName;
			Slot = XmlUtils.GetAttributeValue<ClothingSlot>(item,"Slot");
			ArmorProtection = XmlUtils.GetAttributeValue<float>(item,"ArmorProtection");
			Sturdiness = XmlUtils.GetAttributeValue<float>(item,"Sturdiness");
			Insulation = XmlUtils.GetAttributeValue<float>(item,"Insulation");
			MovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item,"MovementSpeedFactor");
			SteedMovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item,"SteedMovementSpeedFactor");
			DensityModifier = XmlUtils.GetAttributeValue<float>(item,"DensityModifier");
			IsOuter = XmlUtils.GetAttributeValue<bool>(item,"IsOuter");
			CanBeDyed = XmlUtils.GetAttributeValue<bool>(item,"CanBeDyed");
			Layer = XmlUtils.GetAttributeValue<int>(item,"Layer");
			PlayerLevelRequired = XmlUtils.GetAttributeValue<int>(item,"PlayerLevelRequired");
			Texture = ContentManager.Get<Texture2D>(XmlUtils.GetAttributeValue<string>(item,"TextureName"));
			ImpactSoundsFolder = XmlUtils.GetAttributeValue<string>(item,"ImpactSoundsFolder");
			Description = newDescription;
		}

		public int Index;

		public int DisplayIndex;

		public ClothingSlot Slot;

		public float ArmorProtection;

		public float Sturdiness;

		public float Insulation;

		public float MovementSpeedFactor;

		public float SteedMovementSpeedFactor;

		public float DensityModifier;

		public Texture2D Texture;

		public string DisplayName;

		public string Description;

		public string ImpactSoundsFolder;

		public bool IsOuter;

		public bool CanBeDyed;

		public int Layer;

		public int PlayerLevelRequired;

		/// <summary>
		/// 装备
		/// </summary>
		public Action<int, ComponentClothing> Mount;
		/// <summary>
		/// 卸载
		/// </summary>
		public Action<int, ComponentClothing> Dismount;
		/// <summary>
		/// 更新
		/// </summary>
		public Action<int, ComponentClothing> Update;

		/// <summary>
		/// 计算单件护甲的防御
		/// </summary>
		/// <param name="componentClothing"></param>
		/// <param name="clothesBeforeProtection">在结算防御前，玩家的衣物列表</param>
		/// <param name="clothesAfterProtection">在结算防御后，玩家将会有的的衣物列表</param>
		/// <param name="sequence">表示这是结算到第几件护甲</param>
		/// <param name="attackment">导致这次ApplyArmorProtection的攻击，注意attackment.AttackPower指的是被任何护甲结算前的原始攻击力</param>
		/// <param name="attackPowerAfterProtection">被该件护甲结算后，剩余的攻击力</param>
		public virtual void ApplyArmorProtection(ComponentClothing componentClothing, List<int> clothesBeforeProtection, List<int> clothesAfterProtection, int sequence, Attackment attackment, ref float attackPowerAfterProtection)
		{
			int value = clothesBeforeProtection[sequence];
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			float maxDurability = block.GetDurability(value) + 1;
			float remainingSturdiness = (maxDurability - block.GetDamage(value)) / maxDurability * Sturdiness;
			float damageToAbsorb = MathF.Min(attackPowerAfterProtection * MathUtils.Saturate(ArmorProtection), remainingSturdiness);
			if(damageToAbsorb > 0f)
			{
				attackPowerAfterProtection -= damageToAbsorb;
				if(componentClothing.m_subsystemGameInfo.WorldSettings.GameMode != 0)
				{
					float x2 = (damageToAbsorb / Sturdiness * maxDurability) + 0.001f;
					int damageCount = (int)(MathF.Floor(x2) + (componentClothing.m_random.Bool(MathUtils.Remainder(x2,1f)) ? 1 : 0));
					clothesAfterProtection[sequence] = BlocksManager.DamageItem(value,damageCount,componentClothing.Entity);

					Block blockDamaged = BlocksManager.Blocks[Terrain.ExtractContents(clothesAfterProtection[sequence])];
					if(!blockDamaged.CanWear(value))
					{
						componentClothing.m_subsystemParticles.AddParticleSystem(new BlockDebrisParticleSystem(
							componentClothing.m_subsystemTerrain,componentClothing.m_componentBody.Position + (componentClothing.m_componentBody.StanceBoxSize / 2f),1f,1f,Color.White,0));
					}
				}
				if(!string.IsNullOrEmpty(ImpactSoundsFolder))
				{
					componentClothing.m_subsystemAudio.PlayRandomSound(ImpactSoundsFolder,1f,componentClothing.m_random.Float(-0.3f,0.3f),componentClothing.m_componentBody.Position,4f,0.15f);
				}
			}
		}
	}
}

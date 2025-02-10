using Engine.Graphics;
using System;

namespace Game
{
	public class ClothingData
	{
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

	}
}

using Engine;

namespace Game
{
	public class WorldItem
	{
		public int Value;

		public Vector3 Position;

		public Vector3 Velocity;

		public double CreationTime;

		public int Light;

		public bool ToRemove;

		public IInventory Creator;//指明该弹射物，掉落物是从哪一个IInventory生成的。目前这里没有完善。可以实现发射器攻击会让mod生物试图毁坏发射器等操作。

		public bool IsFireProof = false;//该弹射物和掉落物防火，不会被火焰或熔岩烧毁

        public float? MaxTimeExist;

        public float ExplosionMass = 20f;

		public bool LogDrawError = true;
		public virtual void UnderExplosion(Vector3 impulse, float damage) { }
    }
}

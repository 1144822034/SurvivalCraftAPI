namespace Game
{
	public interface IUpdateable
	{
		public UpdateOrder UpdateOrder
		{
			get
			{
				return UpdateOrder.Default;
			}
		}
		public float FloatUpdateOrder
		{
			get
			{
				return (float)UpdateOrder;
			}
		}
		public void Update(float dt);
	}
}

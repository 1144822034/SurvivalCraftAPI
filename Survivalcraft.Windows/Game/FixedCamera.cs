namespace Game
{
	public class FixedCamera : BasePerspectiveCamera
	{
		public override bool UsesMovementControls => false;

		public override bool IsEntityControlEnabled => true;

		public FixedCamera(GameWidget gameWidget)
			: base(gameWidget)
		{
		}

		public override void Activate(Camera previousCamera)
		{
			SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
		}

		public override void Update(float dt)
		{
		}
	}
}

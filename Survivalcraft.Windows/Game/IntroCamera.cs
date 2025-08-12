using Engine;

namespace Game
{
	public class IntroCamera : BasePerspectiveCamera
	{
		public override bool UsesMovementControls => false;

		public override bool IsEntityControlEnabled => false;

		public Vector3 CameraPosition
		{
			get;
			set;
		}

		public Vector3 TargetPosition
		{
			get;
			set;
		}

		public Vector3 TargetCameraPosition
		{
			get;
			set;
		}

		public float Speed
		{
			get;
			set;
		}

		public IntroCamera(GameWidget gameWidget)
			: base(gameWidget)
		{
			Speed = 1f;
		}

		public override void Activate(Camera previousCamera)
		{
			SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
		}

		public override void Update(float dt)
		{
			float x = Vector3.Distance(TargetCameraPosition, CameraPosition);
			CameraPosition += MathUtils.Min(dt * Speed, x) * Vector3.Normalize(TargetCameraPosition - CameraPosition);
			SetupPerspectiveCamera(CameraPosition, TargetPosition - CameraPosition, Vector3.UnitY);
		}
	}
}

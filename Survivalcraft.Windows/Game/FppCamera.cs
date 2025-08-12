using Engine;

namespace Game
{
	public class FppCamera : BasePerspectiveCamera
	{
		public override bool UsesMovementControls => false;

		public override bool IsEntityControlEnabled => true;

		public FppCamera(GameWidget gameWidget)
			: base(gameWidget)
		{
		}

		public override void Activate(Camera previousCamera)
		{
			SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
		}

		public override void Update(float dt)
		{
			if (GameWidget.Target != null)
			{
				var matrix = Matrix.CreateFromQuaternion(GameWidget.Target.ComponentCreatureModel.EyeRotation);
				matrix.Translation = GameWidget.Target.ComponentCreatureModel.EyePosition;
				SetupPerspectiveCamera(matrix.Translation, matrix.Forward, matrix.Up);
			}
		}
	}
}

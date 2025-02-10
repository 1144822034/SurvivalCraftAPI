using Engine;
using Engine.Graphics;

namespace Game
{
	public class VrManager
	{
		public static bool IsVrAvailable => false;

		public static bool IsVrStarted => false;

		public static RenderTarget2D VrRenderTarget => null;

		public static Matrix HmdMatrix => default(Matrix);

		public static Matrix HmdMatrixInverted => default(Matrix);

		public static Vector3 HmdMatrixYpr => default(Vector3);

		public static Matrix HmdLastMatrix => default(Matrix);

		public static Matrix HmdLastMatrixInverted => default(Matrix);

		public static Vector3 HmdLastMatrixYpr => default(Vector3);

		public static Vector2 HeadMove => default(Vector2);

		public static Vector2 WalkingVelocity => default(Vector2);

		public static void Initialize()
		{
		}

		public static void StartVr()
		{
		}

		public static void StopVr()
		{
		}

		public static void WaitGetPoses()
		{
		}

		public static void SubmitEyeTexture(VrEye eye, Texture2D texture)
		{
		}

		public static Matrix GetEyeToHeadTransform(VrEye eye)
		{
			return default(Matrix);
		}

		public static Matrix GetProjectionMatrix(VrEye eye, float near, float far)
		{
			return default(Matrix);
		}

		public static bool IsControllerPresent(VrController controller)
		{
			return false;
		}

		public static Matrix GetControllerMatrix(VrController controller)
		{
			return default(Matrix);
		}

		public static Vector2 GetStickPosition(VrController controller, float deadZone = 0f)
		{
			return default(Vector2);
		}

		public static Vector2? GetTouchpadPosition(VrController controller, float deadZone = 0f)
		{
			return default(Vector2);
		}

		public static float GetTriggerPosition(VrController controller, float deadZone = 0f)
		{
			return 0f;
		}

		public static bool IsButtonDown(VrController controller, VrControllerButton button)
		{
			return false;
		}

		public static bool IsButtonDownOnce(VrController controller, VrControllerButton button)
		{
			return false;
		}

		public static TouchInput? GetTouchInput(VrController controller)
		{
			return null;
		}
	}
}
using Engine;

namespace Game
{
	public class OrbitCamera : BasePerspectiveCamera
	{
		public Vector3 m_position;

		public Vector2 m_angles = new(0f, MathUtils.DegToRad(30f));

		public float m_distance = 6f;

		public override bool UsesMovementControls => true;

		public override bool IsEntityControlEnabled => true;

		public OrbitCamera(GameWidget gameWidget)
			: base(gameWidget)
		{
		}

		public override void Activate(Camera previousCamera)
		{
			SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
		}

		public override void Update(float dt)
		{
			ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
			if (componentPlayer == null || GameWidget.Target == null)
			{
				return;
			}
			ComponentInput componentInput = componentPlayer.ComponentInput;
			Vector3 cameraSneakMove = componentInput.PlayerInput.CameraCrouchMove;
			Vector2 cameraLook = componentInput.PlayerInput.CameraLook;
			m_angles.X = MathUtils.NormalizeAngle(m_angles.X + (4f * cameraLook.X * dt) + (0.5f * cameraSneakMove.X * dt));
			m_angles.Y = Math.Clamp(MathUtils.NormalizeAngle(m_angles.Y + (4f * cameraLook.Y * dt)), MathUtils.DegToRad(-20f), MathUtils.DegToRad(70f));
			m_distance = Math.Clamp(m_distance - (10f * cameraSneakMove.Z * dt), 2f, 16f);
			var v = Vector3.Transform(new Vector3(m_distance, 0f, 0f), Matrix.CreateFromYawPitchRoll(m_angles.X, 0f, m_angles.Y));
			Vector3 vector = GameWidget.Target.ComponentBody.Position + 0.9f * base.GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
			Vector3 vector2 = vector + v;
			if (Vector3.Distance(vector2, m_position) < 10f)
			{
				Vector3 v2 = vector2 - m_position;
				float s = MathUtils.Saturate(10f * dt);
				m_position += s * v2;
			}
			else
			{
				m_position = vector2;
			}
			Vector3 vector3 = m_position - vector;
			float? num = null;
			var vector4 = Vector3.Normalize(Vector3.Cross(vector3, Vector3.UnitY));
			var v3 = Vector3.Normalize(Vector3.Cross(vector3, vector4));
			SubsystemTerrain subsystemTerrain = base.GameWidget.SubsystemGameWidgets.SubsystemTerrain;
			for (int i = 0; i <= 0; i++)
			{
				for (int j = 0; j <= 0; j++)
				{
					Vector3 v4 = 0.6f * ((vector4 * i) + (v3 * j));
					Vector3 vector5 = vector + v4;
					Vector3 end = vector5 + vector3 + (Vector3.Normalize(vector3) * 0.6f);
					TerrainRaycastResult? terrainRaycastResult = subsystemTerrain.Raycast(vector5, end, useInteractionBoxes: false, skipAirBlocks: true, delegate(int value, float distance)
					{
						Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
						for (int k = 0; k < 6; k++)
						{
							if (!block.IsFaceTransparent(subsystemTerrain, k, value))
							{
								return true;
							}
						}
						return false;
					});
					if (terrainRaycastResult.HasValue)
					{
						num = num.HasValue ? MathUtils.Min(num.Value, terrainRaycastResult.Value.Distance) : terrainRaycastResult.Value.Distance;
					}
				}
			}
			Vector3 vector6 = (!num.HasValue) ? (vector + vector3) : (vector + (Vector3.Normalize(vector3) * MathUtils.Max(num.Value - 0.5f, 0.2f)));
			SetupPerspectiveCamera(vector6, vector - vector6, Vector3.UnitY);
		}
	}
}

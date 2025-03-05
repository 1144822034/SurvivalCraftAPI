using Engine;
using Engine.Input;
using GameEntitySystem;
using TemplatesDatabase;
namespace Game
{
	public class ComponentInput : Component, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public ComponentGui m_componentGui;

		public ComponentPlayer m_componentPlayer;

		public PlayerInput m_playerInput;

		public bool m_isViewHoldStarted;

		public double m_lastJumpTime;

		public float m_lastLeftTrigger;

		public float m_lastRightTrigger;

		public Vector2 m_vrSmoothLook;

		public PlayerInput PlayerInput => m_playerInput;
#if ANDROID
		public bool IsControlledByTouch { get; set; } = true;
#endif
#if !ANDROID
		public bool IsControlledByTouch { get; set; }
#endif

		public bool IsControlledByVr
		{
			get
			{
				if (VrManager.IsVrStarted)
				{
					return (m_componentPlayer.GameWidget.Input.Devices & WidgetInputDevice.VrControllers) != 0;
				}
				return false;
			}
		}

		public bool AllowHandleInput { get; set; } = true;

		public IInventory SplitSourceInventory
		{
			get;
			set;
		}

		public int SplitSourceSlotIndex
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Input;
		public bool IsKeyDown(string keyName)
		{
			WidgetInput input = m_componentPlayer.GameWidget.Input;
			object key = SettingsManager.KeyboardMappingSettings.GetValue(keyName,default(object));
			if(key is MouseButton mouseButton)
			{
				return input.IsMouseButtonDown(mouseButton);
			}
			if(key is Key key1)
			{
				return input.IsKeyDown(key1);
			}
			return false;
		}
		public bool IsKeyDownOnce(string keyName)
		{
			WidgetInput input = m_componentPlayer.GameWidget.Input;
			object key = SettingsManager.KeyboardMappingSettings.GetValue(keyName, default(object));
			if(key is MouseButton mouseButton)
			{
				return input.IsMouseButtonDownOnce(mouseButton);
			}
			if(key is Key key1)
			{
				return input.IsKeyDownOnce(key1);
			}
			return false;
		}
		public virtual void SetSplitSourceInventoryAndSlot(IInventory inventory, int slotIndex)
		{
			SplitSourceInventory = inventory;
			SplitSourceSlotIndex = slotIndex;
		}

		public virtual Ray3? CalculateVrHandRay()
		{
			return null;
		}

		public void Update(float dt)
		{
			m_playerInput = default;
			UpdateInputFromMouseAndKeyboard(m_componentPlayer.GameWidget.Input);
			UpdateInputFromGamepad(m_componentPlayer.GameWidget.Input);
			UpdateInputFromVrControllers(m_componentPlayer.GameWidget.Input);
			UpdateInputFromWidgets(m_componentPlayer.GameWidget.Input);
			if (m_playerInput.Jump)
			{
				if (Time.RealTime - m_lastJumpTime < 0.3)
				{
					m_playerInput.ToggleCreativeFly = true;
					m_lastJumpTime = 0.0;
				}
				else
				{
					m_lastJumpTime = Time.RealTime;
				}
			}
			m_playerInput.CameraMove = m_playerInput.Move;
			m_playerInput.CameraCrouchMove = m_playerInput.CrouchMove;
			m_playerInput.CameraLook = m_playerInput.Look;
			if (!Window.IsActive || !m_componentPlayer.PlayerData.IsReadyForPlaying)
			{
				m_playerInput = default;
			}
			else if (m_componentPlayer.ComponentHealth.Health <= 0f || m_componentPlayer.ComponentSleep.SleepFactor > 0f || !m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled)
			{
				m_playerInput = new PlayerInput
				{
					CameraMove = m_playerInput.CameraMove,
					CameraCrouchMove = m_playerInput.CameraCrouchMove,
					CameraLook = m_playerInput.CameraLook,
					TimeOfDay = m_playerInput.TimeOfDay,
					Precipitation = m_playerInput.Precipitation,
					Fog = m_playerInput.Fog,
					TakeScreenshot = m_playerInput.TakeScreenshot,
					KeyboardHelp = m_playerInput.KeyboardHelp
				};
			}
			else if (m_componentPlayer.GameWidget.ActiveCamera.UsesMovementControls)
			{
				m_playerInput.Move = Vector3.Zero;
				m_playerInput.CrouchMove = Vector3.Zero;
				m_playerInput.Look = Vector2.Zero;
				m_playerInput.Jump = false;
				m_playerInput.ToggleCrouch = false;
				m_playerInput.ToggleCreativeFly = false;
			}
			if (m_playerInput.Move.LengthSquared() > 1f)
			{
				m_playerInput.Move = Vector3.Normalize(m_playerInput.Move);
			}
			if (m_playerInput.CrouchMove.LengthSquared() > 1f)
			{
				m_playerInput.CrouchMove = Vector3.Normalize(m_playerInput.CrouchMove);
			}
			if (SplitSourceInventory != null && SplitSourceInventory.GetSlotCount(SplitSourceSlotIndex) == 0)
			{
				SetSplitSourceInventoryAndSlot(null, -1);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_componentGui = Entity.FindComponent<ComponentGui>(throwOnError: true);
			m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		}

		public virtual void UpdateInputFromMouseAndKeyboard(WidgetInput input)
		{
			Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
			Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
			if (m_componentGui.ModalPanelWidget != null || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
			{
				if (!input.IsMouseCursorVisible)
				{
					ViewWidget viewWidget = m_componentPlayer.ViewWidget;
					Vector2 value = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
					input.IsMouseCursorVisible = true;
					input.MousePosition = value;
				}
			}
			else
			{
				input.IsMouseCursorVisible = false;
				Vector2 zero = Vector2.Zero;
				int num = 0;
				if (Window.IsActive && Time.FrameDuration > 0f)
				{
					Point2 mouseMovement = input.MouseMovement;
					int mouseWheelMovement = input.MouseWheelMovement;
					float num2 = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
					zero.X = 0.02f * num2 * mouseMovement.X / Time.FrameDuration / 60f;
					zero.Y = -0.02f * num2 * mouseMovement.Y / Time.FrameDuration / 60f;
					num = mouseWheelMovement / 120;
					if (mouseMovement != Point2.Zero)
					{
						IsControlledByTouch = false;
					}
				}
				Vector3 vector = default(Vector3);
				vector += -Vector3.UnitX * (IsKeyDown("MoveLeft") ? 1 : 0);
				vector += (Vector3.UnitX * (IsKeyDown( "MoveRight") ? 1 : 0));
				vector += Vector3.UnitY * (IsKeyDown("MoveUp") ? 1 : 0);
				vector += -Vector3.UnitY * (IsKeyDown("MoveDown") ? 1 : 0);
				vector += -Vector3.UnitZ * (IsKeyDown( "MoveBack") ? 1 : 0);
				vector += Vector3.UnitZ * (IsKeyDown("MoveFront") ? 1 : 0);
				m_playerInput.Look += new Vector2(Math.Clamp(zero.X, -15f, 15f), Math.Clamp(zero.Y, -15f, 15f));
				m_playerInput.Move += vector;
				m_playerInput.CrouchMove += vector;
				m_playerInput.Jump |= IsKeyDownOnce( "Jump");
				m_playerInput.ScrollInventory -= num;
				m_playerInput.Dig = IsKeyDown( "Dig") ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Dig;
				m_playerInput.Hit = IsKeyDownOnce( "Hit") ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Hit;
				m_playerInput.Aim = IsKeyDown("Aim") ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Aim;
				m_playerInput.Interact = IsKeyDownOnce( "Interact") ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Interact;
				m_playerInput.ToggleCrouch |= IsKeyDownOnce("ToggleCrouch");
				m_playerInput.ToggleMount |= IsKeyDownOnce("ToggleMount");
				m_playerInput.ToggleCreativeFly |= IsKeyDownOnce("ToggleFly");
				m_playerInput.PickBlockType = IsKeyDown("PickBlockType") ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.PickBlockType;
			}
			if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput)
			{
				m_playerInput.ToggleInventory |= IsKeyDownOnce("ToggleInventory");
				m_playerInput.ToggleClothing |= IsKeyDownOnce("ToggleClothing");
				m_playerInput.TakeScreenshot |= IsKeyDownOnce("TakeScreenshot");
				m_playerInput.SwitchCameraMode |= IsKeyDownOnce("SwitchCameraMode");
				m_playerInput.TimeOfDay |= IsKeyDownOnce( "TimeOfDay");
				m_playerInput.Lighting |= IsKeyDownOnce("Lightning");
				m_playerInput.Precipitation |= IsKeyDownOnce("Precipitation");
				m_playerInput.Fog |= IsKeyDownOnce("Fog");
				m_playerInput.Drop |= IsKeyDownOnce("Drop");
				m_playerInput.EditItem |= IsKeyDownOnce("EditItem");
				m_playerInput.KeyboardHelp |= IsKeyDownOnce("KeyboardHelp");
				if (input.IsKeyDownOnce(Key.Number1))
				{
					m_playerInput.SelectInventorySlot = 0;
				}
				if (input.IsKeyDownOnce(Key.Number2))
				{
					m_playerInput.SelectInventorySlot = 1;
				}
				if (input.IsKeyDownOnce(Key.Number3))
				{
					m_playerInput.SelectInventorySlot = 2;
				}
				if (input.IsKeyDownOnce(Key.Number4))
				{
					m_playerInput.SelectInventorySlot = 3;
				}
				if (input.IsKeyDownOnce(Key.Number5))
				{
					m_playerInput.SelectInventorySlot = 4;
				}
				if (input.IsKeyDownOnce(Key.Number6))
				{
					m_playerInput.SelectInventorySlot = 5;
				}
				if (input.IsKeyDownOnce(Key.Number7))
				{
					m_playerInput.SelectInventorySlot = 6;
				}
				if (input.IsKeyDownOnce(Key.Number8))
				{
					m_playerInput.SelectInventorySlot = 7;
				}
				if (input.IsKeyDownOnce(Key.Number9))
				{
					m_playerInput.SelectInventorySlot = 8;
				}
				if (input.IsKeyDownOnce(Key.Number0))
				{
					m_playerInput.SelectInventorySlot = 9;
				}
			}
			ModsManager.HookAction("UpdateInput", loader => { loader.UpdateInput(this, input); return false; });
		}

		public virtual void UpdateInputFromGamepad(WidgetInput input)
		{
			Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
			Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
			if (m_componentGui.ModalPanelWidget != null || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
			{
				if (!input.IsPadCursorVisible)
				{
					ViewWidget viewWidget = m_componentPlayer.ViewWidget;
					Vector2 padCursorPosition = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
					input.IsPadCursorVisible = true;
					input.PadCursorPosition = padCursorPosition;
				}
			}
			else
			{
				input.IsPadCursorVisible = false;
				Vector3 zero = Vector3.Zero;
				Vector2 padStickPosition = input.GetPadStickPosition(GamePadStick.Left, SettingsManager.GamepadDeadZone);
				Vector2 padStickPosition2 = input.GetPadStickPosition(GamePadStick.Right, SettingsManager.GamepadDeadZone);
				float padTriggerPosition = input.GetPadTriggerPosition(GamePadTrigger.Left);
				float padTriggerPosition2 = input.GetPadTriggerPosition(GamePadTrigger.Right);
				float num = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
				zero += new Vector3(2f * padStickPosition.X, 0f, 2f * padStickPosition.Y);
				zero += Vector3.UnitY * (input.IsPadButtonDown(GamePadButton.A) ? 1 : 0);
				zero += -Vector3.UnitY * (input.IsPadButtonDown(GamePadButton.RightShoulder) ? 1 : 0);
				m_playerInput.Move += zero;
				m_playerInput.CrouchMove += zero;
				m_playerInput.Look += 0.75f * num * padStickPosition2 * MathF.Pow(padStickPosition2.LengthSquared(), 0.25f);
				m_playerInput.Jump |= input.IsPadButtonDownOnce(GamePadButton.A);
				m_playerInput.Dig = (padTriggerPosition2 >= 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Dig;
				m_playerInput.Hit = (padTriggerPosition2 >= 0.5f && m_lastRightTrigger < 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Hit;
				m_playerInput.Aim = (padTriggerPosition >= 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Aim;
				m_playerInput.Interact = (padTriggerPosition >= 0.5f && m_lastLeftTrigger < 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Interact;
				m_playerInput.Drop |= input.IsPadButtonDownOnce(GamePadButton.B);
				m_playerInput.ToggleMount |= input.IsPadButtonDownOnce(GamePadButton.LeftThumb) || input.IsPadButtonDownOnce(GamePadButton.DPadUp);
				m_playerInput.EditItem |= input.IsPadButtonDownOnce(GamePadButton.LeftShoulder);
				m_playerInput.ToggleCrouch |= input.IsPadButtonDownOnce(GamePadButton.RightShoulder);
				m_playerInput.SwitchCameraMode |= input.IsPadButtonDownOnce(GamePadButton.RightThumb) || input.IsPadButtonDownOnce(GamePadButton.DPadDown);
				if (input.IsPadButtonDownRepeat(GamePadButton.DPadLeft))
				{
					m_playerInput.ScrollInventory--;
				}
				if (input.IsPadButtonDownRepeat(GamePadButton.DPadRight))
				{
					m_playerInput.ScrollInventory++;
				}
				if (padStickPosition != Vector2.Zero || padStickPosition2 != Vector2.Zero)
				{
					IsControlledByTouch = false;
				}
				m_lastLeftTrigger = padTriggerPosition;
				m_lastRightTrigger = padTriggerPosition2;
			}
			if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput)
			{
				m_playerInput.ToggleInventory |= input.IsPadButtonDownOnce(GamePadButton.X);
				m_playerInput.ToggleClothing |= input.IsPadButtonDownOnce(GamePadButton.Y);
				m_playerInput.GamepadHelp |= input.IsPadButtonDownOnce(GamePadButton.Start);
			}
		}

		public virtual void UpdateInputFromVrControllers(WidgetInput input)
		{
			if (!IsControlledByVr)
			{
				return;
			}
			IsControlledByTouch = false;
			if (m_componentGui.ModalPanelWidget != null || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
			{
				if (!input.IsVrCursorVisible)
				{
					input.IsVrCursorVisible = true;
				}
			}
			else
			{
				input.IsVrCursorVisible = false;
				float num = MathF.Pow(1.4f, 10f * (SettingsManager.MoveSensitivity - 0.5f));
				float num2 = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
				float num3 = Math.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
				Vector2 v = Vector2.Normalize(m_componentPlayer.ComponentBody.Matrix.Right.XZ);
				Vector2 v2 = Vector2.Normalize(m_componentPlayer.ComponentBody.Matrix.Forward.XZ);
				Vector2 vrStickPosition = input.GetVrStickPosition(VrController.Left, 0.2f);
				Vector2 vrStickPosition2 = input.GetVrStickPosition(VrController.Right, 0.2f);
				Matrix m = VrManager.HmdMatrixInverted.OrientationMatrix * m_componentPlayer.ComponentCreatureModel.EyeRotation.ToMatrix();
				Vector2 xZ = Vector3.TransformNormal(new Vector3(VrManager.WalkingVelocity.X, 0f, VrManager.WalkingVelocity.Y), m).XZ;
				Vector3 value = Vector3.TransformNormal(new Vector3(VrManager.HeadMove.X, 0f, VrManager.HeadMove.Y), m);
				Vector3 zero = Vector3.Zero;
				zero += 0.5f * new Vector3(Vector2.Dot(xZ, v), 0f, Vector2.Dot(xZ, v2));
				zero += new Vector3(2f * vrStickPosition.X, 2f * vrStickPosition2.Y, 2f * vrStickPosition.Y);
				m_playerInput.Move += zero;
				m_playerInput.CrouchMove += zero;
				m_playerInput.VrMove = value;
				TouchInput? touchInput = VrManager.GetTouchInput(VrController.Left);
				if (touchInput.HasValue && num3 > 0f)
				{
					if (touchInput.Value.InputType == TouchInputType.Move)
					{
						Vector2 move = touchInput.Value.Move;
						Vector2 vector = 10f * num / num3 * new Vector2(0.5f) * move * MathF.Pow(move.LengthSquared(), 0.175f);
						m_playerInput.CrouchMove.X += vector.X;
						m_playerInput.CrouchMove.Z += vector.Y;
						m_playerInput.Move.X += ProcessInputValue(touchInput.Value.TotalMoveLimited.X, 0.1f, 1f);
						m_playerInput.Move.Z += ProcessInputValue(touchInput.Value.TotalMoveLimited.Y, 0.1f, 1f);
					}
					else if (touchInput.Value.InputType == TouchInputType.Tap)
					{
						m_playerInput.Jump = true;
					}
				}
				m_playerInput.Look += 0.5f * vrStickPosition2 * MathF.Pow(vrStickPosition2.LengthSquared(), 0.25f);
				Vector3 hmdMatrixYpr = VrManager.HmdMatrixYpr;
				Vector3 hmdLastMatrixYpr = VrManager.HmdLastMatrixYpr;
				Vector3 vector2 = hmdMatrixYpr - hmdLastMatrixYpr;
				m_playerInput.VrLook = new Vector2(vector2.X, hmdMatrixYpr.Y);
				TouchInput? touchInput2 = VrManager.GetTouchInput(VrController.Right);
				Vector2 zero2 = Vector2.Zero;
				if (touchInput2.HasValue)
				{
					if (touchInput2.Value.InputType == TouchInputType.Move)
					{
						zero2.X = touchInput2.Value.Move.X;
						m_playerInput.Move.Y += ProcessInputValue(touchInput2.Value.TotalMoveLimited.Y, 0.1f, 1f);
					}
					else if (touchInput2.Value.InputType == TouchInputType.Tap)
					{
						m_playerInput.Jump = true;
					}
				}
				if (num3 > 0f)
				{
					m_vrSmoothLook = Vector2.Lerp(m_vrSmoothLook, zero2, 14f * num3);
					m_playerInput.Look += num2 / num3 * new Vector2(0.25f) * m_vrSmoothLook * MathF.Pow(m_vrSmoothLook.LengthSquared(), 0.3f);
				}
				if (VrManager.IsControllerPresent(VrController.Right))
				{
					m_playerInput.Dig = (VrManager.IsButtonDown(VrController.Right, VrControllerButton.Trigger) ? CalculateVrHandRay() : m_playerInput.Dig);
					m_playerInput.Hit = (VrManager.IsButtonDownOnce(VrController.Right, VrControllerButton.Trigger) ? CalculateVrHandRay() : m_playerInput.Hit);
					m_playerInput.Aim = (VrManager.IsButtonDown(VrController.Left, VrControllerButton.Trigger) ? CalculateVrHandRay() : m_playerInput.Aim);
					m_playerInput.Interact = (VrManager.IsButtonDownOnce(VrController.Left, VrControllerButton.Trigger) ? CalculateVrHandRay() : m_playerInput.Interact);
				}
				m_playerInput.ToggleMount |= input.IsVrButtonDownOnce(VrController.Left, VrControllerButton.TouchpadUp);
				m_playerInput.ToggleCrouch |= input.IsVrButtonDownOnce(VrController.Left, VrControllerButton.TouchpadDown);
				m_playerInput.EditItem |= input.IsVrButtonDownOnce(VrController.Left, VrControllerButton.Grip);
				m_playerInput.ToggleCreativeFly |= input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.TouchpadUp);
				if (input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.TouchpadLeft))
				{
					m_playerInput.ScrollInventory--;
				}
				if (input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.TouchpadRight))
				{
					m_playerInput.ScrollInventory++;
				}
				m_playerInput.Drop |= input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.Grip);
			}
			if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
			{
				m_playerInput.ToggleInventory |= input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.Menu);
			}
	}
		public virtual void UpdateInputFromWidgets(WidgetInput input)
		{
			float num = MathF.Pow(1.25f, 10f * (SettingsManager.MoveSensitivity - 0.5f));
			float num2 = MathF.Pow(1.25f, 10f * (SettingsManager.LookSensitivity - 0.5f));
			float num3 = Math.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
			ViewWidget viewWidget = m_componentPlayer.ViewWidget;
			m_componentGui.MoveWidget.Radius = 30f / num * m_componentGui.MoveWidget.GlobalScale;
			if (m_componentGui.ModalPanelWidget != null || !(m_subsystemTime.GameTimeFactor > 0f) || !(num3 > 0f))
			{
				return;
			}
			var v = new Vector2(SettingsManager.LeftHandedLayout ? 96 : (-96), -96f);
			v = Vector2.TransformNormal(v, input.Widget.GlobalTransform);
			if (m_componentGui.ViewWidget != null && m_componentGui.ViewWidget.TouchInput.HasValue)
			{
				IsControlledByTouch = true;
				TouchInput value = m_componentGui.ViewWidget.TouchInput.Value;
				Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
				Vector3 viewPosition = activeCamera.ViewPosition;
				Vector3 viewDirection = activeCamera.ViewDirection;
				var direction = Vector3.Normalize(activeCamera.ScreenToWorld(new Vector3(value.Position, 1f), Matrix.Identity) - viewPosition);
				var direction2 = Vector3.Normalize(activeCamera.ScreenToWorld(new Vector3(value.Position + v, 1f), Matrix.Identity) - viewPosition);
				if (value.InputType == TouchInputType.Tap)
				{
					if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
					{
						m_playerInput.Interact = new Ray3(viewPosition, viewDirection);
						m_playerInput.Hit = new Ray3(viewPosition, viewDirection);
					}
					else
					{
						m_playerInput.Interact = new Ray3(viewPosition, direction);
						m_playerInput.Hit = new Ray3(viewPosition, direction);
					}
				}
				else if (value.InputType == TouchInputType.Hold && value.DurationFrames > 1 && value.Duration > 0.2f)
				{
					if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
					{
						m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
						m_playerInput.Aim = new Ray3(viewPosition, direction2);
					}
					else
					{
						m_playerInput.Dig = new Ray3(viewPosition, direction);
						m_playerInput.Aim = new Ray3(viewPosition, direction2);
					}
					m_isViewHoldStarted = true;
				}
				else if (value.InputType == TouchInputType.Move)
				{
					if (SettingsManager.LookControlMode == LookControlMode.EntireScreen || SettingsManager.LookControlMode == LookControlMode.SplitTouch)
					{
						var v2 = Vector2.TransformNormal(value.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
						Vector2 vector = num2 / num3 * new Vector2(0.0006f, -0.0006f) * v2 * MathF.Pow(v2.LengthSquared(), 0.125f);
						m_playerInput.Look += vector;
					}
					if (m_isViewHoldStarted)
					{
						if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
						{
							m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
							m_playerInput.Aim = new Ray3(viewPosition, direction2);
						}
						else
						{
							m_playerInput.Dig = new Ray3(viewPosition, direction);
							m_playerInput.Aim = new Ray3(viewPosition, direction2);
						}
					}
				}
			}
			else
			{
				m_isViewHoldStarted = false;
			}
			if (m_componentGui.MoveWidget != null && m_componentGui.MoveWidget.TouchInput.HasValue)
			{
				IsControlledByTouch = true;
				float radius = m_componentGui.MoveWidget.Radius;
				TouchInput value2 = m_componentGui.MoveWidget.TouchInput.Value;
				if (value2.InputType == TouchInputType.Tap)
				{
					m_playerInput.Jump = true;
				}
				else if (value2.InputType == TouchInputType.Move || value2.InputType == TouchInputType.Hold)
				{
					var v3 = Vector2.TransformNormal(value2.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
					Vector2 vector2 = num / num3 * new Vector2(0.003f, -0.003f) * v3 * MathF.Pow(v3.LengthSquared(), 0.175f);
					m_playerInput.CrouchMove.X += vector2.X;
					m_playerInput.CrouchMove.Z += vector2.Y;
					var vector3 = Vector2.TransformNormal(value2.TotalMoveLimited, m_componentGui.ViewWidget.InvertedGlobalTransform);
					m_playerInput.Move.X += ProcessInputValue(vector3.X * viewWidget.GlobalScale, 0.2f * radius, radius);
					m_playerInput.Move.Z += ProcessInputValue((0f - vector3.Y) * viewWidget.GlobalScale, 0.2f * radius, radius);
				}
			}
			if (m_componentGui.MoveRoseWidget != null)
			{
				if (m_componentGui.MoveRoseWidget.Direction != Vector3.Zero || m_componentGui.MoveRoseWidget.Jump)
				{
					IsControlledByTouch = true;
				}
				m_playerInput.Move += m_componentGui.MoveRoseWidget.Direction;
				m_playerInput.CrouchMove += m_componentGui.MoveRoseWidget.Direction;
				m_playerInput.Jump |= m_componentGui.MoveRoseWidget.Jump;
			}
			if (m_componentGui.LookWidget != null && m_componentGui.LookWidget.TouchInput.HasValue)
			{
				IsControlledByTouch = true;
				TouchInput value3 = m_componentGui.LookWidget.TouchInput.Value;
				if (value3.InputType == TouchInputType.Tap)
				{
					m_playerInput.Jump = true;
				}
				else if (value3.InputType == TouchInputType.Move)
				{
					var v4 = Vector2.TransformNormal(value3.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
					Vector2 vector4 = num2 / num3 * new Vector2(0.0006f, -0.0006f) * v4 * MathF.Pow(v4.LengthSquared(), 0.125f);
					m_playerInput.Look += vector4;
				}
			}
		}

		public static float ProcessInputValue(float value, float deadZone, float saturationZone)
		{
			return MathF.Sign(value) * Math.Clamp((MathF.Abs(value) - deadZone) / (saturationZone - deadZone), 0f, 1f);
		}
	}
}

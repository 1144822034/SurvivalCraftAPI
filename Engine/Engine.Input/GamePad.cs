#if ANDROID

using System.Collections.Concurrent;
using Axis = Android.Views.Axis;
using Android.Views;
using System.Collections.Generic;
using System.Linq;
#else
using Silk.NET.Input;
#endif
namespace Engine.Input
{
    public static class GamePad
    {
        private class State
        {
            public bool IsConnected;

            public Vector2[] Sticks = new Vector2[2];

            public float[] Triggers = new float[2];

            public bool[] Buttons = new bool[14];

            public bool[] LastButtons = new bool[14];

            public double[] ButtonsRepeat = new double[14];
        }
#if ANDROID
        public struct KeyInfo
        {
            public int DeviceId;
            public Keycode KeyCode;
            public KeyEventActions Action;
            public KeyInfo(int deviceId, Keycode keyCode, KeyEventActions action){
                DeviceId = deviceId;
                KeyCode = keyCode;
                Action = action;
            }
        }
		public static Dictionary<int, int> m_deviceToIndex = [];
		public static List<int> m_toRemove = [];
        public static ConcurrentQueue<KeyInfo> m_cachedKeyEvents = [];
#else
        public static IReadOnlyList<IGamepad> m_gamepads;
#endif
        public static double m_buttonFirstRepeatTime = 0.2;

        public static double m_buttonNextRepeatTime = 0.04;

        private static State[] m_states = new State[4]
        {
            new(),
            new(),
            new(),
            new()
        };
        internal static void Initialize()
        {
#if !ANDROID
            m_gamepads = Window.m_inputContext.Gamepads;
#endif
        }
        internal static void Dispose()
        {
        }
        internal static void BeforeFrame()
        {
#if ANDROID
			if (Time.PeriodicEvent(2.0, 0.0))
			{
				m_toRemove.Clear();
				foreach (int key in m_deviceToIndex.Keys)
				{
					if (InputDevice.GetDevice(key) == null)
					{
						m_toRemove.Add(key);
					}
				}
				foreach (int item in m_toRemove)
				{
					Disconnect(item);
				}
			}
            while (!m_cachedKeyEvents.IsEmpty)
            {
                if (m_cachedKeyEvents.TryDequeue(out KeyInfo keyInfo))
                {
                    switch (keyInfo.Action)
                    {
                        case KeyEventActions.Down: HandleKeyDown(keyInfo.DeviceId, keyInfo.KeyCode); break;
                        case KeyEventActions.Up: HandleKeyUp(keyInfo.DeviceId, keyInfo.KeyCode); break;
                    }
                }
                else
                {
                    Thread.Yield();
                }
            }
		}

        public static void HandleKeyEvent(KeyEvent e)
        {
            m_cachedKeyEvents.Enqueue(new KeyInfo(e.DeviceId, e.KeyCode, e.Action));
        }

        internal static void HandleKeyDown(int deviceId, Keycode keyCode)
		{
			int num = TranslateDeviceId(deviceId);
			if (num < 0)
			{
				return;
			}
			GamePadButton gamePadButton = TranslateKey(keyCode);
			if (gamePadButton >= GamePadButton.A)
			{
				m_states[num].Buttons[(int)gamePadButton] = true;
				return;
			}
			switch (keyCode)
			{
				case Keycode.ButtonL2:
					m_states[num].Triggers[0] = 1f;
					break;
				case Keycode.ButtonR2:
					m_states[num].Triggers[1] = 1f;
					break;
			}
		}

		internal static void HandleKeyUp(int deviceId, Keycode keyCode)
		{
			int num = TranslateDeviceId(deviceId);
			if (num < 0)
			{
				return;
			}
			GamePadButton gamePadButton = TranslateKey(keyCode);
			if (gamePadButton >= GamePadButton.A)
			{
				m_states[num].Buttons[(int)gamePadButton] = false;
				return;
			}
			switch (keyCode)
			{
				case Keycode.ButtonL2:
					m_states[num].Triggers[0] = 0f;
					break;
				case Keycode.ButtonR2:
					m_states[num].Triggers[1] = 0f;
					break;
			}
		}

		internal static void HandleMotionEvent(MotionEvent e)
		{
			int num = TranslateDeviceId(e.DeviceId);
			if (num >= 0)
			{
				m_states[num].Sticks[0] = new Vector2(e.GetAxisValue(Axis.X), 0f - e.GetAxisValue(Axis.Y));
				m_states[num].Sticks[1] = new Vector2(e.GetAxisValue(Axis.Z), 0f - e.GetAxisValue(Axis.Rz));
				m_states[num].Triggers[0] = MathF.Max(e.GetAxisValue(Axis.Ltrigger), e.GetAxisValue(Axis.Brake));
				m_states[num].Triggers[1] = MathF.Max(e.GetAxisValue(Axis.Rtrigger), e.GetAxisValue(Axis.Gas));
				float axisValue = e.GetAxisValue(Axis.HatX);
				float axisValue2 = e.GetAxisValue(Axis.HatY);
				m_states[num].Buttons[10] = axisValue < -0.5f;
				m_states[num].Buttons[12] = axisValue > 0.5f;
				m_states[num].Buttons[11] = axisValue2 < -0.5f;
				m_states[num].Buttons[13] = axisValue2 > 0.5f;
			}
		}

		public static int TranslateDeviceId(int deviceId)
		{
			if (m_deviceToIndex.TryGetValue(deviceId, out int value))
			{
				return value;
			}
			for (int i = 0; i < 4; i++)
			{
				if (!m_deviceToIndex.Values.Contains(i))
				{
					Connect(deviceId, i);
					return i;
				}
			}
			return -1;
		}

        public static GamePadButton TranslateKey(Keycode keyCode) => keyCode switch
        {
            Keycode.ButtonA => GamePadButton.A,
            Keycode.ButtonB => GamePadButton.B,
            Keycode.ButtonX => GamePadButton.X,
            Keycode.ButtonY => GamePadButton.Y,
            Keycode.Back => GamePadButton.Back,
            Keycode.ButtonL1 => GamePadButton.LeftShoulder,
            Keycode.ButtonR1 => GamePadButton.RightShoulder,
            Keycode.ButtonThumbl => GamePadButton.LeftThumb,
            Keycode.ButtonThumbr => GamePadButton.RightThumb,
            Keycode.DpadLeft => GamePadButton.DPadLeft,
            Keycode.DpadRight => GamePadButton.DPadRight,
            Keycode.DpadUp => GamePadButton.DPadUp,
            Keycode.DpadDown => GamePadButton.DPadDown,
            Keycode.ButtonSelect => GamePadButton.Back,
            Keycode.ButtonStart => GamePadButton.Start,
            _ => (GamePadButton)(-1),
        };

        public static void Connect(int deviceId, int index)
		{
			m_deviceToIndex.Add(deviceId, index);
			m_states[index].IsConnected = true;
		}

		public static void Disconnect(int deviceId)
		{
			if (m_deviceToIndex.TryGetValue(deviceId, out int value))
			{
				m_deviceToIndex.Remove(deviceId);
				m_states[value].IsConnected = false;
			}
		}
#else
            for (int padIndex = 0; padIndex < 4; padIndex++)
            {
                if (padIndex >= m_gamepads.Count)
                {
                    break;
                }
                IGamepad gamepad = m_gamepads[padIndex];
                if (gamepad == null)
                {
                    continue;
                }
                string name = gamepad.Name;
                if (!name.Contains("Unmapped"))
                {
                    State state = m_states[padIndex];
                    if (gamepad.IsConnected)
                    {
                        state.IsConnected = true;
                        if (Window.IsActive)
                        {
                            IReadOnlyList<Thumbstick> thumbsticks = gamepad.Thumbsticks;
                            for (int i = 0; i < 2; i++)
                            {
                                state.Sticks[i] = new Vector2(thumbsticks[i].X, -thumbsticks[i].Y);
                            }
                            IReadOnlyList<Trigger> triggers = gamepad.Triggers;
                            for (int i = 0; i < 2; i++)
                            {
                                state.Triggers[i] = triggers[i].Position;
                            }
                            foreach (Button button in gamepad.Buttons)
                            {
                                switch (button.Name)
                                {
                                    case ButtonName.A: state.Buttons[0] = button.Pressed; break;
                                    case ButtonName.B: state.Buttons[1] = button.Pressed; break;
                                    case ButtonName.X: state.Buttons[2] = button.Pressed; break;
                                    case ButtonName.Y: state.Buttons[3] = button.Pressed; break;
                                    case ButtonName.Back: state.Buttons[4] = button.Pressed; break;
                                    case ButtonName.Start: state.Buttons[5] = button.Pressed; break;
                                    case ButtonName.LeftStick: state.Buttons[6] = button.Pressed; break;
                                    case ButtonName.RightStick: state.Buttons[7] = button.Pressed; break;
                                    case ButtonName.LeftBumper: state.Buttons[8] = button.Pressed; break;
                                    case ButtonName.RightBumper: state.Buttons[9] = button.Pressed; break;
                                    case ButtonName.DPadLeft: state.Buttons[10] = button.Pressed; break;
                                    case ButtonName.DPadRight: state.Buttons[11] = button.Pressed; break;
                                    case ButtonName.DPadUp: state.Buttons[12] = button.Pressed; break;
                                    case ButtonName.DPadDown: state.Buttons[13] = button.Pressed; break;
                                }
                            }
                        }
                    }
                    else
                    {
                        state.IsConnected = false;
                    }
                }
            }
        }
#endif

        public static bool IsConnected(int gamePadIndex)
        {
            return gamePadIndex < 0 || gamePadIndex >= m_states.Length
                ? throw new ArgumentOutOfRangeException("gamePadIndex")
                : m_states[gamePadIndex].IsConnected;
        }

        public static Vector2 GetStickPosition(int gamePadIndex, GamePadStick stick, float deadZone = 0f)
        {
            if (deadZone < 0f || deadZone >= 1f)
            {
                throw new ArgumentOutOfRangeException("deadZone");
            }
            if (IsConnected(gamePadIndex))
            {
                Vector2 result = m_states[gamePadIndex].Sticks[(int)stick];
                if (deadZone > 0f)
                {
                    float num = result.Length();
                    if (num > 0f)
                    {
                        float num2 = ApplyDeadZone(num, deadZone);
                        result *= num2 / num;
                    }
                }
                return result;
            }
            return Vector2.Zero;
        }

        public static float GetTriggerPosition(int gamePadIndex, GamePadTrigger trigger, float deadZone = 0f)
        {
            return deadZone < 0f || deadZone >= 1f
                ? throw new ArgumentOutOfRangeException("deadZone")
                : IsConnected(gamePadIndex) ? ApplyDeadZone(m_states[gamePadIndex].Triggers[(int)trigger], deadZone) : 0f;
        }

        public static bool IsButtonDown(int gamePadIndex, GamePadButton button)
        {
            return IsConnected(gamePadIndex) && m_states[gamePadIndex].Buttons[(int)button];
        }

        public static bool IsButtonDownOnce(int gamePadIndex, GamePadButton button)
        {
            return IsConnected(gamePadIndex)
&& m_states[gamePadIndex].Buttons[(int)button] && !m_states[gamePadIndex].LastButtons[(int)button];
        }

        public static bool IsButtonDownRepeat(int gamePadIndex, GamePadButton button)
        {
            if (IsConnected(gamePadIndex))
            {
                if (m_states[gamePadIndex].Buttons[(int)button] && !m_states[gamePadIndex].LastButtons[(int)button])
                {
                    return true;
                }
                double num = m_states[gamePadIndex].ButtonsRepeat[(int)button];
                return num != 0.0 && Time.FrameStartTime >= num;
            }
            return false;
        }

        public static void Clear()
        {
            for (int i = 0; i < m_states.Length; i++)
            {
                for (int j = 0; j < m_states[i].Sticks.Length; j++)
                {
                    m_states[i].Sticks[j] = Vector2.Zero;
                }
                for (int k = 0; k < m_states[i].Triggers.Length; k++)
                {
                    m_states[i].Triggers[k] = 0f;
                }
                for (int l = 0; l < m_states[i].Buttons.Length; l++)
                {
                    m_states[i].Buttons[l] = false;
                    m_states[i].ButtonsRepeat[l] = 0.0;
                }
            }
        }

        internal static void AfterFrame()
        {
            for (int i = 0; i < m_states.Length; i++)
            {
                if (Keyboard.BackButtonQuitsApp && IsButtonDownOnce(i, GamePadButton.Back))
                {
                    Window.Close();
                }
                State state = m_states[i];
                for (int j = 0; j < state.Buttons.Length; j++)
                {
                    if (state.Buttons[j])
                    {
                        if (!state.LastButtons[j])
                        {
                            state.ButtonsRepeat[j] = Time.FrameStartTime + m_buttonFirstRepeatTime;
                        }
                        else if (Time.FrameStartTime >= state.ButtonsRepeat[j])
                        {
                            state.ButtonsRepeat[j] = Math.Max(Time.FrameStartTime, state.ButtonsRepeat[j] + m_buttonNextRepeatTime);
                        }
                    }
                    else
                    {
                        state.ButtonsRepeat[j] = 0.0;
                    }
                    state.LastButtons[j] = state.Buttons[j];
                }
            }
        }

        public static float ApplyDeadZone(float value, float deadZone)
        {
            return MathF.Sign(value) * MathF.Max(MathF.Abs(value) - deadZone, 0f) / (1f - deadZone);
        }
    }
}
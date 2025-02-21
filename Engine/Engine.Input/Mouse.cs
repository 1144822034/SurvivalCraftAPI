using System;
using System.Drawing;
#if ANDROID
using Android.OS;
using Android.Views;
using Org.Libsdl.App;
#else
using Silk.NET.Input;
#endif

namespace Engine.Input
{
	public static class Mouse
	{
#if ANDROID
        private static Vector2 m_queuedMouseMovement;

        private static float m_queuedMouseWheelMovement;

        private static bool m_pointerCaptureRequested;
#else
        public static IMouse m_mouse;

		public static Point2? m_lastMousePosition;
#endif
		private static bool[] m_mouseButtonsDownArray;

        private static int[] m_mouseButtonsDownFrameArray;

        private static bool[] m_mouseButtonsDelayedUpArray;

		private static bool[] m_mouseButtonsDownOnceArray;

        private static bool[] m_mouseButtonsUpOnceArray;

		public static Point2 MouseMovement
		{
			get;
			private set;
		}

		public static int MouseWheelMovement
		{
			get;
			private set;
		}

		public static Point2? MousePosition
		{
			get;
			private set;
		}

		public static bool IsMouseVisible
		{
			get;
			set;
		}

		public static event Action<MouseEvent> MouseMove;

		public static event Action<MouseButtonEvent> MouseDown;

		public static event Action<MouseButtonEvent> MouseUp;

		public static void SetMousePosition(int x, int y)
		{
#if !ANDROID
            m_mouse.Position = new System.Numerics.Vector2(x, y);
#endif
		}

        internal static void Initialize()
		{
#if !ANDROID
            m_mouse = Window.m_inputContext.Mice[0];
            m_mouse.MouseDown += MouseDownHandler;
            m_mouse.MouseUp += MouseUpHandler;
            m_mouse.MouseMove += MouseMoveHandler;
            m_mouse.Scroll += MouseWheelHandler;
#endif
		}

		internal static void Dispose()
		{
		}

		internal static void BeforeFrame()
		{
#if ANDROID
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }
            if (IsMouseVisible)
            {
                if (m_pointerCaptureRequested)
                {
                    m_pointerCaptureRequested = false;
                    //Window.View.ReleasePointerCapture();
                }
            }
            else
            {
                if (!m_pointerCaptureRequested)
                {
                    m_pointerCaptureRequested = true;
                    //Window.View.RequestPointerCapture();
                }
                MouseMovement = Round(m_queuedMouseMovement.X, m_queuedMouseMovement.Y);
                m_queuedMouseMovement = Vector2.Zero;
            }
            MouseWheelMovement = (int)MathUtils.Round(m_queuedMouseWheelMovement) * 120;
            m_queuedMouseWheelMovement = 0f;
#else
			if (Window.IsActive)
			{
                m_mouse.Cursor.CursorMode = IsMouseVisible ? CursorMode.Normal : CursorMode.Disabled;
                Point2 position = new Point2((int)m_mouse.Position.X, (int)m_mouse.Position.Y);
                int wheel = (int)m_mouse.ScrollWheels[0].Y;
				if (m_lastMousePosition.HasValue)
				{
					MouseMovement = new Point2(position.X - m_lastMousePosition.Value.X, position.Y - m_lastMousePosition.Value.Y);
				}
				m_lastMousePosition = position;
			}
			else
			{
				m_lastMousePosition = null;
			}
#endif
		}

#if ANDROID
        internal static void HandleMotionEvent(MotionEvent e)
        {
            if (e.Action == MotionEventActions.Move)
            {
                for (int num = e.HistorySize - 1; num >= 0; num--)
                {
                    m_queuedMouseMovement += new Vector2(e.GetHistoricalX(num), e.GetHistoricalY(num));
                }
                m_queuedMouseMovement += new Vector2(e.GetX(), e.GetY());
            }
            else if (e.Action == MotionEventActions.HoverMove)
            {
                MousePosition = Round(e.GetX(), e.GetY());
            }
            else if (e.Action == MotionEventActions.ButtonPress)
            {
                ProcessMouseDown(TranslateMouseButton(e.ActionButton), Round(e.GetX(), e.GetY()));
            }
            else if (e.Action == MotionEventActions.ButtonRelease)
            {
                ProcessMouseUp(TranslateMouseButton(e.ActionButton), Round(e.GetX(), e.GetY()));
            }
            else if (e.Action == MotionEventActions.PointerIdShift)
            {
                for (int num2 = e.HistorySize - 1; num2 >= 0; num2--)
                {
                    m_queuedMouseWheelMovement += MathUtils.Sign(e.GetHistoricalAxisValue(Axis.Vscroll, num2));
                }
                m_queuedMouseWheelMovement += MathUtils.Sign(e.GetAxisValue(Axis.Vscroll));
            }
        }

        private static MouseButton TranslateMouseButton(MotionEventButtonState state)
        {
            return state switch
            {
                MotionEventButtonState.Primary => MouseButton.Left,
                MotionEventButtonState.Secondary => MouseButton.Right,
                MotionEventButtonState.Tertiary => MouseButton.Middle,
                _ => MouseButton.Left,
            };
        }
#else
		private static void MouseDownHandler(IMouse mouse, Silk.NET.Input.MouseButton button)
		{
			MouseButton mouseButton = TranslateMouseButton(button);
			if (mouseButton != (MouseButton)(-1))
            {
                var position = mouse.Position;
				ProcessMouseDown(mouseButton, new Point2((int)position.X, (int)position.Y));
			}
		}

		private static void MouseUpHandler(IMouse mouse, Silk.NET.Input.MouseButton button)
		{
			MouseButton mouseButton = TranslateMouseButton(button);
			if (mouseButton != (MouseButton)(-1))
			{
                var position = mouse.Position;
				ProcessMouseUp(mouseButton, new Point2((int)position.X, (int)position.Y));
			}
		}

		private static void MouseMoveHandler(IMouse mouse, System.Numerics.Vector2 position)
		{
			ProcessMouseMove(new Point2((int)position.X, (int)position.Y));
		}

        private static void MouseWheelHandler(IMouse mouse, ScrollWheel scrollWheel)
        {
            ProcessMouseWheel(scrollWheel.Y);
        }

		public static MouseButton TranslateMouseButton(Silk.NET.Input.MouseButton mouseButton)
		{
            return mouseButton switch
            {
                Silk.NET.Input.MouseButton.Left => MouseButton.Left,
                Silk.NET.Input.MouseButton.Right => MouseButton.Right,
                Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
                Silk.NET.Input.MouseButton.Button4=>MouseButton.Ext1,
                Silk.NET.Input.MouseButton.Button5 => MouseButton.Ext2,
                _ => (MouseButton)(-1),
            };
        }
#endif

		static Mouse()
		{
			m_mouseButtonsDownArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
            m_mouseButtonsDownFrameArray = new int[Enum.GetValues(typeof(MouseButton)).Length];
            m_mouseButtonsDelayedUpArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
			m_mouseButtonsDownOnceArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
            m_mouseButtonsUpOnceArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
			IsMouseVisible = true;
		}

		public static bool IsMouseButtonDown(MouseButton mouseButton)
		{
			return m_mouseButtonsDownArray[(int)mouseButton];
		}

		public static bool IsMouseButtonDownOnce(MouseButton mouseButton)
		{
			return m_mouseButtonsDownOnceArray[(int)mouseButton];
		}

        public static bool IsMouseButtonUpOnce(MouseButton mouseButton)
        {
            return m_mouseButtonsUpOnceArray[(int)mouseButton];
        }

		public static void Clear()
		{
			for (int i = 0; i < m_mouseButtonsDownArray.Length; i++)
			{
				m_mouseButtonsDownArray[i] = false;
                m_mouseButtonsDownFrameArray[i] = 0;
                m_mouseButtonsDelayedUpArray[i] = false;
				m_mouseButtonsDownOnceArray[i] = false;
                m_mouseButtonsUpOnceArray[i] = false;
			}
		}

		internal static void AfterFrame()
		{
			for (int i = 0; i < m_mouseButtonsDownArray.Length; i++)
			{
				m_mouseButtonsDownOnceArray[i] = false;
                if (m_mouseButtonsDelayedUpArray[i])
                {
                    m_mouseButtonsDelayedUpArray[i] = false;
                    m_mouseButtonsDownArray[i] = false;
                    m_mouseButtonsUpOnceArray[i] = true;
                }
                else
                {
                    m_mouseButtonsUpOnceArray[i] = false;
                }
			}
			if (!IsMouseVisible)
            {
                MousePosition = null;
#if !ANDROID
                m_mouse.Cursor.CursorMode = Window.IsActive ? CursorMode.Disabled : CursorMode.Normal;
            }
            else
            {
                m_mouse.Cursor.CursorMode = CursorMode.Normal;
#endif
            }
            MouseWheelMovement = 0;
        }

        public static void ProcessMouseDown(MouseButton mouseButton, Point2 position)
		{
			if (Window.IsActive && !Keyboard.IsKeyboardVisible)
			{
                if (!MousePosition.HasValue)
                {
                    ProcessMouseMove(position);
                }
				m_mouseButtonsDownArray[(int)mouseButton] = true;
                m_mouseButtonsDownFrameArray[(int)mouseButton] = Time.FrameIndex;
				m_mouseButtonsDownOnceArray[(int)mouseButton] = true;
				if (IsMouseVisible && Mouse.MouseDown != null)
				{
					Mouse.MouseDown(new MouseButtonEvent
					{
						Button = mouseButton,
						Position = position
					});
				}
			}
		}

        public static void ProcessMouseUp(MouseButton mouseButton, Point2 position)
		{
			if (Window.IsActive && !Keyboard.IsKeyboardVisible)
			{
                if (!MousePosition.HasValue)
                {
                    ProcessMouseMove(position);
                }
                if (m_mouseButtonsDownArray[(int)mouseButton] && Time.FrameIndex == m_mouseButtonsDownFrameArray[(int)mouseButton])
                {
                    m_mouseButtonsDelayedUpArray[(int)mouseButton] = true;
                }
                else
                {
                    m_mouseButtonsDownArray[(int)mouseButton] = false;
                    m_mouseButtonsUpOnceArray[(int)mouseButton] = true;
                }
				if (IsMouseVisible && Mouse.MouseUp != null)
				{
					Mouse.MouseUp(new MouseButtonEvent
					{
						Button = mouseButton,
						Position = position
					});
				}
			}
		}

        public static void ProcessMouseMove(Point2 position)
		{
			if (Window.IsActive && !Keyboard.IsKeyboardVisible && IsMouseVisible)
			{
				MousePosition = position;
				Mouse.MouseMove?.Invoke(new MouseEvent
				{
					Position = position
				});
			}
		}

        public static void ProcessMouseWheel(float value)
        {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible)
            {
                MouseWheelMovement += (int)(120 * value);
            }
        }

        private static Point2 Round(float x, float y)
        {
            return new Point2((int)MathF.Round(x), (int)MathF.Round(y));
        }
	}
}